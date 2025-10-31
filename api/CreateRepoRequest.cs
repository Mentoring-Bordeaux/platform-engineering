
/* Example request model for creating a GitHub repository
   This model is used to deserialize the JSON request body sent to the API endpoint.
   It includes all the necessary fields for creating a new repository on GitHub.
   The fields are annotated with validation attributes to enforce required fields and data types.
   The model is then used in the API endpoint to create the repository using the Pulumi Automation API.
   Example JSON request body:
   {
       "githubToken": "your_github_token",
       "repoName": "your_repo_name",
       "description": "your_repo_description",
       "private": true,
       "orgName": "your_org_name"
   }
*/


public class CreateRepoRequest
{
    // GitHub Personal Access Token (PAT) with repo creation permissions
    public required string GithubToken { get; set; }
    // Repository name to create
    public required string RepoName { get; set; }
    // Optional description
    public string? Description { get; set; }
    // Optional: whether the repository should be private. Defaults to true.
    public bool Private { get; set; } = true;

    // Optional: organization name where the repository should be created. If null or empty, creates under the authenticated user's account.
    public string? OrgName { get; set; }
}

