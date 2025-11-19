<template>
  <UContainer class="flex w-full flex-col items-center py-8">
    <CreateAProjectForm
      v-if="step === 1"
      :project-data="projectData"
      @submit="setProjectData"
    />
    <ProjectConfigurationForm
      v-if="step === 2 && projectData"
      :project-data="projectData"
      @back="handleBackPress"
    />
  </UContainer>
</template>

<script setup lang="ts">
import type { ProjectOptions } from '~/config/project-options'

const projectData = ref<ProjectOptions | null>(null)
const step = ref(1)

const setProjectData = (data: ProjectOptions) => {
  projectData.value = data
  step.value = 2
}

const handleBackPress = () => {
  step.value = 1
}
</script>
