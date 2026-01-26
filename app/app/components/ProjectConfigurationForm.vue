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
    :state="state"
    :schema="validationSchema"
    class="w-full max-w-4xl"
    @error="handleFormValidationErrors"
    @submit="onSubmit"
  >
    <!-- Template Parameters Section -->
    <FormSection title="Template Configuration">
      <UFormField
        v-for="(paramOption, paramKey) in templateParameters"
        :key="`template-param-${paramKey}`"
        :name="`parameters.${paramKey}`"
        :label="paramOption.label"
        :required="paramOption.required || false"
      >
        <GenericFormInput
          v-model="state.parameters[paramKey]"
          :config-option="paramOption"
          :placeholder="paramOption.description"
          class="w-full"
        />
      </UFormField>
    </FormSection>

    <!-- Platform Configuration -->
    <FormSection :title="`${platform.type} Configuration`">
      <UFormField
        name="platform.name"
        label="Repository Name"
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
        :key="`platform-${platform.type}-${configOption.label}-${configKey}`"
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
import { flattenTemplateParameters } from '~/types'
import { generateDefaultConfig } from '~/utils/config'
import { generateProjectConfigurationSchema } from '~/utils/validation'
import InfoModal from './InfoModal.vue'
import type { ProjectOptions } from '~/config/project-options'

const config = useRuntimeConfig()

const props = defineProps<{
  projectData: ProjectOptions
}>()

const emit = defineEmits<{
  back: []
}>()

const platform = computed(() => props.projectData.platform)

// Flatten template parameters for display
const templateParameters = computed(() =>
  flattenTemplateParameters(props.projectData.template.parameters)
)

// Generate validation schema from project data
const validationSchema = computed(() =>
  generateProjectConfigurationSchema(props.projectData)
)

interface ConfigurationFormState {
  parameters: Record<string, unknown>
  platform: {
    name: string
    type: string
    config: Record<string, unknown>
  }
}

const state = reactive<ConfigurationFormState>({
  parameters: generateDefaultConfig(templateParameters.value),
  platform: {
    name: '',
    type: platform.value.type,
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

function handleFormValidationErrors(error: unknown) {
  console.error('Form validation errors:', error)
}

async function onSubmit() {
  console.log('Configuration submitted:', {
    projectData: props.projectData,
    configuration: state
  })

  isLoading.value = true

  try {
    // Build the new request format
    const createProjectRequest = {
      templateName: props.projectData.template.name,
      projectName: props.projectData.name,
      parameters: state.parameters,
      platform: {
        type: state.platform.type,
        name: state.platform.name,
        config: state.platform.config
      }
    }

    console.log('Sending create project request:', createProjectRequest)

    interface CreateProjectResponse {
      statusCode: number
      message: string
      outputs?: Record<string, unknown>
    }

    const { data, error } = await useFetch<CreateProjectResponse>(
      '/create-project',
      {
        server: false,
        baseURL: config.public.apiBase,
        method: 'POST',
        body: createProjectRequest,
        watch: false
      }
    )

    isLoading.value = false

    if (error.value) {
      console.error('Error creating project:', error.value.data)

      const title =
        error.value.statusCode === 400
          ? 'Invalid Configuration'
          : 'Error Creating Project'

      const errorMessage =
        typeof error.value.data === 'object'
          ? error.value.data?.message || JSON.stringify(error.value.data)
          : error.value.data || 'An unexpected error occurred'

      openModal({
        title,
        body: errorMessage
      })
      return
    }

    if (data.value) {
      const successMessage =
        data.value.message || 'Project created successfully'
      console.log('Project created successfully:', data.value)
      openModal({
        title: 'Your Project is Ready!',
        body: successMessage
      })
    }
  } catch (ex) {
    isLoading.value = false
    console.error('Exception creating project:', ex)
    openModal({
      title: 'Error',
      body: 'An unexpected error occurred while creating your project.'
    })
  }
}

function handleBackPress() {
  emit('back')
}
</script>