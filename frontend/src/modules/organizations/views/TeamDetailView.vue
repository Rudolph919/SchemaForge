<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { teamsApi } from '@/modules/organizations/api/teamsApi'
import { organizationsApi } from '@/modules/organizations/api/organizationsApi'
import { ApiError } from '@/shared/api/httpClient'
import type { TeamDetailResponse } from '@/types/teams'
import type { OrganizationMemberResponse } from '@/types/organizations'

const route = useRoute()
const router = useRouter()
const teamId = computed(() => route.params.id as string)

const team = ref<TeamDetailResponse | null>(null)
const orgMembers = ref<OrganizationMemberResponse[]>([])
const loadError = ref<string | null>(null)
const actionError = ref<string | null>(null)
const selectedUserId = ref('')

// Whoever is an active org member and isn't already on this team - the only people
// AddTeamMemberCommand will actually accept (Step 3's cross-aggregate check).
const addableMembers = computed(() => {
  if (!team.value) return []
  const currentMemberIds = new Set(team.value.members.map((m) => m.userId))
  return orgMembers.value.filter((m) => m.status === 'Active' && !currentMemberIds.has(m.userId))
})

function memberDisplayName(userId: string): string {
  return orgMembers.value.find((m) => m.userId === userId)?.displayName ?? userId
}

async function load() {
  loadError.value = null
  try {
    const [teamResponse, membersResponse] = await Promise.all([
      teamsApi.getTeam(teamId.value),
      organizationsApi.listMembers(),
    ])
    team.value = teamResponse
    orgMembers.value = membersResponse
  } catch (error) {
    loadError.value = error instanceof ApiError ? error.message : 'Could not load team.'
  }
}

async function handleAddMember() {
  if (!selectedUserId.value) return
  actionError.value = null
  try {
    await teamsApi.addTeamMember(teamId.value, { userId: selectedUserId.value })
    selectedUserId.value = ''
    await load()
  } catch (error) {
    actionError.value = error instanceof ApiError ? error.message : 'Could not add member.'
  }
}

async function handleRemoveMember(userId: string) {
  actionError.value = null
  try {
    await teamsApi.removeTeamMember(teamId.value, userId)
    await load()
  } catch (error) {
    actionError.value = error instanceof ApiError ? error.message : 'Could not remove member.'
  }
}

onMounted(load)
</script>

<template>
  <div>
    <button type="button" class="text-sm text-slate-500 hover:text-slate-700" @click="router.push('/teams')">
      ← Back to Teams
    </button>

    <p v-if="loadError" class="mt-4 text-sm text-red-600">{{ loadError }}</p>

    <template v-if="team">
      <div class="mt-4 rounded-lg border border-slate-200 bg-white p-6">
        <h1 class="text-lg font-semibold text-slate-900">{{ team.name }}</h1>
        <p v-if="team.description" class="mt-1 text-sm text-slate-500">{{ team.description }}</p>
      </div>

      <div class="mt-6 rounded-lg border border-slate-200 bg-white p-6">
        <h2 class="text-base font-semibold text-slate-900">Members</h2>

        <div class="mt-4 flex gap-2">
          <select
            v-model="selectedUserId"
            class="flex-1 rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
          >
            <option value="" disabled>Add an organization member…</option>
            <option v-for="member in addableMembers" :key="member.userId" :value="member.userId">
              {{ member.displayName }} ({{ member.email }})
            </option>
          </select>
          <button
            type="button"
            :disabled="!selectedUserId"
            class="rounded-md bg-slate-900 px-3 py-2 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
            @click="handleAddMember"
          >
            Add
          </button>
        </div>
        <p v-if="actionError" class="mt-2 text-sm text-red-600">{{ actionError }}</p>

        <p v-if="team.members.length === 0" class="mt-4 text-sm text-slate-500">No members yet.</p>
        <ul v-else class="mt-4 divide-y divide-slate-100">
          <li v-for="member in team.members" :key="member.userId" class="flex items-center justify-between py-2 text-sm">
            <span class="text-slate-900">{{ memberDisplayName(member.userId) }}</span>
            <button type="button" class="text-slate-400 hover:text-red-600" @click="handleRemoveMember(member.userId)">
              Remove
            </button>
          </li>
        </ul>
      </div>
    </template>
  </div>
</template>
