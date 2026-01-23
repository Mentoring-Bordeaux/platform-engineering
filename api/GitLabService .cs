using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

public class GitLabService
{
    private readonly HttpClient _http;

    public GitLabService(string token, string? gitlabBaseUrl = null)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("GitLab token is required", nameof(token));
        }

        var apiBase = NormalizeGitLabApiBaseUrl(gitlabBaseUrl);

        _http = new HttpClient
        {
            BaseAddress = new Uri(apiBase, UriKind.Absolute),
        };

        // PAT authentication.
        _http.DefaultRequestHeaders.Add("PRIVATE-TOKEN", token);
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json")
        );
    }

    public async Task InitializeRepoWithFrameworkAsync(
        string projectPathOrUrl,
        FrameworkType framework,
        string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectPathOrUrl))
        {
            throw new ArgumentException("Project path or URL is required", nameof(projectPathOrUrl));
        }

        string tempDir = Path.Combine(Path.GetTempPath(), projectName);
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, true);
        }

        Directory.CreateDirectory(tempDir);

        GenerateProjectByCli(framework, tempDir);

        var project = await ResolveProjectAsync(projectPathOrUrl);
        await PushDirectoryToGitLab(project.Id, project.DefaultBranch, tempDir, "app");

        Directory.Delete(tempDir, true);
    }

    public async Task PushPulumiCodeAsync(
        string projectPathOrUrl,
        string localPulumiPath,
        Dictionary<string, string> parameters,
        string Name)
    {
        if (string.IsNullOrWhiteSpace(projectPathOrUrl))
        {
            throw new ArgumentException("Project path or URL is required", nameof(projectPathOrUrl));
        }

        parameters["Name"] = Name;

        if (!Directory.Exists(localPulumiPath))
        {
            throw new DirectoryNotFoundException(localPulumiPath);
        }

        var project = await ResolveProjectAsync(projectPathOrUrl);

        var allFiles = Directory.GetFiles(localPulumiPath, "*", SearchOption.AllDirectories)
            .Where(
                f =>
                    !f.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}")
                    && !f.Contains($"{Path.DirectorySeparatorChar}node_modules")
            );

        foreach (var filePath in allFiles)
        {
            var relativePath = Path
                .Combine("infrastructure", Path.GetRelativePath(localPulumiPath, filePath))
                .Replace("\\", "/");

            string content = await File.ReadAllTextAsync(filePath);

            foreach (var kv in parameters)
            {
                string value = kv.Value.Replace("\"", "\\\"");

                content = Regex.Replace(
                    content,
                    $@"config\.require\(\s*[""']{Regex.Escape(kv.Key)}[""']\s*\)",
                    $"\"{value}\""
                );

                content = Regex.Replace(
                    content,
                    $@"config\.get\(\s*[""']{Regex.Escape(kv.Key)}[""']\s*\)\s*(\|\|\s*[""'][^""']*[""'])?",
                    $"\"{value}\""
                );

                content = Regex.Replace(
                    content,
                    $@"config\.get\(\s*[""']{Regex.Escape(kv.Key)}[""']\s*\)",
                    $"\"{value}\""
                );

                content = Regex.Replace(
                    content,
                    $@"config\.getSecret\(\s*[""']{Regex.Escape(kv.Key)}[""']\s*\)",
                    $"\"{value}\""
                );
            }

            await CreateOrUpdateFileAsync(
                project.Id,
                project.DefaultBranch,
                relativePath,
                content,
                commitMessage: $"Add/Update {relativePath}"
            );
        }
    }

    private static string NormalizeGitLabApiBaseUrl(string? gitlabBaseUrl)
    {
        var trimmed = (gitlabBaseUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return "https://gitlab.com/api/v4/";
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException($"Invalid GitLab base URL: '{gitlabBaseUrl}'", nameof(gitlabBaseUrl));
        }

        // If user already provided an API base url (e.g. https://gitlab.example.com/api/v4), keep it.
        var path = uri.AbsolutePath.TrimEnd('/');
        if (path.EndsWith("/api/v4", StringComparison.OrdinalIgnoreCase))
        {
            return uri.ToString().TrimEnd('/') + "/";
        }

        // Otherwise append the API path (e.g. https://gitlab.com -> https://gitlab.com/api/v4/).
        var builder = new UriBuilder(uri)
        {
            Path = uri.AbsolutePath.TrimEnd('/') + "/api/v4/",
        };
        return builder.Uri.ToString();
    }

    private sealed record ResolvedProject(int Id, string DefaultBranch);

    private async Task<ResolvedProject> ResolveProjectAsync(string projectPathOrUrl)
    {
        // GitLab accepts either numeric ID or URL-encoded full path (<group>/<project>) in /projects/:id.
        string idOrPath;
        if (int.TryParse(projectPathOrUrl, out _))
        {
            idOrPath = projectPathOrUrl;
        }
        else
        {
            idOrPath = ExtractProjectPath(projectPathOrUrl);
            idOrPath = Uri.EscapeDataString(idOrPath);
        }

        using var response = await _http.GetAsync($"projects/{idOrPath}");
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Failed to resolve GitLab project '{projectPathOrUrl}'. "
                + $"Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={body}"
            );
        }

        var project = JsonSerializer.Deserialize<GitLabProjectDto>(body);
        if (project == null || project.Id == 0)
        {
            throw new Exception($"Unexpected GitLab project payload for '{projectPathOrUrl}'.");
        }

        var defaultBranch = string.IsNullOrWhiteSpace(project.DefaultBranch) ? "main" : project.DefaultBranch;
        return new ResolvedProject(project.Id, defaultBranch);
    }

    private static string ExtractProjectPath(string projectPathOrUrl)
    {
        if (Uri.TryCreate(projectPathOrUrl, UriKind.Absolute, out var uri))
        {
            // Example: https://gitlab.com/group/subgroup/project -> group/subgroup/project
            return uri.AbsolutePath.Trim('/');
        }

        return projectPathOrUrl.Trim('/');
    }

    private async Task PushDirectoryToGitLab(int projectId, string branch, string localPath, string targetRoot)
    {
        foreach (var filePath in Directory.GetFiles(localPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path
                .Combine(targetRoot, Path.GetRelativePath(localPath, filePath))
                .Replace("\\", "/");

            string content = await File.ReadAllTextAsync(filePath);

            await CreateOrUpdateFileAsync(
                projectId,
                branch,
                relativePath,
                content,
                commitMessage: $"Add/Update {relativePath}"
            );
        }
    }

    private async Task CreateOrUpdateFileAsync(
        int projectId,
        string branch,
        string filePath,
        string content,
        string commitMessage)
    {
        var encodedPath = Uri.EscapeDataString(filePath);

        // Try create
        using var createResponse = await _http.PostAsync(
            $"projects/{projectId}/repository/files/{encodedPath}",
            new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["branch"] = branch,
                    ["content"] = content,
                    ["commit_message"] = commitMessage,
                }
            )
        );

        if (createResponse.IsSuccessStatusCode)
        {
            return;
        }

        if (createResponse.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity)
        {
            using var updateResponse = await _http.PutAsync(
                $"projects/{projectId}/repository/files/{encodedPath}",
                new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["branch"] = branch,
                        ["content"] = content,
                        ["commit_message"] = commitMessage,
                    }
                )
            );

            if (updateResponse.IsSuccessStatusCode)
            {
                return;
            }

            var updateBody = await updateResponse.Content.ReadAsStringAsync();
            throw new Exception(
                $"Failed to update file '{filePath}' on GitLab project {projectId}. "
                + $"Status={(int)updateResponse.StatusCode} {updateResponse.ReasonPhrase}. Body={updateBody}"
            );
        }

        var createBody = await createResponse.Content.ReadAsStringAsync();
        throw new Exception(
            $"Failed to create file '{filePath}' on GitLab project {projectId}. "
            + $"Status={(int)createResponse.StatusCode} {createResponse.ReasonPhrase}. Body={createBody}"
        );
    }

    private void GenerateProjectByCli(FrameworkType framework, string targetDir)
    {
        string args = framework switch
        {
            FrameworkType.dotnet => "new webapi -n app",
            FrameworkType.React => "create vite@latest app -- --template react",
            FrameworkType.Vue => "create vue@latest app",
            FrameworkType.Nuxt => "nuxi init app",
            FrameworkType.JavaSpring => "init app --build=maven --java-version=17",
            _ => throw new ArgumentException("Framework non supporté")
        };

        string executable = framework switch
        {
            FrameworkType.dotnet => "dotnet",
            FrameworkType.React or FrameworkType.Vue => "npm",
            FrameworkType.Nuxt => "npx",
            FrameworkType.JavaSpring => "spring",
            _ => throw new ArgumentException("Unsupported framework")
        };

        RunCommand(executable, args, targetDir);
    }

    private static void RunCommand(string fileName, string args, string workingDir)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            }
        };

        process.Start();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Error executing the command {fileName} {args}:\n{stderr}");
        }
    }

    private sealed class GitLabProjectDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("default_branch")]
        public string? DefaultBranch { get; set; }
    }
}

