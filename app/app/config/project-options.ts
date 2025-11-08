import type { Framework, Platform, Preset } from '~/types'

/**
 * Available project presets
 * These are pre-configured templates with specific tech stacks
 */
export const PRESETS = {
  'nuxt-dotnet': {
    name: 'Nuxt + Dotnet',
    description: 'A starter template with Nuxt.js frontend and Dotnet backend.'
  },
  'react-dotnet': {
    name: 'React + Dotnet',
    description: 'A starter template with React frontend and Dotnet backend.'
  },
  'my-awesome-template': {
    name: 'My Awesome Template',
    description: 'An awesome template for your my project.'
  },
  'react-nestjs': {
    name: 'React + NestJS',
    description: 'A starter template with React frontend and NestJS backend.'
  }
} as const satisfies Record<string, Preset>

/**
 * Available frameworks for project creation
 * Users can select multiple frameworks
 */
export const FRAMEWORKS = {
  html5: {
    name: 'Vanilla',
    icon: 'devicon:html5'
  },
  vue: {
    name: 'Vue.js',
    icon: 'devicon:vuejs'
  },
  react: {
    name: 'React',
    icon: 'devicon:react'
  }
} as const satisfies Record<string, Framework>

/**
 * Available hosting platforms for repositories
 * Users must select exactly one platform
 */
export const PLATFORMS = {
  github: {
    name: 'GitHub',
    icon: 'devicon:github'
  },
  gitlab: {
    name: 'GitLab',
    icon: 'devicon:gitlab'
  }
} as const satisfies Record<string, Platform>

// Export types for type-safe keys
export type PresetKey = keyof typeof PRESETS
export type FrameworkKey = keyof typeof FRAMEWORKS
export type PlatformKey = keyof typeof PLATFORMS
