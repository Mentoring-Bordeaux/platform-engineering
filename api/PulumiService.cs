using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
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

    // Executes a template Pulumi program (e.g., ecommerce).
    public async Task<ResultPulumiAction> ExecuteTemplateAsync(CreateProjectRequest request, IGitRepositoryService gitService)
    {
        string templateName = request.TemplateName;
        var templateDir = Path.Combine(
            Directory.GetCurrentDirectory(),
            "pulumiPrograms/templates",
            templateName
        );

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
            return new ResultPulumiAction
            {
                Name = request.ProjectName,
                ResourceType = "template",
                StatusCode = 400,
                Message = $"Template not found: '{templateName}'",
            };
        }

        var pulumiProgramDir = Path.Combine(templateDir, "pulumi");

        if (!Directory.Exists(pulumiProgramDir))
        {
            _logger.LogWarning(
                "Pulumi program not found in template '{TemplateName}' at path '{PulumiDir}'",
                templateName,
                pulumiProgramDir
            );
            return new ResultPulumiAction
            {
                Name = request.ProjectName,
                ResourceType = "template",
                StatusCode = 400,
                Message = $"Pulumi program not found in template '{templateName}'",
            };
        }

        var result = await ExecuteInternalAsync(pulumiProgramDir, request.ProjectName, "template", request.Parameters, gitService, templateName);
        return result;
    }

    // Executes a platform Pulumi program (e.g., GitHub, GitLab) to create a repository.
    public async Task<ResultPulumiAction> ExecutePlatformAsync(
        CreateProjectRequest request,
        Dictionary<string, string> injectedCredentials,
        IGitRepositoryService gitService
    )
    {
        if (request.Platform == null)
        {
            return new ResultPulumiAction
            {
                Name = request.ProjectName,
                ResourceType = "platform",
                StatusCode = 400,
                Message = "Platform configuration is required",
            };
        }

        string platformType = request.Platform.Type.ToLowerInvariant();
        _logger.LogInformation(
            "Executing platform Pulumi program for type: {PlatformType}",
            platformType
        );
        var platformDir = Path.Combine(
            Directory.GetCurrentDirectory(),
            "pulumiPrograms/platforms",
            platformType
        );

        _logger.LogInformation(
            "Looking for platform '{PlatformType}' at path '{PlatformDir}'",
            platformType,
            platformDir
        );

        if (!Directory.Exists(platformDir))
        {
            _logger.LogWarning(
                "Platform not found: '{PlatformType}' at path '{PlatformDir}'",
                platformType,
                platformDir
            );
            return new ResultPulumiAction
            {
                Name = request.ProjectName,
                ResourceType = platformType,
                StatusCode = 400,
                Message = $"Platform '{platformType}' not found",
            };
        }

        // Merge platform config with injected credentials
        var mergedParams = request.Platform.Config
            .Where(kv => kv.Key != "type")
            .ToDictionary(kv => kv.Key, kv => (object)kv.Value);

        foreach (var kvp in injectedCredentials)
        {
            mergedParams[kvp.Key] = kvp.Value;
        }

        var result = await ExecuteInternalAsync(platformDir, request.ProjectName, platformType, mergedParams);

        return result;
    }

    public async Task InitializeRepo(string projectName, List<FrameworkType> frameworks, IGitRepositoryService gitService)
    {
        if (frameworks == null || !frameworks.Any())
            return;

        _logger.LogInformation("Initializing repository with frameworks: {Frameworks}", string.Join(", ", frameworks));

        await gitService.InitializeRepoWithFrameworksAsync(frameworks, projectName);

    }
    private async Task PushPulumiAsync(string projectName, Dictionary<string, object> parameters, IGitRepositoryService gitService, string? templateName = null)
    {
        string localPath = Path.Combine(Directory.GetCurrentDirectory(), "pulumiPrograms", "templates", templateName!, "pulumi");

        if (!Directory.Exists(localPath))
        {
            _logger.LogWarning("Local Pulumi path not found: {LocalPath}", localPath);
            return;
        }

        switch (gitService)
        {
            case GitHubService githubService:
                await githubService.PushPulumiCodeAsync(
                    githubService.OrgName,
                    githubService.RepoName,
                    localPath,
                    parameters.ToDictionary(kv => kv.Key, kv => kv.Value.ToString() ?? string.Empty),
                    projectName
                );
                break;

            case GitLabService gitlabService:
                await gitlabService.PushPulumiCodeAsync(
                    gitlabService.ProjectPathOrUrl,
                    localPath,
                    parameters.ToDictionary(kv => kv.Key, kv => kv.Value.ToString() ?? string.Empty),
                    projectName
                );
                break;

            default:
                _logger.LogWarning("Unsupported Git service for pushing Pulumi code.");
                break;
        }
    }

    // Internal method that executes a Pulumi program with proper setup and cleanup.
    private async Task<ResultPulumiAction> ExecuteInternalAsync(
        string workingDir,
        string projectName,
        string resourceType,
        Dictionary<string, object> parameters,
        IGitRepositoryService? gitService = null,
        string? templateName = null
    )
    {
        var pulumiHome = GetPulumiHome();
        Directory.CreateDirectory(pulumiHome);

        string stackName = Regex.Replace(
            $"{projectName}-{resourceType}".ToLower(),
            @"[^a-z0-9\-]",
            "-"
        );

        _logger.LogInformation("Using stack name: {StackName}", stackName);

        // Ensure node_modules exists and install if needed
        var nodeModulesPath = Path.Combine(workingDir, "node_modules");
        if (!Directory.Exists(nodeModulesPath))
        {
            _logger.LogInformation(
                "node_modules not found. Running `pulumi install` in '{WorkingDir}'.",
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
                return new ResultPulumiAction
                {
                    Name = projectName,
                    ResourceType = resourceType,
                    StatusCode = 500,
                    Message = "Pulumi dependencies install failed. " + installResult.StandardError,
                };
            }
        }

        WorkspaceStack? stack = null;
        try
        {
            stack = await LocalWorkspace.CreateOrSelectStackAsync(
                new LocalProgramArgs(stackName, workingDir)
            );

            // Install Pulumi dependencies (required for TypeScript programs)
            if (_environment.IsDevelopment())
            {
                await stack.Workspace.InstallAsync();
            }

            // Get the Pulumi project name from Pulumi.yaml
            var pulumiProjectName = TryGetPulumiProjectName(workingDir);

            string QualifyConfigKey(string key)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    return key;
                }

                // Already-qualified keys (e.g. aws:region) or Pulumi internal keys.
                if (
                    key.StartsWith("pulumi:", StringComparison.OrdinalIgnoreCase)
                    || key.Contains(':')
                )
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

            // Remove stale config keys from previous runs
            var desiredKeys = parameters
                .Keys.Where(k => k != "type")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var existingConfig = await stack.GetAllConfigAsync();
            foreach (var existingKey in existingConfig.Keys)
            {
                // Keep Pulumi internal keys
                if (existingKey.StartsWith("pulumi:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Only compare the suffix against desired keys
                string keySuffix = existingKey;
                var colonIndex = existingKey.IndexOf(':');
                if (colonIndex >= 0 && colonIndex < existingKey.Length - 1)
                {
                    var keyPrefix = existingKey[..colonIndex];
                    keySuffix = existingKey[(colonIndex + 1)..];

                    // Only clean keys in the current project namespace
                    if (
                        !string.IsNullOrWhiteSpace(pulumiProjectName)
                        && !keyPrefix.Equals(pulumiProjectName, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        continue;
                    }
                }

                if (!desiredKeys.Contains(keySuffix))
                {
                    await stack.RemoveConfigAsync(existingKey);
                }
            }

            // Set all parameters with proper normalization
            foreach (var kv in parameters.Where(kv => kv.Key != "type"))
            {
                var valueStr = kv.Value?.ToString() ?? "";
                string normalizedValue = bool.TryParse(valueStr, out var boolValue)
                    ? boolValue.ToString().ToLowerInvariant()
                    : valueStr;

                await stack.SetConfigAsync(
                    QualifyConfigKey(kv.Key),
                    new ConfigValue(normalizedValue)
                );
            }

            var result = await stack.UpAsync(
                new UpOptions
                {
                    OnStandardOutput = Console.WriteLine,
                    OnStandardError = Console.Error.WriteLine,
                }
            );

            var outputs = result.Outputs.ToDictionary(
                kv => kv.Key,
                kv => (object?)(kv.Value?.Value)
            );
            _logger.LogInformation(
                "Resource created successfully: {Name} of {ResourceType}",
                projectName,
                resourceType
            );
            if (gitService != null)
            {
                _logger.LogInformation("Pushing Pulumi code to Git repository before destroying the stack...");
                await PushPulumiAsync(projectName, parameters, gitService, templateName);
            }
            return new ResultPulumiAction
            {
                Name = projectName,
                ResourceType = resourceType,
                StatusCode = 200,
                Message = "Resource created successfully",
                Outputs = outputs
                    .Where(kv => kv.Value != null)
                    .ToDictionary(kv => kv.Key, kv => kv.Value!),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error executing {ResourceType} for '{ProjectName}'",
                resourceType,
                projectName
            );
            return new ResultPulumiAction
            {
                Name = projectName,
                ResourceType = resourceType,
                StatusCode = 500,
                Message = $"Execution failed: {ex.Message}",
            };
        }
        finally
        {
            // Clean up the stack YAML file only (do not destroy resources)
            if (stack != null)
            {
                try
                {
                    // Le fichier est dans le dossier du programme Pulumi
                    string stackFileName = $"Pulumi.{stackName}.yaml";
                    string stackFilePath = Path.Combine(workingDir, stackFileName);

                    if (File.Exists(stackFilePath))
                    {
                        File.Delete(stackFilePath);
                        _logger.LogInformation("Stack file '{StackFile}' deleted successfully.", stackFileName);
                    }
                    else
                    {
                        _logger.LogWarning("Stack file '{StackFile}' not found, nothing to delete.", stackFileName);
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete stack file '{StackName}'", stackName);
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

        // Ensure Pulumi can find plugins even when HOME isn't set (common in containers)
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

        return new CommandResult(process.ExitCode, await stdoutTask, await stderrTask);
    }

    private sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError);
}
