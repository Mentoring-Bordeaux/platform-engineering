# Platform Engineering

## 🏗️ Architecture

The project consists of four main components:

1. **Frontend** (`app/`) - Developer portal built with Nuxt 4 + Vue 3 + Pinia + Nuxt UI
2. **API** (`api/`) - .NET 10 API using Pulumi Automation API to provision infrastructure
3. **Infrastructure** (`infrastructure/`) - Pulumi TypeScript code for deployment
4. **Orchestration** (`PlatformEngineering.AppHost/`) - .NET Aspire to manage local development

## 🚀 Getting Started

### Prerequisites

-   [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
-   [Node.js](https://nodejs.org/) (v22 or higher)
-   [pnpm](https://pnpm.io/) (`npm install -g pnpm`)
-   [Pulumi CLI](https://www.pulumi.com/docs/get-started/install/)
-   [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) (for deployment)

### Run the Full Stack

The easiest way to start the entire stack:

```bash
# From the project root
aspire run
```

This command automatically starts:

-   The frontend (app)
-   The API (api)
-   The Aspire dashboard to monitor services

### Run Services Individually

#### Frontend

```bash
cd app
pnpm install
pnpm dev
```

The frontend will be accessible at http://localhost:3000

#### API

```bash
cd api
cd pulumiPrograms
pnpm install
cd ..
dotnet run
```

The API will be accessible at http://localhost:5064

## 🧪 Tests

```bash
# Unit tests
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

### API Configuration

The API uses **dotnet user-secrets** for secure local development configuration:

```bash
cd api

# Set the Nuxt app URL (used for CORS - set to your frontend dev URL)
dotnet user-secrets set "NuxtAppUrl" "http://localhost:3000"

# Set your GitHub token
dotnet user-secrets set "GitHubToken" "your_github_token_here"

# Set your GitHub organization name (optional)
dotnet user-secrets set "GitHubOrganizationName" "your_organization_name"
```

**GitHub token permissions required:**
- Read access to metadata
- Read and Write access to administration

**Note:** User secrets are stored in your user profile and are NOT committed to source control:
- Windows: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- Linux/macOS: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

**CORS behavior:**
- In **development**: The API allows requests from any origin for convenience
- In **production**: Set `NuxtAppUrl` to the exact frontend origin for strict CORS enforcement

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

-   [Frontend README](app/README.md)
-   [API README](api/README.md)
-   [Infrastructure README](infrastructure/README.md)
