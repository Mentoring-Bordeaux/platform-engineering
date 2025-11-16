<template>
  <UContainer class="flex w-full flex-col items-center py-8">
    <CreateAProjectForm
      v-if="!projectData"
      @submit="setProjectData"
    />
    <ProjectConfigurationForm
      v-if="projectData"
      :project-data="projectData"
      @back="projectData = null"
    />
  </UContainer>
</template>

<script setup lang="ts">
import type { ProjectOptions } from '~/config/project-options'

const projectData = ref<ProjectOptions | null>(null)

const setProjectData = (data: ProjectOptions) => {
  projectData.value = data
}
</script>
