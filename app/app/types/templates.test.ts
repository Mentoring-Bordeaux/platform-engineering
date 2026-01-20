import { describe, it, expect } from 'vitest'
import type { RawTemplate, Template, TemplateParameters } from './templates'
import { normalizeTemplate, flattenTemplateParameters } from './templates'

describe('Template Normalization', () => {
  describe('normalizeTemplate', () => {
    it('should convert string defaults to proper types', () => {
      const rawTemplate: RawTemplate = {
        name: 'test-template',
        description: 'A test template',
        parameters: {
          instanceCount: {
            type: 'number',
            label: 'Instance Count',
            required: true,
            default: '5'
          },
          enableCache: {
            type: 'boolean',
            label: 'Enable Cache',
            required: false,
            default: 'true'
          },
          appName: {
            type: 'text',
            label: 'App Name',
            required: true,
            default: 'MyApp'
          }
        }
      }

      const normalized = normalizeTemplate(rawTemplate)

      expect(normalized).toBeDefined()
      expect(normalized.parameters).toBeDefined()
      expect(normalized.parameters.instanceCount).toBeDefined()
      expect(normalized.parameters.enableCache).toBeDefined()
      expect(normalized.parameters.appName).toBeDefined()

      // Check number conversion

      expect(normalized.parameters.instanceCount?.default).toBe(5)

      expect(typeof normalized.parameters.instanceCount?.default).toBe('number')

      // Check boolean conversion

      expect(normalized.parameters.enableCache?.default).toBe(true)

      expect(typeof normalized.parameters.enableCache?.default).toBe('boolean')

      // Check string stays string

      expect(normalized.parameters.appName?.default).toBe('MyApp')

      expect(typeof normalized.parameters.appName?.default).toBe('string')
    })

    it('should handle nested parameter groups', () => {
      const rawTemplate: RawTemplate = {
        name: 'nested-template',
        description: 'Template with nested groups',
        parameters: {
          app: {
            name: {
              type: 'text',
              label: 'App Name',
              required: true,
              default: 'MyApp'
            },
            capacity: {
              type: 'number',
              label: 'Capacity',
              required: true,
              default: '100'
            }
          },
          admin: {
            description: {
              type: 'text',
              label: 'Admin Description',
              required: false,
              default: 'admin user'
            }
          }
        }
      }

      const normalized = normalizeTemplate(rawTemplate)

      // Check nested structure is preserved

      expect(normalized.parameters.app).toBeDefined()

      expect(normalized.parameters.admin).toBeDefined()

      // Check nested number conversion

      expect(normalized.parameters.app).toBeDefined()

      const appParams = normalized.parameters.app as TemplateParameters
      expect(appParams.capacity?.default).toBe(100)

      expect(typeof appParams.capacity?.default).toBe('number')

      // Check nested string stays string

      const adminParams = normalized.parameters.admin as TemplateParameters
      expect(adminParams.description?.default).toBe('admin user')
    })

    it('should handle false boolean values', () => {
      const rawTemplate: RawTemplate = {
        name: 'bool-test',
        description: 'Template to test boolean defaults',
        parameters: {
          debugMode: {
            type: 'boolean',
            label: 'Debug Mode',
            required: false,
            default: 'false'
          }
        }
      }

      const normalized = normalizeTemplate(rawTemplate)

      expect(normalized.parameters).toBeDefined()
      expect(normalized.parameters['debugMode']?.default).toBe(false)
    })
  })

  describe('flattenTemplateParameters', () => {
    it('should flatten nested parameters to dot notation', () => {
      const template: Template = {
        name: 'nested',
        description: 'Test',
        parameters: {
          app: {
            name: {
              type: 'text',
              label: 'App Name',
              required: true,
              default: 'MyApp'
            }
          },
          database: {
            host: {
              type: 'text',
              label: 'Host',
              required: true,
              default: 'localhost'
            },
            port: {
              type: 'number',
              label: 'Port',
              required: true,
              default: 5432
            }
          }
        }
      }

      const flattened = flattenTemplateParameters(template.parameters)

      expect(flattened['app.name']).toBeDefined()

      expect(flattened['app.name']?.default).toBe('MyApp')

      expect(flattened['database.host']).toBeDefined()

      expect(flattened['database.host']?.default).toBe('localhost')

      expect(flattened['database.port']).toBeDefined()

      expect(flattened['database.port']?.default).toBe(5432)
    })
  })
})
