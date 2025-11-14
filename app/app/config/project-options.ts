import type { ConfigOption } from '~/types'

export interface Resource {
  name: string
  icon: string
  config: Record<string, ConfigOption>
}

export interface Platform {
  name: string
  icon: string
  config: Record<string, ConfigOption>
}

/**
 * Available resources for project creation
 */
export const RESOURCES = {
  swa: {
    name: 'Static Web App',
    icon: 'devicon:azure',
    config: {
      name: {
        type: 'text',
        label: 'Project Name',
        description: 'The name of the Static Web App project.'
      },
      framework: {
        type: 'enum',
        label: 'Framework',
        description: 'The framework used for the Static Web App.',
        values: ['vanilla', 'react', 'vue', 'angular', 'svelte', 'nextjs', 'nuxtjs'],
        default: 'vanilla'
      },
      description: {
        type: 'textarea',
        label: 'Description',
        description: 'A brief description of the project.',
        default: 'A blank Static Web App project'
      }
    }
  },
  'azure-app-service': {
    name: 'Azure App Service',
    icon: 'devicon:azure',
    config: {
      description: {
        type: 'textarea',
        label: 'Description',
        description: 'A brief description of the project.',
        default: 'A blank Azure App Service project'
      }
    }
  },
  'azure-cosmosdb': {
    name: 'Azure Cosmos DB',
    icon: 'devicon:azure',
    config: {
      description: {
        type: 'textarea',
        label: 'Description',
        description: 'A brief description of the project.',
        default: 'A blank Azure Cosmos DB project'
      }
    }
  },
  'aws-lambda': {
    name: 'AWS Lambda',
    icon: 'devicon:amazonwebservices',
    config: {
      description: {
        type: 'textarea',
        label: 'Description',
        description: 'A brief description of the project.',
        default: 'A blank AWS Lambda project'
      }
    }
  },
} as const satisfies Record<string, Resource>

/**
 * Available hosting platforms for repositories
 */
export const PLATFORMS = {
  github: {
    name: 'GitHub',
    icon: 'devicon:github',
    config: {
      name: {
        type: 'text',
        label: 'Repository Name',
        description: 'The name of the GitHub repository to create.'
      },
      description: {
        type: 'textarea',
        label: 'Repository Description',
        description: 'A brief description of the GitHub repository.',
        default: ''
      },
      visibility: {
        type: 'enum',
        label: 'Repository Visibility',
        description: 'Whether the repository should be public or private.',
        values: ['public', 'private'],
        default: 'private'
      },
      owner: {
        type: 'text',
        label: 'Repository Owner',
        description:
          'The owner (user or organization) of the GitHub repository.'
      },
      template_owner: {
        type: 'text',
        label: 'Template Owner',
        description:
          'The owner (user or organization) of the template repository.'
      },
      template_repo: {
        type: 'text',
        label: 'Template Repository',
        description: 'The name of the template repository.'
      }
    }
  },
  gitlab: {
    name: 'GitLab',
    icon: 'devicon:gitlab',
    config: {
      description: {
        type: 'textarea',
        label: 'Description',
        description: 'A brief description of the Gitlab repository.',
        default: ''
      }
    }
  }
} as const satisfies Record<string, Platform>

export interface Preset {
  name: string
  description: string
  resources: (keyof typeof RESOURCES)[]
}

/**
 * Available project presets
 */
export const PRESETS = {
  'my-awesome-template': {
    name: 'My Awesome Template',
    description: 'An awesome template for your next project.',
    resources: ['swa', 'swa', 'azure-app-service']
  },
  'swa-starter': {
    name: 'Static Web App Starter',
    description: 'A starter template for Static Web Apps.',
    resources: ['swa']
  },
  'azure-app-service-starter': {
    name: 'Azure App Service Starter',
    description: 'A starter template for Azure App Service.',
    resources: ['azure-app-service']
  },
  'aws-lambda-starter': {
    name: 'AWS Lambda Starter',
    description: 'A starter template for AWS Lambda.',
    resources: ['aws-lambda']
  },
  'swa-app-service-combo': {
    name: 'SWA + App Service Combo',
    description: 'A combo template for Static Web Apps and Azure App Service.',
    resources: ['swa', 'azure-app-service']
  },
  'mathieu-s-super-template': {
    name: "Mathieu Chaillon's Super Template",
    description: 'The ultimate template for your next project.',
    resources: ['swa', 'swa', 'swa', 'swa', 'swa', 'swa', 'azure-cosmosdb', 'azure-app-service', 'azure-app-service']
  }
} as const satisfies Record<string, Preset>

// Export types for type-safe keys
export type PresetKey = keyof typeof PRESETS
export type ResourceKey = keyof typeof RESOURCES
export type PlatformKey = keyof typeof PLATFORMS

export interface ResourceOption {
  key: ResourceKey
  id: string
}

export interface ProjectData {
  name: string
  description?: string
  preset: PresetKey
  resources: ResourceOption[]
  platform: PlatformKey
}
