using Octokit;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

public enum FrameworkType
{
    dotnet,
    React,
    Vue,
    Nuxt,
    JavaSpring
}

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
    public async Task InitializeRepoWithFrameworkAsync(
        string orgName,
        string repoName,
        FrameworkType framework,
        string projectName)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), projectName);
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);
        Directory.CreateDirectory(tempDir);

        GenerateProjectByCli(framework, tempDir);

        await PushDirectoryToGitHub(orgName, repoName, tempDir, "app");

        Directory.Delete(tempDir, true);
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

    private void RunCommand(string fileName, string args, string workingDir)
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
                UseShellExecute = false
            }
        };

        process.Start();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new Exception($"Error executing the command {fileName} {args}:\n{stderr}");
    }

    private async Task PushDirectoryToGitHub(string orgName, string repoName, string localPath, string targetRoot)
    {
        foreach (var filePath in Directory.GetFiles(localPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.Combine(targetRoot, Path.GetRelativePath(localPath, filePath)).Replace("\\", "/");
            string content = await File.ReadAllTextAsync(filePath);

            try
            {
                await _client.Repository.Content.CreateFile(
                    orgName,
                    repoName,
                    relativePath,
                    new CreateFileRequest($"Add {relativePath}", content, branch: "main")
                );
            }
            catch (Octokit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
            {
                var existingFile = await _client.Repository.Content.GetAllContentsByRef(orgName, repoName, relativePath, "main");
                await _client.Repository.Content.UpdateFile(
                    orgName,
                    repoName,
                    relativePath,
                    new UpdateFileRequest($"Update {relativePath}", content, existingFile[0].Sha, "main")
                );
            }
        }
    }

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
        var allFiles = Directory.GetFiles(localPulumiPath, "*", SearchOption.AllDirectories)
        .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}") &&
                    !f.Contains($"{Path.DirectorySeparatorChar}node_modules"));

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
