import type { Field, RawField } from './fields'

/**
 * Template resource
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
}

/**
 * Raw template parameters as received from API (all values are strings or nested)
 */
export interface RawTemplateParameters {
  [key: string]: RawField | RawTemplateParameters
}

/**
 * Normalized template parameters (with proper types)
 */
export interface TemplateParameters {
  [key: string]: Field | TemplateParameters
}

/**
 * Base template interface (shared between Raw and Normalized)
 */
interface TemplateBase {
  name: string
  displayName?: string
  description?: string
  version?: string
  kind?: string
  resources?: TemplateResource[]
}

/**
 * Raw template as received from API
 */
export interface RawTemplate extends TemplateBase {
  parameters: RawTemplateParameters
}

/**
 * Template received from API and normalized
 */
export interface Template extends TemplateBase {
  parameters: TemplateParameters
}

/**
 * Normalize a raw template from the API to a properly typed template
 * Converts all string defaults to their proper types based on field type
 */
export function normalizeTemplate(rawTemplate: RawTemplate): Template {
  return {
    ...rawTemplate,
    parameters: normalizeTemplateParameters(rawTemplate.parameters)
  }
}

/**
 * Recursively normalize template parameters, converting string defaults to proper types
 */
function normalizeTemplateParameters(
  rawParams: RawTemplateParameters
): TemplateParameters {
  const normalized: TemplateParameters = {}

  for (const [key, value] of Object.entries(rawParams)) {
    if (isFieldValue(value)) {
      normalized[key] = normalizeField(value)
    } else {
      // It's a nested group - recurse
      normalized[key] = normalizeTemplateParameters(value)
    }
  }

  return normalized
}

/**
 * Normalize a single raw field to a properly typed field
 * Converts string defaults to boolean or number based on field type
 */
function normalizeField(rawField: RawField): Field {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const field = { ...rawField } as any

  // Convert default value based on field type
  if (field.type === 'boolean' && typeof field.default === 'string') {
    field.default = field.default.toLowerCase() === 'true'
  } else if (field.type === 'number' && typeof field.default === 'string') {
    field.default = parseFloat(field.default)
  }

  return field as Field
}

/**
 * Type guard to check if value is a field (raw or normalized)
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
function isFieldValue(value: any): value is RawField | Field {
  return value && typeof value === 'object' && 'type' in value
}

/**
 * Helper function to flatten nested template parameters
 * Converts grouped parameters to flat structure for frontend display
 * Supports arbitrary nesting depth - all nested fields are flattened to root level
 */
export function flattenTemplateParameters(
  parameters: TemplateParameters
): Record<string, Field> {
  const flattened: Record<string, Field> = {}

  const flatten = (obj: TemplateParameters, prefix = ''): void => {
    for (const [key, value] of Object.entries(obj)) {
      const fullKey = prefix ? `${prefix}.${key}` : key

      if (isField(value)) {
        flattened[fullKey] = value
      } else {
        // It's a group - recurse into it
        flatten(value, fullKey)
      }
    }
  }

  flatten(parameters)
  return flattened
}

// Type guard to check if value is a Field
// eslint-disable-next-line @typescript-eslint/no-explicit-any
function isField(value: any): value is Field {
  return value && typeof value === 'object' && 'type' in value
}
