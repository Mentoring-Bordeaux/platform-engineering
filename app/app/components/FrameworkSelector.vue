<template>
  <FormSection title="Framework Selection">
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <u-page-card
        icon="devicon:html5"
        @click="onFrameworkSelect('vanilla')"
        title="Vanilla"
      />
    </div>
  </FormSection>
</template>

<script setup lang="ts">
import FormSection from './FormSection.vue'

const onFrameworkSelect = (framework: string) => {
  console.log('Selected framework:', framework)
}
</script>
