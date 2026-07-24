<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { teamsApi } from '@/modules/organizations/api/teamsApi'
import { ApiError } from '@/shared/api/httpClient'
import { useIdempotencyKey } from '@/shared/api/idempotencyKey'
import Modal from '@/shared/components/Modal.vue'
import type { TeamSummaryResponse } from '@/types/teams'

const teams = ref<TeamSummaryResponse[]>([])
const isLoading = ref(true)
const loadError = ref<string | null>(null)

const isCreateOpen = ref(false)
const name = ref('')
const description = ref('')
const createError = ref<string | null>(null)
const isSubmitting = ref(false)
const createTeamKey = useIdempotencyKey()

async function loadTeams() {
  isLoading.value = true
  loadError.value = null
  try {
    teams.value = await teamsApi.listTeams()
  } catch (error) {
    loadError.value = error instanceof ApiError ? error.message : 'Could not load teams.'
  } finally {
    isLoading.value = false
  }
}

function openCreate() {
  name.value = ''
  description.value = ''
  createError.value = null
  createTeamKey.reset()
  isCreateOpen.value = true
}

async function handleCreate() {
  createError.value = null
  isSubmitting.value = true
  try {
    await teamsApi.createTeam({ name: name.value, description: description.value || null }, createTeamKey.get())
    createTeamKey.reset()
    isCreateOpen.value = false
    await loadTeams()
  } catch (error) {
    createError.value = error instanceof ApiError ? error.message : 'Could not create team.'
  } finally {
    isSubmitting.value = false
  }
}

onMounted(loadTeams)
</script>

<template>
  <div>
    <div class="flex items-center justify-between">
      <h1 class="text-lg font-semibold text-slate-900">Teams</h1>
      <button
        type="button"
        class="rounded-md bg-slate-900 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-800"
        @click="openCreate"
      >
        New Team
      </button>
    </div>

    <p v-if="loadError" class="mt-4 text-sm text-red-600">{{ loadError }}</p>
    <p v-else-if="isLoading" class="mt-4 text-sm text-slate-500">Loading…</p>
    <p v-else-if="teams.length === 0" class="mt-4 text-sm text-slate-500">No teams yet. Create one to get started.</p>

    <ul v-else class="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
      <li v-for="team in teams" :key="team.id">
        <router-link
          :to="`/teams/${team.id}`"
          class="block h-full rounded-lg border border-slate-200 bg-white p-4 hover:border-slate-300 hover:shadow-sm"
        >
          <h2 class="font-medium text-slate-900">{{ team.name }}</h2>
          <p v-if="team.description" class="mt-1 text-sm text-slate-500">{{ team.description }}</p>
          <p class="mt-2 text-xs text-slate-400">{{ team.memberCount }} member{{ team.memberCount === 1 ? '' : 's' }}</p>
        </router-link>
      </li>
    </ul>

    <Modal v-if="isCreateOpen" title="New Team" @close="isCreateOpen = false">
      <form class="space-y-4" @submit.prevent="handleCreate">
        <div>
          <label for="team-name" class="block text-sm font-medium text-slate-700">Name</label>
          <input
            id="team-name"
            v-model="name"
            type="text"
            required
            class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
          />
        </div>
        <div>
          <label for="team-description" class="block text-sm font-medium text-slate-700">
            Description <span class="text-slate-400">(optional)</span>
          </label>
          <textarea
            id="team-description"
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
