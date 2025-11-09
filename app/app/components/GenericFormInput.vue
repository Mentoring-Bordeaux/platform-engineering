<template>
  <UInput
    v-if="
      configOption.type === 'text' ||
      configOption.type === 'number' ||
      configOption.type === 'password'
    "
    :type="configOption.type"
  />
  <UTextarea v-else-if="configOption.type === 'textarea'" />
  <USwitch v-else-if="configOption.type === 'boolean'" />
  <USelect
    v-else-if="configOption.type === 'enum'"
    :items="configOption.values"
  />
  <p v-else>Unsupported input type: {{ configOption.type }}</p>
</template>

<script setup lang="ts">
import type { ConfigOption } from '~/types'

defineProps<{
  configOption: ConfigOption
}>()
</script>
