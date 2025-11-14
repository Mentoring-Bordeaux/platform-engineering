<template>
  <UForm
    :schema="CreateAProjectFormSchema"
    :state="state"
    class="w-full max-w-4xl"
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

    <!-- Preset Selection Section -->
    <FormSection
      id="preset-selection"
      title="Preset"
    >
      <UFormField
        name="preset"
        required
      >
        <div class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
          <CardSelect
            v-for="(preset, key) in PRESETS"
            :key="key"
            :title="preset.name"
            :description="preset.description"
            :is-selected="state.preset === key"
            @select="onPresetSelect(key, preset)"
          />
        </div>
        <USeparator label="or" />
        <CardSelect
          title="Blank Template"
          description="Use your own custom template repository."
          :is-selected="state.preset === 'blank'"
          @select="onPresetSelect('blank')"
        />
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
        required
      >
        <p class="mb-4 text-sm text-gray-600 dark:text-gray-400">
          Choose where to host your project repository
        </p>
        <div class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
          <CardSelect
            v-for="(platform, key) in PLATFORMS"
            :key="key"
            :icon="platform.icon"
            :title="platform.name"
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
import { ref, reactive } from 'vue'
import { z } from 'zod'
import {
  PRESETS,
  RESOURCES,
  PLATFORMS,
  type Preset,
  type ProjectData,
  type PlatformKey,
  type ResourceOption
} from '~/config/project-options'

const emit = defineEmits<{
  submit: [data: ProjectData]
}>()

// Section display logic

const showPlatformSection = ref(false)
const showSubmitButton = ref(false)
const showSuccessMessage = ref(false)

const onPresetSelect = (key: string, preset?: Preset) => {
  showPlatformSection.value = true
  state.preset = key
  if (key !== 'blank' && preset) {
    state.resources = preset.resources.map(resourceKey => ({
      key: resourceKey,
      id: `${resourceKey}-${Math.random().toString(36)}`
    }))
    console.log('Preset selected, resources updated:', state.resources)
  } else {
    state.resources = []
  }
}

const onPlatformSelect = (key: string) => {
  showSubmitButton.value = true
  state.platform = key
}

const resourceOption = z.object({
  key: z.literal(Object.keys(RESOURCES), {
    message: `Resource must be one of: ${Object.keys(RESOURCES).join(', ')}`
  }),
  id: z.string(),
})



// Schema for form data validation

const CreateAProjectFormSchema = z.object({
  name: z.string().min(1, 'Project name is required'),
  description: z.string().optional(),
  preset: z.literal(Object.keys(PRESETS)).or(z.literal('blank')),
  resources: z.array(resourceOption),
  platform: z.literal(Object.keys(PLATFORMS))
})

type CreateAProjectFormType = z.infer<typeof CreateAProjectFormSchema>

const state = reactive<CreateAProjectFormType>({
  name: '',
  description: '',
  preset: '',
  resources: [],
  platform: ''
})

// Submit action

async function onSubmit(event: FormSubmitEvent<CreateAProjectFormType>) {
  console.log('Submitting form...')
  showSuccessMessage.value = false

  const validation = CreateAProjectFormSchema.safeParse(event.data)

  if (!validation.success) {
    console.error('Form validation failed:', validation.error)

    // Scroll to top to show errors
    window.scrollTo({ top: 0, behavior: 'smooth' })
    return
  }

  console.log('Form submitted with data:', validation.data)

  const projectData = {
    ...validation.data,
    resource: validation.data.resources as ResourceOption[],
    platform: validation.data.platform as PlatformKey
  } satisfies ProjectData

  emit('submit', projectData)
}
</script>
