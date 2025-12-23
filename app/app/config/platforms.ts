import type { Platform } from '~/types'

/**
 * Available hosting platforms for repositories
 */
export const PLATFORMS = {
  github: {
    type: 'GitHub',
    icon: 'devicon:github',
    config: {
      Description: {
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
    type: 'GitLab',
    icon: 'devicon:gitlab',
    config: {
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
