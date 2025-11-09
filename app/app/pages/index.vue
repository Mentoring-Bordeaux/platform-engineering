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
import type { ProjectData } from '~/config/project-options'

const projectData = ref<ProjectData | null>(null)

const setProjectData = (data: ProjectData) => {
  projectData.value = data
}
</script>
