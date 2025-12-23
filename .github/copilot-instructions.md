# Platform Engineering - Copilot Instructions

## Architecture Overview

This is an Internal Developer Platform (IDP) with three main components:

1. **Frontend (`app/`)** - Nuxt 4 + Vue 3 + Pinia + Nuxt UI for the developer portal
2. **API (`api/`)** - .NET 10 minimal API using **Pulumi Automation API** to provision infrastructure
3. **Infrastructure (`infrastructure/`)** - Pulumi TypeScript for Azure deployment (Static Web App, Container Apps, ACR)

**Orchestration**: .NET Aspire (`PlatformEngineering.AppHost/`) manages local development, handling service discovery and CORS between frontend and API automatically.

## Development Workflow

### Starting the Full Stack (Recommended)

```bash
aspire run  # From repository root - starts both app and API with service discovery
```

### Individual Services

```bash
# Frontend only (app/)
pnpm install && pnpm dev  # http://localhost:3000

# API only (api/)
dotnet run  # http://localhost:5064
```

### Testing

```bash
cd app && pnpm test        # Run Vitest unit tests
cd app && pnpm test:watch  # Watch mode
```

## Key Patterns

### Pulumi Automation API Pattern

The API uses Pulumi Automation API (not CLI) to provision resources dynamically. Each endpoint:

1. Creates a temporary Pulumi stack (`api/pulumiPrograms/{service}/`)
2. Sets config values, runs `stack.UpAsync()`
3. **Always cleans up** with `stack.DestroyAsync()` and removes the stack YAML

See `api/Program.cs` → `CreateGitHubRepository()` for the canonical pattern.

### Frontend Configuration-Driven UI

Forms are generated from typed config objects:

-   `app/app/config/platforms.ts` - Git hosting platforms (GitHub, GitLab)
-   `app/app/config/resources.ts` - Cloud resources (Azure SWA, App Service, etc.)
-   `app/app/utils/validation.ts` - Zod schema generation from field configs

**Adding a new resource**: Add entry to `RESOURCES` in `resources.ts` with `config` defining fields.

### Service Discovery (Aspire)

CORS and API URLs are resolved via Aspire service discovery:

```csharp
// API reads app URL from: builder.Configuration["services:app:https:0"]
// Frontend reads API URL from: NUXT_API_URL environment variable
```

## Project Structure Conventions

```
app/app/
├── components/     # Vue SFCs using Nuxt UI (UButton, UForm, etc.)
├── config/         # Type-safe configuration for platforms/resources
├── pages/          # File-based routing (index.vue → /, configure.vue → /configure)
├── stores/         # Pinia stores (project.ts manages wizard state)
├── types/          # TypeScript interfaces (Field, Platform, Resource)
└── utils/          # Helpers including Zod validation generators

api/
├── Program.cs           # All endpoints defined here (minimal API style)
├── *Request.cs          # Request DTOs
└── pulumiPrograms/      # Each subfolder is a standalone Pulumi program
    ├── github/          # GitHub repo provisioning
    └── staticWebApp/    # Azure Static Web App provisioning
```

## Technology Stack Details

| Component     | Stack                                    | Version                           |
| ------------- | ---------------------------------------- | --------------------------------- |
| Frontend      | Nuxt 4, Vue 3, Pinia, Nuxt UI, Zod       | See `app/package.json`            |
| API           | .NET 10 (pre-release), Pulumi.Automation | `net10.0-rc.2`                    |
| IaC           | Pulumi TypeScript, Azure Native provider | See `infrastructure/package.json` |
| Orchestration | .NET Aspire                              | `13.0.1`                          |

## Environment Variables

Required in `.env` at `api/` root:

-   `GITHUB_TOKEN` - PAT for GitHub repo creation
-   `ORGANIZATION_NAME` (optional) - GitHub org for repos

## Common Tasks

**Add new API endpoint**: Define in `api/Program.cs` using `app.MapPost()` pattern, create request DTO as `*Request.cs`

**Add new cloud resource**:

1. Create Pulumi program in `api/pulumiPrograms/{name}/`
2. Add resource config to `app/app/config/resources.ts`
3. Add API endpoint following existing patterns
