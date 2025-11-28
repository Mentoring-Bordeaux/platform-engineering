import type { RESOURCES } from './resources'

export interface Preset {
  name: string
  description: string
  resources: (keyof typeof RESOURCES)[]
}

const BLANK_PRESET = {
  name: 'Blank Template',
  description: 'Start from scratch with a blank template.',
  resources: []
} as const satisfies Preset

/**
 * Available project presets
 */
export const PRESETS = {
  blank: BLANK_PRESET,
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
    resources: [
      'swa',
      'swa',
      'swa',
      'swa',
      'swa',
      'swa',
      'azure-cosmosdb',
      'azure-app-service',
      'azure-app-service'
    ]
  }
} as const satisfies Record<string, Preset>

export type PresetKey = keyof typeof PRESETS
