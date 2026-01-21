## Stack

This project uses:
- .NET 10
- ASP.NET Core Web API

## Configuration

This project uses ASP.NET Core User Secrets for secure configuration management.
To set up your environment, manage your user secrets using your IDE, or via the command line as shown below:

```bash
# Set the Nuxt app URL (default: http://localhost:3000)
dotnet user-secrets set "NuxtAppUrl" "http://localhost:3000"

# Set your GitHub token
dotnet user-secrets set "GitHubToken" "your_github_token_here"

# Set your GitHub organization name
dotnet user-secrets set "GitHubOrganizationName" "your_organization_name"

# Set your GitLab token (PAT with `api` scope)
dotnet user-secrets set "GitLabToken" "your_gitlab_token_here"

# Optional: set GitLab API base URL (only for self-hosted GitLab)
# Example: https://gitlab.example.com/api/v4
dotnet user-secrets set "GitLabBaseUrl" "https://gitlab.example.com/api/v4"
```

GitHub token requires the following Repository Permissions:
- Read access to metadata
- Read and Write access to administration

**Note:** User secrets are stored in your user profile directory and are NOT committed to source control. The secrets are stored at:
- Windows: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- Linux/macOS: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`


# Setup

dotnet run 

## Test the API

Go to `https://localhost:5064/scalar/v1` and use the UI.
Use the `api.http` file from your IDE.