export interface FieldBase {
  /** Label to display to the user */
  label: string
  /** Description to display to the user */
  description: string
}

export interface TextField extends FieldBase {
  type: 'text' | 'password'
  default?: string // Optional default value (if not provided, field is considered required)
}

export interface TextAreaField extends FieldBase {
  type: 'textarea'
  default?: string
}

export interface NumberField extends FieldBase {
  type: 'number'
  min?: number
  max?: number
  default?: number
}

export interface SelectField extends FieldBase {
  type: 'enum'
  values: string[]
  default?: string
}

export interface CheckboxField extends FieldBase {
  type: 'boolean'
  default?: boolean
}

export type ConfigOption =
  | TextField
  | TextAreaField
  | NumberField
  | SelectField
  | CheckboxField
