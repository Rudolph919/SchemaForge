<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { schemaDefinitionsApi } from '@/modules/schemas/api/schemaDefinitionsApi'
import { schemaVersionsApi } from '@/modules/schemas/api/schemaVersionsApi'
import SuggestedNodeTree from '@/modules/schemas/components/SuggestedNodeTree.vue'
import { testSuitesApi } from '@/modules/testing/api/testSuitesApi'
import { documentsApi } from '@/modules/workspaces/api/documentsApi'
import { ApiError } from '@/shared/api/httpClient'
import { useIdempotencyKey, useIdempotencyKeyMap } from '@/shared/api/idempotencyKey'
import Modal from '@/shared/components/Modal.vue'
import type { SchemaDefinitionDetailResponse, SchemaSuggestionResponse, SchemaVersionSummaryResponse, VersionBumpKind } from '@/types/schemas'
import type { TestSuiteSummaryResponse } from '@/types/testing'
import type { SourceDocumentResponse } from '@/types/sourceDocuments'

const route = useRoute()
const router = useRouter()
const schemaId = computed(() => route.params.schemaId as string)

const schema = ref<SchemaDefinitionDetailResponse | null>(null)
const schemaETag = ref<string | null>(null)
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
const createVersionKey = useIdempotencyKey()
const publishKeys = useIdempotencyKeyMap()
const deprecateKeys = useIdempotencyKeyMap()

const hasDraft = computed(() => versions.value.some((v) => v.status === 'Draft'))

const testSuites = ref<TestSuiteSummaryResponse[]>([])
const newSuiteName = ref('')
const newSuiteDescription = ref('')
const suiteError = ref<string | null>(null)
const isCreatingSuite = ref(false)
const createSuiteKey = useIdempotencyKey()

async function load() {
  loadError.value = null
  try {
    const [schemaResponse, versionsResponse, testSuitesResponse] = await Promise.all([
      schemaDefinitionsApi.getSchema(schemaId.value),
      schemaVersionsApi.listVersions(schemaId.value),
      testSuitesApi.listSuites(schemaId.value),
    ])
    schema.value = schemaResponse.data
    schemaETag.value = schemaResponse.etag
    versions.value = versionsResponse
    testSuites.value = testSuitesResponse
  } catch (error) {
    loadError.value = error instanceof ApiError ? error.message : 'Could not load schema.'
  }
}

async function handleCreateSuite() {
  suiteError.value = null
  isCreatingSuite.value = true
  try {
    await testSuitesApi.createSuite(
      schemaId.value,
      { name: newSuiteName.value, description: newSuiteDescription.value || null },
      createSuiteKey.get(),
    )
    createSuiteKey.reset()
    newSuiteName.value = ''
    newSuiteDescription.value = ''
    await load()
  } catch (error) {
    suiteError.value = error instanceof ApiError ? error.message : 'Could not create test suite.'
  } finally {
    isCreatingSuite.value = false
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
  if (!schemaETag.value) {
    editError.value = 'Missing schema ETag - reloading before saving.'
    await load()
    return
  }
  isSaving.value = true
  try {
    const tags = editTags.value
      .split(',')
      .map((t) => t.trim())
      .filter((t) => t.length > 0)

    await schemaDefinitionsApi.updateSchemaDetails(
      schemaId.value,
      { name: editName.value, description: editDescription.value || null, tags },
      schemaETag.value,
    )
    isEditing.value = false
    await load()
  } catch (error) {
    if (error instanceof ApiError && error.status === 409) {
      editError.value = 'This schema changed elsewhere. Reloaded the latest version - please redo your edit.'
      await load()
    } else {
      editError.value = error instanceof ApiError ? error.message : 'Could not save changes.'
    }
  } finally {
    isSaving.value = false
  }
}

async function handleCreateVersion() {
  versionError.value = null
  isCreatingVersion.value = true
  try {
    await schemaVersionsApi.createVersion(
      schemaId.value,
      { bumpKind: newVersionBumpKind.value, changeSummary: newVersionSummary.value || null },
      createVersionKey.get(),
    )
    createVersionKey.reset()
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
    await schemaVersionsApi.publish(versionId, publishKeys.get(versionId))
    publishKeys.reset(versionId)
    await load()
  } catch (error) {
    versionError.value = error instanceof ApiError ? error.message : 'Could not publish version.'
  }
}

async function handleDeprecate(versionId: string) {
  versionError.value = null
  try {
    await schemaVersionsApi.deprecate(versionId, deprecateKeys.get(versionId))
    deprecateKeys.reset(versionId)
    await load()
  } catch (error) {
    versionError.value = error instanceof ApiError ? error.message : 'Could not deprecate version.'
  }
}

// --- Import ---
const isImportOpen = ref(false)
const importDocumentText = ref('')
const importBumpKind = ref<VersionBumpKind>('Minor')
const importSummary = ref('')
const importError = ref<string | null>(null)
const isImporting = ref(false)
const importSchemaKey = useIdempotencyKey()

function openImport() {
  importDocumentText.value = ''
  importBumpKind.value = 'Minor'
  importSummary.value = ''
  importError.value = null
  importSchemaKey.reset()
  isImportOpen.value = true
}

async function handleImport() {
  let document: unknown
  try {
    document = JSON.parse(importDocumentText.value)
  } catch {
    importError.value = 'Not valid JSON.'
    return
  }

  importError.value = null
  isImporting.value = true
  try {
    await schemaVersionsApi.importSchema(
      schemaId.value,
      document,
      importBumpKind.value,
      importSummary.value || null,
      importSchemaKey.get(),
    )
    importSchemaKey.reset()
    isImportOpen.value = false
    await load()
  } catch (error) {
    importError.value = error instanceof ApiError ? error.message : 'Could not import schema.'
  } finally {
    isImporting.value = false
  }
}

// --- Suggest schema from document (Step 9 §2) ---
const isSuggestOpen = ref(false)
const suggestDocuments = ref<SourceDocumentResponse[]>([])
const suggestDocumentsError = ref<string | null>(null)
const selectedDocumentId = ref('')
const suggestion = ref<SchemaSuggestionResponse | null>(null)
const acceptedNodeIds = ref<Set<string>>(new Set())
const suggestBumpKind = ref<VersionBumpKind>('Minor')
const suggestSummary = ref('')
const suggestError = ref<string | null>(null)
const isSuggesting = ref(false)
const isCreatingDraftFromSuggestion = ref(false)
const createDraftFromSuggestionKey = useIdempotencyKey()

async function openSuggest() {
  if (!schema.value) return
  selectedDocumentId.value = ''
  suggestion.value = null
  suggestBumpKind.value = 'Minor'
  suggestSummary.value = ''
  suggestError.value = null
  suggestDocumentsError.value = null
  createDraftFromSuggestionKey.reset()
  isSuggestOpen.value = true

  try {
    suggestDocuments.value = await documentsApi.listDocuments(schema.value.projectId)
  } catch (error) {
    suggestDocumentsError.value = error instanceof ApiError ? error.message : 'Could not load documents.'
  }
}

function collectNodeIds(nodes: SchemaSuggestionResponse['nodes'], into: Set<string>) {
  for (const node of nodes) {
    into.add(node.id)
    collectNodeIds(node.children, into)
  }
}

function findSuggestedNode(
  nodes: SchemaSuggestionResponse['nodes'],
  nodeId: string,
): SchemaSuggestionResponse['nodes'][number] | null {
  for (const node of nodes) {
    if (node.id === nodeId) return node
    const found = findSuggestedNode(node.children, nodeId)
    if (found) return found
  }
  return null
}

async function handleGenerateSuggestion() {
  suggestError.value = null
  suggestion.value = null
  isSuggesting.value = true
  try {
    const result = await documentsApi.suggestSchema(selectedDocumentId.value)
    suggestion.value = result
    const allIds = new Set<string>()
    collectNodeIds(result.nodes, allIds)
    acceptedNodeIds.value = allIds
  } catch (error) {
    suggestError.value = error instanceof ApiError ? error.message : 'Could not generate a suggestion.'
  } finally {
    isSuggesting.value = false
  }
}

function toggleAccepted(nodeId: string) {
  if (!suggestion.value) return
  const next = new Set(acceptedNodeIds.value)
  if (next.has(nodeId)) {
    // Rejecting a node also rejects its whole subtree - a rejected node has no attachment
    // point for its children once materialized, and SuggestedNodeTree.vue already disables
    // (but doesn't un-check) descendant checkboxes to reflect that. Without this cascade the
    // still-checked descendants get submitted anyway, and CreateDraftFromSuggestion rejects
    // the whole request with SchemaNode.NotAnObject.
    next.delete(nodeId)
    const node = findSuggestedNode(suggestion.value.nodes, nodeId)
    if (node) {
      const descendantIds = new Set<string>()
      collectNodeIds(node.children, descendantIds)
      for (const id of descendantIds) next.delete(id)
    }
  } else {
    next.add(nodeId)
  }
  acceptedNodeIds.value = next
}

async function handleCreateDraftFromSuggestion() {
  if (!suggestion.value) return
  suggestError.value = null
  isCreatingDraftFromSuggestion.value = true
  try {
    await schemaVersionsApi.createDraftFromSuggestion(
      schemaId.value,
      {
        suggestion: suggestion.value,
        acceptedNodeIds: [...acceptedNodeIds.value],
        bumpKind: suggestBumpKind.value,
        changeSummary: suggestSummary.value || null,
      },
      createDraftFromSuggestionKey.get(),
    )
    createDraftFromSuggestionKey.reset()
    isSuggestOpen.value = false
    await load()
  } catch (error) {
    suggestError.value = error instanceof ApiError ? error.message : 'Could not create draft from suggestion.'
  } finally {
    isCreatingDraftFromSuggestion.value = false
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
          <div class="flex gap-2">
            <button
              type="button"
              :disabled="hasDraft"
              :title="hasDraft ? 'A draft version already exists' : undefined"
              class="rounded-md border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50"
              @click="openSuggest"
            >
              Suggest Schema from Document
            </button>
            <button
              type="button"
              :disabled="hasDraft"
              :title="hasDraft ? 'A draft version already exists' : undefined"
              class="rounded-md border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50"
              @click="openImport"
            >
              Import JSON Schema
            </button>
          </div>
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
              <td class="space-x-3 py-2 text-right">
                <router-link :to="`/schema-versions/${version.id}`" class="text-slate-500 hover:text-slate-900">
                  {{ version.status === 'Draft' ? 'Edit' : 'View' }}
                </router-link>
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

      <div class="mt-6 rounded-lg border border-slate-200 bg-white p-6">
        <h2 class="text-base font-semibold text-slate-900">Test Suites</h2>

        <form class="mt-4 flex flex-wrap items-end gap-2 border-b border-slate-100 pb-4" @submit.prevent="handleCreateSuite">
          <div class="flex-1">
            <label for="new-suite-name" class="block text-xs font-medium text-slate-500">Name</label>
            <input
              id="new-suite-name"
              v-model="newSuiteName"
              type="text"
              required
              class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
            />
          </div>
          <div class="flex-1">
            <label for="new-suite-description" class="block text-xs font-medium text-slate-500">
              Description <span class="text-slate-400">(optional)</span>
            </label>
            <input
              id="new-suite-description"
              v-model="newSuiteDescription"
              type="text"
              class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
            />
          </div>
          <button
            type="submit"
            :disabled="isCreatingSuite"
            class="rounded-md bg-slate-900 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
          >
            {{ isCreatingSuite ? 'Creating…' : 'New Suite' }}
          </button>
        </form>

        <p v-if="suiteError" class="mt-2 text-sm text-red-600">{{ suiteError }}</p>
        <p v-if="testSuites.length === 0" class="mt-4 text-sm text-slate-500">No test suites yet.</p>

        <table v-else class="mt-4 w-full text-sm">
          <thead>
            <tr class="border-b border-slate-200 text-left text-slate-500">
              <th class="pb-2 font-medium">Name</th>
              <th class="pb-2 font-medium">Description</th>
              <th class="pb-2 font-medium">Cases</th>
              <th class="pb-2"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="suite in testSuites" :key="suite.id" class="border-b border-slate-100 last:border-0">
              <td class="py-2 font-medium text-slate-900">{{ suite.name }}</td>
              <td class="py-2 text-slate-500">{{ suite.description ?? '—' }}</td>
              <td class="py-2 text-slate-500">{{ suite.caseCount }}</td>
              <td class="py-2 text-right">
                <router-link :to="`/test-suites/${suite.id}`" class="text-slate-500 hover:text-slate-900">
                  Open
                </router-link>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>

    <Modal v-if="isImportOpen" title="Import JSON Schema" @close="isImportOpen = false">
      <form class="space-y-4" @submit.prevent="handleImport">
        <div>
          <label for="import-document" class="block text-sm font-medium text-slate-700">JSON Schema document</label>
          <textarea
            id="import-document"
            v-model="importDocumentText"
            rows="10"
            spellcheck="false"
            required
            class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 font-mono text-sm focus:border-slate-500 focus:outline-none"
          />
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label for="import-bump-kind" class="block text-xs font-medium text-slate-500">Bump</label>
            <select
              id="import-bump-kind"
              v-model="importBumpKind"
              class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
            >
              <option value="Major">Major</option>
              <option value="Minor">Minor</option>
              <option value="Patch">Patch</option>
            </select>
          </div>
          <div>
            <label for="import-summary" class="block text-xs font-medium text-slate-500">
              Change summary <span class="text-slate-400">(optional)</span>
            </label>
            <input
              id="import-summary"
              v-model="importSummary"
              type="text"
              class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
            />
          </div>
        </div>
        <p v-if="importError" class="text-sm text-red-600">{{ importError }}</p>
        <button
          type="submit"
          :disabled="isImporting"
          class="w-full rounded-md bg-slate-900 px-3 py-2 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
        >
          {{ isImporting ? 'Importing…' : 'Import' }}
        </button>
      </form>
    </Modal>

    <Modal v-if="isSuggestOpen" title="Suggest Schema from Document" @close="isSuggestOpen = false">
      <div class="space-y-4">
        <p v-if="suggestDocumentsError" class="text-sm text-red-600">{{ suggestDocumentsError }}</p>
        <p v-else-if="suggestDocuments.length === 0" class="text-sm text-slate-500">
          No documents uploaded to this project yet.
        </p>

        <form v-else class="flex flex-wrap items-end gap-2" @submit.prevent="handleGenerateSuggestion">
          <div class="flex-1">
            <label for="suggest-document" class="block text-xs font-medium text-slate-500">Source document</label>
            <select
              id="suggest-document"
              v-model="selectedDocumentId"
              required
              class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
            >
              <option value="" disabled>Select a document…</option>
              <option v-for="doc in suggestDocuments" :key="doc.id" :value="doc.id">{{ doc.fileName }}</option>
            </select>
          </div>
          <button
            type="submit"
            :disabled="isSuggesting || !selectedDocumentId"
            class="rounded-md bg-slate-900 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
          >
            {{ isSuggesting ? 'Generating…' : 'Generate Suggestion' }}
          </button>
        </form>

        <p v-if="suggestError" class="text-sm text-red-600">{{ suggestError }}</p>

        <template v-if="suggestion">
          <div class="rounded-md border border-slate-200">
            <div class="border-b border-slate-100 bg-slate-50 px-3 py-1.5 text-xs text-slate-500">
              Suggested by {{ suggestion.providerName }}
              <span v-if="suggestion.overallConfidence != null">
                · {{ Math.round(suggestion.overallConfidence * 100) }}% overall confidence
              </span>
            </div>
            <div class="max-h-72 overflow-y-auto py-1">
              <p v-if="suggestion.nodes.length === 0" class="px-3 py-2 text-sm text-slate-500">No nodes suggested.</p>
              <SuggestedNodeTree
                v-for="node in suggestion.nodes"
                :key="node.id"
                :node="node"
                :accepted-ids="acceptedNodeIds"
                :depth="0"
                :parent-accepted="true"
                @toggle="toggleAccepted"
              />
            </div>
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label for="suggest-bump-kind" class="block text-xs font-medium text-slate-500">Bump</label>
              <select
                id="suggest-bump-kind"
                v-model="suggestBumpKind"
                class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
              >
                <option value="Major">Major</option>
                <option value="Minor">Minor</option>
                <option value="Patch">Patch</option>
              </select>
            </div>
            <div>
              <label for="suggest-summary" class="block text-xs font-medium text-slate-500">
                Change summary <span class="text-slate-400">(optional)</span>
              </label>
              <input
                id="suggest-summary"
                v-model="suggestSummary"
                type="text"
                class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
              />
            </div>
          </div>

          <button
            type="button"
            :disabled="isCreatingDraftFromSuggestion || acceptedNodeIds.size === 0"
            class="w-full rounded-md bg-slate-900 px-3 py-2 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
            @click="handleCreateDraftFromSuggestion"
          >
            {{ isCreatingDraftFromSuggestion ? 'Creating…' : `Create Draft (${acceptedNodeIds.size} accepted)` }}
          </button>
        </template>
      </div>
    </Modal>
  </div>
</template>
