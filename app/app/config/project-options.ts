import type { Platform, Template } from '~/types'

export interface ProjectOptions {
  name: string
  description?: string
  template: Template
  platform: Platform
}
