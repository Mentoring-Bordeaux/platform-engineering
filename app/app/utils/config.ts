import type { Field } from '~/types'

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
