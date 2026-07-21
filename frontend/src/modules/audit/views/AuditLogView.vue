<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { auditLogApi } from '@/modules/audit/api/auditLogApi'
import { ApiError } from '@/shared/api/httpClient'
import type { AuditLogEntryResponse } from '@/types/audit'

const PAGE_SIZE = 25

const entries = ref<AuditLogEntryResponse[]>([])
const totalCount = ref(0)
const page = ref(1)
const loadError = ref<string | null>(null)
const isLoading = ref(false)

const filterEntityType = ref('')
const filterEntityId = ref('')
const filterActorUserId = ref('')
const filterFromDate = ref('')
const filterToDate = ref('')

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / PAGE_SIZE)))

async function load() {
  loadError.value = null
  isLoading.value = true
  try {
    const result = await auditLogApi.list({
      entityType: filterEntityType.value || undefined,
      entityId: filterEntityId.value || undefined,
      actorUserId: filterActorUserId.value || undefined,
      occurredFrom: filterFromDate.value ? `${filterFromDate.value}T00:00:00Z` : undefined,
      occurredTo: filterToDate.value ? `${filterToDate.value}T23:59:59Z` : undefined,
      page: page.value,
      pageSize: PAGE_SIZE,
    })
    entries.value = result.items
    totalCount.value = result.totalCount
  } catch (error) {
    loadError.value = error instanceof ApiError ? error.message : 'Could not load audit log.'
  } finally {
    isLoading.value = false
  }
}

function applyFilters() {
  page.value = 1
  void load()
}

function clearFilters() {
  filterEntityType.value = ''
  filterEntityId.value = ''
  filterActorUserId.value = ''
  filterFromDate.value = ''
  filterToDate.value = ''
  page.value = 1
  void load()
}

function goToPage(newPage: number) {
  if (newPage < 1 || newPage > totalPages.value) return
  page.value = newPage
  void load()
}

function formatMetadata(metadataJson: string | null): string {
  if (!metadataJson) return '—'
  try {
    const parsed = JSON.parse(metadataJson) as Record<string, unknown>
    return Object.entries(parsed)
      .map(([key, value]) => `${key}: ${String(value)}`)
      .join(', ')
  } catch {
    return metadataJson
  }
}

onMounted(load)
</script>

<template>
  <div>
    <h1 class="text-lg font-semibold text-slate-900">Audit Log</h1>
    <p class="mt-1 text-sm text-slate-500">A record of who did what, to what, when - across this organization.</p>

    <div class="mt-4 rounded-lg border border-slate-200 bg-white p-6">
      <form class="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5" @submit.prevent="applyFilters">
        <div>
          <label for="filter-entity-type" class="block text-xs font-medium text-slate-500">Entity type</label>
          <input
            id="filter-entity-type"
            v-model="filterEntityType"
            type="text"
            placeholder="SchemaVersion"
            class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
          />
        </div>
        <div>
          <label for="filter-entity-id" class="block text-xs font-medium text-slate-500">Entity ID</label>
          <input
            id="filter-entity-id"
            v-model="filterEntityId"
            type="text"
            class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm font-mono focus:border-slate-500 focus:outline-none"
          />
        </div>
        <div>
          <label for="filter-actor" class="block text-xs font-medium text-slate-500">Actor user ID</label>
          <input
            id="filter-actor"
            v-model="filterActorUserId"
            type="text"
            class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm font-mono focus:border-slate-500 focus:outline-none"
          />
        </div>
        <div>
          <label for="filter-from" class="block text-xs font-medium text-slate-500">From</label>
          <input
            id="filter-from"
            v-model="filterFromDate"
            type="date"
            class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
          />
        </div>
        <div>
          <label for="filter-to" class="block text-xs font-medium text-slate-500">To</label>
          <input
            id="filter-to"
            v-model="filterToDate"
            type="date"
            class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
          />
        </div>
        <div class="col-span-2 flex items-end gap-2 sm:col-span-3 lg:col-span-5">
          <button
            type="submit"
            class="rounded-md bg-slate-900 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-800"
          >
            Apply filters
          </button>
          <button
            type="button"
            class="rounded-md border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50"
            @click="clearFilters"
          >
            Clear
          </button>
        </div>
      </form>
    </div>

    <div class="mt-6 rounded-lg border border-slate-200 bg-white p-6">
      <p v-if="loadError" class="text-sm text-red-600">{{ loadError }}</p>
      <p v-else-if="isLoading" class="text-sm text-slate-500">Loading…</p>
      <p v-else-if="entries.length === 0" class="text-sm text-slate-500">No audit log entries match these filters.</p>

      <table v-else class="w-full text-sm">
        <thead>
          <tr class="border-b border-slate-200 text-left text-slate-500">
            <th class="pb-2 font-medium">Occurred</th>
            <th class="pb-2 font-medium">Action</th>
            <th class="pb-2 font-medium">Entity</th>
            <th class="pb-2 font-medium">Actor</th>
            <th class="pb-2 font-medium">Details</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="entry in entries" :key="entry.id" class="border-b border-slate-100 last:border-0 align-top">
            <td class="whitespace-nowrap py-2 text-slate-500">{{ new Date(entry.occurredAt).toLocaleString() }}</td>
            <td class="py-2 font-medium text-slate-900">{{ entry.action }}</td>
            <td class="py-2 text-slate-500">
              <span class="font-mono text-xs">{{ entry.entityType }}</span>
              <br />
              <span class="font-mono text-xs">{{ entry.entityId }}</span>
            </td>
            <td class="py-2 font-mono text-xs text-slate-500">{{ entry.actorUserId }}</td>
            <td class="py-2 text-xs text-slate-500">{{ formatMetadata(entry.metadataJson) }}</td>
          </tr>
        </tbody>
      </table>

      <div v-if="entries.length > 0" class="mt-4 flex items-center justify-between text-sm text-slate-500">
        <span>Page {{ page }} of {{ totalPages }} ({{ totalCount }} total)</span>
        <div class="space-x-2">
          <button
            type="button"
            :disabled="page <= 1"
            class="rounded-md border border-slate-300 px-3 py-1.5 font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50"
            @click="goToPage(page - 1)"
          >
            Previous
          </button>
          <button
            type="button"
            :disabled="page >= totalPages"
            class="rounded-md border border-slate-300 px-3 py-1.5 font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50"
            @click="goToPage(page + 1)"
          >
            Next
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
