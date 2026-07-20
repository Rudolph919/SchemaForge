<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { schemaDefinitionsApi } from '@/modules/schemas/api/schemaDefinitionsApi'
import { ApiError } from '@/shared/api/httpClient'
import Modal from '@/shared/components/Modal.vue'
import type { SchemaDefinitionSummaryResponse } from '@/types/schemas'

const route = useRoute()
const router = useRouter()
const projectId = computed(() => route.params.projectId as string)

const schemas = ref<SchemaDefinitionSummaryResponse[]>([])
const isLoading = ref(true)
const loadError = ref<string | null>(null)

const isCreateOpen = ref(false)
const name = ref('')
const description = ref('')
const createError = ref<string | null>(null)
const isSubmitting = ref(false)

async function loadSchemas() {
  isLoading.value = true
  loadError.value = null
  try {
    schemas.value = await schemaDefinitionsApi.listSchemas(projectId.value)
  } catch (error) {
    loadError.value = error instanceof ApiError ? error.message : 'Could not load schemas.'
  } finally {
    isLoading.value = false
  }
}

function openCreate() {
  name.value = ''
  description.value = ''
  createError.value = null
  isCreateOpen.value = true
}

async function handleCreate() {
  createError.value = null
  isSubmitting.value = true
  try {
    await schemaDefinitionsApi.createSchema(projectId.value, {
      name: name.value,
      description: description.value || null,
    })
    isCreateOpen.value = false
    await loadSchemas()
  } catch (error) {
    createError.value = error instanceof ApiError ? error.message : 'Could not create schema.'
  } finally {
    isSubmitting.value = false
  }
}

onMounted(loadSchemas)
</script>

<template>
  <div>
    <button
      type="button"
      class="text-sm text-slate-500 hover:text-slate-700"
      @click="router.push(`/projects/${projectId}`)"
    >
      ← Back to Project
    </button>

    <div class="mt-4 flex items-center justify-between">
      <h1 class="text-lg font-semibold text-slate-900">Schemas</h1>
      <button
        type="button"
        class="rounded-md bg-slate-900 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-800"
        @click="openCreate"
      >
        New Schema
      </button>
    </div>

    <p v-if="loadError" class="mt-4 text-sm text-red-600">{{ loadError }}</p>
    <p v-else-if="isLoading" class="mt-4 text-sm text-slate-500">Loading…</p>
    <p v-else-if="schemas.length === 0" class="mt-4 text-sm text-slate-500">
      No schemas yet. Create one to get started.
    </p>

    <ul v-else class="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
      <li v-for="schema in schemas" :key="schema.id">
        <router-link
          :to="`/schemas/${schema.id}`"
          class="block h-full rounded-lg border border-slate-200 bg-white p-4 hover:border-slate-300 hover:shadow-sm"
        >
          <h2 class="font-medium text-slate-900">{{ schema.name }}</h2>
          <p v-if="schema.description" class="mt-1 text-sm text-slate-500">{{ schema.description }}</p>
          <div v-if="schema.tags.length > 0" class="mt-2 flex flex-wrap gap-1">
            <span
              v-for="tag in schema.tags"
              :key="tag"
              class="rounded-full bg-slate-100 px-2 py-0.5 text-xs text-slate-600"
            >
              {{ tag }}
            </span>
          </div>
        </router-link>
      </li>
    </ul>

    <Modal v-if="isCreateOpen" title="New Schema" @close="isCreateOpen = false">
      <form class="space-y-4" @submit.prevent="handleCreate">
        <div>
          <label for="schema-name" class="block text-sm font-medium text-slate-700">Name</label>
          <input
            id="schema-name"
            v-model="name"
            type="text"
            required
            class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
          />
        </div>
        <div>
          <label for="schema-description" class="block text-sm font-medium text-slate-700">
            Description <span class="text-slate-400">(optional)</span>
          </label>
          <textarea
            id="schema-description"
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
