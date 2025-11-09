<template>
  <UForm
    :schema="ConfigurationSchema"
    :state="state"
    class="w-full max-w-4xl"
    @submit="onSubmit"
  >
    <!-- Framework Configurations -->
    <FormSection
      v-for="frameworkKey in projectData.frameworks"
      :key="`framework-${frameworkKey}`"
      :title="`${FRAMEWORKS[frameworkKey].name} Configuration`"
    >
      <UFormField
        v-for="(configOption, configKey) in FRAMEWORKS[frameworkKey].config"
        :key="`${frameworkKey}-${String(configKey)}`"
        :name="`frameworks.${frameworkKey}.${String(configKey)}`"
        :label="configOption.label"
      >
        <GenericFormInput
          v-model="state.frameworks[frameworkKey]![String(configKey)]"
          :config-option="configOption"
          :placeholder="configOption.description"
          class="w-full"
        />
      </UFormField>
    </FormSection>

    <!-- Platform Configuration -->
    <FormSection
      :title="`${PLATFORMS[projectData.platform].name} Configuration`"
    >
      <UFormField
        v-for="(configOption, configKey) in PLATFORMS[projectData.platform]
          .config"
        :key="`platform-${configKey}`"
        :name="`platform.${configKey}`"
        :label="configOption.label"
      >
        <GenericFormInput
          v-model="state.platform[configKey]"
          :config-option="configOption"
          :placeholder="configOption.description"
          class="w-full"
        />
      </UFormField>
    </FormSection>

    <!-- Action Buttons -->
    <div class="flex w-full justify-between">
      <UButton
        icon="i-lucide-arrow-left"
        size="md"
        variant="outline"
        @click="handleBackPress"
      >
        Back
      </UButton>
      <UButton
        icon="i-lucide-check"
        size="md"
        variant="solid"
        type="submit"
      >
        Create Project
      </UButton>
    </div>
  </UForm>
</template>

<script setup lang="ts">
import type { FormSubmitEvent } from '@nuxt/ui'
import { reactive } from 'vue'
import { z } from 'zod'
import {
  FRAMEWORKS,
  PLATFORMS,
  type FrameworkKey,
  type PlatformKey
} from '~/config/project-options'

interface ProjectData {
  name: string
  description?: string
  preset: string
  frameworks: FrameworkKey[]
  platform: PlatformKey
}

const props = defineProps<{
  projectData: ProjectData
}>()

const emit = defineEmits<{
  back: []
}>()

// Build dynamic schema based on selected frameworks and platform
const ConfigurationSchema = z.object({
  frameworks: z.record(
    z.string(),
    z.record(z.string(), z.union([z.string(), z.number(), z.boolean()]))
  ),
  platform: z.record(z.string(), z.union([z.string(), z.number(), z.boolean()]))
})

type ConfigurationFormType = z.infer<typeof ConfigurationSchema>

// Initialize state with default values from config
const initializeState = (): ConfigurationFormType => {
  const frameworksConfig: Record<
    string,
    Record<string, string | number | boolean>
  > = {}
  const platformConfig: Record<string, string | number | boolean> = {}

  // Initialize framework configs with defaults
  props.projectData.frameworks.forEach(frameworkKey => {
    frameworksConfig[frameworkKey] = {}
    const frameworkConfig = FRAMEWORKS[frameworkKey].config
    Object.entries(frameworkConfig).forEach(([key, option]) => {
      frameworksConfig[frameworkKey]![key] = option.default
    })
  })

  // Initialize platform config with defaults
  const platformConfigOptions = PLATFORMS[props.projectData.platform].config
  Object.entries(platformConfigOptions).forEach(([key, option]) => {
    platformConfig[key] = option.default
  })

  return {
    frameworks: frameworksConfig,
    platform: platformConfig
  }
}

const state = reactive<ConfigurationFormType>(initializeState())

// Submit action
async function onSubmit(event: FormSubmitEvent<ConfigurationFormType>) {
  console.log('Configuration submitted:', {
    projectData: props.projectData,
    configuration: event.data
  })

  // Here you would typically send this data to your API
  // For now, we'll just log it and show a success message
  alert('Project configuration saved successfully!')
}

function handleBackPress() {
  emit('back')
}
</script>
