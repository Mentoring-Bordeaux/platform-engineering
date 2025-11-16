import { z } from 'zod'
import type { Field, Platform, Resource } from '~/types'
import type { ProjectOptions } from '~/config/project-options'

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
        return schema.optional().nullable()
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
        return schema.optional().nullable()
      }
      return schema
    }

    case 'boolean': {
      const schema = z.boolean({
        message: `${field.label} must be true or false`
      })

      if (!field.required) {
        return schema.optional().nullable()
      }
      return schema
    }

    default:
      return z.unknown()
  }
}

/**
 * Generate a Zod schema for a resource configuration
 */
export function generateResourceConfigSchema(resource: Resource) {
  const configShape: Record<string, z.ZodTypeAny> = {}

  Object.entries(resource.config).forEach(([key, field]) => {
    configShape[key] = generateFieldSchema(field)
  })

  return z.object({
    name: z.string(),
    config: z.object(configShape)
  })
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
    name: z.string(),
    config: z.object(configShape)
  })
}

/**
 * Generate a complete validation schema for the project configuration form
 */
export function generateProjectConfigurationSchema(
  projectData: ProjectOptions
) {
  return z.object({
    resources: z.array(z.any()).superRefine((resources, ctx) => {
      resources.forEach((res, index) => {
        const expectedSchema = generateResourceConfigSchema(
          projectData.resources[index] as Resource
        )
        const parseResult = expectedSchema.safeParse(res)
        if (!parseResult.success) {
          console.log('Validating resource:', parseResult)
          parseResult.error.issues.forEach(issue => {
            console.log('Parse result:', issue)
            ctx.addIssue({
              ...issue,
              path: ['resources', index, ...issue.path]
            })
          })
        }
      })
      console.log(ctx.issues)
    }),
    platform: generatePlatformConfigSchema(projectData.platform).superRefine(
      platformData => {
        console.log('Validating platform:', platformData)
      }
    )
  })
}
