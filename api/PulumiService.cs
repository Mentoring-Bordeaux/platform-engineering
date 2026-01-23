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


    public async Task<IResult> ExecuteTemplateAsync(CreateProjectRequest request)
    {
        // Recherche du template par nom (templateName)
        string templateName = request.TemplateName;
        var templateDir = Path.Combine(Directory.GetCurrentDirectory(), "templates", templateName);

        _logger.LogInformation(
            "Looking for template '{TemplateName}' at path '{TemplateDir}'",
            templateName,
            templateDir
        );

        if (!Directory.Exists(templateDir))
        {
            _logger.LogWarning(
                "Template not found: '{TemplateName}' at path '{TemplateDir}'",
                templateName,
                templateDir
            );
            return Results.BadRequest($"Template not found: '{templateName}'");
        }

        var pulumiProgramDir = Path.Combine(templateDir, "pulumi");

        if (!Directory.Exists(pulumiProgramDir))
        {
            _logger.LogWarning(
                "Pulumi program not found in template '{TemplateName}' at path '{PulumiDir}'",
                templateName,
                pulumiProgramDir
            );
            return Results.BadRequest($"Pulumi program not found in template '{templateName}'");
        }

        string stackName = Regex.Replace(request.ProjectName.ToLower(), @"[^a-z0-9\-]", "-");

        _logger.LogInformation(
            "Using stack name: {StackName} for template: {TemplateName}",
            stackName,
            templateName
        );

        WorkspaceStack? stack = null;
        try
        {
            // Pulumi NodeJS templates require their SDK dependencies to be installed.
            // If node_modules is missing, run `pulumi install` in the program folder.
            // This matches Pulumi's own guidance and avoids manual setup for every template.
            var nodeModulesPath = Path.Combine(pulumiProgramDir, "node_modules");
            if (!Directory.Exists(nodeModulesPath))
            {
                _logger.LogInformation(
                    "node_modules not found for template '{TemplateName}'. Running `pulumi install` in '{PulumiDir}'.",
                    templateName,
                    pulumiProgramDir
                );

                var installResult = await RunCommandAsync("pulumi", "install", pulumiProgramDir);
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
                new LocalProgramArgs(stackName, pulumiProgramDir)
            );

            // Install Pulumi dependencies (required for TypeScript programs)
            if (_environment.IsDevelopment())
            {
                await stack.Workspace.InstallAsync();
            }

            // GitHub template requires `name`; GitLab template requires `Name`.
            // Setting both is safe and avoids template-specific branching.
            await stack.SetConfigAsync("name", new ConfigValue(request.ProjectName));
            await stack.SetConfigAsync("Name", new ConfigValue(request.ProjectName));

            // Set all template parameters (excluding "type")
            foreach (var kv in request.Parameters.Where(kv => kv.Key != "type"))
            {
                var valueStr = kv.Value?.ToString() ?? "";
                string normalizedValue = bool.TryParse(valueStr, out var boolValue)
                    ? boolValue.ToString().ToLowerInvariant()
                    : valueStr;
                await stack.SetConfigAsync(kv.Key, new ConfigValue(normalizedValue));
            }
            var result = await stack.UpAsync(
                new UpOptions
                {
                    OnStandardOutput = Console.WriteLine,
                    OnStandardError = Console.Error.WriteLine,
                }
            );

            var outputs = result.Outputs.ToDictionary(kv => kv.Key, kv => kv.Value?.Value);
            return Results.Json(outputs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing template '{TemplateName}'", templateName);
            return Results.Problem(ex.Message, statusCode: 500);
        }
        finally
        {
            // Clean up the stack
            if (stack != null)
            {
                try
                {
                    await stack.DestroyAsync();
                    await stack.Workspace.RemoveStackAsync(stackName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clean up stack '{StackName}'", stackName);
                }
            }
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
