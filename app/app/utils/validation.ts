import { z } from 'zod'
import type { Field, Platform } from '~/types'
import type { ProjectOptions } from '~/config/project-options'
import { flattenTemplateParameters } from '~/types'

/**
 * Generate a Zod schema for a single field based on its type and constraints
 */
export function generateFieldSchema(field: Field): z.ZodTypeAny {
  switch (field.type) {
    case 'text':
    case 'password': {
      if (field.required) {
        return z.string().min(1, {
          message: `${field.label} is required`
        })
      }
      return z.string().optional()
    }

    case 'textarea': {
      if (field.required) {
        return z.string().min(1, {
          message: `${field.label} is required`
        })
      }
      return z.string().optional()
    }

    case 'number': {
      let schema: z.ZodNumber = z.number({
        message: `${field.label} must be a number`
      })

      if (field.min !== undefined) {
        schema = schema.min(field.min, {
          message: `${field.label} must be at least ${field.min}`
        })
      }
      if (field.max !== undefined) {
        schema = schema.max(field.max, {
          message: `${field.label} must be at most ${field.max}`
        })
      }

      if (!field.required) {
        return schema.nullish()
      }
      return schema
    }

    case 'enum': {
      if (!field.values || field.values.length === 0) {
        return z.string()
      }

      const schema = z.literal(field.values, {
        message: `${field.label} must be one of: ${field.values.join(', ')}`
      })

      if (!field.required) {
        return schema.nullish()
      }
      return schema
    }

    case 'boolean': {
      const schema = z.boolean({
        message: `${field.label} must be true or false`
      })

      if (!field.required) {
        return schema.nullish()
      }
      return schema
    }

    default:
      return z.unknown()
  }
}

/**
 * Generate a Zod schema for a platform configuration
 */
export function generatePlatformConfigSchema(platform: Platform) {
  const configShape: Record<string, z.ZodTypeAny> = {}

  Object.entries(platform.config).forEach(([key, field]) => {
    configShape[key] = generateFieldSchema(field)
  })

  return z.object({
    config: z.object(configShape)
  })
}

/**
 * Generate a complete validation schema for the project configuration form
 */
export function generateProjectConfigurationSchema(
  projectData: ProjectOptions
) {
  // Build schema for template parameters
  const parameterShape: Record<string, z.ZodTypeAny> = {}

  if (projectData.template) {
    const flattenedParams = flattenTemplateParameters(
      projectData.template.parameters
    )
    Object.entries(flattenedParams).forEach(([key, field]) => {
      parameterShape[key] = generateFieldSchema(field)
    })
  }

  return z.object({
    parameters: z.object(parameterShape),
    platform: generatePlatformConfigSchema(projectData.platform)
  })
}
