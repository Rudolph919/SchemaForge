<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { schemaVersionsApi } from '@/modules/schemas/api/schemaVersionsApi'
import { testSuitesApi } from '@/modules/testing/api/testSuitesApi'
import { ApiError } from '@/shared/api/httpClient'
import Modal from '@/shared/components/Modal.vue'
import type { SchemaVersionSummaryResponse } from '@/types/schemas'
import type {
  ExpectedErrorDto,
  TestCaseResponse,
  TestExpectationKind,
  TestRunResponse,
  TestSuiteDetailResponse,
} from '@/types/testing'

const route = useRoute()
const router = useRouter()
const testSuiteId = computed(() => route.params.testSuiteId as string)

const suite = ref<TestSuiteDetailResponse | null>(null)
const versions = ref<SchemaVersionSummaryResponse[]>([])
const loadError = ref<string | null>(null)
const actionError = ref<string | null>(null)

async function load() {
  loadError.value = null
  try {
    suite.value = await testSuitesApi.getSuite(testSuiteId.value)
    versions.value = await schemaVersionsApi.listVersions(suite.value.schemaDefinitionId)
  } catch (error) {
    loadError.value = error instanceof ApiError ? error.message : 'Could not load test suite.'
  }
}

// --- Rename / describe ---
const isEditing = ref(false)
const editName = ref('')
const editDescription = ref('')
const editDetailsError = ref<string | null>(null)
const isSavingDetails = ref(false)

function startEditing() {
  if (!suite.value) return
  editName.value = suite.value.name
  editDescription.value = suite.value.description ?? ''
  editDetailsError.value = null
  isEditing.value = true
}

async function handleSaveDetails() {
  editDetailsError.value = null
  isSavingDetails.value = true
  try {
    await testSuitesApi.updateSuiteDetails(testSuiteId.value, {
      name: editName.value,
      description: editDescription.value || null,
    })
    isEditing.value = false
    await load()
  } catch (error) {
    editDetailsError.value = error instanceof ApiError ? error.message : 'Could not save changes.'
  } finally {
    isSavingDetails.value = false
  }
}

// --- Add / edit case ---
const editingCase = ref<TestCaseResponse | 'new' | null>(null)
const caseName = ref('')
const caseInputJsonText = ref('{\n  \n}')
const caseExpectationKind = ref<TestExpectationKind>('Valid')
const caseExpectedErrors = ref<ExpectedErrorDto[]>([])
const caseError = ref<string | null>(null)
const isSavingCase = ref(false)

function openAddCase() {
  editingCase.value = 'new'
  caseName.value = ''
  caseInputJsonText.value = '{\n  \n}'
  caseExpectationKind.value = 'Valid'
  caseExpectedErrors.value = []
  caseError.value = null
}

function openEditCase(testCase: TestCaseResponse) {
  editingCase.value = testCase
  caseName.value = testCase.name
  caseInputJsonText.value = JSON.stringify(testCase.inputJson, null, 2)
  caseExpectationKind.value = testCase.expectation.kind
  caseExpectedErrors.value = testCase.expectation.expectedErrors
    ? testCase.expectation.expectedErrors.map((e) => ({ ...e }))
    : []
  caseError.value = null
}

function addExpectedErrorRow() {
  caseExpectedErrors.value.push({ path: '$.', errorCodePattern: '' })
}

function removeExpectedErrorRow(index: number) {
  caseExpectedErrors.value.splice(index, 1)
}

async function handleSaveCase() {
  if (!editingCase.value) return
  caseError.value = null

  let inputJson: unknown
  try {
    inputJson = JSON.parse(caseInputJsonText.value)
  } catch {
    caseError.value = 'Input JSON is not valid JSON.'
    return
  }

  const expectation =
    caseExpectationKind.value === 'Valid'
      ? { kind: 'Valid' as const, expectedErrors: null }
      : { kind: 'Errors' as const, expectedErrors: caseExpectedErrors.value }

  isSavingCase.value = true
  try {
    if (editingCase.value === 'new') {
      await testSuitesApi.addCase(testSuiteId.value, { name: caseName.value, inputJson, expectation })
    } else {
      await testSuitesApi.updateCase(testSuiteId.value, editingCase.value.id, {
        name: caseName.value,
        inputJson,
        expectation,
      })
    }
    editingCase.value = null
    await load()
  } catch (error) {
    caseError.value = error instanceof ApiError ? error.message : 'Could not save test case.'
  } finally {
    isSavingCase.value = false
  }
}

async function handleRemoveCase(testCase: TestCaseResponse) {
  actionError.value = null
  try {
    await testSuitesApi.removeCase(testSuiteId.value, testCase.id)
    await load()
  } catch (error) {
    actionError.value = error instanceof ApiError ? error.message : 'Could not remove test case.'
  }
}

// --- Run ---
const runTargetVersionId = ref('')
const isRunning = ref(false)
const runError = ref<string | null>(null)
const currentRun = ref<TestRunResponse | null>(null)

async function handleRun() {
  if (!runTargetVersionId.value) return
  runError.value = null
  currentRun.value = null
  isRunning.value = true
  try {
    const { testRunId } = await testSuitesApi.run(testSuiteId.value, runTargetVersionId.value)
    await pollRun(testRunId)
  } catch (error) {
    runError.value = error instanceof ApiError ? error.message : 'Could not start test run.'
    isRunning.value = false
  }
}

// Hangfire's queue poll interval is a few seconds, so a short client-side poll loop (not a
// single fetch) is needed to actually observe the Pending -> Completed transition described in
// Step 6 §2.7/§4's "always async, GET /test-runs/{id} is cheap to poll" contract.
async function pollRun(testRunId: string) {
  for (let attempt = 0; attempt < 30; attempt++) {
    const run = await testSuitesApi.getRun(testRunId)
    currentRun.value = run
    if (run.status === 'Completed') {
      isRunning.value = false
      return
    }
    await new Promise((resolve) => setTimeout(resolve, 1000))
  }
  isRunning.value = false
  runError.value = 'Test run is taking longer than expected - it may still complete; refresh to check.'
}

const passedCount = computed(() => currentRun.value?.results.filter((r) => r.passed).length ?? 0)

onMounted(load)
</script>

<template>
  <div>
    <button type="button" class="text-sm text-slate-500 hover:text-slate-700" @click="router.back()">
      ← Back to Schema
    </button>

    <p v-if="loadError" class="mt-4 text-sm text-red-600">{{ loadError }}</p>

    <template v-if="suite">
      <div class="mt-4 rounded-lg border border-slate-200 bg-white p-6">
        <div v-if="!isEditing" class="flex items-start justify-between">
          <div>
            <h1 class="text-lg font-semibold text-slate-900">{{ suite.name }}</h1>
            <p v-if="suite.description" class="mt-1 text-sm text-slate-500">{{ suite.description }}</p>
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
            <label for="edit-suite-name" class="block text-sm font-medium text-slate-700">Name</label>
            <input
              id="edit-suite-name"
              v-model="editName"
              type="text"
              required
              class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
            />
          </div>
          <div>
            <label for="edit-suite-description" class="block text-sm font-medium text-slate-700">Description</label>
            <textarea
              id="edit-suite-description"
              v-model="editDescription"
              rows="2"
              class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
            />
          </div>
          <p v-if="editDetailsError" class="text-sm text-red-600">{{ editDetailsError }}</p>
          <div class="flex gap-2">
            <button
              type="submit"
              :disabled="isSavingDetails"
              class="rounded-md bg-slate-900 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
            >
              {{ isSavingDetails ? 'Saving…' : 'Save' }}
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
          <h2 class="text-base font-semibold text-slate-900">Cases</h2>
          <button
            type="button"
            class="rounded-md border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50"
            @click="openAddCase"
          >
            Add Case
          </button>
        </div>

        <p v-if="actionError" class="mt-2 text-sm text-red-600">{{ actionError }}</p>
        <p v-if="suite.cases.length === 0" class="mt-4 text-sm text-slate-500">No cases yet.</p>

        <ul v-else class="mt-4 space-y-2">
          <li
            v-for="testCase in suite.cases"
            :key="testCase.id"
            class="flex items-center justify-between rounded-md border border-slate-100 px-3 py-2"
          >
            <div>
              <span class="text-sm font-medium text-slate-900">{{ testCase.name }}</span>
              <span
                class="ml-2 rounded-full px-2 py-0.5 text-xs font-medium"
                :class="testCase.expectation.kind === 'Valid' ? 'bg-emerald-100 text-emerald-800' : 'bg-amber-100 text-amber-800'"
              >
                {{ testCase.expectation.kind === 'Valid' ? 'Expect valid' : 'Expect errors' }}
              </span>
            </div>
            <div class="space-x-3 text-sm">
              <button type="button" class="text-slate-500 hover:text-slate-900" @click="openEditCase(testCase)">
                Edit
              </button>
              <button type="button" class="text-slate-500 hover:text-red-600" @click="handleRemoveCase(testCase)">
                Remove
              </button>
            </div>
          </li>
        </ul>
      </div>

      <div class="mt-6 rounded-lg border border-slate-200 bg-white p-6">
        <h2 class="text-base font-semibold text-slate-900">Run</h2>

        <div class="mt-4 flex gap-2">
          <select
            v-model="runTargetVersionId"
            class="rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
          >
            <option value="">Select a version…</option>
            <option v-for="v in versions" :key="v.id" :value="v.id">{{ v.versionNumber }} ({{ v.status }})</option>
          </select>
          <button
            type="button"
            :disabled="isRunning || !runTargetVersionId"
            class="rounded-md bg-slate-900 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
            @click="handleRun"
          >
            {{ isRunning ? 'Running…' : 'Run' }}
          </button>
        </div>
        <p v-if="runError" class="mt-2 text-sm text-red-600">{{ runError }}</p>

        <div v-if="currentRun" class="mt-4">
          <p class="text-sm text-slate-500">
            <span
              class="rounded-full px-2 py-0.5 text-xs font-medium"
              :class="currentRun.status === 'Completed' ? 'bg-emerald-100 text-emerald-800' : 'bg-amber-100 text-amber-800'"
            >
              {{ currentRun.status }}
            </span>
            <span v-if="currentRun.status === 'Completed'" class="ml-2">
              {{ passedCount }} / {{ currentRun.results.length }} passed
            </span>
          </p>

          <ul v-if="currentRun.results.length > 0" class="mt-3 space-y-2">
            <li v-for="result in currentRun.results" :key="result.testCaseId" class="rounded-md border border-slate-100 px-3 py-2">
              <span
                class="rounded-full px-2 py-0.5 text-xs font-medium"
                :class="result.passed ? 'bg-emerald-100 text-emerald-800' : 'bg-red-100 text-red-800'"
              >
                {{ result.passed ? 'Passed' : 'Failed' }}
              </span>
              <span class="ml-2 text-sm font-medium text-slate-900">{{ result.testCaseName }}</span>
              <ul v-if="result.actualErrors.length > 0" class="mt-1 space-y-0.5 pl-4">
                <li v-for="(err, i) in result.actualErrors" :key="i" class="text-xs text-slate-500">
                  <span class="font-mono">{{ err.path }}</span> — {{ err.message }}
                  <span class="text-slate-400">({{ err.code }})</span>
                </li>
              </ul>
            </li>
          </ul>
        </div>
      </div>
    </template>

    <Modal v-if="editingCase" :title="editingCase === 'new' ? 'Add Case' : 'Edit Case'" @close="editingCase = null">
      <form class="space-y-4" @submit.prevent="handleSaveCase">
        <div>
          <label for="case-name" class="block text-sm font-medium text-slate-700">Name</label>
          <input
            id="case-name"
            v-model="caseName"
            type="text"
            required
            class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
          />
        </div>
        <div>
          <label for="case-input-json" class="block text-sm font-medium text-slate-700">Input JSON</label>
          <textarea
            id="case-input-json"
            v-model="caseInputJsonText"
            rows="6"
            spellcheck="false"
            class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 font-mono text-sm focus:border-slate-500 focus:outline-none"
          />
        </div>
        <div>
          <label for="case-expectation" class="block text-sm font-medium text-slate-700">Expectation</label>
          <select
            id="case-expectation"
            v-model="caseExpectationKind"
            class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
          >
            <option value="Valid">Expect valid</option>
            <option value="Errors">Expect specific errors</option>
          </select>
        </div>

        <div v-if="caseExpectationKind === 'Errors'">
          <div class="flex items-center justify-between">
            <span class="text-xs font-medium text-slate-500">Expected errors</span>
            <button type="button" class="text-xs text-slate-500 hover:text-slate-900" @click="addExpectedErrorRow">
              + Add
            </button>
          </div>
          <div v-for="(err, i) in caseExpectedErrors" :key="i" class="mt-2 flex gap-2">
            <input
              v-model="err.path"
              type="text"
              placeholder="$.propertyName"
              class="flex-1 rounded-md border border-slate-300 px-2 py-1.5 text-sm font-mono focus:border-slate-500 focus:outline-none"
            />
            <input
              v-model="err.errorCodePattern"
              type="text"
              placeholder="object.required-property-missing"
              class="flex-1 rounded-md border border-slate-300 px-2 py-1.5 text-sm font-mono focus:border-slate-500 focus:outline-none"
            />
            <button
              type="button"
              aria-label="Remove expected error"
              class="text-slate-400 hover:text-red-600"
              @click="removeExpectedErrorRow(i)"
            >
              ✕
            </button>
          </div>
        </div>

        <p v-if="caseError" class="text-sm text-red-600">{{ caseError }}</p>
        <button
          type="submit"
          :disabled="isSavingCase"
          class="w-full rounded-md bg-slate-900 px-3 py-2 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
        >
          {{ isSavingCase ? 'Saving…' : 'Save' }}
        </button>
      </form>
    </Modal>
  </div>
</template>
