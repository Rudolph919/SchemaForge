<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { schemaDefinitionsApi } from '@/modules/schemas/api/schemaDefinitionsApi'
import { schemaVersionsApi } from '@/modules/schemas/api/schemaVersionsApi'
import { ApiError } from '@/shared/api/httpClient'
import type { SchemaDefinitionDetailResponse, SchemaVersionSummaryResponse, VersionBumpKind } from '@/types/schemas'

const route = useRoute()
const router = useRouter()
const schemaId = computed(() => route.params.schemaId as string)

const schema = ref<SchemaDefinitionDetailResponse | null>(null)
const versions = ref<SchemaVersionSummaryResponse[]>([])
const loadError = ref<string | null>(null)

const isEditing = ref(false)
const editName = ref('')
const editDescription = ref('')
const editTags = ref('')
const editError = ref<string | null>(null)
const isSaving = ref(false)

const newVersionBumpKind = ref<VersionBumpKind>('Minor')
const newVersionSummary = ref('')
const versionError = ref<string | null>(null)
const isCreatingVersion = ref(false)

const hasDraft = computed(() => versions.value.some((v) => v.status === 'Draft'))

async function load() {
  loadError.value = null
  try {
    const [schemaResponse, versionsResponse] = await Promise.all([
      schemaDefinitionsApi.getSchema(schemaId.value),
      schemaVersionsApi.listVersions(schemaId.value),
    ])
    schema.value = schemaResponse
    versions.value = versionsResponse
  } catch (error) {
    loadError.value = error instanceof ApiError ? error.message : 'Could not load schema.'
  }
}

function startEditing() {
  if (!schema.value) return
  editName.value = schema.value.name
  editDescription.value = schema.value.description ?? ''
  editTags.value = schema.value.tags.join(', ')
  editError.value = null
  isEditing.value = true
}

async function handleSaveDetails() {
  editError.value = null
  isSaving.value = true
  try {
    const tags = editTags.value
      .split(',')
      .map((t) => t.trim())
      .filter((t) => t.length > 0)

    await schemaDefinitionsApi.updateSchemaDetails(schemaId.value, {
      name: editName.value,
      description: editDescription.value || null,
      tags,
    })
    isEditing.value = false
    await load()
  } catch (error) {
    editError.value = error instanceof ApiError ? error.message : 'Could not save changes.'
  } finally {
    isSaving.value = false
  }
}

async function handleCreateVersion() {
  versionError.value = null
  isCreatingVersion.value = true
  try {
    await schemaVersionsApi.createVersion(schemaId.value, {
      bumpKind: newVersionBumpKind.value,
      changeSummary: newVersionSummary.value || null,
    })
    newVersionSummary.value = ''
    await load()
  } catch (error) {
    versionError.value = error instanceof ApiError ? error.message : 'Could not create version.'
  } finally {
    isCreatingVersion.value = false
  }
}

async function handlePublish(versionId: string) {
  versionError.value = null
  try {
    await schemaVersionsApi.publish(versionId)
    await load()
  } catch (error) {
    versionError.value = error instanceof ApiError ? error.message : 'Could not publish version.'
  }
}

async function handleDeprecate(versionId: string) {
  versionError.value = null
  try {
    await schemaVersionsApi.deprecate(versionId)
    await load()
  } catch (error) {
    versionError.value = error instanceof ApiError ? error.message : 'Could not deprecate version.'
  }
}

function statusClass(status: SchemaVersionSummaryResponse['status']): string {
  switch (status) {
    case 'Draft':
      return 'bg-amber-100 text-amber-800'
    case 'Published':
      return 'bg-emerald-100 text-emerald-800'
    case 'Deprecated':
      return 'bg-slate-100 text-slate-600'
  }
}

onMounted(load)
</script>

<template>
  <div>
    <button type="button" class="text-sm text-slate-500 hover:text-slate-700" @click="router.back()">
      ← Back to Schemas
    </button>

    <p v-if="loadError" class="mt-4 text-sm text-red-600">{{ loadError }}</p>

    <template v-if="schema">
      <div class="mt-4 rounded-lg border border-slate-200 bg-white p-6">
        <div v-if="!isEditing" class="flex items-start justify-between">
          <div>
            <h1 class="text-lg font-semibold text-slate-900">{{ schema.name }}</h1>
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
          </div>
          <button
            type="button"
            class="shrink-0 rounded-md border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50"
            @click="startEditing"
          >
            Edit
          </button>
        </div>

        <form v-else class="space-y-4" @submit.prevent="handleSaveDetails">
          <div>
            <label for="edit-name" class="block text-sm font-medium text-slate-700">Name</label>
            <input
              id="edit-name"
              v-model="editName"
              type="text"
              required
              class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
            />
          </div>
          <div>
            <label for="edit-description" class="block text-sm font-medium text-slate-700">Description</label>
            <textarea
              id="edit-description"
              v-model="editDescription"
              rows="3"
              class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
            />
          </div>
          <div>
            <label for="edit-tags" class="block text-sm font-medium text-slate-700">
              Tags <span class="text-slate-400">(comma-separated)</span>
            </label>
            <input
              id="edit-tags"
              v-model="editTags"
              type="text"
              class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
            />
          </div>
          <p v-if="editError" class="text-sm text-red-600">{{ editError }}</p>
          <div class="flex gap-2">
            <button
              type="submit"
              :disabled="isSaving"
              class="rounded-md bg-slate-900 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
            >
              {{ isSaving ? 'Saving…' : 'Save' }}
            </button>
            <button
              type="button"
              class="rounded-md border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50"
              @click="isEditing = false"
            >
              Cancel
            </button>
          </div>
        </form>
      </div>

      <div class="mt-6 rounded-lg border border-slate-200 bg-white p-6">
        <div class="flex items-center justify-between">
          <h2 class="text-base font-semibold text-slate-900">Versions</h2>
        </div>

        <form class="mt-4 flex flex-wrap items-end gap-2 border-b border-slate-100 pb-4" @submit.prevent="handleCreateVersion">
          <div>
            <label for="bump-kind" class="block text-xs font-medium text-slate-500">Bump</label>
            <select
              id="bump-kind"
              v-model="newVersionBumpKind"
              class="mt-1 rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
            >
              <option value="Major">Major</option>
              <option value="Minor">Minor</option>
              <option value="Patch">Patch</option>
            </select>
          </div>
          <div class="flex-1">
            <label for="change-summary" class="block text-xs font-medium text-slate-500">
              Change summary <span class="text-slate-400">(optional)</span>
            </label>
            <input
              id="change-summary"
              v-model="newVersionSummary"
              type="text"
              class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
            />
          </div>
          <button
            type="submit"
            :disabled="isCreatingVersion || hasDraft"
            :title="hasDraft ? 'A draft version already exists' : undefined"
            class="rounded-md bg-slate-900 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
          >
            {{ isCreatingVersion ? 'Creating…' : 'New Draft' }}
          </button>
        </form>

        <p v-if="versionError" class="mt-2 text-sm text-red-600">{{ versionError }}</p>
        <p v-if="versions.length === 0" class="mt-4 text-sm text-slate-500">No versions yet.</p>

        <table v-else class="mt-4 w-full text-sm">
          <thead>
            <tr class="border-b border-slate-200 text-left text-slate-500">
              <th class="pb-2 font-medium">Version</th>
              <th class="pb-2 font-medium">Status</th>
              <th class="pb-2 font-medium">Change Summary</th>
              <th class="pb-2"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="version in versions" :key="version.id" class="border-b border-slate-100 last:border-0">
              <td class="py-2 font-medium text-slate-900">{{ version.versionNumber }}</td>
              <td class="py-2">
                <span class="rounded-full px-2 py-0.5 text-xs font-medium" :class="statusClass(version.status)">
                  {{ version.status }}
                </span>
              </td>
              <td class="py-2 text-slate-500">{{ version.changeSummary ?? '—' }}</td>
              <td class="py-2 text-right">
                <button
                  v-if="version.status === 'Draft'"
                  type="button"
                  class="text-slate-500 hover:text-emerald-700"
                  @click="handlePublish(version.id)"
                >
                  Publish
                </button>
                <button
                  v-if="version.status === 'Published'"
                  type="button"
                  class="text-slate-500 hover:text-red-600"
                  @click="handleDeprecate(version.id)"
                >
                  Deprecate
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>
  </div>
</template>
