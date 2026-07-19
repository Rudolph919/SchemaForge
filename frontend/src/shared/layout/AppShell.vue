<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import OrgSwitcher from '@/modules/organizations/components/OrgSwitcher.vue'

const router = useRouter()
const authStore = useAuthStore()

const navLinks = [
  { to: '/projects', label: 'Projects' },
  { to: '/teams', label: 'Teams' },
  { to: '/members', label: 'Members' },
  { to: '/settings', label: 'Settings' },
]

function handleLogout() {
  authStore.logout()
  router.push('/login')
}
</script>

<template>
  <div class="min-h-svh bg-slate-50">
    <header class="border-b border-slate-200 bg-white">
      <div class="mx-auto flex max-w-6xl items-center justify-between px-4 py-3">
        <div class="flex items-center gap-6">
          <span class="text-sm font-semibold tracking-tight text-slate-900">SchemaForge</span>
          <nav class="flex items-center gap-1">
            <router-link
              v-for="link in navLinks"
              :key="link.to"
              :to="link.to"
              class="rounded-md px-3 py-1.5 text-sm font-medium text-slate-600 hover:bg-slate-100 hover:text-slate-900"
              active-class="bg-slate-100 text-slate-900"
            >
              {{ link.label }}
            </router-link>
          </nav>
        </div>

        <div class="flex items-center gap-3">
          <OrgSwitcher />
          <span class="text-sm text-slate-600">{{ authStore.displayName }}</span>
          <button
            type="button"
            class="rounded-md border border-slate-300 px-2.5 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50"
            @click="handleLogout"
          >
            Log out
          </button>
        </div>
      </div>
    </header>

    <main class="mx-auto max-w-6xl px-4 py-8">
      <router-view />
    </main>
  </div>
</template>
