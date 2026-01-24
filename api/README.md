## Stack

This project uses:

- .NET 10
- ASP.NET Core Web API
- Pulumi Automation API

## Configuration

This project uses ASP.NET Core User Secrets for secure configuration management.
To set up your environment, manage your user secrets using your IDE, or via the command line as shown below:

### GitHub Configuration

Required to create repositories on GitHub:

```bash
# Set your GitHub Personal Access Token (PAT)
# Required scopes: repo (full control), admin:org (if using organization)
dotnet user-secrets set "GitHubToken" "ghp_xxxxxxxxxxxxxxxxxxxx"

# Set your GitHub organization name (optional, defaults to user)
dotnet user-secrets set "GitHubOrganizationName" "your-organization-name"
```

### GitLab Configuration

Required to create projects on GitLab:

```bash
# Set your GitLab Personal Access Token (PAT)
# Required scopes: api, read_user, read_repository
dotnet user-secrets set "GitLabToken" "glpat_xxxxxxxxxxxxxxxxxxxx"

# For self-hosted GitLab: set the API base URL (optional)
# For gitlab.com, this is automatically set to https://gitlab.com/api/v4
# Example for self-hosted: https://gitlab.example.com/api/v4
dotnet user-secrets set "GitLabBaseUrl" "https://gitlab.example.com/api/v4"
```

### Optional Configuration

```bash
# Set the Nuxt app URL (default: http://localhost:3000)
dotnet user-secrets set "NuxtAppUrl" "http://localhost:3000"
```

### Token Permissions Reference

**GitHub Token:**

- `repo` - Full control of private repositories
- `admin:org` - Manage organization (if creating repos in org)
- `user` - Manage user account

**GitLab Token:**

- `api` - Read and write access to API
- `read_user` - Read user profile
- `read_repository` - Read repository access

**Note:** User secrets are stored in your user profile directory and are NOT committed to source control. The secrets are stored at:

- Windows: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- Linux/macOS: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

## Setup

```bash
dotnet run
```

## Test the API

1. **Using Scalar UI**: Go to `https://localhost:5064/scalar/v1`
2. **Using api.http file**: Use the `api.http` file from your IDE REST client extension
3. **Using curl**: See examples in the API endpoint documentation

## API Endpoints

### POST /create-project

Creates a project by:

1. Deploying a template (e.g., ecommerce, enseirb)
2. Optionally creating a repository on GitHub or GitLab

**Request body:**

```json
{
    "templateName": "ecommerce",
    "projectName": "my-project",
    "parameters": {
        "location": "westeurope"
    },
    "platform": {
        "type": "github",
        "name": "my-repo",
        "config": {
            "description": "My awesome project",
            "isPrivate": "true"
        }
    }
}
```

**Response:**

```json
[
    {
        "name": "my-project",
        "resourceType": "template",
        "statusCode": 200,
        "message": "Resource created successfully",
        "outputs": {
            "appUrl": "https://myapp.azurewebsites.net"
        }
    },
    {
        "name": "my-project",
        "resourceType": "github",
        "statusCode": 200,
        "message": "Resource created successfully",
        "outputs": {
            "repositoryUrl": "https://github.com/my-org/my-repo"
        }
    }
]
```

### GET /templates

Lists all available templates with their configuration.

**Response:**

```json
[
    {
        "name": "ecommerce",
        "description": "E-commerce application template",
        "parameters": {
            "location": {
                "type": "string",
                "description": "Azure region"
            }
        }
    }
]
```
