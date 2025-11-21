import { defineStore } from 'pinia'
import type { ProjectOptions } from '~/config/project-options'

export const useProjectStore = defineStore('project', () => {
  const projectData = ref<ProjectOptions | null>(null)
  const step = ref(1)

  const setProjectData = (data: ProjectOptions) => {
    projectData.value = data
    step.value = 2
  }

  const goToStep = (newStep: number) => {
    step.value = newStep
  }

  const resetProject = () => {
    projectData.value = null
    step.value = 1
  }

  return {
    projectData,
    step,
    setProjectData,
    goToStep,
    resetProject
  }
})
