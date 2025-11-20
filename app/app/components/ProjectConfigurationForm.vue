<template>
  <UForm
    :state="state as any"
    :schema="validationSchema"
    class="w-full max-w-4xl"
    @error="handleFormValidationErrors"
    @submit="onSubmit"
  >
    <!-- Resource Configurations -->
    <FormSection
      v-for="(resource, index) in resources"
      :key="`resource-${resource.name}-${index}`"
      :title="`${resource.name} Configuration`"
    >
      <UFormField
        v-for="(configOption, configKey) in resource.config"
        :key="`resource-${resource.name}-${index}-${configKey}`"
        :name="`resources.${index}.config.${configKey}`"
        :label="configOption.label"
        :required="configOption.required || false"
      >
        <GenericFormInput
          v-model="state.resources[index]!.config[configKey]"
          :config-option="configOption"
          :placeholder="configOption.description"
          class="w-full"
        />
      </UFormField>
    </FormSection>

    <!-- Platform Configuration -->
    <FormSection :title="`${platform.name} Configuration`">
      <UFormField
        v-for="(configOption, configKey) in platform.config"
        :key="`platform-${platform.name}-${configOption.label}-${configKey}`"
        :name="`platform.config.${configKey}`"
        :label="configOption.label"
        :required="configOption.required || false"
      >
        <GenericFormInput
          v-model="state.platform.config[configKey]"
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
        class="cursor-pointer"
        type="submit"
      >
        Create Project
      </UButton>
    </div>
  </UForm>
</template>

<script setup lang="ts">
import type { ProjectOptions } from '~/config/project-options'
import type { ConfiguredPlatform, ConfiguredResource } from '~/types'

const config = useRuntimeConfig()

const props = defineProps<{
  projectData: ProjectOptions
}>()

const emit = defineEmits<{
  back: []
}>()

const resources = computed(() => props.projectData.resources)
const platform = computed(() => props.projectData.platform)

// Generate validation schema from project data
const validationSchema = computed(() =>
  generateProjectConfigurationSchema(props.projectData)
)

interface ConfigurationFormState {
  resources: ConfiguredResource[]
  platform: ConfiguredPlatform
}

const state = ref<ConfigurationFormState>({
  resources: resources.value.map(resource => ({
    name: resource.name,
    config: generateDefaultConfig(resource.config)
  })),
  platform: {
    name: platform.value.name,
    config: generateDefaultConfig(platform.value.config)
  }
})

function handleFormValidationErrors(error: unknown) {
  console.error('Form validation errors:', error)
}

// Submit action
async function onSubmit() {
  console.log('Configuration submitted:', {
    projectData: props.projectData,
    configuration: state.value
  })

  const apiUrl = config.public.apiBase + '/create-repo'
  try {
    const response = await $fetch(apiUrl, {
      method: 'POST',
      body: state.value.platform.config
    })
    console.log('API response:', response)
  } catch (error) {
    console.error('Error submitting configuration:', error)
    alert('Failed to create repository. Please try again.')
  }
}

function handleBackPress() {
  emit('back')
}
</script>
