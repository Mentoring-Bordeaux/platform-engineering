import type { Field } from './fields'

/**
 * Template resource from API
 */
export interface TemplateResource {
  /** Type of resource being provisioned
   * @example "azure.swa"
   */
  type: string
  /** Name of the resource instance
   * @example "my-ecommerce-app"
   */
  name: string
  /** Pre-configured properties (golden paths) - user doesn't see these */
  properties: Record<string, unknown>
  /** User-configurable parameters */
  parameters: Record<string, Field>
}

/**
 * Template received from API
 */
export interface Template {
  /** Template name */
  name: string
  /** Template description */
  description: string
  /** Template version */
  version: string
  /** Template kind */
  kind: string
  /** Resources included in this template */
  resources: TemplateResource[]
}
