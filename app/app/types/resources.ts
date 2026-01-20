import type { Field } from './fields'

export interface Resource {
  /** Type of resource being configured.
   * @example "static-web-app"
   */
  type: string
  /** Human-friendly name for this resource instance.
   * @example "My Static Web App"
   */
  name: string
  /**
   * User-configurable parameters for this resource type.
   */
  parameters: Record<string, Field>
}

export interface ConfiguredResource {
  /** Type of resource being configured.
   * @example "static-web-app"
   */
  type: string
  /** Human-friendly name for this resource instance.
   * @example "My Static Web App"
   */
  name: string
  /** User-provided configuration values for this resource instance.
   * This combines properties (golden paths) and parameter responses.
   */
  config: Record<string, unknown>
}
