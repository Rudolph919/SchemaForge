<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { projectsApi } from '@/modules/workspaces/api/projectsApi'
import { ApiError } from '@/shared/api/httpClient'
import Modal from '@/shared/components/Modal.vue'
import type { ProjectSummaryResponse } from '@/types/projects'

const projects = ref<ProjectSummaryResponse[]>([])
const isLoading = ref(true)
const loadError = ref<string | null>(null)

const isCreateOpen = ref(false)
const name = ref('')
const description = ref('')
const createError = ref<string | null>(null)
const isSubmitting = ref(false)

async function loadProjects() {
  isLoading.value = true
  loadError.value = null
  try {
    projects.value = await projectsApi.listProjects()
  } catch (error) {
    loadError.value = error instanceof ApiError ? error.message : 'Could not load projects.'
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
    await projectsApi.createProject({ name: name.value, description: description.value || null })
    isCreateOpen.value = false
    await loadProjects()
  } catch (error) {
    createError.value = error instanceof ApiError ? error.message : 'Could not create project.'
  } finally {
    isSubmitting.value = false
  }
}

onMounted(loadProjects)
</script>

<template>
  <div>
    <div class="flex items-center justify-between">
      <h1 class="text-lg font-semibold text-slate-900">Projects</h1>
      <button
        type="button"
        class="rounded-md bg-slate-900 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-800"
        @click="openCreate"
      >
        New Project
      </button>
    </div>

    <p v-if="loadError" class="mt-4 text-sm text-red-600">{{ loadError }}</p>
    <p v-else-if="isLoading" class="mt-4 text-sm text-slate-500">Loading…</p>
    <p v-else-if="projects.length === 0" class="mt-4 text-sm text-slate-500">
      No projects yet. Create one to get started.
    </p>

    <ul v-else class="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
      <li v-for="project in projects" :key="project.id">
        <router-link
          :to="`/projects/${project.id}`"
          class="block h-full rounded-lg border border-slate-200 bg-white p-4 hover:border-slate-300 hover:shadow-sm"
        >
          <div class="flex items-start justify-between">
            <h2 class="font-medium text-slate-900">{{ project.name }}</h2>
            <span
              class="rounded-full px-2 py-0.5 text-xs font-medium"
              :class="project.status === 'Active' ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-100 text-slate-600'"
            >
              {{ project.status }}
            </span>
          </div>
          <p v-if="project.description" class="mt-1 text-sm text-slate-500">{{ project.description }}</p>
        </router-link>
      </li>
    </ul>

    <Modal v-if="isCreateOpen" title="New Project" @close="isCreateOpen = false">
      <form class="space-y-4" @submit.prevent="handleCreate">
        <div>
          <label for="project-name" class="block text-sm font-medium text-slate-700">Name</label>
          <input
            id="project-name"
            v-model="name"
            type="text"
            required
            class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
          />
        </div>
        <div>
          <label for="project-description" class="block text-sm font-medium text-slate-700">
            Description <span class="text-slate-400">(optional)</span>
          </label>
          <textarea
            id="project-description"
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
