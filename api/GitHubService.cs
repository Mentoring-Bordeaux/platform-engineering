using Octokit;

public class GitHubService : GitRepositoryServiceBase
{
    private readonly GitHubClient _client;
    private readonly string _orgName;
    private readonly string _repoName;

    public string OrgName => _orgName;
    public string RepoName => _repoName;

    public GitHubService(string token, string orgName, string repoName)
    {
        _client = new GitHubClient(new ProductHeaderValue("InfraAutomation"))
        {
            Credentials = new Credentials(token)
        };
        _orgName = orgName;
        _repoName = repoName;
    }
    public async Task PushPulumiCodeAsync(
        string orgName,
        string repoName,
        string localPulumiPath,
        Dictionary<string, string> parameters,
        string name)
    {
        parameters["Name"] = name;

        await PushPulumiAsync(
            localPulumiPath,
            parameters,
            async (path, content) =>
            {
                try
                {
                    await _client.Repository.Content.CreateFile(
                        orgName,
                        repoName,
                        path,
                        new CreateFileRequest($"Add {path}", content, "main"));
                }
                catch (ApiException)
                {
                    var existing = await _client.Repository.Content
                        .GetAllContentsByRef(orgName, repoName, path, "main");

                    await _client.Repository.Content.UpdateFile(
                        orgName,
                        repoName,
                        path,
                        new UpdateFileRequest($"Update {path}", content, existing[0].Sha, "main"));
                }
            });
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
    protected override async Task PushFrameworkDirectoryAsync(string localPath, FrameworkType framework)
    {
        await PushDirectoryToGitHub(_orgName, _repoName, localPath, framework.ToString());
    }

}