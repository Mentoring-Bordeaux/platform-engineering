import type { Field } from '~/types'

// Return an initial value based on field type
// '' for text-based fields, null otherwise
const initializeFieldValue = (type: string | undefined) => {
  switch (type) {
    case 'text':
    case 'password':
    case 'textarea':
      return ''
    default:
      return null
  }
}

export const generateDefaultConfig = (config: Record<string, Field>) => {
  return Object.keys(config).reduce((acc, key) => {
    const field = config[key] as Field
    const val = field.default ?? initializeFieldValue(field.type)
    return { ...acc, [key]: val }
  }, {})
}
