<script setup lang="ts">
import { onMounted, onUnmounted, ref, useId } from 'vue'

defineProps<{ title: string }>()
const emit = defineEmits<{ close: [] }>()

const titleId = useId()
const dialogRef = ref<HTMLElement | null>(null)
let previouslyFocused: HTMLElement | null = null

function focusableElements(): HTMLElement[] {
  if (!dialogRef.value) return []
  return Array.from(
    dialogRef.value.querySelectorAll<HTMLElement>(
      'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])',
    ),
  )
}

function handleKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    emit('close')
    return
  }

  // Basic focus trap: Tab/Shift+Tab wraps within the dialog instead of escaping to the page
  // behind the backdrop.
  if (event.key !== 'Tab') return
  const focusable = focusableElements()
  if (focusable.length === 0) return

  const first = focusable[0]!
  const last = focusable[focusable.length - 1]!

  if (event.shiftKey && document.activeElement === first) {
    event.preventDefault()
    last.focus()
  } else if (!event.shiftKey && document.activeElement === last) {
    event.preventDefault()
    first.focus()
  }
}

onMounted(() => {
  previouslyFocused = document.activeElement as HTMLElement | null
  document.addEventListener('keydown', handleKeydown)
  // Focus the first focusable control inside the dialog (falls back to the dialog itself) so
  // keyboard/screen-reader users land inside it immediately, not on whatever was behind it.
  const target = focusableElements()[0] ?? dialogRef.value
  target?.focus()
})

onUnmounted(() => {
  document.removeEventListener('keydown', handleKeydown)
  previouslyFocused?.focus()
})
</script>

<template>
  <div class="fixed inset-0 z-20 flex items-center justify-center bg-slate-900/40 px-4">
    <div
      ref="dialogRef"
      role="dialog"
      aria-modal="true"
      :aria-labelledby="titleId"
      tabindex="-1"
      class="w-full max-w-md rounded-lg bg-white p-6 shadow-xl"
    >
      <div class="flex items-center justify-between">
        <h2 :id="titleId" class="text-base font-semibold text-slate-900">{{ title }}</h2>
        <button
          type="button"
          aria-label="Close"
          class="text-slate-400 hover:text-slate-600"
          @click="emit('close')"
        >
          ✕
        </button>
      </div>
      <div class="mt-4">
        <slot />
      </div>
    </div>
  </div>
</template>
