# Platform Engineering

## 🏗️ Architecture

The project consists of four main components:

1. **Frontend** (`app/`) - Developer portal built with Nuxt 4 + Vue 3 + Pinia + Nuxt UI
2. **API** (`api/`) - .NET 10 API using Pulumi Automation API to provision infrastructure
3. **Infrastructure** (`infrastructure/`) - Pulumi TypeScript code for deployment
4. **Orchestration** (`PlatformEngineering.AppHost/`) - .NET Aspire to manage local development

dotnet run

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js](https://nodejs.org/) (v22 or higher)
- [pnpm](https://pnpm.io/) (`npm install -g pnpm`)
- [Pulumi CLI](https://www.pulumi.com/docs/get-started/install/)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) (for deployment)

---

## ☁️ Cloud Deployment (CI/CD)

Le déploiement cloud est automatisé via GitHub Actions :

1. **Provisionner l’infrastructure Azure**
   - Lancer manuellement le pipeline `infrastructure.yaml` (Azure DevOps ou GitHub Actions) pour créer les ressources cloud (ACR, Static Web App, Container App, Key Vault…)
2. **Déployer l’API et le Frontend**
   - Le pipeline [`ci-build.yaml`](.github/workflows/ci-build.yaml) build et push l’image Docker de l’API, build le frontend Nuxt, et publie les artefacts
   - Le pipeline [`cd-deploy.yaml`](.github/workflows/cd-deploy.yaml) déploie l’API sur Azure Container Apps et le frontend sur Azure Static Web Apps

**Secrets requis dans GitHub Actions** :

- `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`

---

## 🖥️ Local Development

### Run the Full Stack (Recommended)

```bash
aspire run
```

This command starts:

- The frontend (Nuxt, app/)
- The API (.NET, api/)
- Aspire dashboard for service discovery

### Run Services Individually

#### Frontend

```bash
cd app
pnpm install
pnpm dev
# http://localhost:3000
```

#### API

```bash
cd api
cd pulumiPrograms && pnpm install && cd ..
dotnet run
# http://localhost:5064
```

#### Pulumi Infrastructure (optional, for local preview)

```bash
cd infrastructure
pnpm install
pulumi up
```

---

## 🧪 Tests

```bash
# Unit tests (frontend)
cd app
pnpm test
```

## 📁 Project Structure

```
├── app/                          # Nuxt 4 Frontend
│   ├── app/
│   │   ├── assets/               # Static Assets (CSS, images, etc.)
│   │   ├── components/           # Vue Components
│   │   ├── config/               # Platform/Resource Configuration
│   │   ├── pages/                # Pages (file-based routing)
│   │   ├── stores/               # Pinia Stores
│   │   ├── types/                # TypeScript types for some entities
│   │   └── utils/                # Utilities and Zod validation
│   └── package.json
│
├── api/                          # .NET 10 API
│   ├── Program.cs                # API entry points (minimal API)
│   ├── PulumiService.cs          # Pulumi Automation API service
│   └── pulumiPrograms/           # Pulumi programs for each resource
│       ├── platforms/            # Pulumi programs for platforms
│       ├── resources/            # Pulumi programs for resources
│
├── infrastructure/               # Pulumi Infrastructure
│
└── PlatformEngineering.AppHost/  # Aspire Orchestration
```

## 🔧 Configuration

The API uses **dotnet user-secrets** for secure local development configuration. Manage your secrets using your IDE or the command line:

### GitHub Configuration

Required to create repositories on GitHub:

```bash
cd api

# Set your GitHub Personal Access Token (PAT)
# Required scopes: repo (full control), admin:org (if using organization)
dotnet user-secrets set "GitHubToken" "ghp_xxxxxxxxxxxxxxxxxxxx"

# Set your GitHub organization name (optional, defaults to user)
dotnet user-secrets set "GitHubOrganizationName" "your-organization-name"

# Set the Nuxt app URL (used for CORS)
dotnet user-secrets set "NuxtAppUrl" "http://localhost:3000"
```

### GitLab Configuration

Required to create projects on GitLab:

```bash
cd api

# Set your GitLab Personal Access Token (PAT)
# Required scopes: api, read_user, read_repository
dotnet user-secrets set "GitLabToken" "glpat_xxxxxxxxxxxxxxxxxxxx"

# For self-hosted GitLab: set the API base URL (optional)
# For gitlab.com, this is automatically set to https://gitlab.com/api/v4
# Example for self-hosted: https://gitlab.example.com/api/v4
dotnet user-secrets set "GitLabBaseUrl" "https://gitlab.example.com/api/v4"
```

**GitHub token permissions required:**

- Read access to metadata
- Read and Write access to administration

**Note:** User secrets are stored in your user profile and are NOT committed to source control:

- Windows: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- Linux/macOS: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

### Frontend Configuration

The frontend uses environment variables. Create a `.env` file in the `app/` directory:

```bash
cp app/.env.example app/.env
```

## 🛠️ Technology Stack

| Component      | Technologies                             |
| -------------- | ---------------------------------------- |
| Frontend       | Nuxt 4, Vue 3, Pinia, Nuxt UI, Zod       |
| API            | .NET 10, Pulumi Automation API           |
| Infrastructure | Pulumi TypeScript, Azure Native Provider |
| Orchestration  | .NET Aspire                              |

## 📚 Documentation

- [Templates Documentation](TEMPLATES.md) - How templates work and how to create new ones
- [Frontend README](app/README.md)
- [API README](api/README.md) - Configuration details for GitHub and GitLab
- [Infrastructure README](infrastructure/README.md) - Details on Pulumi infrastructure
- [doc.md](doc.md) - Project documentation
