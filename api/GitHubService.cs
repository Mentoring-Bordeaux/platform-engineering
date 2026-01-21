using Octokit;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Pulumi.Automation;

public class GitHubService
{
    private readonly GitHubClient _client;

    public GitHubService(string token)
    {
        _client = new GitHubClient(new ProductHeaderValue("InfraAutomation"))
        {
            Credentials = new Credentials(token)
        };
    }

    // Initialise le repo avec le framework en remplaçant les placeholders {{Key}}
    public async Task InitializeRepoWithFrameworkAsync(
        string orgName, string repoName, string localTemplatePath, Dictionary<string, string>? parameters = null)
    {
        parameters ??= new Dictionary<string, string>();
        if (!Directory.Exists(localTemplatePath))
            throw new DirectoryNotFoundException($"Template path does not exist: {localTemplatePath}");

        foreach (var filePath in Directory.GetFiles(localTemplatePath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.Combine("app", Path.GetRelativePath(localTemplatePath, filePath)).Replace("\\", "/");
            var content = await File.ReadAllTextAsync(filePath);

            foreach (var kv in parameters)
            {
                string pattern = @"\{\{\s*" + Regex.Escape(kv.Key) + @"\s*\}\}";
                content = Regex.Replace(content, pattern, kv.Value);
            }

            try
            {
                await _client.Repository.Content.CreateFile(
                    orgName,
                    repoName,
                    relativePath,
                    new CreateFileRequest(message: $"Add {relativePath}", content: content, branch: "main")
                );
            }
            catch (Octokit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
            {
                // Le fichier existe déjà → mettre à jour si besoin
                var existingFile = await _client.Repository.Content.GetAllContentsByRef(orgName, repoName, relativePath, "main");
                await _client.Repository.Content.UpdateFile(orgName, repoName, relativePath,
                    new UpdateFileRequest($"Update {relativePath}", content, existingFile[0].Sha, "main"));
            }
        }
    }

    // Pousse les fichiers Pulumi dans le repo
    public async Task PushPulumiCodeAsync(
    string orgName,
    string repoName,
    string localPulumiPath,
    Dictionary<string, string> parameters,
    string Name)
    {
        parameters["Name"] = Name;
        if (!Directory.Exists(localPulumiPath))
            throw new DirectoryNotFoundException(localPulumiPath);

        foreach (var filePath in Directory.GetFiles(localPulumiPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path
            .Combine("infrastructure", Path.GetRelativePath(localPulumiPath, filePath))
            .Replace("\\", "/");

            string content = await File.ReadAllTextAsync(filePath);

            foreach (var kv in parameters)
            {
                string value = kv.Value.Replace("\"", "\\\"");

                // Remplace config.require("Key")
                content = Regex.Replace(
                    content,
                    $@"config\.require\(\s*[""']{Regex.Escape(kv.Key)}[""']\s*\)",
                    $"\"{value}\""
                );

                // Remplace config.get("Key") || "default"
                content = Regex.Replace(
                    content,
                    $@"config\.get\(\s*[""']{Regex.Escape(kv.Key)}[""']\s*\)\s*(\|\|\s*[""'][^""']*[""'])?",
                    $"\"{value}\""
                );

                // Remplace config.get("Key") simple
                content = Regex.Replace(
                    content,
                    $@"config\.get\(\s*[""']{Regex.Escape(kv.Key)}[""']\s*\)",
                    $"\"{value}\""
                );
            }


            try
            {
                await _client.Repository.Content.CreateFile(
                    orgName,
                    repoName,
                    relativePath,
                    new CreateFileRequest(
                        $"Add {relativePath}",
                        content,
                        "main"
                    )
                );
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
            {
                var existing = await _client.Repository.Content
                    .GetAllContentsByRef(orgName, repoName, relativePath, "main");

                await _client.Repository.Content.UpdateFile(
                    orgName,
                    repoName,
                    relativePath,
                    new UpdateFileRequest(
                        $"Update {relativePath}",
                        content,
                        existing[0].Sha,
                        "main"
                    )
                );
            }
        }
    }

}
