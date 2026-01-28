public abstract class GitRepositoryServiceBase : IGitRepositoryService
{
    public async Task PushPulumiAsync(
        string localPulumiPath,
        Dictionary<string, string> parameters,
        Func<string, string, Task> pushFile
    )
    {
        var files = Directory
            .GetFiles(localPulumiPath, "*", SearchOption.AllDirectories)
            .Where(f => !f.Contains("node_modules"));

        foreach (var file in files)
        {
            var relativePath = Path.Combine(
                    "infrastructure",
                    Path.GetRelativePath(localPulumiPath, file)
                )
                .Replace("\\", "/");

            var content = await File.ReadAllTextAsync(file);
            content = ProjectScaffoldingUtils.ApplyPulumiParameters(content, parameters);

            await pushFile(relativePath, content);
        }
    }

    protected async Task GenerateAndPushMultipleFrameworksAsync(
        List<FrameworkType> frameworks,
        string projectName,
        Func<string, Task> pushAction
    )
    {
        string tempDir = Path.Combine(Path.GetTempPath(), projectName);

        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);

        Directory.CreateDirectory(tempDir);

        foreach (var framework in frameworks)
        {
            string fwDir = Path.Combine(tempDir, framework.ToString());
            Directory.CreateDirectory(fwDir);

            ProjectScaffoldingUtils.GenerateProjectByCli(framework, fwDir);
        }

        await pushAction(tempDir);

        Directory.Delete(tempDir, true);
    }

    public async Task InitializeRepoWithFrameworksAsync(
        List<FrameworkType> frameworks,
        string projectName
    )
    {
        async Task PushAction(string tempDir)
        {
            foreach (var framework in frameworks)
            {
                string fwDir = Path.Combine(tempDir, framework.ToString());
                if (Directory.Exists(fwDir))
                {
                    await PushFrameworkDirectoryAsync(fwDir, framework);
                }
            }
        }

        await GenerateAndPushMultipleFrameworksAsync(frameworks, projectName, PushAction);
    }

    protected abstract Task PushFrameworkDirectoryAsync(string localPath, FrameworkType framework);
}
