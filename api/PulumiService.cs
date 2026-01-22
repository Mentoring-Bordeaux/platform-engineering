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

    public async Task<IResult> ExecuteAsync(TemplateRequest request)
    {
        // Pulumi environment variables are set per-process in RunCommandAsync to avoid global state/race conditions.
        var pulumiHome = GetPulumiHome();
        Directory.CreateDirectory(pulumiHome);

        string resourceType = request.ResourceType;

        // The frontend encodes nested folders as `platforms//github`.
        // Normalize to a real filesystem path segment (e.g. platforms\github).
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
            _logger.LogWarning(
                "Pulumi program not found for type '{ResourceType}' at path '{WorkingDir}'",
                resourceType,
                workingDir
            );
            return Results.BadRequest(
                $"Pulumi program not found for type '{resourceType}' at path '{workingDir}'."
            );
        }

        string requestAndType = $"{request.Name}-{resourceType}";

        string stackName = Regex.Replace(requestAndType.ToLower(), @"[^a-z0-9\-]", "-");

        _logger.LogInformation("Using stack name: {StackName}", stackName);

        WorkspaceStack? stack = null;
        try
        {
            // Pulumi NodeJS templates require their SDK dependencies to be installed.
            // If node_modules is missing, run `pulumi install` in the program folder.
            // This matches Pulumi's own guidance and avoids manual setup for every template.
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
                    _logger.LogError(
                        "Pulumi install failed (exit {ExitCode}). stderr: {Stderr}",
                        installResult.ExitCode,
                        installResult.StandardError
                    );
                    return Results.Problem(
                        detail:
                            "Pulumi dependencies install failed. Ensure Pulumi CLI is installed, and the selected packagemanager (pnpm/npm) is available.\n"
                            + "If you see 'no language plugin pulumi-language-nodejs', ensure the bundled language host binary (pulumi-language-nodejs) is available on PATH (typically by installing Pulumi correctly inside the container/image).\n"
                            + installResult.StandardError,
                        statusCode: 500
                    );
                }
            }

            stack = await LocalWorkspace.CreateOrSelectStackAsync(
                new LocalProgramArgs(stackName, workingDir)
            );

            // Install Pulumi dependencies (required for TypeScript programs)
            if (_environment.IsDevelopment())
            {
                await stack.Workspace.InstallAsync();
            }

            var pulumiProjectName = TryGetPulumiProjectName(workingDir);

            string QualifyConfigKey(string key)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    return key;
                }

                // Already-qualified keys (e.g. aws:region) or Pulumi internal keys.
                if (key.StartsWith("pulumi:", StringComparison.OrdinalIgnoreCase) || key.Contains(':'))
                {
                    return key;
                }

                // User config keys must be namespaced as "<project>:<key>".
                if (!string.IsNullOrWhiteSpace(pulumiProjectName))
                {
                    return $"{pulumiProjectName}:{key}";
                }

                return key;
            }
            // Configurate the stack with parameters from the request (excluding "type")
            foreach (var kv in request.Parameters.Where(kv => kv.Key != "type"))
                await stack.SetConfigAsync(QualifyConfigKey(kv.Key), new ConfigValue(kv.Value));


            // GitHub template requires `name`; GitLab template requires `Name`.
            // Setting both is safe and avoids template-specific branching.
            await stack.SetConfigAsync(QualifyConfigKey("name"), new ConfigValue(request.Name));
            await stack.SetConfigAsync(QualifyConfigKey("Name"), new ConfigValue(request.Name));

            // Remove stale config keys from previous runs when they are not provided anymore.
            // Pulumi stack config persists across updates, so an old value (e.g. an invalid gitlabBaseUrl)
            // can keep breaking runs even after we stop injecting it.
            var desiredKeys = request.Parameters.Keys
                .Where(k => k != "type")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            desiredKeys.Add("name");
            desiredKeys.Add("Name");

            var existingConfig = await stack.GetAllConfigAsync();
            foreach (var existingKey in existingConfig.Keys)
            {
                // Keep Pulumi internal keys.
                if (existingKey.StartsWith("pulumi:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Pulumi stores user config keys namespaced by project (e.g. "myproj:Name").
                // Only compare the suffix ("Name") against desired keys.
                string? keyPrefix = null;
                var keySuffix = existingKey;
                var colonIndex = existingKey.IndexOf(':');
                if (colonIndex >= 0 && colonIndex < existingKey.Length - 1)
                {
                    keyPrefix = existingKey[..colonIndex];
                    keySuffix = existingKey[(colonIndex + 1)..];
                }

                // Only clean keys in the current project namespace (avoid touching unrelated namespaces).
                if (!string.IsNullOrWhiteSpace(pulumiProjectName) && keyPrefix != null && !keyPrefix.Equals(pulumiProjectName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!desiredKeys.Contains(keySuffix))
                {
                    await stack.RemoveConfigAsync(existingKey);
                }
            }

            var result = await stack.UpAsync(
                new UpOptions
                {
                    OnStandardOutput = Console.WriteLine,
                    OnStandardError = Console.Error.WriteLine,
                }
            );

            var outputs = result.Outputs.ToDictionary(kv => kv.Key, kv => kv.Value?.Value);
            if (request.Parameters.TryGetValue("githubToken", out var token) &&
                request.Parameters.TryGetValue("githubOrganizationName", out var orgName))
            {
                var gitService = new GitHubService(token);

                string repoToPush = request.Name;

                if (!string.IsNullOrEmpty(request.Framework))
                {
                    // Initialisation dynamique via CLI selon le type de framework
                    if (Enum.TryParse<FrameworkType>(request.Framework, true, out var frameworkType))
                    {
                        await gitService.InitializeRepoWithFrameworkAsync(
                            orgName,
                            repoToPush,
                            frameworkType,
                            request.Name
                        );
                    }
                    else
                    {
                        _logger.LogWarning("Framework non supporté : {Framework}", request.Framework);
                    }
                }
                else
                {
                    // Si pas de framework, on peut éventuellement utiliser un repo cible existant
                    repoToPush = request.Parameters.ContainsKey("targetRepo") ? request.Parameters["targetRepo"] : request.Name;
                }

                // Push le code Pulumi si ce n'est pas une plateforme GitHub
                if (!resourceType.StartsWith("platforms/github"))
                {
                    if (Directory.Exists(workingDir))
                    {
                        await gitService.PushPulumiCodeAsync(
                            orgName,
                            repoToPush,
                            workingDir,
                            request.Parameters,
                            request.Name
                        );
                    }
                    else
                    {
                        _logger.LogWarning("Pulumi program path does not exist: {PulumiPath}", workingDir);
                    }
                }
            }

            return Results.Json(outputs);
        }
        catch (Exception ex)
        {
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
