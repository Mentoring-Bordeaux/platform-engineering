<template>
  <div class="p-4 gap-2 flex flex-col">
    <h1>{{ props.title }}</h1>
    <slot />
  </div>
</template>

<script setup lang="ts">
const props = defineProps<{
  title: string
}>()
</script>
