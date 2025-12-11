import type { Field } from './fields'

export interface Platform {
  platformType: string
  icon: string
  config: Record<string, Field>
}

export interface ConfiguredPlatform {
  platformType: string
  name: string
  config: Record<string, unknown>
}
