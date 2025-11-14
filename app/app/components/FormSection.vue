<template>
  <div class="flex flex-col gap-2 p-4">
    <h2>{{ props.title }}</h2>
    <slot />
  </div>
</template>

<script setup lang="ts">
const props = defineProps<{
  title: string
}>()
</script>
