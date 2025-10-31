<template>
  <u-page-card
    :title="title"
    :icon="icon"
    :description="description"
    :class="styleClasses"
    @click="onSelect"
  />
</template>

<script setup lang="ts">
const { icon, title, isSelected } = defineProps<{
  title: string
  icon?: string
  description?: string
  isSelected: boolean
}>()

const emits = defineEmits<{
  (e: 'select', isSelected: boolean): void
}>()

const styleClasses = computed(() => {
  const defaultClasses = 'border box-border cursor-pointer'
  return isSelected
    ? `${defaultClasses} border-primary`
    : `${defaultClasses} border-transparent`
})

const onSelect = () => {
  emits('select', isSelected)
}
</script>
