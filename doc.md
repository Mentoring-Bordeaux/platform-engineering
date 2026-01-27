# Documentation

## Introduction

This project is an Internal Developer Platform (IDP) designed to automate the creation, configuration, and deployment of modern cloud applications. It combines a frontend portal (Nuxt 4), a backend API (Minimal API .NET 10), and infrastructure as code (Pulumi TypeScript for Azure). The goal is to simplify the developer experience: provision cloud resources, generate GitHub/GitLab repositories, and deploy applications in a few clicks, while following DevOps and cloud native best practices.

The platform orchestrates the entire project lifecycle: from template selection, to remote repository creation, to deployment on Azure. It leverages modern tools (Aspire, Pulumi, GitHub Actions CI/CD) to ensure speed, security, and reproducibility.

This document details the architecture, dependencies, infrastructure workflows, and useful commands to contribute to or deploy the solution.

## Overview

The project is organized as follows:

```
platform-engineering/
├── app/                       # Frontend Nuxt 4 (Vue 3, Pinia, Nuxt UI)
│   ├── app.config.ts
│   ├── components/            # Vue components
│   ├── config/                # Type-safe config (platforms, resources)
│   ├── pages/                 # Nuxt pages (routing)
│   ├── stores/                # Pinia stores (state management)
│   ├── types/                 # TypeScript types/interfaces
│   └── utils/                 # Helpers, validation, etc.
│   └── ...
├── api/                       # .NET 10 Minimal API (Pulumi Automation)
│   ├── Program.cs             # All endpoints (minimal API style)
│   ├── pulumiPrograms/        # Pulumi programs (TS, for dynamic infra)
│   ├── *.cs                   # API logic, DTOs, services
│   └── ...
├── infrastructure/            # Pulumi TypeScript (Azure infra as code)
│   ├── index.ts               # Main Pulumi program
│   ├── Pulumi.yaml            # Stack metadata
│   ├── Pulumi.dev.yaml        # Dev stack config
│   └── ...
├── PlatformEngineering.AppHost/ # .NET Aspire orchestration (dev only)
│   └── ...
├── .github/
│   └── workflows/             # CI/CD pipelines (ci-build.yaml, cd-deploy.yaml)
├── README.md                  # Project overview
├── doc.md                     # Technical documentation (this file)
└── ...
```

**Main folders:**

- `app/`: Frontend Nuxt 4 (Vue 3, Pinia, Nuxt UI)
- `api/`: .NET 10 Minimal API, Pulumi Automation API
- `infrastructure/`: Pulumi TypeScript for Azure
- `PlatformEngineering.AppHost/`: Local orchestration (Aspire)
- `.github/workflows/`: CI/CD pipelines (GitHub Actions)

## Setup

### Prerequisites

- [Pulumi CLI](https://www.pulumi.com/docs/get-started/install/)
- [Node.js](https://nodejs.org/) (v22 or higher)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
- [pnpm](https://pnpm.io/)

### Install Dependencies

```bash
cd infrastructure
pnpm install
```

# Infrastructure As Code

### Pulumi Configuration

```bash
pulumi config set --secret PULUMI_ACCESS_TOKEN <your-pulumi-access-token>
pulumi config set --secret GithubToken <your-github-token>
pulumi config set --secret GitLabToken <your-gitlab-token>
pulumi config set --secret GitHubOrganizationName <your-github-org>
pulumi config set --secret GitLabBaseUrl <your-gitlab-url>
```

## Provisioning

### Deploy to Development

Deployment via CI/CD pipeline:

Manually trigger the `infrastructure.yaml` pipeline (in Azure DevOps, GitHub Actions, etc.) to provision the infrastructure.

This pipeline will:

1. Preview infrastructure changes
2. Ask for confirmation (if configured)
3. Apply the changes to your Azure subscription

### Destroy Infrastructure

To clean up all resources:

```bash
pulumi destroy
```

## Infrastructure Stack

The Pulumi program provisions the following Azure resources:

### Static Web App (Frontend)

- Hosts the Nuxt 4 frontend

### Container App (API)

- Hosts the .NET 10 API

### Container Registry (ACR)

- Stores Docker images

### Azure Key Vault

- Stores secrets and certificates

### Managed Identity

- Provides secure access to resources

## Infrastructure Outputs

The Pulumi program exports the following outputs after deployment:

- **staticWebUrl**: Hostname of the Static Web App (frontend URL)
- **staticWebAppName**: Name of the Static Web App resource
- **backendUrl**: Public URL of the Container App (API endpoint)
- **resourceGroupName**: Name of the Azure Resource Group
- **containerRegistryName**: Name of the Azure Container Registry (ACR)
- **containerAppName**: Name of the Azure Container App (API)
- **acrServer**: Login server URL for the ACR
- **keyVault**: Name of the Azure Key Vault

# CI/CD Pipelines

The project uses **GitHub Actions** to automate build, test, deployment, and infrastructure management.

### Required secrets in GitHub Actions

For CI/CD pipelines to work properly, the following secrets must be set in the GitHub repository settings (Settings > Secrets and variables > Actions):

- `AZURE_CLIENT_ID`: Azure AD application ID (Service Principal)
- `AZURE_TENANT_ID`: Azure AD tenant ID
- `AZURE_SUBSCRIPTION_ID`: Azure subscription ID

These secrets are used for Azure authentication during infrastructure, backend (API), and frontend deployment.

### Main workflows

- **[.github/workflows/ci-build.yaml](.github/workflows/ci-build.yaml)**
  - Builds, tests, and pushes Docker images for the API
  - Builds and uploads the Nuxt frontend
  - Downloads infrastructure outputs to configure builds

- **[.github/workflows/cd-deploy.yaml](.github/workflows/cd-deploy.yaml)**
  - Deploys the API to Azure Container Apps
  - Deploys the frontend to Azure Static Web Apps
  - Uses infrastructure outputs generated by Pulumi

### Triggers

- On push/pull request to `main`
- Manually (`workflow_dispatch`)
- After successful completion of the `ci-build` workflow

---

# Backend (.NET 10 API)

The API is developed in .NET 10 (Minimal API) and runs in a Docker container.

### Main backend structure

- **Program.cs**:
  - API entry point. Defines all REST endpoints (minimal API), configures CORS, OpenAPI documentation, and orchestrates the project creation cycle.
  - Handles routing logic: `/api/create-project` to provision a project, `/api/templates` to list available templates.
  - Validates requests and injects required services (including PulumiService).

- **PulumiService.cs**:
  - Central service for running Pulumi programs on the backend.
  - Manages repository creation (GitHub/GitLab), template initialization, and cloud resource provisioning via Pulumi Automation API.
  - Handles parameter management, dependency installation, execution, and cleanup of Pulumi stacks.
  - Provides methods to run Pulumi programs according to the selected platform or template, and to push code to the remote repository.

### Main endpoints

The API exposes the following endpoints:

- **POST /api/create-project**:
  - Creates a new project by provisioning the remote repository (GitHub/GitLab) and associated cloud infrastructure via Pulumi.
  - Receives a JSON object with project parameters, target platform, and template.
  - Returns the result of the actions (repo URL, endpoints, etc.).

- **GET /api/templates**:
  - Returns the list of available project templates (dotnet-api, ecommerce, enseirb, etc.).
  - Allows the frontend to display template options when creating a project.

> These endpoints are also accessible without the `/api/` prefix for compatibility with Azure Static Web Apps (e.g., `/create-project`, `/templates`).

### Required environment variables (backend)

To work properly, the API requires the following environment variables (to be set in `api/.env`):

- `GITHUB_TOKEN`: GitHub personal access token to create repositories.
- `GITHUB_ORGANIZATION_NAME`: Target GitHub organization name.
- `GITLAB_TOKEN`: GitLab personal access token to create repositories.
- `GITLAB_BASE_URL`: Base URL of the GitLab instance (e.g., https://gitlab.com).

These variables are used for authentication and platform configuration during project creation. They must be set before starting the API.

> See also the example file `api/.env.example` for the expected format.

### Local execution

**Prerequisites:**

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/)
- Environment variables in `api/.env` (see `api/.env.example`)

**Run locally (without Docker):**

```bash
cd api
dotnet run
# API available at http://localhost:5064
```

**Run locally with Docker:**

```bash
cd api
docker build -t platform-engineering-api .
docker run --env-file .env -p 5064:5064 platform-engineering-api
```

### Cloud execution (via pipelines)

- The Docker image is built and pushed to Azure Container Registry by the [`ci-build.yaml`](.github/workflows/ci-build.yaml) workflow.
- Deployment to Azure Container Apps is automated by the [`cd-deploy.yaml`](.github/workflows/cd-deploy.yaml) pipeline.

### Backend Dockerfile (api/)

The backend Dockerfile (api/ folder) prepares a complete environment to run the .NET API and provision cloud infrastructure via Pulumi. Here are the main dependencies installed and their purpose:

1. **.NET ASP.NET 10.0**: Base image to run the .NET API.
2. **Pulumi CLI**: Allows the API to dynamically provision cloud resources via Pulumi Automation API.
3. **Node.js**: Required to run Pulumi programs written in TypeScript/JavaScript (in pulumiPrograms/).
4. **pnpm**: Fast package manager used to install Node.js dependencies for Pulumi programs.
5. **Azure CLI**: Allows Pulumi and the API to interact with Azure (create cloud resources).
6. **Copy .NET SDK into the final image**: Allows running dotnet SDK commands at runtime (useful for Pulumi or other .NET tools).
7. **Permissions and environment variable configuration**: So the non-root user can write to /app, /app/.pulumi, /app/.dotnet (required for Pulumi and .NET).

See the Dockerfile in api/Dockerfile for step details.

## Pulumi Automation API

The .NET API uses **Pulumi Automation API** to dynamically provision infrastructure and code platforms (GitHub, GitLab, etc.).

### How it works

1. The user selects a platform (GitHub, GitLab) and a project template via the frontend.
2. The API orchestrates the execution of a specific Pulumi program (in `api/pulumiPrograms/`) to create the remote repository, initialize the code, and provision the associated cloud infrastructure.
3. User parameters (project name, options, secrets, etc.) are injected into the Pulumi program.
4. The API runs the full Pulumi cycle (Up/Destroy) and returns useful information (repo URL, endpoints, etc.).

### pulumiPrograms directory structure

Each subfolder of `api/pulumiPrograms/` corresponds to a platform, resource type, or provisionable template:

```
api/
└── pulumiPrograms/
    ├── platforms/             # Provisioning GitHub/GitLab repositories
    │   ├── github/
    │   │   └── index.ts
    │   └── gitlab/
    │       └── index.ts
    ├── templates/          # Project templates (dotnet-api, ecommerce, etc.)
    │   ├── dotnet-api/
    │   ├── ecommerce/
    │   └── enseirb/
    └── ...                 # Other resources or templates
```

### Project templates

Code templates (e.g., dotnet-api, ecommerce, enseirb…) are stored in `api/pulumiPrograms/templates/` and copied to the remote repository when creating a project.

---

# Frontend (Nuxt 4)

The frontend is developed with Nuxt 4 (Vue 3, Pinia, Nuxt UI).

### Local execution

**Prerequisites:**

- [Node.js 22+](https://nodejs.org/)
- [pnpm](https://pnpm.io/)

**Run locally:**

```bash
cd app
pnpm install
pnpm dev
# Frontend available at http://localhost:3000
```

> The API URL must be configured via the `NUXT_API_URL` environment variable (see `app/.env` or Aspire config).

### Cloud execution (via pipelines)

- The Nuxt build is generated and uploaded as an artifact by the CI/CD workflow.
- Deployment to Azure Static Web Apps is automated by the [`cd-deploy.yaml`](.github/workflows/cd-deploy.yaml) pipeline.

---

# Summary of Useful Commands

| Action                      | Local command                        | CI/CD Pipeline             |
| --------------------------- | ------------------------------------ | -------------------------- |
| Run everything (Aspire)     | `aspire run`                         | -                          |
| Run API (.NET)              | `cd api && dotnet run`               | Build/test in `ci-build`   |
| Run API (Docker)            | `docker build/run` in `api/`         | Build/push in `ci-build`   |
| Run Frontend (Nuxt)         | `cd app && pnpm install && pnpm dev` | Build/upload in `ci-build` |
| Deploy infra (Pulumi)       | `cd infrastructure && pulumi up`     | `infrastructure` pipeline  |
| Deploy API/Frontend (Azure) | -                                    | `cd-deploy.yaml`           |

---


