<template>
  <UContainer class="flex w-full flex-col items-center py-8">
    <ProjectConfigurationForm
      v-if="projectStore.projectData"
      :project-data="projectStore.projectData"
      @back="handleBackPress"
    />
    <div
      v-else
      class="text-center"
    >
      <p class="mb-4 text-gray-600 dark:text-gray-400">
        No project data found. Please create a project first.
      </p>
      <UButton to="/">Go to Create Project</UButton>
    </div>
  </UContainer>
</template>

<script setup lang="ts">
const projectStore = useProjectStore()
const router = useRouter()

const handleBackPress = () => {
  router.push('/')
}
</script>
