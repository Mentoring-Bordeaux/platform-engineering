import type { Platform, Resource, Template } from '~/types'

export interface ProjectOptions {
  name: string
  description?: string
  template: Template
  resources: Resource[]
  platform: Platform
}
