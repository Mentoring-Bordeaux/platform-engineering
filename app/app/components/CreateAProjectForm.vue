<template>
  <UForm
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

    <!-- Preset Selection Section -->
    <FormSection
      id="preset-selection"
      title="Preset"
    >
      <UFormField
        name="preset"
        label="Select a project preset"
        required
      >
        <div class="mt-3 grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
          <CardSelect
            v-for="(preset, key) in PRESETS_WITHOUT_BLANK"
            :key="key"
            :title="preset.name"
            :description="preset.description"
            :is-selected="state.preset === key"
            @select="onPresetSelect(key)"
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
        label="Choose where to host your project repository"
        required
      >
        <div class="mt-3 grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
          <CardSelect
            v-for="(platform, key) in PLATFORMS"
            :key="key"
            :icon="platform.icon"
            :title="platform.platformType"
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
import { PLATFORMS, type PlatformKey } from '~/config/platforms'
import { PRESETS, type PresetKey } from '~/config/presets'
import type { ProjectOptions } from '~/config/project-options'
import { RESOURCES } from '~/config/resources'

const projectStore = useProjectStore()

const router = useRouter()

const { blank: _, ...PRESETS_WITHOUT_BLANK } = PRESETS

// Section display logic

const showPlatformSection = ref(false)
const showSubmitButton = ref(false)

const onPresetSelect = (key: PresetKey) => {
  showPlatformSection.value = true
  state.preset = key
}

const onPlatformSelect = (key: PlatformKey) => {
  showSubmitButton.value = true
  state.platform = key
}

// Schema for form data validation

const CreateAProjectFormSchema = z.object({
  name: z.string().min(1, 'Project name is required'),
  description: z.string().optional(),
  preset: z.literal(Object.keys(PRESETS)).or(z.literal('blank')),
  platform: z.literal(Object.keys(PLATFORMS))
})

type CreateAProjectFormType = z.infer<typeof CreateAProjectFormSchema>

const state = reactive<CreateAProjectFormType>({
  name: projectStore.projectData?.name || '',
  description: projectStore.projectData?.description || '',
  preset: '',
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

  const presetKey = validation.data.preset as PresetKey
  const platformKey = validation.data.platform as PlatformKey

  const preset = PRESETS[presetKey]

  const projectData = {
    ...validation.data,
    resources: preset.resources.map(resourceName => RESOURCES[resourceName]),
    platform: PLATFORMS[platformKey]
  } satisfies ProjectOptions

  projectStore.setProjectData(projectData)
  router.push('/configure')
}
</script>
