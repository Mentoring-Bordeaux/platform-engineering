<template>
  <FormSection title="Preset">
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <u-page-card
        @click="onPresetSelect('nuxt-dotnet')"
        title="Nuxt + Dotnet"
        description="A starter template with Nuxt.js frontend and Dotnet backend."
      />
      <u-page-card
        @click="onPresetSelect('react-dotnet')"
        title="React + Dotnet"
        description="A starter template with React frontend and Dotnet backend."
      />
      <u-page-card
        @click="onPresetSelect('my-awesome-template')"
        title="My Awesome Template"
        description="An awesome template for your my project."
      />
      <u-page-card
        @click="onPresetSelect('react-nestjs')"
        title="React + NestJS"
        description="A starter template with React frontend and NestJS backend."
      />
    </div>
    <u-separator label="or" />
    <u-page-card
      title="Blank Template"
      description="Start from scratch with a blank template."
    />
  </FormSection>
</template>

<script setup lang="ts">
import FormSection from './FormSection.vue'

const onPresetSelect = (preset: string) => {
  console.log('Selected preset:', preset)
}
</script>
