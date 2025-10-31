<template>
  <u-page-card
    :icon="icon"
    :title="title"
    :class="styleClasses"
    @click="onSelect"
  />
</template>

<script setup lang="ts">
const { icon, title, isSelected } = defineProps<{
  icon: string
  title: string
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
