<template>
  <div class="flex flex-col gap-2 p-4">
    <h1>{{ props.title }}</h1>
    <slot />
  </div>
</template>

<script setup lang="ts">
const props = defineProps<{
  title: string
}>()
</script>
