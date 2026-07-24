<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { componentsApi } from '@/modules/components-library/api/componentsApi'
import { ApiError } from '@/shared/api/httpClient'
import { useIdempotencyKey } from '@/shared/api/idempotencyKey'
import Modal from '@/shared/components/Modal.vue'
import type { ComponentDefinitionSummaryResponse } from '@/types/components'

const components = ref<ComponentDefinitionSummaryResponse[]>([])
const isLoading = ref(true)
const loadError = ref<string | null>(null)

const isCreateOpen = ref(false)
const name = ref('')
const description = ref('')
const createError = ref<string | null>(null)
const isSubmitting = ref(false)
const createComponentKey = useIdempotencyKey()

async function loadComponents() {
  isLoading.value = true
  loadError.value = null
  try {
    components.value = await componentsApi.listComponents()
  } catch (error) {
    loadError.value = error instanceof ApiError ? error.message : 'Could not load components.'
  } finally {
    isLoading.value = false
  }
}

function openCreate() {
  name.value = ''
  description.value = ''
  createError.value = null
  createComponentKey.reset()
  isCreateOpen.value = true
}

async function handleCreate() {
  createError.value = null
  isSubmitting.value = true
  try {
    await componentsApi.createComponent(
      { name: name.value, description: description.value || null },
      createComponentKey.get(),
    )
    createComponentKey.reset()
    isCreateOpen.value = false
    await loadComponents()
  } catch (error) {
    createError.value = error instanceof ApiError ? error.message : 'Could not create component.'
  } finally {
    isSubmitting.value = false
  }
}

onMounted(loadComponents)
</script>

<template>
  <div>
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-lg font-semibold text-slate-900">Components</h1>
        <p class="mt-1 text-sm text-slate-500">Reusable schema fragments shared across every project in this organization.</p>
      </div>
      <button
        type="button"
        class="rounded-md bg-slate-900 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-800"
        @click="openCreate"
      >
        New Component
      </button>
    </div>

    <p v-if="loadError" class="mt-4 text-sm text-red-600">{{ loadError }}</p>
    <p v-else-if="isLoading" class="mt-4 text-sm text-slate-500">Loading…</p>
    <p v-else-if="components.length === 0" class="mt-4 text-sm text-slate-500">
      No components yet. Create one to get started.
    </p>

    <ul v-else class="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
      <li v-for="component in components" :key="component.id">
        <router-link
          :to="`/components/${component.id}`"
          class="block h-full rounded-lg border border-slate-200 bg-white p-4 hover:border-slate-300 hover:shadow-sm"
        >
          <h2 class="font-medium text-slate-900">{{ component.name }}</h2>
          <p v-if="component.description" class="mt-1 text-sm text-slate-500">{{ component.description }}</p>
        </router-link>
      </li>
    </ul>

    <Modal v-if="isCreateOpen" title="New Component" @close="isCreateOpen = false">
      <form class="space-y-4" @submit.prevent="handleCreate">
        <div>
          <label for="component-name" class="block text-sm font-medium text-slate-700">Name</label>
          <input
            id="component-name"
            v-model="name"
            type="text"
            required
            class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
          />
        </div>
        <div>
          <label for="component-description" class="block text-sm font-medium text-slate-700">
            Description <span class="text-slate-400">(optional)</span>
          </label>
          <textarea
            id="component-description"
            v-model="description"
            rows="3"
            class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
          />
        </div>
        <p v-if="createError" class="text-sm text-red-600">{{ createError }}</p>
        <button
          type="submit"
          :disabled="isSubmitting"
          class="w-full rounded-md bg-slate-900 px-3 py-2 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
        >
          {{ isSubmitting ? 'Creating…' : 'Create' }}
        </button>
      </form>
    </Modal>
  </div>
</template>
