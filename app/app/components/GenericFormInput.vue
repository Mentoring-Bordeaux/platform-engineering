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
  <USwitch
    v-else-if="configOption.type === 'boolean'"
    :required="configOption.required || false"
  />
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
