import type { Platform, Resource } from '~/types'
// import type { Preset } from './presets'

export interface ProjectOptions {
  name: string
  description?: string
  resources: Resource[]
  platform: Platform
}
