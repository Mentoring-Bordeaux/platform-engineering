<template>
  <div
    class="focus-visible:outline-primary transition-outline cursor-pointer rounded-lg duration-150 ease-in-out focus-visible:outline-2 focus-visible:outline-offset-4"
    tabindex="0"
    @keydown="onKeydown"
  >
    <u-page-card
      :title="title"
      :icon="icon"
      :description="description"
      :highlight="isSelected"
      class="flex h-full flex-1"
      @click.stop="onSelect"
    />
  </div>
</template>

<script setup lang="ts">
defineProps<{
  title: string
  icon?: string
  description?: string
  isSelected: boolean
}>()

const emit = defineEmits<{
  select: []
}>()

const onSelect = () => {
  emit('select')
}


// Permit selection via keyboard with Tab + Enter/Space
function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Enter' || event.key === ' ') {
    event.preventDefault()
    onSelect()
    return
  }
}
</script>
