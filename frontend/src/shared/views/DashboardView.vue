<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()

function handleLogout() {
  authStore.logout()
  router.push('/login')
}
</script>

<template>
  <div class="min-h-svh bg-slate-50 px-4 py-8">
    <div class="mx-auto max-w-2xl rounded-lg border border-slate-200 bg-white p-8 shadow-sm">
      <h1 class="text-xl font-semibold text-slate-900">Welcome, {{ authStore.displayName }}</h1>
      <dl class="mt-4 space-y-1 text-sm text-slate-600">
        <div class="flex gap-2">
          <dt class="font-medium text-slate-700">User ID:</dt>
          <dd>{{ authStore.userId }}</dd>
        </div>
        <div class="flex gap-2">
          <dt class="font-medium text-slate-700">Organization ID:</dt>
          <dd>{{ authStore.organizationId }}</dd>
        </div>
      </dl>

      <button
        type="button"
        class="mt-6 rounded-md border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
        @click="handleLogout"
      >
        Log out
      </button>
    </div>
  </div>
</template>
