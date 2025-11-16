import type { Field } from './fields'

export interface Platform {
  name: string
  icon: string
  config: Record<string, Field>
}

export interface ConfiguredPlatform {
  name: string
  config: Record<string, unknown>
}
