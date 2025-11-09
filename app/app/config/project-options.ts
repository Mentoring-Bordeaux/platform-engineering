import type { ConfigOption } from '~/types'

export interface Framework {
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
 * Available frameworks for project creation
 * Users can select multiple frameworks
 */
export const FRAMEWORKS = {
  html5: {
    name: 'Vanilla',
    icon: 'devicon:html5',
    config: {
      description: {
        type: 'textarea',
        label: 'Description',
        description: 'A brief description of the project.',
        default: 'A vanilla HTML5 project'
      }
    }
  },
  vue: {
    name: 'Vue.js',
    icon: 'devicon:vuejs',
    config: {
      description: {
        type: 'textarea',
        label: 'Description',
        description: 'A brief description of the project.',
        default: 'A Vue.js project'
      }
    }
  },
  react: {
    name: 'React',
    icon: 'devicon:react',
    config: {
      description: {
        type: 'textarea',
        label: 'Description',
        description: 'A brief description of the project.',
        default: 'A React project'
      }
    }
  },
  nuxt: {
    name: 'Nuxt.js',
    icon: 'devicon:nuxtjs',
    config: {
      description: {
        type: 'textarea',
        label: 'Description',
        description: 'A brief description of the project.',
        default: 'A Nuxt.js project'
      }
    }
  },
  dotnet: {
    name: 'Dotnet',
    icon: 'devicon:dot-net',
    config: {
      description: {
        type: 'textarea',
        label: 'Description',
        description: 'A brief description of the project.',
        default: 'A Dotnet project'
      }
    }
  },
  nestjs: {
    name: 'NestJS',
    icon: 'devicon:nestjs',
    config: {
      description: {
        type: 'textarea',
        label: 'Description',
        description: 'A brief description of the project.',
        default: 'A NestJS project'
      }
    }
  }
} as const satisfies Record<string, Framework>

/**
 * Available hosting platforms for repositories
 * Users must select exactly one platform
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
        description: 'A brief description of the project.',
        default: 'A vanilla HTML5 project'
      }
    }
  }
} as const satisfies Record<string, Platform>

export interface Preset {
  name: string
  description: string
  frameworks: (keyof typeof FRAMEWORKS)[]
}

/**
 * Available project presets
 * These are pre-configured templates with specific tech stacks
 */
export const PRESETS = {
  'nuxt-dotnet': {
    name: 'Nuxt + Dotnet',
    description: 'A starter template with Nuxt.js frontend and Dotnet backend.',
    frameworks: ['nuxt', 'dotnet']
  },
  'react-dotnet': {
    name: 'React + Dotnet',
    description: 'A starter template with React frontend and Dotnet backend.',
    frameworks: ['react', 'dotnet']
  },
  'my-awesome-template': {
    name: 'My Awesome Template',
    description: 'An awesome template for your my project.',
    frameworks: []
  },
  'react-nestjs': {
    name: 'React + NestJS',
    description: 'A starter template with React frontend and NestJS backend.',
    frameworks: ['react', 'nestjs']
  }
} as const satisfies Record<string, Preset>

// Export types for type-safe keys
export type PresetKey = keyof typeof PRESETS
export type FrameworkKey = keyof typeof FRAMEWORKS
export type PlatformKey = keyof typeof PLATFORMS

export interface ProjectData {
  name: string
  description?: string
  preset: string
  frameworks: FrameworkKey[]
  platform: PlatformKey
}
