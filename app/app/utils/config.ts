import type { Field } from '~/types'

const generateEmpryValue = (type: string | undefined) => {
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
    const val = field.default ?? generateEmpryValue(field.type)
    return { ...acc, [key]: val }
  }, {})
}
