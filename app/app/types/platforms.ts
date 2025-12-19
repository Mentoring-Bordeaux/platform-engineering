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

export interface ConfiguredPlatform {
  /** Type of platform where the repository is hosted.
   * @example "github"
   */
  type: string
  /** Human-friendly name for this repository
   * @example "My GitHub Repository"
   */
  name: string
  /** User-provided configuration values for the created repository.
   */
  config: Record<string, unknown>
}
