<template>
  <UModal v-model:open="isLoading">
    <template #content>
      <div class="flex flex-col items-center justify-center gap-4 p-6">
        <span
          aria-live="assertive"
          class="sr-only"
        >
          {{ progressState.title }}
        </span>
        <USkeleton class="bg-primary h-5 w-5 rounded-full" />

        <div
          class="flex flex-col items-center justify-center gap-2 text-center"
        >
          <p class="text-lg font-semibold">{{ progressState.title }}</p>
          <p class="text-sm text-gray-500">{{ progressState.body }}</p>
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
    <FormSection
      title="Template Configuration"
      class="capitalize"
    >
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
    <FormSection
      :title="`${platform.type} Configuration`"
      class="capitalize"
    >
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
    type: string
    config: Record<string, unknown>
  }
}

const state = reactive<ConfigurationFormState>({
  parameters: generateDefaultConfig(templateParameters.value),
  platform: {
    type: platform.value.type,
    config: generateDefaultConfig(platform.value.config)
  }
})

const isLoading = ref(false)

const progressState = reactive({
  title: 'Creating your project repository',
  body: 'This may take a few moments...'
})

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
  isLoading.value = false
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
      templateParameters: state.parameters,
      platform: {
        type: state.platform.type,
        config: state.platform.config
      }
    }

    console.log('Sending create project request:', createProjectRequest)

    interface CreateProjectResponse {
      projectName: string
      idProjectCreation: number
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

    if (data.value) {
      console.log('Project creation started', data.value)

      progressState.title = 'Project Creation Started'
      progressState.body = `Step 1/6 : The creation of your project '${data.value.projectName}' has been initiated successfully. You will receive further updates as the process continues.`
      startPollingProjectStatus(
        data.value.idProjectCreation,
        data.value.projectName
      )
      return
    } else if (error.value) {
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
        body: `There was an error creating your project: ${errorMessage}`
      })
      return
    } else {
      console.error('Unknown error creating project')
      openModal({
        title: 'Error Creating Project',
        body: 'An unexpected error occurred while creating your project.'
      })
    }
  } catch (ex) {
    console.error('Exception creating project:', ex)
    progressState.title = 'Error'
    progressState.body =
      'An unexpected error occurred while creating your project.'
  }
}

async function startPollingProjectStatus(
  idProjectCreation: number,
  projectName: string
): Promise<void> {
  const pollingInterval = 3000 // 3 seconds

  const poll = async () => {
    try {
      interface ProjectStatusResponse {
        stepsCompleted: number
        currentStep: string
        status: 'InProgress' | 'Completed' | 'Failed'
        errorMessage?: string
        Outputs?: Record<string, unknown>
      }

      const { data, error } = await useFetch<ProjectStatusResponse>(
        `/create-project/status/${idProjectCreation}`,
        {
          key: `status-poll-${Date.now()}`, // Unique key to avoid caching
          server: false,
          baseURL: config.public.apiBase,
          method: 'GET',
          watch: false
        }
      )

      if (data.value) {
        console.log('Project status:', data.value)

        if (data.value.status === 'InProgress') {
          progressState.title = `Your project '${projectName}' is currently being created`
          progressState.body = `Current Step: ${data.value.currentStep} (${data.value.stepsCompleted}/5 completed). Please wait...`
          // Continue polling
          setTimeout(poll, pollingInterval)
        } else if (data.value.status === 'Completed') {
          openModal({
            title: `Project '${projectName}' Created Successfully`,
            body: `Your project has been created successfully!
            All outputs you can need: ${JSON.stringify(data.value.Outputs)}`
          })
          return
        } else if (data.value.status === 'Failed') {
          openModal({
            title: `Project '${projectName}' Creation Failed`,
            body: `There was an error creating your project: ${
              data.value.errorMessage || 'Unknown error'
            } during step: ${data.value.currentStep}`
          })
          return
        }
      } else if (error.value) {
        console.error('Error fetching project status:', error.value.data)
      }
    } catch (ex) {
      console.error('Exception while polling project status:', ex)
    }
  }

  // Start the polling loop
  poll()
}

function handleBackPress() {
  emit('back')
}
</script>
