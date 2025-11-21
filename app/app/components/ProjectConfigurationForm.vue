<template>
  <UModal v-model:open="isLoading">
    <template #content>
      <div class="flex flex-col items-center justify-center gap-4 p-6">
        <span aria-live="assertive" class="sr-only">Loading, creating your project repository</span>
        <USkeleton class="bg-primary h-5 w-5 rounded-full" />
        <div class="flex flex-col items-center justify-center text-center">
          <p>Creating your project repository</p>
          <p>This may take a few moments...</p>
        </div>
      </div>
    </template>
  </UModal>
  <UForm :state="state as any" :schema="validationSchema" class="w-full max-w-4xl" @error="handleFormValidationErrors"
    @submit="onSubmit">
    <!-- Resource Configurations -->
    <FormSection v-for="(resource, index) in resources" :key="`resource-${resource.name}-${index}`"
      :title="`${resource.name} Configuration`">
      <UFormField v-for="(configOption, configKey) in resource.config" :key="`resources.${index}.config.${configKey}`"
        :name="`resources.${index}.config.${configKey}`" :label="configOption.label"
        :required="configOption.required || false">
        <GenericFormInput v-model="state.resources[index]!.config[configKey]" :config-option="configOption"
          :placeholder="configOption.description" class="w-full" />
      </UFormField>
    </FormSection>

    <!-- Platform Configuration -->
    <FormSection :title="`${platform.name} Configuration`">
      <UFormField v-for="(configOption, configKey) in platform.config"
        :key="`platform-${platform.name}-${configOption.label}-${configKey}`" :name="`platform.config.${configKey}`"
        :label="configOption.label" :required="configOption.required || false" :class="configOption.type === 'boolean'
          ? 'flex items-center justify-between py-2'
          : ''
          ">
        <GenericFormInput v-model="state.platform.config[configKey]" :config-option="configOption"
          :placeholder="configOption.description" class="w-full" />
      </UFormField>
    </FormSection>

    <!-- Action Buttons -->
    <div class="flex w-full justify-between">
      <UButton icon="i-lucide-arrow-left" size="md" variant="outline" @click="handleBackPress">
        Back
      </UButton>
      <UButton icon="i-lucide-check" size="md" variant="solid" class="cursor-pointer" type="submit">
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
    name: resource.name,
    config: generateDefaultConfig(resource.config)
  })),
  platform: {
    name: platform.value.name,
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

// Submit action
async function onSubmit() {
  console.log('Configuration submitted:', {
    projectData: props.projectData,
    configuration: state.value
  })

  isLoading.value = true
  interface RepoCreationResponse {
  repoName: string
  repoUrl: string
}
  const { data, error } = await useFetch<RepoCreationResponse>('/create-repo', {
    server: false,
    baseURL: config.public.apiBase,
    method: 'POST',
    body: state.value.platform.config,
    watch: false
  })
  isLoading.value = false

  if (error.value) {
    console.error('Error creating repository:', error.value.data)
    openModal({
      title: error.value.data?.title || 'Repository Creation Error',
      body:
        error.value.data?.detail ||
        'There was an error creating the project repository. Please try again.'
    })
    return
  }
  if (data.value) {
    if (data.value) {
      console.log('Repository created successfully:', data.value)
      openModal({
        title: 'Your Project is Ready!',
        body: 'Your project repository has been created successfully!\
  You can now access it on GitHub at the following link: \n' +
          `${data.value.repoUrl}`
      })
      return
    }
  }
}

function handleBackPress() {
  emit('back')
}
</script>
