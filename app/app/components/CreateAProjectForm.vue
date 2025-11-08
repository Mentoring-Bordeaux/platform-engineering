<template>
  <!-- Base Information Section -->
  <FormSection title="Base Information">
    <u-input
      v-model="name"
      label="Name"
      placeholder="My awesome project"
      size="md"
    />
    <u-textarea
      v-model="description"
      label="Description"
      placeholder="Describe your project..."
    />
  </FormSection>

  <!-- Preset Selection Section -->
  <FormSection title="Preset">
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <CardSelect
        v-for="(preset, key) in presets"
        :key="key"
        :title="preset.name"
        :description="preset.description"
        :is-selected="selectedPreset === key"
        @select="onPresetSelect(key)"
      />
    </div>
    <u-separator label="or" />
    <CardSelect
      title="Blank Template"
      description="Use your own custom template repository."
      :is-selected="selectedPreset === 'blank'"
      @select="onPresetSelect('blank')"
    />
  </FormSection>

  <!-- Framework Selection Section -->
  <FormSection title="Framework Selection">
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <CardSelect
        v-for="(framework, key) in frameworks"
        :key="key"
        :icon="framework.icon"
        :title="framework.name"
        :is-selected="selectedFramework.has(key)"
        @select="onFrameworkSelect(key)"
      />
    </div>
  </FormSection>

  <!-- Platform Selection Section -->
  <FormSection title="Platform Selection">
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <CardSelect
        v-for="(platform, key) in platforms"
        :key="key"
        :icon="platform.icon"
        :title="platform.name"
        :is-selected="selectedPlatform === key"
        @select="onPlatformSelect(key)"
      />
    </div>
  </FormSection>

  <!-- Submit Button -->
  <div class="w-full flex justify-end">
    <u-button
      icon="i-lucide-cog"
      size="md"
      variant="solid"
      class="cursor-pointer"
      @click.prevent="onSubmit"
    >
      Configure project
    </u-button>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import type { Framework, Platform, Preset } from '~/types'

// Base Information
const name = ref('')
const description = ref('')

// Presets

const presets = {
  'nuxt-dotnet': {
    name: 'Nuxt + Dotnet',
    description: 'A starter template with Nuxt.js frontend and Dotnet backend.'
  },
  'react-dotnet': {
    name: 'React + Dotnet',
    description: 'A starter template with React frontend and Dotnet backend.'
  },
  'my-awesome-template': {
    name: 'My Awesome Template',
    description: 'An awesome template for your my project.'
  },
  'react-nestjs': {
    name: 'React + NestJS',
    description: 'A starter template with React frontend and NestJS backend.'
  }
} as const satisfies Record<string, Preset>

type PresetKey = keyof typeof presets | 'blank'

const selectedPreset = ref<string | null>(null)

const onPresetSelect = (key: PresetKey) => {
  selectedPreset.value = key
}

// Frameworks
const frameworks = {
  html5: { name: 'Vanilla', icon: 'devicon:html5' },
  vue: { name: 'Vue.js', icon: 'devicon:vuejs' },
  react: { name: 'React', icon: 'devicon:react' }
} as const satisfies Record<string, Framework>

type FrameworkKey = keyof typeof frameworks

const selectedFramework = ref(new Set<FrameworkKey>())

const onFrameworkSelect = (key: FrameworkKey) => {
  if (selectedFramework.value.has(key)) {
    selectedFramework.value.delete(key)
  } else {
    selectedFramework.value.add(key)
  }
}

// Platforms

const platforms = {
  github: { name: 'GitHub', icon: 'devicon:github' },
  gitlab: { name: 'GitLab', icon: 'devicon:gitlab' }
} as const satisfies Record<string, Platform>

type PlatformKey = keyof typeof platforms

const selectedPlatform = ref<PlatformKey | null>(null)

const onPlatformSelect = (key: PlatformKey) => {
  selectedPlatform.value = key
}

// Submit action

interface FormData {
  name: string
  description: string
  preset: string
  frameworks: FrameworkKey[]
  platform: PlatformKey
}

const onSubmit = () => {
  if (!name.value) {
    alert('Please enter a project name.')
    return
  }
  if (!selectedPlatform.value) {
    alert('Please select a platform.')
    return
  }
  const frameworksArray = Array.from(selectedFramework.value)
  if (frameworksArray.length === 0) {
    alert('Please select at least one framework.')
    return
  }
  if (!selectedPreset.value) {
    alert('Please select a preset.')
    return
  }
  const formObject: FormData = {
    name: name.value,
    description: description.value,
    preset: selectedPreset.value,
    frameworks: frameworksArray,
    platform: selectedPlatform.value
  }
  console.log('Form submitted with data:', formObject)
}
</script>
