<template>
  <FormSection title="Preset">
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <CardSelect
        v-for="(preset, key) in presets"
        :key="key"
        :title="preset.name"
        :description="preset.description"
        :is-selected="selectedPreset === key"
        @click="onPresetSelect(key)"
      />
    </div>
    <u-separator label="or" />
    <CardSelect
      :is-selected="selectedPreset === 'blank'"
      title="Blank Template"
      description="Use your own custom template repository."
      @click="onPresetSelect('blank')"
    />
  </FormSection>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import FormSection from './FormSection.vue'

interface Preset {
  name: string
  description: string
}

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
  // console.log('Selected preset:', presets[key].name)
  selectedPreset.value = key
}
</script>
