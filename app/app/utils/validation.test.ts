import { describe, it, expect } from 'vitest'
import {
  generateFieldSchema,
  generatePlatformConfigSchema,
  generateProjectConfigurationSchema
} from './validation'
import type { ProjectOptions } from '~/config/project-options'
import type { Platform } from '~/types'
import { normalizeTemplate } from '~/types/templates'
import type { RawTemplate } from '~/types/templates'

describe('Validation Schema', () => {
  describe('generateFieldSchema', () => {
    it('should generate required text field schema', () => {
      const schema = generateFieldSchema({
        type: 'text',
        required: true,
        label: 'Test Field',
        description: 'Test description'
      })

      expect(() => schema.parse('')).toThrow()
      expect(() => schema.parse('valid text')).not.toThrow()
    })

    it('should generate optional text field schema', () => {
      const schema = generateFieldSchema({
        type: 'text',
        required: false,
        label: 'Test Field',
        description: 'Test description'
      })

      expect(() => schema.parse('')).not.toThrow()
      expect(() => schema.parse(undefined)).not.toThrow()
    })

    it('should generate number field schema', () => {
      const schema = generateFieldSchema({
        type: 'number',
        required: true,
        label: 'Test Number',
        description: 'Test description',
        min: 1,
        max: 100
      })

      expect(() => schema.parse(0)).toThrow()
      expect(() => schema.parse(50)).not.toThrow()
      expect(() => schema.parse(101)).toThrow()
    })

    it('should generate enum field schema', () => {
      const schema = generateFieldSchema({
        type: 'enum',
        required: true,
        label: 'Test Enum',
        description: 'Test description',
        values: ['option1', 'option2', 'option3']
      })

      expect(() => schema.parse('option1')).not.toThrow()
      expect(() => schema.parse('invalid')).toThrow()
    })

    it('should generate boolean field schema', () => {
      const schema = generateFieldSchema({
        type: 'boolean',
        required: true,
        label: 'Test Boolean',
        description: 'Test description'
      })

      expect(() => schema.parse(true)).not.toThrow()
      expect(() => schema.parse(false)).not.toThrow()
      expect(() => schema.parse('true')).toThrow()
    })
  })

  describe('Platform Configuration Schema', () => {
    it('should generate valid schema for any platform', () => {
      // Test that schema generation works for any platform
      const platform: Platform = {
        type: 'test-platform',
        icon: 'test-icon',
        config: {
          requiredField: {
            type: 'text' as const,
            required: true,
            label: 'Required Field',
            description: 'A required field'
          },
          optionalField: {
            type: 'text' as const,
            required: false,
            label: 'Optional Field',
            description: 'An optional field'
          }
        }
      }
      const schema = generatePlatformConfigSchema(platform)

      const validConfig = {
        name: platform.type,
        config: {
          requiredField: 'valid value',
          optionalField: 'optional value'
        }
      }
      expect(() => schema.parse(validConfig)).not.toThrow()

      const invalidConfig = {
        name: platform.type,
        config: {
          requiredField: '', // Empty required field
          optionalField: 'optional value'
        }
      }
      expect(() => schema.parse(invalidConfig)).toThrow()
    })
  })

  describe('Project Configuration Schema - State Shape Match', () => {
    it('should match state shape for any project configuration', () => {
      // Create a generic project with mock template and platform
      const rawTemplate: RawTemplate = {
        name: 'test-template',
        displayName: 'Test Template',
        description: 'A test template',
        parameters: {
          field1: {
            type: 'text',
            label: 'Field 1',
            required: true,
            description: 'Description 1',
            default: 'default value'
          },
          field2: {
            type: 'enum',
            label: 'Field 2',
            required: true,
            description: 'Description 2',
            values: ['option1', 'option2'],
            default: 'option1'
          }
        }
      }

      const mockPlatform: Platform = {
        type: 'test-platform',
        icon: 'platform-icon',
        config: {
          platformField: {
            type: 'text' as const,
            required: true,
            label: 'Platform Field',
            description: 'Platform Description'
          }
        }
      }

      const projectData: ProjectOptions = {
        name: 'Test Project',
        template: normalizeTemplate(rawTemplate),
        platform: mockPlatform
      }

      const schema = generateProjectConfigurationSchema(projectData)

      // Valid state matching the schema structure
      const validState = {
        parameters: {
          field1: 'valid value',
          field2: 'option1'
        },
        platform: {
          name: 'test-platform',
          config: {
            platformField: 'valid platform value'
          }
        }
      }

      const result = schema.safeParse(validState)
      expect(result.success).toBe(true)
    })

    it('should reject invalid configurations', () => {
      const rawTemplate: RawTemplate = {
        name: 'test-template',
        description: 'Test',
        parameters: {
          requiredField: {
            type: 'text',
            required: true,
            label: 'Required Field',
            description: 'A required field'
          }
        }
      }

      const mockPlatform: Platform = {
        type: 'test-platform',
        icon: 'platform-icon',
        config: {
          requiredPlatformField: {
            type: 'text' as const,
            required: true,
            label: 'Required Platform Field',
            description: 'A required platform field'
          }
        }
      }

      const projectData: ProjectOptions = {
        name: 'Test Project',
        template: normalizeTemplate(rawTemplate),
        platform: mockPlatform
      }

      const schema = generateProjectConfigurationSchema(projectData)

      // Invalid state - empty required field in template
      const invalidResourceState = {
        parameters: {
          requiredField: ''
        },
        platform: {
          name: 'test-platform',
          config: {
            requiredPlatformField: 'valid value'
          }
        }
      }

      const templateResult = schema.safeParse(invalidResourceState)
      expect(templateResult.success).toBe(false)

      // Invalid state - empty required field in platform
      const invalidPlatformState = {
        parameters: {
          requiredField: 'valid value'
        },
        platform: {
          name: 'test-platform',
          config: {
            requiredPlatformField: ''
          }
        }
      }

      const platformResult = schema.safeParse(invalidPlatformState)
      expect(platformResult.success).toBe(false)
    })
  })

  describe('Type Compatibility', () => {
    it('should reject state with invalid data at runtime', () => {
      // Create a template with validation constraints
      const rawTemplate: RawTemplate = {
        name: 'test-template',
        description: 'Test',
        parameters: {
          field1: {
            type: 'text',
            required: true,
            label: 'Field 1',
            description: 'Description'
          }
        }
      }

      const mockPlatform: Platform = {
        type: 'test-platform',
        icon: 'platform-icon',
        config: {
          platformField: {
            type: 'text' as const,
            required: true,
            label: 'Platform Field',
            description: 'Platform Description'
          }
        }
      }

      const projectData: ProjectOptions = {
        name: 'Test Project',
        template: normalizeTemplate(rawTemplate),
        platform: mockPlatform
      }

      const schema = generateProjectConfigurationSchema(projectData)

      // Invalid state - empty required field in template
      const invalidState = {
        parameters: {
          field1: ''
        },
        platform: {
          name: 'test-platform',
          config: {
            platformField: ''
          }
        }
      }

      const result = schema.safeParse(invalidState)
      expect(result.success).toBe(false)
    })
  })
})
