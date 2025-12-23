import type { Resource } from '~/types'

/**
 * Available resources for project creation
 */
export const RESOURCES = {
  swa: {
    type: 'Static Web App',
    icon: 'devicon:azure',
    config: {
      framework: {
        type: 'enum',
        required: true,
        label: 'Framework',
        description: 'The framework used for the Static Web App.',
        values: [
          'vanilla',
          'react',
          'vue',
          'angular',
          'svelte',
          'nextjs',
          'nuxtjs'
        ],
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
    type: 'Azure App Service',
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
    type: 'Azure Cosmos DB',
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
    type: 'AWS Lambda',
    icon: 'devicon:amazonwebservices',
    config: {
      description: {
        type: 'textarea',
        label: 'Description',
        description: 'A brief description of the project.',
        default: 'A blank AWS Lambda project'
      }
    }
  }
} as const satisfies Record<string, Resource>

export type ResourceKey = keyof typeof RESOURCES
