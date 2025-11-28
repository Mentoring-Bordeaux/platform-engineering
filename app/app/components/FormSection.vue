<template>
  <div class="flex flex-col gap-2 p-4">
    <h2 class="mb-3 text-3xl font-semibold">{{ props.title }}</h2>
    <slot />
  </div>
</template>

<script setup lang="ts">
const props = defineProps<{
  title: string
}>()
</script>
