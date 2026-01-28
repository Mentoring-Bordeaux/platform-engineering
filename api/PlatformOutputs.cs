public class GitRepositoryCreationOutputs
{
    public string RepositoryName { get; set; } = string.Empty;
    public string RepositoryUrl { get; set; } = string.Empty;

    public GitRepositoryCreationOutputs(string repositoryName, string repositoryUrl)
    {
        RepositoryName = repositoryName;
        RepositoryUrl = repositoryUrl;
    }
}
