import type { Field } from './fields'

export interface Resource {
  resourceType: string
  icon: string
  config: Record<string, Field>
}

export interface ConfiguredResource {
  resourceType: string
  name: string
  config: Record<string, unknown>
}
