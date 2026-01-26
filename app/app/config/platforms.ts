import type { Platform } from '~/types'

/**
 * Available hosting platforms for repositories
 */
export const PLATFORMS = {
  github: {
    type: 'github',
    icon: 'devicon:github',
    config: {
      name: {
        type: 'text',
        label: 'Repository Name',
        description: 'The name of the GitHub repository to create.',
        required: true
      },
      description: {
        type: 'textarea',
        label: 'Repository Description',
        description: 'A brief description of the GitHub repository.',
        default: ''
      },
      isPrivate: {
        type: 'boolean',
        label: 'Private Repository',
        description: 'Whether the repository should be private or public.',
        default: true
      }
    }
  },
  gitlab: {
    type: 'gitlab',
    icon: 'devicon:gitlab',
    config: {
      name: {
        type: 'text',
        label: 'Repository Name',
        description: 'The name of the Gitlab repository to create.',
        required: true
      },
      description: {
        type: 'textarea',
        label: 'Description',
        description: 'A brief description of the GitLab repository.',
        default: ''
      }
    }
  }
} as const satisfies Record<string, Platform>

export type PlatformKey = keyof typeof PLATFORMS
