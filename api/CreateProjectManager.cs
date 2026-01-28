using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Humanizer;

public class CreateProjectManager
{
    private readonly ILogger<CreateProjectManager> _logger;

    private ConcurrentDictionary<int, StatePulumi> _projectCreationStates = new();

    public CreateProjectManager(ILogger<CreateProjectManager> logger)
    {
        _logger = logger;
    }

    public async Task CreateNewProject(
        int id,
        CreateProjectRequest request,
        PulumiService pulumiService,
        WebApplication app
    )
    {
        _projectCreationStates[id] = new StatePulumi();
        _projectCreationStates[id].Status = StatePulumi.StateStatus.InProgress;
        _projectCreationStates[id].CurrentStep = StatePulumi.Step.InputVerification;

        // Step 1: Input Verification
        try
        {
            ValidateCreateProjectRequest(request);
        }
        catch (Exception ex)
        {
            _projectCreationStates[id].Status = StatePulumi.StateStatus.Failed;
            _projectCreationStates[id].ErrorMessage = ex.Message;
            return;
        }

        // Step 2: Git Session Setup
        _projectCreationStates[id].CurrentStep = StatePulumi.Step.GitSessionSetup;
        Dictionary<string, string> injectedCredentials;
        var platformType = request.Platform!.Type.Trim().ToLowerInvariant();
        try
        {
            injectedCredentials = retrieveGitCredentials(app, platformType);
        }
        catch (Exception ex)
        {
            _projectCreationStates[id].Status = StatePulumi.StateStatus.Failed;
            _projectCreationStates[id].ErrorMessage = ex.Message;
            return;
        }

        // Step 3: Git Repository Creation
        _projectCreationStates[id].CurrentStep = StatePulumi.Step.GitRepositoryCreation;
        GitRepositoryCreationOutputs gitRepositoryCreationOutputs;
        try
        {
            gitRepositoryCreationOutputs = await pulumiService.CreateGitRepositoryAsync(
                request,
                injectedCredentials
            );
        }
        catch (Exception ex)
        {
            _projectCreationStates[id].Status = StatePulumi.StateStatus.Failed;
            _projectCreationStates[id].ErrorMessage = ex.Message;
            return;
        }

        // Initialize Git Service for further steps
        IGitRepositoryService gitService;
        if (platformType == "github")
        {
            gitService = new GitHubService(
                injectedCredentials["githubToken"],
                injectedCredentials["githubOrganizationName"],
                gitRepositoryCreationOutputs.RepositoryName
            );
        }
        else // gitlab (Only other supported platform for now and verified before that the type is valid)
        {
            gitService = new GitLabService(
                injectedCredentials["gitlabToken"],
                gitRepositoryCreationOutputs.RepositoryUrl,
                injectedCredentials["gitlabBaseUrl"]
            );
        }

        // Step 4: Framework on Git Repository Initialization
        _projectCreationStates[id].CurrentStep = StatePulumi
            .Step
            .FrameworkOnGitRepositoryInitialization;
        try
        {
            // Retrieve frameworks from TemplateParameters
            List<FrameworkType> frameworks = request
                .TemplateParameters.Where(kv =>
                    kv.Key.Contains("framework", StringComparison.OrdinalIgnoreCase)
                )
                .Select(kv =>
                    Enum.TryParse<FrameworkType>(kv.Value?.ToString() ?? "", true, out var fw)
                        ? fw
                        : (FrameworkType?)null
                )
                .Where(fw => fw.HasValue)
                .Select(fw => fw!.Value)
                .ToList();

            // Only initialize if there are frameworks to set up
            if (frameworks.Any())
            {
                await pulumiService.InitializeRepo(request.ProjectName, frameworks, gitService);
            }
        }
        catch (Exception ex)
        {
            _projectCreationStates[id].Status = StatePulumi.StateStatus.Failed;
            _projectCreationStates[id].ErrorMessage = ex.Message;
            // await pulumiService.DeleteGitRepository(gitService, gitRepositoryCreationOutputs.RepositoryName);
            return;
        }

        // Step 5: Pulumi Template Resource Creation
        _projectCreationStates[id].CurrentStep = StatePulumi.Step.PulumiTemplateRessourceCreation;
        Dictionary<string, object> templateExecutionOutputs;
        try
        {
            templateExecutionOutputs = await pulumiService.ExecuteTemplateAsync(
                request,
                gitService
            );
        }
        catch (Exception ex)
        {
            _projectCreationStates[id].Status = StatePulumi.StateStatus.Failed;
            _projectCreationStates[id].ErrorMessage = ex.Message;
            // await pulumiService.DeleteGitRepository(gitService, gitRepositoryCreationOutputs.RepositoryName);
            // await pulumiService.CleanupPulumiStack(request.ProjectName);
            return;
        }

        // Step 6: Stack Pulumi Transfer on Git Repository
        // (Implemented inside ExecuteTemplateAsync for now)

        // Success
        _projectCreationStates[id].CurrentStep = StatePulumi.Step.Success;
        _projectCreationStates[id].Status = StatePulumi.StateStatus.Success;
        _projectCreationStates[id].Outputs!["gitRepositoryUrl"] =
            gitRepositoryCreationOutputs.RepositoryUrl;
        _projectCreationStates[id].Outputs!["gitRepositoryName"] =
            gitRepositoryCreationOutputs.RepositoryName;
        _projectCreationStates[id].Outputs = templateExecutionOutputs;

        return;
    }

    private static void ValidateCreateProjectRequest(CreateProjectRequest request)
    {
        if (request == null)
        {
            throw new Exception("Request body is null");
        }

        if (string.IsNullOrWhiteSpace(request.TemplateName))
        {
            throw new Exception("Missing 'TemplateName' in request");
        }

        if (string.IsNullOrWhiteSpace(request.ProjectName))
        {
            throw new Exception("Missing 'ProjectName' in request");
        }

        if (request.Platform == null)
        {
            throw new Exception("Missing 'Platform' in request");
        }

        if (string.IsNullOrWhiteSpace(request.Platform.Type))
        {
            throw new Exception("Missing 'Platform.Type' in request");
        }
    }

    private static Dictionary<string, string> retrieveGitCredentials(
        WebApplication app,
        string platformType
    )
    {
        var injectedCredentials = new Dictionary<string, string>();

        // Verify credentials
        if (platformType == "github")
        {
            var githubToken = app.Configuration["GitHubToken"];
            var githubOrg = app.Configuration["GitHubOrganizationName"];

            if (!HasRealConfigValue(githubToken) || !HasRealConfigValue(githubOrg))
            {
                throw new Exception(
                    "GitHubToken or GitHubOrganizationName is missing in configuration."
                );
            }

            injectedCredentials["githubToken"] = githubToken!;
            injectedCredentials["githubOrganizationName"] = githubOrg!;

            return injectedCredentials;
        }
        else if (platformType == "gitlab")
        {
            var gitlabToken = app.Configuration["GitLabToken"];
            var gitlabBaseUrl = app.Configuration["GitLabBaseUrl"];

            if (!HasRealConfigValue(gitlabToken) || !HasValidHttpUrl(gitlabBaseUrl))
            {
                throw new Exception(
                    "GitLabToken or GitLabBaseUrl is missing or invalid in configuration."
                );
            }

            injectedCredentials["gitlabToken"] = gitlabToken!;
            injectedCredentials["gitlabBaseUrl"] = gitlabBaseUrl!;
            return injectedCredentials;
        }
        else
        {
            throw new Exception($"Unsupported platform type: {platformType}");
        }
    }

    private static bool HasRealConfigValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        var lower = trimmed.ToLowerInvariant();
        if (
            lower.Contains("should be set")
            || lower.StartsWith("optional")
            || lower.Contains("replace_with")
        )
        {
            return false;
        }

        return true;
    }

    private static bool HasValidHttpUrl(string? value)
    {
        if (!HasRealConfigValue(value))
        {
            return false;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }
}
