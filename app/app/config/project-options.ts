import type { Platform, Resource } from '~/types'
import type { Preset } from './presets'

export interface ProjectOptions {
  name: string
  description?: string
  preset: Preset
  resources: Resource[]
  platform: Platform
}
