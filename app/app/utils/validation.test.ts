import { describe, it, expect } from 'vitest'
import {
  generateFieldSchema,
  generateResourceConfigSchema,
  generatePlatformConfigSchema,
  generateProjectConfigurationSchema
} from './validation'
import type { ProjectOptions } from '~/config/project-options'
import type { Platform, Resource } from '~/types'

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

  describe('Resource Configuration Schema', () => {
    it('should generate valid schema for any resource', () => {
      // Test that schema generation works for any resource
      const resource: Resource = {
        type: 'Test Resource',
        icon: 'test-icon',
        config: {
          testField: {
            type: 'text' as const,
            required: true,
            label: 'Test Field',
            description: 'A test field'
          }
        }
      }
      const schema = generateResourceConfigSchema(resource)

      const validConfig = {
        name: 'Config Name',
        config: {
          testField: 'valid value'
        }
      }
      expect(() => schema.parse(validConfig)).not.toThrow()

      const invalidConfig = {
        name: 'Config Name',
        config: {
          testField: '' // Empty required field
        }
      }
      expect(() => schema.parse(invalidConfig)).toThrow()
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
      // Create a generic project with mock resources and platform
      const mockResource1 = {
        type: 'resource-1',
        name: 'Resource 1',
        icon: 'icon1',
        config: {
          field1: {
            type: 'text' as const,
            required: true,
            label: 'Field 1',
            description: 'Description 1'
          }
        }
      }

      const mockResource2 = {
        type: 'resource-2',
        name: 'Resource 2',
        icon: 'icon2',
        config: {
          field2: {
            type: 'enum' as const,
            required: true,
            label: 'Field 2',
            description: 'Description 2',
            values: ['option1', 'option2']
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
        resources: [mockResource1, mockResource2],
        platform: mockPlatform
      }

      const schema = generateProjectConfigurationSchema(projectData)

      // Valid state matching the schema structure
      const validState = {
        resources: [
          {
            name: 'Resource 1',
            config: {
              field1: 'valid value'
            }
          },
          {
            name: 'Resource 2',
            config: {
              field2: 'option1'
            }
          }
        ],
        platform: {
          name: 'Test Platform',
          config: {
            platformField: 'valid platform value'
          }
        }
      }

      const result = schema.safeParse(validState)
      expect(result.success).toBe(true)
    })

    it('should reject invalid configurations', () => {
      const mockResource: Resource = {
        type: 'test-resource',
        icon: 'icon',
        config: {
          requiredField: {
            type: 'text' as const,
            required: true,
            label: 'Required Field',
            description: 'A required field'
          }
        }
      }

      const mockPlatform: Platform = {
        type: 'test-platform',
        icon: 'icon',
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
        resources: [mockResource],
        platform: mockPlatform
      }

      const schema = generateProjectConfigurationSchema(projectData)

      // Invalid state - empty required field in resource
      const invalidResourceState = {
        resources: [
          {
            name: 'Resource',
            config: {
              requiredField: '' // Empty required field
            }
          }
        ],
        platform: {
          name: 'Platform',
          config: {
            requiredPlatformField: 'valid value'
          }
        }
      }

      const resourceResult = schema.safeParse(invalidResourceState)
      expect(resourceResult.success).toBe(false)
      if (!resourceResult.success) {
        expect(resourceResult.error.issues.length).toBeGreaterThan(0)
        expect(resourceResult.error.issues[0]?.path).toContain('resources')
      }

      // Invalid state - empty required field in platform
      const invalidPlatformState = {
        resources: [
          {
            name: 'Resource',
            config: {
              requiredField: 'valid value'
            }
          }
        ],
        platform: {
          name: 'Platform',
          config: {
            requiredPlatformField: '' // Empty required field
          }
        }
      }

      const platformResult = schema.safeParse(invalidPlatformState)
      expect(platformResult.success).toBe(false)
      if (!platformResult.success) {
        expect(platformResult.error.issues.length).toBeGreaterThan(0)
        expect(platformResult.error.issues[0]?.path).toContain('platform')
      }
    })

    it('should provide correct error paths for field validation', () => {
      const mockResource: Platform = {
        type: 'resource',
        icon: 'icon',
        config: {
          testField: {
            type: 'text' as const,
            required: true,
            label: 'Test Field',
            description: 'Test'
          }
        }
      }

      const mockPlatform: Platform = {
        type: 'platform',
        icon: 'icon',
        config: {
          platformField: {
            type: 'text' as const,
            required: true,
            label: 'Platform Field',
            description: 'Test'
          }
        }
      }

      const projectData: ProjectOptions = {
        name: 'Test Project',
        resources: [mockResource],
        platform: mockPlatform
      }

      const schema = generateProjectConfigurationSchema(projectData)

      const invalidState = {
        resources: [
          {
            name: 'Resource',
            config: {
              testField: '' // Empty required field
            }
          }
        ],
        platform: {
          name: 'Platform',
          config: {
            platformField: '' // Empty required field
          }
        }
      }

      const result = schema.safeParse(invalidState)
      expect(result.success).toBe(false)

      if (!result.success) {
        const paths = result.error.issues.map(issue => issue.path.join('.'))

        // Verify error paths match UFormField naming convention
        expect(paths.some(p => p.startsWith('resources.'))).toBe(true)
        expect(paths.some(p => p.startsWith('platform.'))).toBe(true)
      }
    })
  })

  describe('Type Compatibility', () => {
    it('should accept array state for tuple schema at runtime', () => {
      // Create mock resources with different config shapes
      const mockResource1: Resource = {
        type: 'resource-1',
        icon: 'icon1',
        config: {
          field1: {
            type: 'text' as const,
            required: true,
            label: 'Field 1',
            description: 'Description'
          }
        }
      }

      const mockResource2: Resource = {
        type: 'resource-2',
        icon: 'icon2',
        config: {
          field1: {
            type: 'number' as const,
            required: true,
            label: 'Field 2',
            description: 'Description',
            min: 1,
            max: 100
          },
          field2: {
            type: 'enum' as const,
            required: false,
            label: 'Field 3',
            description: 'Description',
            values: ['option1', 'option2']
          }
        }
      }

      const mockPlatform: Platform = {
        type: 'platform',
        icon: 'icon',
        config: {
          platformField: {
            type: 'text' as const,
            required: true,
            label: 'Platform Field',
            description: 'Description'
          }
        }
      }

      const projectData: ProjectOptions = {
        name: 'Test Project',
        resources: [mockResource1, mockResource2],
        platform: mockPlatform
      }

      const schema = generateProjectConfigurationSchema(projectData)

      // State is defined as an array, schema uses tuple
      // This should work at runtime
      const arrayState = {
        resources: [
          {
            name: 'Resource 1',
            config: {
              field1: 'valid text'
            }
          },
          {
            name: 'Resource 2',
            config: {
              field1: 50,
              field2: 'option1'
            }
          }
        ],
        platform: {
          name: 'Platform',
          config: {
            platformField: 'valid platform value'
          }
        }
      }

      // Verify runtime compatibility
      const result = schema.safeParse(arrayState)
      expect(result.success).toBe(true)

      // Verify that validated data maintains array structure
      if (result.success) {
        expect(Array.isArray(result.data.resources)).toBe(true)
        expect(result.data.resources.length).toBe(2)
      }
    })

    it('should reject array state with invalid data at runtime', () => {
      // Create mock resources with validation constraints
      const mockResource1: Resource = {
        type: 'resource-1',
        icon: 'icon1',
        config: {
          field1: {
            type: 'text' as const,
            required: true,
            label: 'Field 1',
            description: 'Description'
          }
        }
      }

      const mockResource2: Resource = {
        type: 'resource-2',
        icon: 'icon2',
        config: {
          field1: {
            type: 'number' as const,
            required: true,
            label: 'Field 2',
            description: 'Description',
            min: 10,
            max: 100
          },
          field2: {
            type: 'enum' as const,
            required: true,
            label: 'Field 3',
            description: 'Description',
            values: ['option1', 'option2']
          }
        }
      }

      const mockPlatform: Platform = {
        type: 'platform',
        icon: 'icon',
        config: {
          platformField: {
            type: 'text' as const,
            required: true,
            label: 'Platform Field',
            description: 'Description'
          }
        }
      }

      const projectData: ProjectOptions = {
        name: 'Test Project',
        resources: [mockResource1, mockResource2],
        platform: mockPlatform
      }

      const schema = generateProjectConfigurationSchema(projectData)

      // Test 1: Empty required field in first resource
      const invalidState1 = {
        resources: [
          {
            name: 'Resource 1',
            config: {
              field1: '' // Empty required field
            }
          },
          {
            name: 'Resource 2',
            config: {
              field1: 50,
              field2: 'option1'
            }
          }
        ],
        platform: {
          name: 'Platform',
          config: {
            platformField: 'valid value'
          }
        }
      }

      const result1 = schema.safeParse(invalidState1)
      expect(result1.success).toBe(false)
      if (!result1.success) {
        expect(result1.error.issues[0]?.path).toContain('resources')
      }

      // Test 2: Number out of range in second resource
      const invalidState2 = {
        resources: [
          {
            name: 'Resource 1',
            config: {
              field1: 'valid text'
            }
          },
          {
            name: 'Resource 2',
            config: {
              field1: 5, // Below min (10)
              field2: 'option1'
            }
          }
        ],
        platform: {
          name: 'Platform',
          config: {
            platformField: 'valid value'
          }
        }
      }

      const result2 = schema.safeParse(invalidState2)
      expect(result2.success).toBe(false)
      if (!result2.success) {
        const paths = result2.error.issues.map(issue => issue.path.join('.'))
        expect(paths.some(p => p.includes('resources.1'))).toBe(true)
      }

      // Test 3: Invalid enum value
      const invalidState3 = {
        resources: [
          {
            name: 'Resource 1',
            config: {
              field1: 'valid text'
            }
          },
          {
            name: 'Resource 2',
            config: {
              field1: 50,
              field2: 'invalid-option' // Not in enum values
            }
          }
        ],
        platform: {
          name: 'Platform',
          config: {
            platformField: 'valid value'
          }
        }
      }

      const result3 = schema.safeParse(invalidState3)
      expect(result3.success).toBe(false)
      if (!result3.success) {
        const paths = result3.error.issues.map(issue => issue.path.join('.'))
        expect(paths.some(p => p.includes('field2'))).toBe(true)
      }

      // Test 4: Wrong number of resources (tuple mismatch)
      const invalidState4 = {
        resources: [
          {
            name: 'Resource 1',
            config: {
              field1: 'valid text'
            }
          }
          // Missing second resource
        ],
        platform: {
          name: 'Platform',
          config: {
            platformField: 'valid value'
          }
        }
      }

      const result4 = schema.safeParse(invalidState4)
      expect(result4.success).toBe(false)
    })
  })
})
