<template>
  <UForm
    :schema="ConfigurationSchema"
    :state="state"
    class="w-full max-w-4xl"
    @submit="onSubmit"
  >
    <!-- Resource Configurations -->
    <FormSection
      v-for="resourceKey in projectData.resource"
      :key="`resource-${resourceKey}`"
      :title="`${RESOURCES[resourceKey].name} Configuration`"
    >
      <UFormField
        v-for="(configOption, configKey) in RESOURCES[resourceKey].config"
        :key="`${resourceKey}-${String(configKey)}`"
        :name="`resource.${resourceKey}.${String(configKey)}`"
        :label="configOption.label"
      >
        <GenericFormInput
          v-model="state.resource[resourceKey]![String(configKey)]"
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
  RESOURCES,
  PLATFORMS,
  type ResourceKey,
  type PlatformKey
} from '~/config/project-options'

interface ProjectData {
  name: string
  description?: string
  preset: string
  resource: ResourceKey[]
  platform: PlatformKey
}

const props = defineProps<{
  projectData: ProjectData
}>()

const emit = defineEmits<{
  back: []
}>()

// Build dynamic schema based on selected resource and platform
const ConfigurationSchema = z.object({
  resource: z.record(
    z.string(),
    z.record(z.string(), z.union([z.string(), z.number(), z.boolean()]))
  ),
  platform: z.record(z.string(), z.union([z.string(), z.number(), z.boolean()]))
})

type ConfigurationFormType = z.infer<typeof ConfigurationSchema>

// Initialize state with default values from config
const initializeState = (): ConfigurationFormType => {
  const resourcesConfig: Record<
    string,
    Record<string, string | number | boolean>
  > = {}
  const platformConfig: Record<string, string | number | boolean> = {}

  // Initialize resource configs with defaults
  props.projectData.resource.forEach(resourceKey => {
    resourcesConfig[resourceKey] = {}
    const resourceConfig = RESOURCES[resourceKey].config
    Object.entries(resourceConfig).forEach(([key, option]) => {
      resourcesConfig[resourceKey]![key] = option.default
    })
  })

  // Initialize platform config with defaults
  const platformConfigOptions = PLATFORMS[props.projectData.platform].config
  Object.entries(platformConfigOptions).forEach(([key, option]) => {
    platformConfig[key] = option.default
  })

  return {
    resource: resourcesConfig,
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
