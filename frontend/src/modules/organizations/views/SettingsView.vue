<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { organizationsApi } from '@/modules/organizations/api/organizationsApi'
import { ApiError } from '@/shared/api/httpClient'
import type { MembershipResponse } from '@/types/organizations'

const authStore = useAuthStore()

const memberships = ref<MembershipResponse[]>([])
const loadError = ref<string | null>(null)

// No dedicated "get current organization" endpoint exists yet - the membership list already
// carries the organization's name/slug for every org the caller belongs to, current one included.
const currentOrg = computed(() => memberships.value.find((m) => m.organizationId === authStore.organizationId))

onMounted(async () => {
  try {
    memberships.value = await organizationsApi.listMyMemberships()
  } catch (error) {
    loadError.value = error instanceof ApiError ? error.message : 'Could not load organization details.'
  }
})
</script>

<template>
  <div>
    <h1 class="text-lg font-semibold text-slate-900">Settings</h1>

    <p v-if="loadError" class="mt-4 text-sm text-red-600">{{ loadError }}</p>

    <div class="mt-4 rounded-lg border border-slate-200 bg-white p-6">
      <h2 class="text-base font-semibold text-slate-900">Organization</h2>
      <dl v-if="currentOrg" class="mt-4 space-y-2 text-sm">
        <div class="flex gap-2">
          <dt class="w-28 shrink-0 font-medium text-slate-700">Name</dt>
          <dd class="text-slate-600">{{ currentOrg.organizationName }}</dd>
        </div>
        <div class="flex gap-2">
          <dt class="w-28 shrink-0 font-medium text-slate-700">Slug</dt>
          <dd class="text-slate-600">{{ currentOrg.organizationSlug }}</dd>
        </div>
        <div class="flex gap-2">
          <dt class="w-28 shrink-0 font-medium text-slate-700">Your role</dt>
          <dd class="text-slate-600">{{ currentOrg.role }}</dd>
        </div>
      </dl>
    </div>

    <div class="mt-6 rounded-lg border border-slate-200 bg-white p-6">
      <h2 class="text-base font-semibold text-slate-900">Account</h2>
      <dl class="mt-4 space-y-2 text-sm">
        <div class="flex gap-2">
          <dt class="w-28 shrink-0 font-medium text-slate-700">Name</dt>
          <dd class="text-slate-600">{{ authStore.displayName }}</dd>
        </div>
        <div class="flex gap-2">
          <dt class="w-28 shrink-0 font-medium text-slate-700">User ID</dt>
          <dd class="text-slate-600">{{ authStore.userId }}</dd>
        </div>
      </dl>
    </div>
  </div>
</template>
