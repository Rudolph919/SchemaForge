<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { organizationsApi } from '@/modules/organizations/api/organizationsApi'
import { ApiError } from '@/shared/api/httpClient'
import { useIdempotencyKey } from '@/shared/api/idempotencyKey'
import Modal from '@/shared/components/Modal.vue'
import type { OrganizationMemberResponse, OrganizationRole } from '@/types/organizations'

const ROLES: OrganizationRole[] = ['Owner', 'Admin', 'Member']

const members = ref<OrganizationMemberResponse[]>([])
const isLoading = ref(true)
const loadError = ref<string | null>(null)
const actionError = ref<string | null>(null)

const isInviteOpen = ref(false)
const inviteEmail = ref('')
const inviteRole = ref<OrganizationRole>('Member')
const inviteError = ref<string | null>(null)
const isSubmitting = ref(false)
const inviteMemberKey = useIdempotencyKey()

async function loadMembers() {
  isLoading.value = true
  loadError.value = null
  try {
    members.value = await organizationsApi.listMembers()
  } catch (error) {
    loadError.value = error instanceof ApiError ? error.message : 'Could not load members.'
  } finally {
    isLoading.value = false
  }
}

function openInvite() {
  inviteEmail.value = ''
  inviteRole.value = 'Member'
  inviteError.value = null
  inviteMemberKey.reset()
  isInviteOpen.value = true
}

async function handleInvite() {
  inviteError.value = null
  isSubmitting.value = true
  try {
    await organizationsApi.inviteMember({ email: inviteEmail.value, role: inviteRole.value }, inviteMemberKey.get())
    inviteMemberKey.reset()
    isInviteOpen.value = false
    await loadMembers()
  } catch (error) {
    inviteError.value = error instanceof ApiError ? error.message : 'Could not send invitation.'
  } finally {
    isSubmitting.value = false
  }
}

async function handleRoleChange(membershipId: string, newRole: OrganizationRole) {
  actionError.value = null
  try {
    await organizationsApi.changeMemberRole(membershipId, { newRole })
    await loadMembers()
  } catch (error) {
    actionError.value = error instanceof ApiError ? error.message : 'Could not change role.'
  }
}

async function handleRevoke(membershipId: string) {
  actionError.value = null
  try {
    await organizationsApi.revokeMember(membershipId)
    await loadMembers()
  } catch (error) {
    actionError.value = error instanceof ApiError ? error.message : 'Could not revoke membership.'
  }
}

onMounted(loadMembers)
</script>

<template>
  <div>
    <div class="flex items-center justify-between">
      <h1 class="text-lg font-semibold text-slate-900">Members</h1>
      <button
        type="button"
        class="rounded-md bg-slate-900 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-800"
        @click="openInvite"
      >
        Invite Member
      </button>
    </div>

    <p v-if="loadError" class="mt-4 text-sm text-red-600">{{ loadError }}</p>
    <p v-else-if="isLoading" class="mt-4 text-sm text-slate-500">Loading…</p>

    <div v-else class="mt-4 overflow-hidden rounded-lg border border-slate-200 bg-white">
      <p v-if="actionError" class="border-b border-red-100 bg-red-50 px-4 py-2 text-sm text-red-600">
        {{ actionError }}
      </p>
      <table class="w-full text-sm">
        <thead>
          <tr class="border-b border-slate-200 text-left text-slate-500">
            <th class="px-4 py-2 font-medium">Name</th>
            <th class="px-4 py-2 font-medium">Email</th>
            <th class="px-4 py-2 font-medium">Role</th>
            <th class="px-4 py-2 font-medium">Status</th>
            <th class="px-4 py-2"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="member in members" :key="member.membershipId" class="border-b border-slate-100 last:border-0">
            <td class="px-4 py-2 text-slate-900">{{ member.displayName }}</td>
            <td class="px-4 py-2 text-slate-500">{{ member.email }}</td>
            <td class="px-4 py-2">
              <select
                :value="member.role"
                class="rounded-md border border-slate-300 px-2 py-1 text-sm focus:border-slate-500 focus:outline-none"
                @change="handleRoleChange(member.membershipId, ($event.target as HTMLSelectElement).value as OrganizationRole)"
              >
                <option v-for="role in ROLES" :key="role" :value="role">{{ role }}</option>
              </select>
            </td>
            <td class="px-4 py-2">
              <span
                class="rounded-full px-2 py-0.5 text-xs font-medium"
                :class="{
                  'bg-emerald-100 text-emerald-800': member.status === 'Active',
                  'bg-amber-100 text-amber-800': member.status === 'Invited',
                  'bg-slate-100 text-slate-600': member.status === 'Revoked',
                }"
              >
                {{ member.status }}
              </span>
            </td>
            <td class="px-4 py-2 text-right">
              <button
                v-if="member.status !== 'Revoked'"
                type="button"
                class="text-slate-400 hover:text-red-600"
                @click="handleRevoke(member.membershipId)"
              >
                Revoke
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <Modal v-if="isInviteOpen" title="Invite Member" @close="isInviteOpen = false">
      <form class="space-y-4" @submit.prevent="handleInvite">
        <div>
          <label for="invite-email" class="block text-sm font-medium text-slate-700">Email</label>
          <input
            id="invite-email"
            v-model="inviteEmail"
            type="email"
            required
            class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
          />
          <p class="mt-1 text-xs text-slate-400">They must already have a SchemaForge account.</p>
        </div>
        <div>
          <label for="invite-role" class="block text-sm font-medium text-slate-700">Role</label>
          <select
            id="invite-role"
            v-model="inviteRole"
            class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
          >
            <option v-for="role in ROLES" :key="role" :value="role">{{ role }}</option>
          </select>
        </div>
        <p v-if="inviteError" class="text-sm text-red-600">{{ inviteError }}</p>
        <button
          type="submit"
          :disabled="isSubmitting"
          class="w-full rounded-md bg-slate-900 px-3 py-2 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
        >
          {{ isSubmitting ? 'Sending…' : 'Send Invitation' }}
        </button>
      </form>
    </Modal>

    <p class="mt-2 text-xs text-slate-400">
      To accept an invitation to another organization, use the organization switcher above.
    </p>
  </div>
</template>
