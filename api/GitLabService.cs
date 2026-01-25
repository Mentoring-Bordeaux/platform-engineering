using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

public class GitLabService : GitRepositoryServiceBase
{
    private readonly HttpClient _http;
    private readonly string _projectPathOrUrl;
    public GitLabService(string token, string projectPathOrUrl, string? gitlabBaseUrl = null)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("GitLab token is required", nameof(token));

        var apiBase = NormalizeGitLabApiBaseUrl(gitlabBaseUrl);

        _http = new HttpClient
        {
            BaseAddress = new Uri(apiBase, UriKind.Absolute)
        };

        _http.DefaultRequestHeaders.Add("PRIVATE-TOKEN", token);
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json")
        );
        _projectPathOrUrl = projectPathOrUrl;
    }
    private static string NormalizeGitLabApiBaseUrl(string? gitlabBaseUrl)
    {
        var trimmed = (gitlabBaseUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return "https://gitlab.com/api/v4/";

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            throw new ArgumentException($"Invalid GitLab base URL: '{gitlabBaseUrl}'", nameof(gitlabBaseUrl));

        var path = uri.AbsolutePath.TrimEnd('/');
        if (path.EndsWith("/api/v4", StringComparison.OrdinalIgnoreCase))
            return uri.ToString().TrimEnd('/') + "/";

        var builder = new UriBuilder(uri)
        {
            Path = uri.AbsolutePath.TrimEnd('/') + "/api/v4/",
        };
        return builder.Uri.ToString();
    }
    private sealed record ResolvedProject(int Id, string DefaultBranch);
    private async Task<ResolvedProject> ResolveProjectAsync(string projectPathOrUrl)
    {
        string idOrPath;
        if (int.TryParse(projectPathOrUrl, out _))
            idOrPath = projectPathOrUrl;
        else
        {
            idOrPath = Uri.EscapeDataString(ExtractProjectPath(projectPathOrUrl));
        }

        int retries = 3;
        while (retries-- > 0)
        {
            using var response = await _http.GetAsync($"projects/{idOrPath}");
            var body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var project = JsonSerializer.Deserialize<GitLabProjectDto>(body);
                if (project != null && project.Id != 0)
                {
                    var defaultBranch = string.IsNullOrWhiteSpace(project.DefaultBranch) ? "main" : project.DefaultBranch;
                    return new ResolvedProject(project.Id, defaultBranch);
                }
            }
            await Task.Delay(2000);
        }

        throw new Exception($"GitLab project '{projectPathOrUrl}' not accessible yet.");
    }
    private static string ExtractProjectPath(string projectPathOrUrl)
    {
        if (Uri.TryCreate(projectPathOrUrl, UriKind.Absolute, out var uri))
            return uri.AbsolutePath.Trim('/');
        return projectPathOrUrl.Trim('/');
    }
    private sealed class GitLabProjectDto
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("default_branch")] public string? DefaultBranch { get; set; }
    }
    protected override async Task PushFrameworkDirectoryAsync(string localPath, FrameworkType framework)
    {
        var project = await ResolveProjectAsync(_projectPathOrUrl);
        await PushDirectoryToGitLab(project.Id, project.DefaultBranch, localPath, $"{framework}");
    }
    private async Task PushDirectoryToGitLab(int projectId, string branch, string localPath, string targetRoot)
    {
        foreach (var filePath in Directory.GetFiles(localPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path
                .Combine(targetRoot, Path.GetRelativePath(localPath, filePath))
                .Replace("\\", "/");

            string content = await File.ReadAllTextAsync(filePath);

            await CreateOrUpdateFileAsync(projectId, branch, relativePath, content, $"Add/Update {relativePath}");
        }
    }
    private async Task CreateOrUpdateFileAsync(int projectId, string branch, string filePath, string content, string commitMessage)
    {
        var encodedPath = Uri.EscapeDataString(filePath);

        using var createResponse = await _http.PostAsync(
            $"projects/{projectId}/repository/files/{encodedPath}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["branch"] = branch,
                ["content"] = content,
                ["commit_message"] = commitMessage
            })
        );

        if (createResponse.IsSuccessStatusCode) return;

        if (createResponse.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity)
        {
            using var updateResponse = await _http.PutAsync(
                $"projects/{projectId}/repository/files/{encodedPath}",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["branch"] = branch,
                    ["content"] = content,
                    ["commit_message"] = commitMessage
                })
            );

            if (updateResponse.IsSuccessStatusCode) return;

            var updateBody = await updateResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to update file '{filePath}'. {updateBody}");
        }
        var createBody = await createResponse.Content.ReadAsStringAsync();
        throw new Exception($"Failed to create file '{filePath}'. {createBody}");
    }
    public async Task PushPulumiCodeAsync(string projectPathOrUrl, string localPulumiPath, Dictionary<string, string> parameters, string Name)
    {
        if (string.IsNullOrWhiteSpace(projectPathOrUrl))
            throw new ArgumentException("Project path or URL is required", nameof(projectPathOrUrl));

        parameters["Name"] = Name;

        if (!Directory.Exists(localPulumiPath))
            throw new DirectoryNotFoundException(localPulumiPath);

        var project = await ResolveProjectAsync(projectPathOrUrl);

        await PushPulumiAsync(localPulumiPath, parameters, async (relativePath, content) =>
        {
            await CreateOrUpdateFileAsync(project.Id, project.DefaultBranch, relativePath, content, $"Add/Update {relativePath}");
        });
    }
}
