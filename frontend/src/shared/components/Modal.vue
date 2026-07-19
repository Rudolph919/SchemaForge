<script setup lang="ts">
defineProps<{ title: string }>()
const emit = defineEmits<{ close: [] }>()
</script>

<template>
  <div class="fixed inset-0 z-20 flex items-center justify-center bg-slate-900/40 px-4">
    <div class="w-full max-w-md rounded-lg bg-white p-6 shadow-xl">
      <div class="flex items-center justify-between">
        <h2 class="text-base font-semibold text-slate-900">{{ title }}</h2>
        <button type="button" class="text-slate-400 hover:text-slate-600" @click="emit('close')">✕</button>
      </div>
      <div class="mt-4">
        <slot />
      </div>
    </div>
  </div>
</template>
