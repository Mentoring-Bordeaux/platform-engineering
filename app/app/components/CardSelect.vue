<template>
  <div
    class="focus-visible:outline-primary transition-outline rounded-lg duration-150 ease-in-out focus-visible:outline-2 focus-visible:outline-offset-4"
    tabindex="0"
    role="button"
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
const { icon, title, isSelected } = defineProps<{
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

function onKeydown(event: KeyboardEvent) {
  // Enter or Space: prevent default and select
  if (event.key === 'Enter' || event.key === ' ') {
    event.preventDefault()
    onSelect()
    return
  }
}
</script>
