<template>
  <FormSection title="Platform Selection">
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <u-page-card
        icon="devicon:github"
        title="GitHub"
        @click="onPlatformSelect('my-awesome-template')"
      />
    </div>
  </FormSection>
</template>

<script setup lang="ts">
const onPlatformSelect = (platform: string) => {
  console.log('Selected platform:', platform)
}
</script>
