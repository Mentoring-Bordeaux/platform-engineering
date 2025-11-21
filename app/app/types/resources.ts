import type { Field } from './fields'

export interface Resource {
  name: string
  icon: string
  config: Record<string, Field>
}

export interface ConfiguredResource {
  name: string
  config: Record<string, unknown>
}
