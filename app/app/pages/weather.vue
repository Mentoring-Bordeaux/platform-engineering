<template>
  <div>
    <UPageHero
      title="First Page - Platform Engineering"
      description="Example to wait the mockup of the homepage"
    >
      <UButton color="primary" size="md" class="mt-6" @click="askTheWeather">
        I want to know the weather!
      </UButton>
      {{ weatherAnswer }}
    </UPageHero>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'

const config = useRuntimeConfig()

const weatherAnswer = ref('')

async function askTheWeather() {
  try {
    const apiUrl = config.public.apiBase + '/weatherforecast'
    const weatherData = await $fetch(apiUrl)

    weatherAnswer.value = JSON.stringify(weatherData, null, 2)
  } catch (error) {
    console.error('Error fetching weather data:', error)
    alert('Failed to fetch weather data. Check console for details.')
  }
}
</script>
