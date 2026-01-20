import type { Field } from './fields'

export interface Platform {
  /** Type of platform where the repository is hosted.
   * @example "github"
   */
  type: string
  icon: string
  /**
   * Configuration fields for this platform.
   */
  config: Record<string, Field>
}
