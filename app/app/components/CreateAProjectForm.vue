<template>
  <div
    v-if="loading"
    class="flex justify-center py-8"
  >
    <UIcon
      name="i-lucide-loader-circle"
      class="animate-spin"
      size="32"
    />
  </div>
  <div
    v-else-if="error"
    class="py-8 text-center"
  >
    <p class="mb-4 text-red-600 dark:text-red-400">
      Failed to load templates: {{ error.message }}
    </p>
    <UButton @click="fetchTemplates">Retry</UButton>
  </div>
  <UForm
    v-else
    :schema="CreateAProjectFormSchema"
    :state="state"
    class="w-full max-w-4xl"
    @error="handleFormValidationErrors"
    @submit="onSubmit"
  >
    <!-- Base Information Section -->
    <FormSection
      id="base-info"
      title="Base Information"
    >
      <UFormField
        name="name"
        label="Project Name"
        required
      >
        <UInput
          v-model="state.name"
          placeholder="My awesome project"
          class="w-full"
          size="md"
          required
        />
      </UFormField>
      <UFormField
        name="description"
        label="Description (optional)"
        class="w-full"
      >
        <UTextarea
          v-model="state.description"
          class="w-full"
          placeholder="A brief description of your project"
          :rows="3"
        />
      </UFormField>
    </FormSection>

    <!-- Template Selection Section -->
    <FormSection
      id="template-selection"
      title="Template"
    >
      <UFormField
        name="template"
        label="Select a project template"
        required
      >
        <div
          v-if="templates.length === 0"
          class="py-8 text-center text-gray-600 dark:text-gray-400"
        >
          No templates available
        </div>
        <div
          v-else
          class="mt-3 grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3"
        >
          <CardSelect
            v-for="(template, index) in templates"
            :key="index"
            :title="template.displayName || template.name"
            :description="template.description"
            :is-selected="state.templateIndex === index"
            @select="onTemplateSelect(index)"
          />
        </div>
      </UFormField>
    </FormSection>

    <!-- Platform Selection Section -->
    <FormSection
      v-if="showPlatformSection"
      id="platform-selection"
      title="Platform Selection"
    >
      <UFormField
        name="platform"
        label="Choose where to host your project repository"
        required
      >
        <div class="mt-3 grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
          <CardSelect
            v-for="(platform, key) in PLATFORMS"
            :key="key"
            :icon="platform.icon"
            :title="platform.type"
            :is-selected="state.platform === key"
            @select="onPlatformSelect(key)"
          />
        </div>
      </UFormField>
    </FormSection>

    <!-- Submit Button -->
    <div
      v-if="showSubmitButton"
      class="flex w-full justify-end"
    >
      <UButton
        icon="i-lucide-cog"
        size="md"
        variant="solid"
        class="cursor-pointer"
        type="submit"
      >
        Configure project
      </UButton>
    </div>
  </UForm>
</template>

<script setup lang="ts">
import type { FormSubmitEvent } from '@nuxt/ui'
import { ref, reactive, onMounted } from 'vue'
import { z } from 'zod'
import { PLATFORMS, type PlatformKey } from '~/config/platforms'
import type { ProjectOptions } from '~/config/project-options'

const projectStore = useProjectStore()
const router = useRouter()
const { templates, loading, error, fetchTemplates } = useTemplates()

// Section display logic
const showPlatformSection = ref(false)
const showSubmitButton = ref(false)

const onTemplateSelect = (index: number) => {
  showPlatformSection.value = true
  state.templateIndex = index
}

const onPlatformSelect = (key: PlatformKey) => {
  showSubmitButton.value = true
  state.platform = key
}

// Schema for form data validation
const CreateAProjectFormSchema = z.object({
  name: z.string().min(1, 'Project name is required'),
  description: z.string().optional(),
  templateIndex: z.number().min(0),
  platform: z.literal(Object.keys(PLATFORMS))
})

type CreateAProjectFormType = z.infer<typeof CreateAProjectFormSchema>

const state = reactive<CreateAProjectFormType>({
  name: projectStore.projectData?.name || '',
  description: projectStore.projectData?.description || '',
  templateIndex: -1,
  platform: ''
})

function handleFormValidationErrors(error: unknown) {
  console.error('Form validation errors:', error)
}

// Submit action
async function onSubmit(event: FormSubmitEvent<CreateAProjectFormType>) {
  const validation = CreateAProjectFormSchema.safeParse(event.data)

  if (!validation.success) {
    console.error('Form validation failed:', validation.error)
    return
  }

  console.log('Form submitted with data:', validation.data)

  const platformKey = validation.data.platform as PlatformKey
  const selectedTemplate = templates.value[validation.data.templateIndex]

  if (!selectedTemplate) {
    console.error('Template not found')
    return
  }

  const projectData: ProjectOptions = {
    name: validation.data.name,
    description: validation.data.description,
    template: selectedTemplate,
    platform: PLATFORMS[platformKey]
  }

  projectStore.setProjectData(projectData)
  router.push('/configure')
}

// Fetch templates on mount
onMounted(() => {
  fetchTemplates()
})
</script>
