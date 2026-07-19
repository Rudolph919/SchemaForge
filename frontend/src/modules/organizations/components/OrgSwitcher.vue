<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { organizationsApi } from '@/modules/organizations/api/organizationsApi'
import type { MembershipResponse } from '@/types/organizations'

const authStore = useAuthStore()

const memberships = ref<MembershipResponse[]>([])
const isOpen = ref(false)
const isSwitching = ref(false)
const errorMessage = ref<string | null>(null)

const activeMemberships = computed(() => memberships.value.filter((m) => m.status === 'Active'))
const invitedMemberships = computed(() => memberships.value.filter((m) => m.status === 'Invited'))

const currentOrgName = computed(
  () => activeMemberships.value.find((m) => m.organizationId === authStore.organizationId)?.organizationName ?? '…',
)

async function loadMemberships() {
  try {
    memberships.value = await organizationsApi.listMyMemberships()
  } catch {
    // The switcher degrades to just showing the current org name if this fails - not worth a
    // blocking error state for a background convenience list.
  }
}

async function handleSwitch(organizationId: string) {
  if (organizationId === authStore.organizationId) {
    isOpen.value = false
    return
  }

  errorMessage.value = null
  isSwitching.value = true
  try {
    await authStore.switchOrganization(organizationId)
    isOpen.value = false
    window.location.href = '/'
  } catch {
    errorMessage.value = 'Could not switch organizations.'
  } finally {
    isSwitching.value = false
  }
}

async function handleAccept(membershipId: string) {
  errorMessage.value = null
  try {
    await organizationsApi.acceptInvitation(membershipId)
    await loadMemberships()
  } catch {
    errorMessage.value = 'Could not accept invitation.'
  }
}

onMounted(loadMemberships)
</script>

<template>
  <div class="relative">
    <button
      type="button"
      class="flex items-center gap-2 rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50"
      @click="isOpen = !isOpen"
    >
      {{ currentOrgName }}
      <span v-if="invitedMemberships.length > 0" class="rounded-full bg-amber-100 px-1.5 py-0.5 text-xs text-amber-800">
        {{ invitedMemberships.length }}
      </span>
      <svg class="size-4 text-slate-400" viewBox="0 0 20 20" fill="currentColor">
        <path
          fill-rule="evenodd"
          d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z"
          clip-rule="evenodd"
        />
      </svg>
    </button>

    <div
      v-if="isOpen"
      class="absolute left-0 z-10 mt-2 w-72 rounded-md border border-slate-200 bg-white py-1 shadow-lg"
    >
      <p class="px-3 pb-1 pt-2 text-xs font-medium uppercase tracking-wide text-slate-400">Organizations</p>
      <button
        v-for="membership in activeMemberships"
        :key="membership.membershipId"
        type="button"
        :disabled="isSwitching"
        class="flex w-full items-center justify-between px-3 py-2 text-left text-sm text-slate-700 hover:bg-slate-50 disabled:opacity-50"
        @click="handleSwitch(membership.organizationId)"
      >
        <span>{{ membership.organizationName }}</span>
        <span v-if="membership.organizationId === authStore.organizationId" class="text-xs text-slate-400">current</span>
      </button>

      <template v-if="invitedMemberships.length > 0">
        <p class="px-3 pb-1 pt-3 text-xs font-medium uppercase tracking-wide text-slate-400">Pending invitations</p>
        <div
          v-for="membership in invitedMemberships"
          :key="membership.membershipId"
          class="flex items-center justify-between px-3 py-2 text-sm text-slate-700"
        >
          <span>{{ membership.organizationName }}</span>
          <button
            type="button"
            class="rounded-md border border-slate-300 px-2 py-1 text-xs font-medium hover:bg-slate-50"
            @click="handleAccept(membership.membershipId)"
          >
            Accept
          </button>
        </div>
      </template>

      <p v-if="errorMessage" class="px-3 pt-2 text-xs text-red-600">{{ errorMessage }}</p>
    </div>
  </div>
</template>
