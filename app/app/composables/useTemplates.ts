import type { Template, RawTemplate } from '~/types'
import { normalizeTemplate } from '~/types/templates'

/**
 * Composable to fetch templates from API
 */
export const useTemplates = () => {
  const config = useRuntimeConfig()
  const apiBase = config.public.apiBase

  const templates = ref<Template[]>([])
  const loading = ref(false)
  const error = ref<Error | null>(null)

  const fetchTemplates = async () => {
    loading.value = true
    error.value = null

    try {
      const response = await $fetch<RawTemplate[]>(`${apiBase}/api/templates`)
      // Normalize each raw template from API to properly typed template
      templates.value = response.map(normalizeTemplate)
    } catch (err) {
      error.value = err as Error
      console.error('Failed to fetch templates:', err)
      templates.value = []
    } finally {
      loading.value = false
    }
  }

  return {
    templates,
    loading,
    error,
    fetchTemplates
  }
}
