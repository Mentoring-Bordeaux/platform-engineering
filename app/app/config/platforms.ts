import type { Platform } from '~/types'

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
        required: true,
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
        required: true,
        label: 'Repository Visibility',
        description: 'Whether the repository should be public or private.',
        values: ['public', 'private'],
        default: 'private'
      }
    }
  },
  gitlab: {
    name: 'GitLab',
    icon: 'devicon:gitlab',
    config: {
      name: {
        type: 'text',
        required: true,
        label: 'Repository Name',
        description: 'The name of the GitLab repository to create.'
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
