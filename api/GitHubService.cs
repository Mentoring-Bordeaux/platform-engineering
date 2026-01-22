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

    // Initialise le repo avec un projet généré dynamiquement via CLI
    public async Task InitializeRepoWithFrameworkAsync(
        string orgName,
        string repoName,
        FrameworkType framework,
        string projectName)
    {
        // Crée un dossier temporaire pour générer le projet
        string tempDir = Path.Combine(Path.GetTempPath(), projectName);
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);
        Directory.CreateDirectory(tempDir);

        // Génération du projet selon le framework
        GenerateProjectByCli(framework, tempDir);

        // Pousse tous les fichiers générés vers GitHub
        await PushDirectoryToGitHub(orgName, repoName, tempDir, "app");

        // Supprime le dossier temporaire
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
            _ => throw new ArgumentException("Framework non supporté")
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
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new Exception($"Erreur lors de l'exécution de la commande {fileName} {args}:\n{stderr}");
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
