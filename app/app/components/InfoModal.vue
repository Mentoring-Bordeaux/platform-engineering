<template>
  <UModal
    :title="title || 'Error'"
    :ui="{ footer: 'justify-end' }"
  >
    <template #body>
      <p>
        {{ body || 'An unexpected error has occurred' }}
      </p>
    </template>

    <template #footer="{ close }">
      <UButton
        label="Close"
        color="neutral"
        class="cursor-pointer"
        @click="close"
      />
    </template>
  </UModal>
</template>

<script setup lang="ts">
defineProps<{
  title?: string
  body?: string
}>()
</script>
