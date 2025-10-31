<template>
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
</template>

<script setup lang="ts">
import CardSelect from './CardSelect.vue'
import { ref } from 'vue'

interface Platform {
  name: string
  icon: string
}

const platforms = {
  github: { name: 'GitHub', icon: 'devicon:github' },
  gitlab: { name: 'GitLab', icon: 'devicon:gitlab' }
} as const satisfies Record<string, Platform>

type PlatformKey = keyof typeof platforms

const selectedPlatform = ref<PlatformKey | null>(null)

const onPlatformSelect = (key: PlatformKey) => {
  // console.log('Selected platform:', platforms[key].name)
  selectedPlatform.value = key
}
</script>
