using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Pulumi.Automation;

public class PulumiService
{
    private readonly ILogger<PulumiService> _logger;

    private readonly IWebHostEnvironment _environment;

    private static string GetPulumiHome()
    {
        // Pulumi stores plugins in $PULUMI_HOME/plugins (defaults to ~/.pulumi).
        // In containers the non-root user may not have a usable HOME, which causes
        // Pulumi to fail to locate language plugins like pulumi-language-nodejs.
        return Path.Combine(Directory.GetCurrentDirectory(), ".pulumi");
    }

    public PulumiService(ILogger<PulumiService> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async Task<Dictionary<string, object?>> RunPulumiAsync(TemplateRequest request)
    {
        var pulumiHome = GetPulumiHome();
        Directory.CreateDirectory(pulumiHome);
        Environment.SetEnvironmentVariable("PULUMI_HOME", pulumiHome);

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HOME")))
            Environment.SetEnvironmentVariable("HOME", Directory.GetCurrentDirectory());

        string resourceType = request.ResourceType;

        var normalizedResourceType = resourceType
            .Replace("//", Path.DirectorySeparatorChar.ToString())
            .Replace('/', Path.DirectorySeparatorChar)
            .Trim(Path.DirectorySeparatorChar);

        var workingDir = Path.Combine(
            Directory.GetCurrentDirectory(),
            "pulumiPrograms",
            normalizedResourceType
        );

        _logger.LogInformation(
            "Looking for Pulumi program for type '{ResourceType}' at path '{WorkingDir}'",
            resourceType,
            workingDir
        );

        if (!Directory.Exists(workingDir))
        {
            // Ici on ne retourne plus BadRequest, on jette une exception claire
            throw new DirectoryNotFoundException(
                $"Pulumi program not found for type '{resourceType}' at path '{workingDir}'."
            );
        }

        string requestAndType = $"{request.Name}-{resourceType}";
        string stackName = Regex.Replace(requestAndType.ToLower(), @"[^a-z0-9\-]", "-");

        _logger.LogInformation("Using stack name: {StackName}", stackName);

        try
        {
            var nodeModulesPath = Path.Combine(workingDir, "node_modules");
            if (!Directory.Exists(nodeModulesPath))
            {
                _logger.LogInformation(
                    "node_modules not found for '{ResourceType}'. Running `pulumi install` in '{WorkingDir}'.",
                    resourceType,
                    workingDir
                );

                var installResult = await RunCommandAsync("pulumi", "install", workingDir);
                if (installResult.ExitCode != 0)
                {
                    throw new Exception(
                        "Pulumi dependencies install failed:\n" + installResult.StandardError
                    );
                }
            }

            var stack = await LocalWorkspace.CreateOrSelectStackAsync(
                new LocalProgramArgs(stackName, workingDir)
            );

            if (_environment.IsDevelopment())
                await stack.Workspace.InstallAsync();

            var pulumiProjectName = TryGetPulumiProjectName(workingDir);

            string QualifyConfigKey(string key)
            {
                if (string.IsNullOrWhiteSpace(key)) return key;
                if (key.StartsWith("pulumi:", StringComparison.OrdinalIgnoreCase) || key.Contains(':')) return key;
                return !string.IsNullOrWhiteSpace(pulumiProjectName) ? $"{pulumiProjectName}:{key}" : key;
            }

            await stack.SetConfigAsync(QualifyConfigKey("name"), new ConfigValue(request.Name));
            await stack.SetConfigAsync(QualifyConfigKey("Name"), new ConfigValue(request.Name));

            var desiredKeys = request.Parameters.Keys
                .Where(k => k != "type")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            desiredKeys.Add("name");
            desiredKeys.Add("Name");

            var existingConfig = await stack.GetAllConfigAsync();
            foreach (var existingKey in existingConfig.Keys)
            {
                if (existingKey.StartsWith("pulumi:", StringComparison.OrdinalIgnoreCase))
                    continue;

                string? keyPrefix = null;
                var keySuffix = existingKey;
                var colonIndex = existingKey.IndexOf(':');
                if (colonIndex >= 0 && colonIndex < existingKey.Length - 1)
                {
                    keyPrefix = existingKey[..colonIndex];
                    keySuffix = existingKey[(colonIndex + 1)..];
                }

                if (!string.IsNullOrWhiteSpace(pulumiProjectName) &&
                    keyPrefix != null &&
                    !keyPrefix.Equals(pulumiProjectName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!desiredKeys.Contains(keySuffix))
                    await stack.RemoveConfigAsync(existingKey);
            }

            foreach (var kv in request.Parameters.Where(kv => kv.Key != "type"))
                await stack.SetConfigAsync(QualifyConfigKey(kv.Key), new ConfigValue(kv.Value));

            var result = await stack.UpAsync(new UpOptions
            {
                OnStandardOutput = Console.WriteLine,
                OnStandardError = Console.Error.WriteLine,
            });

            return result.Outputs.ToDictionary(kv => kv.Key, kv => kv.Value?.Value);
        }
        catch
        {
            // Laisse remonter l’exception au caller async job
            throw;
        }
    }

    public async Task<IResult> ExecuteAsync(TemplateRequest request)
    {
        try
        {
            var outputs = await RunPulumiAsync(request);
            return Results.Json(outputs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pulumi failed");
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }


    private static string? TryGetPulumiProjectName(string workingDir)
    {
        try
        {
            var pulumiYamlPath = Path.Combine(workingDir, "Pulumi.yaml");
            if (!File.Exists(pulumiYamlPath))
            {
                return null;
            }

            foreach (var line in File.ReadLines(pulumiYamlPath, Encoding.UTF8))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("name:", StringComparison.OrdinalIgnoreCase))
                {
                    var value = trimmed["name:".Length..].Trim();
                    return string.IsNullOrWhiteSpace(value) ? null : value;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }


    private static async Task<CommandResult> RunCommandAsync(
        string fileName,
        string arguments,
        string workingDirectory
    )
    {
        var pulumiHome = GetPulumiHome();
        Directory.CreateDirectory(pulumiHome);

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        // Ensure Pulumi can find plugins even when HOME isn't set (common in containers).
        startInfo.Environment["PULUMI_HOME"] = pulumiHome;
        if (!startInfo.Environment.ContainsKey("HOME"))
        {
            startInfo.Environment["HOME"] = Directory.GetCurrentDirectory();
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return new CommandResult(
            process.ExitCode,
            await stdoutTask,
            await stderrTask
        );
    }

    private sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError);
}
