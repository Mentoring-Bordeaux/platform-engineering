<template>
  <UModal v-model:open="isLoading">
    <template #content>
      <div class="flex flex-col items-center justify-center gap-4 p-6">
        <span
          aria-live="assertive"
          class="sr-only"
          >Loading, creating your project repository</span
        >
        <USkeleton class="bg-primary h-5 w-5 rounded-full" />
        <div class="flex flex-col items-center justify-center text-center">
          <p>Creating your project repository</p>
          <p>This may take a few moments...</p>
        </div>
      </div>
    </template>
  </UModal>
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
<<<<<<< HEAD
      :key="`resource-${resource.resourceType}-${index}`"
      :title="`${resource.resourceType} Configuration`"
=======
      :key="`resource-${resource.type}-${index}`"
      :title="`${resource.type} Configuration`"
>>>>>>> origin/main
    >
      <UFormField
        :name="`resources.${index}.name`"
        label="Resource Name"
        required
      >
        <UInput
          v-model="state.resources[index]!.name"
          placeholder="Enter resource name"
          class="w-full"
        />
      </UFormField>
      <UFormField
        v-for="(configOption, configKey) in resource.config"
        :key="`resources.${index}.config.${configKey}`"
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
<<<<<<< HEAD
    <FormSection :title="`${platform.platformType} Configuration`">
      <UFormField
        name="platform.name"
        label="Platform Name"
=======
    <FormSection :title="`${platform.type} Configuration`">
      <UFormField
        name="platform.name"
        label="Repository Name"
>>>>>>> origin/main
        required
      >
        <UInput
          v-model="state.platform.name"
          placeholder="Enter repo name"
          class="w-full"
        />
      </UFormField>
      <UFormField
        v-for="(configOption, configKey) in platform.config"
<<<<<<< HEAD
        :key="`platform-${platform.platformType}-${configOption.label}-${configKey}`"
=======
        :key="`platform-${platform.type}-${configOption.label}-${configKey}`"
>>>>>>> origin/main
        :name="`platform.config.${configKey}`"
        :label="configOption.label"
        :required="configOption.required || false"
        :class="
          configOption.type === 'boolean'
            ? 'flex items-center justify-between py-2'
            : ''
        "
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

import InfoModal from './InfoModal.vue'

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
    name: '',
<<<<<<< HEAD
    resourceType: resource.resourceType,
=======
    type: resource.type,
>>>>>>> origin/main
    config: generateDefaultConfig(resource.config)
  })),
  platform: {
    name: '',
<<<<<<< HEAD
    platformType: platform.value.platformType,
=======
    type: platform.value.type,
>>>>>>> origin/main
    config: generateDefaultConfig(platform.value.config)
  }
})

const isLoading = ref(false)

const overlay = useOverlay()

interface ModalOptions {
  title: string
  body: string
}

async function openModal({ title, body }: ModalOptions) {
  const modal = overlay.create(InfoModal, {
    props: {
      title,
      body
    }
  })
  modal.open()
}

// Handlers

function handleFormValidationErrors(error: unknown) {
  console.error('Form validation errors:', error)
}

function formatResourceType(str: string): string {
  const resourceType = str.replace(/\s+/g, '-')
  return resourceType.toLowerCase().trim()
}

// Submit action
async function onSubmit() {
  console.log('Configuration submitted:', {
    projectData: props.projectData,
    configuration: state.value
  })

  isLoading.value = true

  interface RequestReponse {
    name: string
    ressourceType: string
    statusCode: number
    message: string
    outputs?: Record<string, unknown>
  }

  interface RequestElementTemplate {
    name: string
    resourceType: string
    framework?: string
    parameters: Record<string, string>
  }

  const listRessources: RequestElementTemplate[] = state.value.resources.map(
    (resource): RequestElementTemplate => {
      return {
        name: resource.name,
<<<<<<< HEAD
        resourceType: 'resources//' + formatResourceType(resource.resourceType),
=======
        resourceType: 'resources//' + formatResourceType(resource.type),
>>>>>>> origin/main
        parameters: Object.fromEntries(
          Object.entries(resource.config).map(([key, value]) => [
            key,
            String(value)
          ])
        )
      }
    }
  )

  listRessources.push({
    name: state.value.platform.name,
<<<<<<< HEAD
    resourceType:
      'platforms//' + formatResourceType(state.value.platform.platformType),
=======
    resourceType: 'platforms//' + formatResourceType(state.value.platform.type),
>>>>>>> origin/main
    parameters: Object.fromEntries(
      Object.entries(state.value.platform.config).map(([key, value]) => [
        key,
        String(value)
      ])
    )
  })

  console.log('Sending resources to API:', listRessources)

  const { data, error } = await useFetch<RequestReponse[]>('/create-project', {
    server: false,
    baseURL: config.public.apiBase,
    method: 'POST',
    body: listRessources,
    watch: false
  })
  isLoading.value = false

  if (error.value) {
    console.error('Error creating repository:', error.value.data)

    const title =
      error.value.statusCode === 400
        ? 'Invalid Configuration'
        : 'Error Creating Resources'

    if (!error.value.data) {
      openModal({
        title: title,
        body: 'There was an unexpected error while creating your project resources. Please try again.'
      })
      return
    }

    if (!Array.isArray(error.value.data)) {
      openModal({
        title: title,
        body:
          error.value.data.message ||
          'There was an unexpected error while creating your project resources. Please try again.'
      })
      return
    }
    const message = error.value.data
      .map(
        (err: RequestReponse) =>
          `- ${err.message} to ${err.name} of type ${err.ressourceType}`
      )
      .join('\n')
    openModal({
      title: title,
      body:
        message ||
        'There was an unexpected error while creating your project resources. Please try again.'
    })
    return
  }
  if (data.value) {
    const message = data.value
      .map(
        (data: RequestReponse) =>
          `- ${data.name} of type ${data.ressourceType} created successfully. \n
            ${data.outputs ? JSON.stringify(data.outputs) : ''}`
      )
      .join('\n')
    console.log('Repository created successfully:', data.value)
    openModal({
      title: 'Your Project is Ready!',
      body: `Ressources created successfully. ${message}`
    })
    return
  }
}

function handleBackPress() {
  emit('back')
}
</script>
