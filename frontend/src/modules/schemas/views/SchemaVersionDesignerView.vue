<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { schemaVersionsApi } from '@/modules/schemas/api/schemaVersionsApi'
import SchemaNodeTree from '@/modules/schemas/components/SchemaNodeTree.vue'
import { ApiError } from '@/shared/api/httpClient'
import Modal from '@/shared/components/Modal.vue'
import type {
  NodeKind,
  SchemaFormat,
  SchemaNodeResponse,
  SchemaVersionDetailResponse,
  UpdateSchemaNodeRequest,
} from '@/types/schemas'

// Object properties only in this slice - array items, composition branches, and conditional
// if/then/else attachment are a separate follow-up (they each need their own attachment UX, and
// half-wiring them here would leave nodes in the tree with no way to fully configure them).
const ADDABLE_KINDS: NodeKind[] = ['Object', 'String', 'Number', 'Integer', 'Boolean', 'Null']
const FORMATS: SchemaFormat[] = [
  'Date',
  'DateTime',
  'Time',
  'Email',
  'Hostname',
  'Ipv4',
  'Ipv6',
  'Uri',
  'UriReference',
  'Uuid',
  'Custom',
]

const route = useRoute()
const router = useRouter()
const versionId = computed(() => route.params.versionId as string)

const version = ref<SchemaVersionDetailResponse | null>(null)
const loadError = ref<string | null>(null)
const actionError = ref<string | null>(null)

const isEditable = computed(() => version.value?.status === 'Draft')

async function load() {
  loadError.value = null
  try {
    version.value = await schemaVersionsApi.getVersion(versionId.value)
  } catch (error) {
    loadError.value = error instanceof ApiError ? error.message : 'Could not load schema version.'
  }
}

// --- Add property ---
const addParentId = ref<string | null>(null)
const addPropertyName = ref('')
const addKind = ref<NodeKind>('String')
const addError = ref<string | null>(null)
const isAdding = ref(false)

function openAddProperty(parentNodeId: string) {
  addParentId.value = parentNodeId
  addPropertyName.value = ''
  addKind.value = 'String'
  addError.value = null
}

async function handleAddProperty() {
  if (!addParentId.value) return
  addError.value = null
  isAdding.value = true
  try {
    await schemaVersionsApi.addNode(versionId.value, {
      parentNodeId: addParentId.value,
      attachmentKind: 'ObjectProperty',
      propertyName: addPropertyName.value,
      kind: addKind.value,
    })
    addParentId.value = null
    await load()
  } catch (error) {
    addError.value = error instanceof ApiError ? error.message : 'Could not add property.'
  } finally {
    isAdding.value = false
  }
}

// --- Edit node ---
const editingNode = ref<SchemaNodeResponse | null>(null)
const editDescription = ref('')
const editNotes = ref('')
const editNullable = ref(false)
const editRequired = ref(false)
const editMinLength = ref<number | null>(null)
const editMaxLength = ref<number | null>(null)
const editPattern = ref('')
const editFormat = ref<SchemaFormat | ''>('')
const editCustomFormat = ref('')
const editMinimum = ref<number | null>(null)
const editMaximum = ref<number | null>(null)
const editExclusiveMin = ref(false)
const editExclusiveMax = ref(false)
const editMultipleOf = ref<number | null>(null)
const editMinProperties = ref<number | null>(null)
const editMaxProperties = ref<number | null>(null)
const editAdditionalProperties = ref(true)
const editError = ref<string | null>(null)
const isSavingNode = ref(false)

function openEditNode(node: SchemaNodeResponse) {
  editingNode.value = node
  editDescription.value = node.description ?? ''
  editNotes.value = node.notes ?? ''
  editNullable.value = node.isNullable
  editRequired.value = node.isRequiredByParent
  editMinLength.value = node.stringConstraints?.minLength ?? null
  editMaxLength.value = node.stringConstraints?.maxLength ?? null
  editPattern.value = node.stringConstraints?.pattern ?? ''
  editFormat.value = node.stringConstraints?.format ?? ''
  editCustomFormat.value = node.stringConstraints?.customFormatValue ?? ''
  editMinimum.value = node.numericConstraints?.minimum ?? null
  editMaximum.value = node.numericConstraints?.maximum ?? null
  editExclusiveMin.value = node.numericConstraints?.exclusiveMinimum ?? false
  editExclusiveMax.value = node.numericConstraints?.exclusiveMaximum ?? false
  editMultipleOf.value = node.numericConstraints?.multipleOf ?? null
  editMinProperties.value = node.objectConstraints?.minProperties ?? null
  editMaxProperties.value = node.objectConstraints?.maxProperties ?? null
  editAdditionalProperties.value = node.objectConstraints?.additionalPropertiesAllowed ?? true
  editError.value = null
}

async function handleSaveNode() {
  if (!editingNode.value) return
  const node = editingNode.value
  editError.value = null
  isSavingNode.value = true
  try {
    const request: UpdateSchemaNodeRequest = {
      kind: node.kind,
      description: editDescription.value || null,
      notes: editNotes.value || null,
      isNullable: editNullable.value,
      isRequiredByParent: editRequired.value,
      // Not editable in this slice - round-trip whatever the node already has.
      examples: node.examples,
      defaultValue: node.defaultValue,
      allowedValues: node.allowedValues,
      constValue: node.constValue,
      dependentRequired: node.dependentRequired,
      composition: node.composition,
      componentReference: node.componentReference,
      localDefinitionRef: node.localDefinitionRef,
      objectConstraints:
        node.kind === 'Object'
          ? {
              minProperties: editMinProperties.value,
              maxProperties: editMaxProperties.value,
              additionalPropertiesAllowed: editAdditionalProperties.value,
            }
          : null,
      arrayConstraints: node.arrayConstraints,
      stringConstraints:
        node.kind === 'String'
          ? {
              minLength: editMinLength.value,
              maxLength: editMaxLength.value,
              pattern: editPattern.value || null,
              format: editFormat.value || null,
              customFormatValue: editFormat.value === 'Custom' ? editCustomFormat.value || null : null,
            }
          : null,
      numericConstraints:
        node.kind === 'Number' || node.kind === 'Integer'
          ? {
              minimum: editMinimum.value,
              maximum: editMaximum.value,
              exclusiveMinimum: editExclusiveMin.value,
              exclusiveMaximum: editExclusiveMax.value,
              multipleOf: editMultipleOf.value,
            }
          : null,
    }

    await schemaVersionsApi.updateNode(versionId.value, node.id, request)
    editingNode.value = null
    await load()
  } catch (error) {
    editError.value = error instanceof ApiError ? error.message : 'Could not save node.'
  } finally {
    isSavingNode.value = false
  }
}

// --- Remove / move ---
async function handleRemoveNode(node: SchemaNodeResponse) {
  actionError.value = null
  try {
    await schemaVersionsApi.removeNode(versionId.value, node.id)
    await load()
  } catch (error) {
    actionError.value = error instanceof ApiError ? error.message : 'Could not remove node.'
  }
}

// MoveNode sets a node's Order to exactly the value it's given - it doesn't renumber siblings
// (Step 6 §2.4's "reorder, not reparent" scope). A true swap needs both nodes' orders exchanged
// via two sequential calls, or repeated moves would eventually collide on duplicate order values.
async function handleMoveNode(node: SchemaNodeResponse, direction: 'up' | 'down', siblings: SchemaNodeResponse[]) {
  const sorted = [...siblings].sort((a, b) => a.order - b.order)
  const index = sorted.findIndex((n) => n.id === node.id)
  const partnerIndex = direction === 'up' ? index - 1 : index + 1
  const partner = sorted[partnerIndex]
  if (!partner) return

  actionError.value = null
  try {
    await schemaVersionsApi.moveNode(versionId.value, node.id, { newOrder: partner.order })
    await schemaVersionsApi.moveNode(versionId.value, partner.id, { newOrder: node.order })
    await load()
  } catch (error) {
    actionError.value = error instanceof ApiError ? error.message : 'Could not move node.'
  }
}

onMounted(load)
</script>

<template>
  <div>
    <button type="button" class="text-sm text-slate-500 hover:text-slate-700" @click="router.back()">
      ← Back to Schema
    </button>

    <p v-if="loadError" class="mt-4 text-sm text-red-600">{{ loadError }}</p>

    <template v-if="version">
      <div class="mt-4 flex items-center justify-between">
        <div>
          <h1 class="text-lg font-semibold text-slate-900">Version {{ version.versionNumber }}</h1>
          <p class="text-sm text-slate-500">{{ version.status }}</p>
        </div>
      </div>

      <p v-if="!isEditable" class="mt-2 text-sm text-slate-500">
        Only Draft versions can be edited. This version is read-only.
      </p>
      <p v-if="actionError" class="mt-2 text-sm text-red-600">{{ actionError }}</p>

      <div class="mt-4">
        <SchemaNodeTree
          :node="version.rootNode"
          :siblings="[]"
          :depth="0"
          :editable="isEditable"
          @add-property="openAddProperty"
          @edit-node="openEditNode"
          @remove-node="handleRemoveNode"
          @move-node="handleMoveNode"
        />
      </div>
    </template>

    <Modal v-if="addParentId" title="Add Property" @close="addParentId = null">
      <form class="space-y-4" @submit.prevent="handleAddProperty">
        <div>
          <label for="add-name" class="block text-sm font-medium text-slate-700">Property name</label>
          <input
            id="add-name"
            v-model="addPropertyName"
            type="text"
            required
            class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
          />
        </div>
        <div>
          <label for="add-kind" class="block text-sm font-medium text-slate-700">Kind</label>
          <select
            id="add-kind"
            v-model="addKind"
            class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
          >
            <option v-for="kind in ADDABLE_KINDS" :key="kind" :value="kind">{{ kind }}</option>
          </select>
        </div>
        <p v-if="addError" class="text-sm text-red-600">{{ addError }}</p>
        <button
          type="submit"
          :disabled="isAdding"
          class="w-full rounded-md bg-slate-900 px-3 py-2 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
        >
          {{ isAdding ? 'Adding…' : 'Add' }}
        </button>
      </form>
    </Modal>

    <Modal v-if="editingNode" title="Edit Node" @close="editingNode = null">
      <form class="space-y-4" @submit.prevent="handleSaveNode">
        <div>
          <label for="edit-node-description" class="block text-sm font-medium text-slate-700">Description</label>
          <textarea
            id="edit-node-description"
            v-model="editDescription"
            rows="2"
            class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
          />
        </div>
        <div>
          <label for="edit-node-notes" class="block text-sm font-medium text-slate-700">
            Internal notes <span class="text-slate-400">(not exported)</span>
          </label>
          <textarea
            id="edit-node-notes"
            v-model="editNotes"
            rows="2"
            class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
          />
        </div>
        <div class="flex gap-4">
          <label class="flex items-center gap-2 text-sm text-slate-700">
            <input v-model="editNullable" type="checkbox" />
            Nullable
          </label>
          <label class="flex items-center gap-2 text-sm text-slate-700">
            <input v-model="editRequired" type="checkbox" />
            Required by parent
          </label>
        </div>

        <template v-if="editingNode.kind === 'String'">
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label for="edit-min-length" class="block text-xs font-medium text-slate-500">Min length</label>
              <input
                id="edit-min-length"
                v-model.number="editMinLength"
                type="number"
                class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
              />
            </div>
            <div>
              <label for="edit-max-length" class="block text-xs font-medium text-slate-500">Max length</label>
              <input
                id="edit-max-length"
                v-model.number="editMaxLength"
                type="number"
                class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
              />
            </div>
          </div>
          <div>
            <label for="edit-pattern" class="block text-xs font-medium text-slate-500">Pattern (regex)</label>
            <input
              id="edit-pattern"
              v-model="editPattern"
              type="text"
              class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm font-mono focus:border-slate-500 focus:outline-none"
            />
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label for="edit-format" class="block text-xs font-medium text-slate-500">Format</label>
              <select
                id="edit-format"
                v-model="editFormat"
                class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
              >
                <option value="">None</option>
                <option v-for="format in FORMATS" :key="format" :value="format">{{ format }}</option>
              </select>
            </div>
            <div v-if="editFormat === 'Custom'">
              <label for="edit-custom-format" class="block text-xs font-medium text-slate-500">Custom format name</label>
              <input
                id="edit-custom-format"
                v-model="editCustomFormat"
                type="text"
                class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
              />
            </div>
          </div>
        </template>

        <template v-if="editingNode.kind === 'Number' || editingNode.kind === 'Integer'">
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label for="edit-minimum" class="block text-xs font-medium text-slate-500">Minimum</label>
              <input
                id="edit-minimum"
                v-model.number="editMinimum"
                type="number"
                class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
              />
            </div>
            <div>
              <label for="edit-maximum" class="block text-xs font-medium text-slate-500">Maximum</label>
              <input
                id="edit-maximum"
                v-model.number="editMaximum"
                type="number"
                class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
              />
            </div>
          </div>
          <div class="flex gap-4">
            <label class="flex items-center gap-2 text-sm text-slate-700">
              <input v-model="editExclusiveMin" type="checkbox" />
              Exclusive min
            </label>
            <label class="flex items-center gap-2 text-sm text-slate-700">
              <input v-model="editExclusiveMax" type="checkbox" />
              Exclusive max
            </label>
          </div>
          <div>
            <label for="edit-multiple-of" class="block text-xs font-medium text-slate-500">Multiple of</label>
            <input
              id="edit-multiple-of"
              v-model.number="editMultipleOf"
              type="number"
              class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
            />
          </div>
        </template>

        <template v-if="editingNode.kind === 'Object'">
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label for="edit-min-props" class="block text-xs font-medium text-slate-500">Min properties</label>
              <input
                id="edit-min-props"
                v-model.number="editMinProperties"
                type="number"
                class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
              />
            </div>
            <div>
              <label for="edit-max-props" class="block text-xs font-medium text-slate-500">Max properties</label>
              <input
                id="edit-max-props"
                v-model.number="editMaxProperties"
                type="number"
                class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
              />
            </div>
          </div>
          <label class="flex items-center gap-2 text-sm text-slate-700">
            <input v-model="editAdditionalProperties" type="checkbox" />
            Allow additional properties
          </label>
        </template>

        <p v-if="editError" class="text-sm text-red-600">{{ editError }}</p>
        <button
          type="submit"
          :disabled="isSavingNode"
          class="w-full rounded-md bg-slate-900 px-3 py-2 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
        >
          {{ isSavingNode ? 'Saving…' : 'Save' }}
        </button>
      </form>
    </Modal>
  </div>
</template>
