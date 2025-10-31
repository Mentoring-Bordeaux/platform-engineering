<template>
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
</template>

<script setup lang="ts">
import { ref } from 'vue'
import FormSection from './FormSection.vue'

interface Framework {
  name: string
  icon: string
}

const frameworks = {
  html5: { name: 'Vanilla', icon: 'devicon:html5' },
  vue: { name: 'Vue.js', icon: 'devicon:vuejs' },
  react: { name: 'React', icon: 'devicon:react' }
} as const satisfies Record<string, Framework>

type FrameworkKey = keyof typeof frameworks

const selectedFramework = ref(new Set<FrameworkKey>())

const onFrameworkSelect = (key: FrameworkKey) => {
  // console.log('Selected framework:', frameworks[key].name)
  if (selectedFramework.value.has(key)) {
    selectedFramework.value.delete(key)
  } else {
    selectedFramework.value.add(key)
  }
}
</script>
