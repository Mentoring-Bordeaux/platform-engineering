export interface FieldBase {
  /** Label to display to the user */
  label: string
  /** Description to display to the user */
  description?: string
  /** Whether the field is required */
  required?: boolean
}

export interface TextField extends FieldBase {
  type: 'text' | 'password'
  default?: string
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

export type Field =
  | TextField
  | TextAreaField
  | NumberField
  | SelectField
  | CheckboxField

/**
 * Raw field types as received from API (all defaults are strings)
 * Text, textarea, and select fields are identical in raw and normalized forms.
 * Number and boolean fields differ only in their default value type.
 */

// Raw versions of fields where default is always string from YAML
export type RawTextField = TextField
export type RawTextAreaField = TextAreaField

export interface RawNumberField extends FieldBase {
  type: 'number'
  min?: number
  max?: number
  default?: string
}

export type RawSelectField = SelectField

export interface RawCheckboxField extends FieldBase {
  type: 'boolean'
  default?: string
}

export type RawField =
  | RawTextField
  | RawTextAreaField
  | RawNumberField
  | RawSelectField
  | RawCheckboxField
