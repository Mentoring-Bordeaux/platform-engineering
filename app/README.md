## Frontend Documentation

This project uses **Nuxt 4** with **Vue 3**, **Pinia**, **Nuxt UI**, and **Zod** for a modern, type-safe developer portal.

## Quick Start

To install all necessary dependencies:

```bash
pnpm install
```

## Development

Start the development server. Your application will be available at http://localhost:3000:

```bash
pnpm dev
```

Note: The homepage (URL `/`) is handled by the `pages/index.vue` file.

## Production

Build the application for production:

```bash
pnpm build
```

Locally preview production build:

```bash
pnpm preview
```

## Architecture

### Project Structure

```
app/
├── assets/              # Static Assets (CSS, images, etc.)
├── components/          # Vue SFCs (Reusable components)
├── config/              # Type-safe configuration
├── pages/               # File-based routing
├── stores/              # Pinia state management
├── types/               # TypeScript interfaces
├── utils/               # Helpers and utilities
└── app.config.ts        # App configuration
```

### State Management with Pinia

The project uses **Pinia** for state management. The main store is `stores/project.ts`, which manages:

- Project wizard state (current step, form data)
- Selected platform and template
- Configuration parameters

Example:

```typescript
// stores/project.ts
import { defineStore } from 'pinia'

export const useProjectStore = defineStore('project', {
  state: () => ({
    currentStep: 1,
    projectName: '',
    selectedTemplate: null,
    parameters: {}
  }),

  actions: {
    setProjectName(name: string) {
      this.projectName = name
    },
    setParameters(params: Record<string, any>) {
      this.parameters = params
    }
  }
})
```

### Type-Safe Configuration with Zod

The project uses **Zod** for runtime schema validation. Configuration is defined in `config/` and validated automatically:

- **`config/platforms.ts`** - Git hosting platforms (GitHub, GitLab)
- **`config/project-options.ts`** - Project configuration options
- **`utils/validation.ts`** - Zod schema generation helpers

Example configuration:

```typescript
// config/platforms.ts
export const PLATFORMS = {
  github: {
    name: 'GitHub',
    fields: {
      repositoryName: {
        type: 'text',
        label: 'Repository Name',
        required: true
      }
    }
  },
  gitlab: {
    name: 'GitLab',
    fields: {
      projectName: {
        type: 'text',
        label: 'Project Name',
        required: true
      }
    }
  }
}

// Auto-generate Zod schema from config
const githubSchema = generateZodSchema(PLATFORMS.github.fields)
```

## Testing

Run unit tests with Vitest:

```bash
pnpm test
```

Watch mode for development:

```bash
pnpm test:watch
```

## Configuration Files

- **`nuxt.config.ts`** - Nuxt framework configuration
- **`tsconfig.json`** - TypeScript configuration
- **`vitest.config.ts`** - Vitest testing configuration
- **`eslint.config.mjs`** - ESLint configuration

## Integration with API

The frontend communicates with the .NET API at `http://localhost:5064` (in development). The API URL is set via the `NUXT_API_URL` environment variable or through service discovery in the Aspire orchestration layer.

Example API call:

```typescript
// Fetch available templates
const { data: templates } = await useFetch('/api/templates', {
  baseURL: useRuntimeConfig().public.apiUrl
})

// Create a new project
await fetch('/api/create-project', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    templateName: 'ecommerce',
    projectName: 'my-project',
    parameters: {
      /* ... */
    }
  })
})
```

For more information on Nuxt, see the [official documentation](https://nuxt.com/docs/getting-started/deployment).
