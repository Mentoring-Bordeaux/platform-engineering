import type { Template } from '~/types'

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
      const response = await $fetch<Template[]>(`${apiBase}/templates`)
      templates.value = response
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
