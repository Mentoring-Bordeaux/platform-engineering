<template>
  <UInput
    v-if="
      configOption.type === 'text' ||
      configOption.type === 'number' ||
      configOption.type === 'password'
    "
    :type="configOption.type"
    :required="configOption.required || false"
  />
  <UTextarea
    v-else-if="configOption.type === 'textarea'"
    :required="configOption.required || false"
  />
  <div
    v-else-if="configOption.type === 'boolean'"
    class="flex w-full items-center justify-between pb-2"
  >
    <label class="mr-4">{{ configOption.description }}</label>
    <USwitch :required="configOption.required || false" />
  </div>
  <USelect
    v-else-if="configOption.type === 'enum'"
    :required="configOption.required || false"
    :items="configOption.values"
  />
  <p v-else>Unsupported input type: {{ configOption.type }}</p>
</template>

<script setup lang="ts">
import type { Field } from '~/types'

defineProps<{
  configOption: Field
}>()
</script>
