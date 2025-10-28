<template>
  <div class="flex flex-col items-center w-full py-8">
    <div class="max-w-4xl w-full">
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
      <PresetSelector />
      <FrameworkSelector />
      <PlatformSelector />
      <div class="w-full flex justify-end">
        <u-button
          icon="i-lucide-cog"
          size="md"
          variant="solid"
          class="cursor-pointer"
        >
          Configure project
        </u-button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'

const name = ref('')
const description = ref('')
</script>
