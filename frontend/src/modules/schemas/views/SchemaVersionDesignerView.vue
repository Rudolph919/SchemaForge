<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { componentsApi } from '@/modules/components-library/api/componentsApi'
import { componentVersionsApi } from '@/modules/components-library/api/componentVersionsApi'
import {
  DOCUMENTATION_FORMATS,
  EXPORT_FORMATS,
  schemaVersionsApi,
  type DocumentationFormat,
  type ExportFormat,
} from '@/modules/schemas/api/schemaVersionsApi'
import SchemaNodeTree from '@/modules/schemas/components/SchemaNodeTree.vue'
import { ApiError } from '@/shared/api/httpClient'
import Modal from '@/shared/components/Modal.vue'
import type { ComponentDefinitionSummaryResponse, ComponentVersionSummaryResponse } from '@/types/components'
import type {
  CompositionKind,
  NodeAttachmentKind,
  NodeKind,
  SchemaDiffResponse,
  SchemaFormat,
  SchemaNodeResponse,
  SchemaVersionDetailResponse,
  SchemaVersionSummaryResponse,
  UpdateSchemaNodeRequest,
} from '@/types/schemas'
import type { ValidateJsonPayloadResponse, ValidationRunSummaryResponse } from '@/types/validation'

const ADDABLE_KINDS: NodeKind[] = ['Object', 'Array', 'String', 'Number', 'Integer', 'Boolean', 'Null']
const COMPOSITION_KINDS: CompositionKind[] = ['OneOf', 'AnyOf', 'AllOf', 'Not']

const ATTACHMENT_LABELS: Record<NodeAttachmentKind, string> = {
  ObjectProperty: 'Property',
  ArrayPrefixItem: 'Prefix Item',
  ArrayItems: 'Items',
  CompositionBranch: 'Branch',
  ConditionalIf: 'If',
  ConditionalThen: 'Then',
  ConditionalElse: 'Else',
}
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
const versionETag = ref<string | null>(null)
const loadError = ref<string | null>(null)
const actionError = ref<string | null>(null)

const isEditable = computed(() => version.value?.status === 'Draft')

async function load() {
  loadError.value = null
  try {
    const { data, etag } = await schemaVersionsApi.getVersion(versionId.value)
    version.value = data
    versionETag.value = etag
  } catch (error) {
    loadError.value = error instanceof ApiError ? error.message : 'Could not load schema version.'
  }
}

// --- Attach node (property / prefix item / items / composition branch / if / then / else) ---
const addParentId = ref<string | null>(null)
const addAttachmentKind = ref<NodeAttachmentKind>('ObjectProperty')
const addPropertyName = ref('')
const addKind = ref<NodeKind>('String')
const addError = ref<string | null>(null)
const isAdding = ref(false)

function openAttachNode(parentNodeId: string, attachmentKind: NodeAttachmentKind) {
  addParentId.value = parentNodeId
  addAttachmentKind.value = attachmentKind
  addPropertyName.value = ''
  addKind.value = 'String'
  addError.value = null
}

async function handleAttachNode() {
  if (!addParentId.value) return
  addError.value = null
  isAdding.value = true
  try {
    await schemaVersionsApi.addNode(versionId.value, {
      parentNodeId: addParentId.value,
      attachmentKind: addAttachmentKind.value,
      propertyName: addAttachmentKind.value === 'ObjectProperty' ? addPropertyName.value : null,
      kind: addKind.value,
    })
    addParentId.value = null
    await load()
  } catch (error) {
    addError.value = error instanceof ApiError ? error.message : `Could not add ${ATTACHMENT_LABELS[addAttachmentKind.value].toLowerCase()}.`
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
const editComposition = ref<CompositionKind | ''>('')
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
const editMinItems = ref<number | null>(null)
const editMaxItems = ref<number | null>(null)
const editUniqueItems = ref(false)
const editError = ref<string | null>(null)
const isSavingNode = ref(false)

// --- Component reference picker ---
// Only "Latest" is supported here, not an exact/minimum version pin - keeps the picker to two
// dropdowns (component, then one of its Published versions) instead of a third constraint-kind
// control. Reverse-resolving an existing reference's component/version pair isn't attempted (no
// "which component owns this version" lookup exists) - editReferenceVersionId is seeded from the
// node's current reference either way, so leaving the pickers untouched preserves it correctly;
// picking a new component/version just overwrites it.
const allComponents = ref<ComponentDefinitionSummaryResponse[]>([])
const editReferenceComponentId = ref('')
const editReferenceVersionId = ref('')
const referenceVersions = ref<ComponentVersionSummaryResponse[]>([])
const isLoadingReferenceVersions = ref(false)

async function ensureComponentsLoaded() {
  if (allComponents.value.length > 0) return
  try {
    allComponents.value = await componentsApi.listComponents()
  } catch {
    // Non-fatal - the picker just stays empty; the rest of node editing still works.
  }
}

async function handleReferenceComponentChange() {
  editReferenceVersionId.value = ''
  referenceVersions.value = []
  if (!editReferenceComponentId.value) return
  isLoadingReferenceVersions.value = true
  try {
    const versions = await componentVersionsApi.listVersions(editReferenceComponentId.value)
    referenceVersions.value = versions.filter((v) => v.status === 'Published')
  } catch {
    // Non-fatal
  } finally {
    isLoadingReferenceVersions.value = false
  }
}

function clearReference() {
  editReferenceComponentId.value = ''
  editReferenceVersionId.value = ''
  referenceVersions.value = []
}

// --- Local definition reference picker ---
// Simpler than the component picker above - local definitions already live on the loaded
// version (no separate fetch), and there's no "version" concept to pin, just the definition id.
const editLocalDefinitionRef = ref('')

function openEditNode(node: SchemaNodeResponse) {
  editingNode.value = node
  editDescription.value = node.description ?? ''
  editNotes.value = node.notes ?? ''
  editNullable.value = node.isNullable
  editRequired.value = node.isRequiredByParent
  editComposition.value = node.composition ?? ''
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
  editMinItems.value = node.arrayConstraints?.minItems ?? null
  editMaxItems.value = node.arrayConstraints?.maxItems ?? null
  editUniqueItems.value = node.arrayConstraints?.uniqueItems ?? false
  editReferenceComponentId.value = ''
  editReferenceVersionId.value = node.componentReference?.componentVersionId ?? ''
  referenceVersions.value = []
  editLocalDefinitionRef.value = node.localDefinitionRef ?? ''
  editError.value = null
  void ensureComponentsLoaded()
}

async function handleSaveNode() {
  if (!editingNode.value) return
  const node = editingNode.value
  editError.value = null
  if (!versionETag.value) {
    editError.value = 'Missing version ETag - reloading before saving.'
    await load()
    return
  }
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
      composition: editComposition.value || null,
      componentReference: editReferenceVersionId.value
        ? { componentVersionId: editReferenceVersionId.value, constraint: { kind: 'Latest', version: null } }
        : null,
      localDefinitionRef: editLocalDefinitionRef.value || null,
      objectConstraints:
        node.kind === 'Object'
          ? {
              minProperties: editMinProperties.value,
              maxProperties: editMaxProperties.value,
              additionalPropertiesAllowed: editAdditionalProperties.value,
            }
          : null,
      arrayConstraints:
        node.kind === 'Array'
          ? { minItems: editMinItems.value, maxItems: editMaxItems.value, uniqueItems: editUniqueItems.value }
          : null,
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

    await schemaVersionsApi.updateNode(versionId.value, node.id, request, versionETag.value)
    editingNode.value = null
    await load()
  } catch (error) {
    if (error instanceof ApiError && error.status === 409) {
      editError.value = 'This version changed elsewhere. Reloaded the latest version - please redo your edit.'
      await load()
    } else {
      editError.value = error instanceof ApiError ? error.message : 'Could not save node.'
    }
  } finally {
    isSavingNode.value = false
  }
}

// --- Remove / move ---
async function handleRemoveNode(node: SchemaNodeResponse) {
  actionError.value = null
  if (!versionETag.value) {
    actionError.value = 'Missing version ETag - reloading before removing.'
    await load()
    return
  }
  try {
    await schemaVersionsApi.removeNode(versionId.value, node.id, versionETag.value)
    await load()
  } catch (error) {
    if (error instanceof ApiError && error.status === 409) {
      actionError.value = 'This version changed elsewhere. Reloaded the latest version - please retry.'
      await load()
    } else {
      actionError.value = error instanceof ApiError ? error.message : 'Could not remove node.'
    }
  }
}

// MoveNode sets a node's Order to exactly the value it's given - it doesn't renumber siblings
// (openReparentNode/handleReparentNode below is the move-to-a-different-parent counterpart). A
// true swap needs both nodes' orders exchanged via two sequential calls, or repeated moves would
// eventually collide on duplicate order values.
async function handleMoveNode(node: SchemaNodeResponse, direction: 'up' | 'down', siblings: SchemaNodeResponse[]) {
  const sorted = [...siblings].sort((a, b) => a.order - b.order)
  const index = sorted.findIndex((n) => n.id === node.id)
  const partnerIndex = direction === 'up' ? index - 1 : index + 1
  const partner = sorted[partnerIndex]
  if (!partner) return

  actionError.value = null
  try {
    await schemaVersionsApi.moveNode(versionId.value, node.id, {
      newOrder: partner.order,
      newParentNodeId: null,
      attachmentKind: null,
      propertyName: null,
    })
    await schemaVersionsApi.moveNode(versionId.value, partner.id, {
      newOrder: node.order,
      newParentNodeId: null,
      attachmentKind: null,
      propertyName: null,
    })
    await load()
  } catch (error) {
    actionError.value = error instanceof ApiError ? error.message : 'Could not move node.'
  }
}

// --- Reparent (move to a different parent node) ---
interface NodeOption {
  id: string
  label: string
}

// Depth-first path label for every node in the tree, so the "new parent" picker reads like
// "customer.address" rather than a bare, meaningless id.
function flattenNodeOptions(node: SchemaNodeResponse, path: string, into: NodeOption[]) {
  into.push({ id: node.id, label: path })
  for (const child of [...node.properties].sort((a, b) => a.order - b.order)) {
    flattenNodeOptions(child, `${path}.${child.propertyName}`, into)
  }
  for (const [i, child] of [...node.prefixItems].sort((a, b) => a.order - b.order).entries()) {
    flattenNodeOptions(child, `${path}[${i}]`, into)
  }
  if (node.itemsNode) flattenNodeOptions(node.itemsNode, `${path}[]`, into)
  for (const [i, child] of [...node.compositionBranches].sort((a, b) => a.order - b.order).entries()) {
    flattenNodeOptions(child, `${path}(branch ${i})`, into)
  }
  if (node.ifNode) flattenNodeOptions(node.ifNode, `${path}(if)`, into)
  if (node.thenNode) flattenNodeOptions(node.thenNode, `${path}(then)`, into)
  if (node.elseNode) flattenNodeOptions(node.elseNode, `${path}(else)`, into)
}

function collectNodeAndDescendantIds(node: SchemaNodeResponse, into: Set<string>) {
  into.add(node.id)
  for (const child of node.properties) collectNodeAndDescendantIds(child, into)
  for (const child of node.prefixItems) collectNodeAndDescendantIds(child, into)
  if (node.itemsNode) collectNodeAndDescendantIds(node.itemsNode, into)
  for (const child of node.compositionBranches) collectNodeAndDescendantIds(child, into)
  if (node.ifNode) collectNodeAndDescendantIds(node.ifNode, into)
  if (node.thenNode) collectNodeAndDescendantIds(node.thenNode, into)
  if (node.elseNode) collectNodeAndDescendantIds(node.elseNode, into)
}

const reparentingNode = ref<SchemaNodeResponse | null>(null)
const reparentTargetParentId = ref('')
const reparentAttachmentKind = ref<NodeAttachmentKind>('ObjectProperty')
const reparentPropertyName = ref('')
const reparentError = ref<string | null>(null)
const isReparenting = ref(false)

// Excludes the node itself and its own descendants - moving it under one of them would detach
// the very subtree the prospective new parent lives in (the backend rejects this too, but
// filtering it out of the picker means the user never sees it as an option in the first place).
const reparentCandidates = computed<NodeOption[]>(() => {
  if (!version.value || !reparentingNode.value) return []
  const excluded = new Set<string>()
  collectNodeAndDescendantIds(reparentingNode.value, excluded)
  const options: NodeOption[] = []
  flattenNodeOptions(version.value.rootNode, '(root)', options)
  return options.filter((o) => !excluded.has(o.id))
})

function openReparentNode(node: SchemaNodeResponse) {
  reparentingNode.value = node
  reparentTargetParentId.value = ''
  reparentAttachmentKind.value = 'ObjectProperty'
  reparentPropertyName.value = node.propertyName ?? ''
  reparentError.value = null
}

async function handleReparentNode() {
  if (!reparentingNode.value || !reparentTargetParentId.value) return
  reparentError.value = null
  isReparenting.value = true
  try {
    await schemaVersionsApi.moveNode(versionId.value, reparentingNode.value.id, {
      newOrder: 0,
      newParentNodeId: reparentTargetParentId.value,
      attachmentKind: reparentAttachmentKind.value,
      propertyName: reparentAttachmentKind.value === 'ObjectProperty' ? reparentPropertyName.value : null,
    })
    reparentingNode.value = null
    await load()
  } catch (error) {
    reparentError.value = error instanceof ApiError ? error.message : 'Could not move node.'
  } finally {
    isReparenting.value = false
  }
}

// --- Local definitions (within-version reuse for recursive schemas) ---
const newLocalDefinitionName = ref('')
const newLocalDefinitionRootKind = ref<NodeKind>('Object')
const localDefinitionError = ref<string | null>(null)
const isCreatingLocalDefinition = ref(false)

async function handleCreateLocalDefinition() {
  localDefinitionError.value = null
  isCreatingLocalDefinition.value = true
  try {
    await schemaVersionsApi.addLocalDefinition(versionId.value, {
      name: newLocalDefinitionName.value,
      rootKind: newLocalDefinitionRootKind.value,
    })
    newLocalDefinitionName.value = ''
    await load()
  } catch (error) {
    localDefinitionError.value = error instanceof ApiError ? error.message : 'Could not create local definition.'
  } finally {
    isCreatingLocalDefinition.value = false
  }
}

async function handleRemoveLocalDefinition(localDefinitionId: string) {
  localDefinitionError.value = null
  if (!versionETag.value) {
    localDefinitionError.value = 'Missing version ETag - reloading before removing.'
    await load()
    return
  }
  try {
    await schemaVersionsApi.removeLocalDefinition(versionId.value, localDefinitionId, versionETag.value)
    await load()
  } catch (error) {
    if (error instanceof ApiError && error.status === 409) {
      localDefinitionError.value = 'This version changed elsewhere. Reloaded the latest version - please retry.'
      await load()
    } else {
      localDefinitionError.value = error instanceof ApiError ? error.message : 'Could not remove local definition.'
    }
  }
}

// --- Validate ---
const validatePayloadText = ref('{\n  \n}')
const validateResult = ref<ValidateJsonPayloadResponse | null>(null)
const validateError = ref<string | null>(null)
const isValidating = ref(false)
const validationRuns = ref<ValidationRunSummaryResponse[]>([])
const isHistoryOpen = ref(false)

async function handleValidate() {
  validateError.value = null
  validateResult.value = null
  let payload: unknown
  try {
    payload = JSON.parse(validatePayloadText.value)
  } catch {
    validateError.value = 'Not valid JSON.'
    return
  }

  isValidating.value = true
  try {
    validateResult.value = await schemaVersionsApi.validate(versionId.value, payload)
    if (isHistoryOpen.value) await loadValidationRuns()
  } catch (error) {
    validateError.value = error instanceof ApiError ? error.message : 'Could not run validation.'
  } finally {
    isValidating.value = false
  }
}

async function loadValidationRuns() {
  try {
    validationRuns.value = await schemaVersionsApi.listValidationRuns(versionId.value)
  } catch (error) {
    validateError.value = error instanceof ApiError ? error.message : 'Could not load validation history.'
  }
}

async function toggleHistory() {
  isHistoryOpen.value = !isHistoryOpen.value
  if (isHistoryOpen.value && validationRuns.value.length === 0) {
    await loadValidationRuns()
  }
}

// --- Export ---
const EXPORT_EXTENSIONS: Record<ExportFormat, string> = {
  'json-schema': 'json',
  openapi: 'json',
  typescript: 'ts',
  csharp: 'cs',
}
const exportFormat = ref<ExportFormat>('json-schema')
const isExporting = ref(false)
const exportError = ref<string | null>(null)

function downloadText(content: string, filename: string) {
  const url = URL.createObjectURL(new Blob([content], { type: 'text/plain' }))
  const link = document.createElement('a')
  link.href = url
  link.download = filename
  link.click()
  URL.revokeObjectURL(url)
}

async function handleExport() {
  exportError.value = null
  isExporting.value = true
  try {
    const content = await schemaVersionsApi.export(versionId.value, exportFormat.value)
    downloadText(content, `${version.value?.versionNumber ?? 'schema'}.${EXPORT_EXTENSIONS[exportFormat.value]}`)
  } catch (error) {
    exportError.value = error instanceof ApiError ? error.message : 'Could not export.'
  } finally {
    isExporting.value = false
  }
}

// --- Documentation ---
const DOCUMENTATION_MIME_TYPES: Record<DocumentationFormat, string> = {
  html: 'text/html',
  markdown: 'text/markdown',
  json: 'application/json',
}
const documentationFormat = ref<DocumentationFormat>('html')
const isLoadingDocumentation = ref(false)
const documentationError = ref<string | null>(null)

async function handleViewDocumentation() {
  documentationError.value = null
  isLoadingDocumentation.value = true
  try {
    const content = await schemaVersionsApi.documentation(versionId.value, documentationFormat.value)
    // Opened as a Blob URL rather than rendered inline - html needs real page rendering, and
    // markdown/json are just as readable as a standalone tab. Deliberately not revoking the
    // object URL here: the new tab still needs it after this function returns.
    window.open(URL.createObjectURL(new Blob([content], { type: DOCUMENTATION_MIME_TYPES[documentationFormat.value] })), '_blank')
  } catch (error) {
    documentationError.value = error instanceof ApiError ? error.message : 'Could not load documentation.'
  } finally {
    isLoadingDocumentation.value = false
  }
}

// --- Diff ---
const allVersions = ref<SchemaVersionSummaryResponse[]>([])
const diffAgainstId = ref('')
const diffResult = ref<SchemaDiffResponse | null>(null)
const diffError = ref<string | null>(null)
const isDiffing = ref(false)

async function loadOtherVersions() {
  if (!version.value) return
  try {
    allVersions.value = (await schemaVersionsApi.listVersions(version.value.schemaDefinitionId)).filter(
      (v) => v.id !== versionId.value,
    )
  } catch {
    // Non-fatal - the picker just stays empty.
  }
}

async function handleDiff() {
  if (!diffAgainstId.value) return
  diffError.value = null
  diffResult.value = null
  isDiffing.value = true
  try {
    diffResult.value = await schemaVersionsApi.diff(versionId.value, diffAgainstId.value)
  } catch (error) {
    diffError.value = error instanceof ApiError ? error.message : 'Could not compute diff.'
  } finally {
    isDiffing.value = false
  }
}

onMounted(async () => {
  await load()
  await loadOtherVersions()
})
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
          @attach-node="openAttachNode"
          @edit-node="openEditNode"
          @remove-node="handleRemoveNode"
          @move-node="handleMoveNode"
          @reparent-node="openReparentNode"
        />
      </div>

      <div class="mt-6 rounded-lg border border-slate-200 bg-white p-6">
        <h2 class="text-base font-semibold text-slate-900">Local Definitions</h2>
        <p class="mt-1 text-sm text-slate-500">
          Reusable node subtrees within this version - reference one from a node's edit form to reuse it recursively.
        </p>

        <form
          v-if="isEditable"
          class="mt-4 flex flex-wrap items-end gap-2 border-b border-slate-100 pb-4"
          @submit.prevent="handleCreateLocalDefinition"
        >
          <div class="flex-1">
            <label for="new-local-definition-name" class="block text-xs font-medium text-slate-500">Name</label>
            <input
              id="new-local-definition-name"
              v-model="newLocalDefinitionName"
              type="text"
              required
              class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
            />
          </div>
          <div>
            <label for="new-local-definition-kind" class="block text-xs font-medium text-slate-500">Root kind</label>
            <select
              id="new-local-definition-kind"
              v-model="newLocalDefinitionRootKind"
              class="mt-1 rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
            >
              <option v-for="kind in ADDABLE_KINDS" :key="kind" :value="kind">{{ kind }}</option>
            </select>
          </div>
          <button
            type="submit"
            :disabled="isCreatingLocalDefinition"
            class="rounded-md bg-slate-900 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
          >
            {{ isCreatingLocalDefinition ? 'Creating…' : 'New Definition' }}
          </button>
        </form>

        <p v-if="localDefinitionError" class="mt-2 text-sm text-red-600">{{ localDefinitionError }}</p>
        <p v-if="version.localDefinitions.length === 0" class="mt-4 text-sm text-slate-500">No local definitions yet.</p>

        <ul v-else class="mt-4 space-y-2">
          <li
            v-for="definition in version.localDefinitions"
            :key="definition.id"
            class="flex items-center justify-between rounded-md border border-slate-100 px-3 py-2"
          >
            <div>
              <span class="font-mono text-sm font-medium text-slate-900">{{ definition.name }}</span>
              <span class="ml-2 rounded bg-slate-100 px-1.5 py-0.5 text-xs text-slate-600">
                {{ definition.rootNode.kind ?? 'Unspecified' }}
              </span>
            </div>
            <button
              v-if="isEditable"
              type="button"
              class="text-slate-400 hover:text-red-600"
              @click="handleRemoveLocalDefinition(definition.id)"
            >
              Remove
            </button>
          </li>
        </ul>
      </div>

      <div class="mt-6 rounded-lg border border-slate-200 bg-white p-6">
        <h2 class="text-base font-semibold text-slate-900">Export &amp; Documentation</h2>

        <div class="mt-4 grid grid-cols-1 gap-6 sm:grid-cols-2">
          <div>
            <label for="export-format" class="block text-sm font-medium text-slate-700">Export format</label>
            <div class="mt-1 flex gap-2">
              <select
                id="export-format"
                v-model="exportFormat"
                class="rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
              >
                <option v-for="format in EXPORT_FORMATS" :key="format" :value="format">{{ format }}</option>
              </select>
              <button
                type="button"
                :disabled="isExporting"
                class="rounded-md bg-slate-900 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
                @click="handleExport"
              >
                {{ isExporting ? 'Exporting…' : 'Download' }}
              </button>
            </div>
            <p v-if="exportError" class="mt-2 text-sm text-red-600">{{ exportError }}</p>
          </div>

          <div>
            <label for="documentation-format" class="block text-sm font-medium text-slate-700">Documentation format</label>
            <div class="mt-1 flex gap-2">
              <select
                id="documentation-format"
                v-model="documentationFormat"
                class="rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
              >
                <option v-for="format in DOCUMENTATION_FORMATS" :key="format" :value="format">{{ format }}</option>
              </select>
              <button
                type="button"
                :disabled="isLoadingDocumentation"
                class="rounded-md bg-slate-900 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
                @click="handleViewDocumentation"
              >
                {{ isLoadingDocumentation ? 'Loading…' : 'View' }}
              </button>
            </div>
            <p v-if="documentationError" class="mt-2 text-sm text-red-600">{{ documentationError }}</p>
          </div>
        </div>
      </div>

      <div class="mt-6 rounded-lg border border-slate-200 bg-white p-6">
        <h2 class="text-base font-semibold text-slate-900">Compare</h2>
        <p class="mt-1 text-sm text-slate-500">Diff this version against another version of the same schema.</p>

        <div class="mt-4 flex gap-2">
          <select
            v-model="diffAgainstId"
            class="rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
          >
            <option value="">Select a version…</option>
            <option v-for="v in allVersions" :key="v.id" :value="v.id">{{ v.versionNumber }} ({{ v.status }})</option>
          </select>
          <button
            type="button"
            :disabled="isDiffing || !diffAgainstId"
            class="rounded-md bg-slate-900 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
            @click="handleDiff"
          >
            {{ isDiffing ? 'Comparing…' : 'Compare' }}
          </button>
        </div>
        <p v-if="diffError" class="mt-2 text-sm text-red-600">{{ diffError }}</p>

        <div v-if="diffResult" class="mt-4 space-y-3">
          <div v-if="diffResult.addedPaths.length > 0">
            <h3 class="text-xs font-semibold uppercase text-emerald-700">Added</h3>
            <ul class="mt-1 space-y-0.5">
              <li v-for="p in diffResult.addedPaths" :key="p" class="font-mono text-xs text-emerald-800">{{ p }}</li>
            </ul>
          </div>
          <div v-if="diffResult.removedPaths.length > 0">
            <h3 class="text-xs font-semibold uppercase text-red-700">Removed</h3>
            <ul class="mt-1 space-y-0.5">
              <li v-for="p in diffResult.removedPaths" :key="p" class="font-mono text-xs text-red-800">{{ p }}</li>
            </ul>
          </div>
          <div v-if="diffResult.changedPaths.length > 0">
            <h3 class="text-xs font-semibold uppercase text-amber-700">Changed</h3>
            <ul class="mt-1 space-y-0.5">
              <li v-for="c in diffResult.changedPaths" :key="c.path" class="font-mono text-xs text-amber-800">
                {{ c.path }} <span class="text-slate-500">({{ c.changes.join(', ') }})</span>
              </li>
            </ul>
          </div>
          <p
            v-if="diffResult.addedPaths.length === 0 && diffResult.removedPaths.length === 0 && diffResult.changedPaths.length === 0"
            class="text-sm text-slate-500"
          >
            No differences.
          </p>
        </div>
      </div>

      <div class="mt-6 rounded-lg border border-slate-200 bg-white p-6">
        <h2 class="text-base font-semibold text-slate-900">Validate</h2>
        <p class="mt-1 text-sm text-slate-500">
          Check a JSON payload against this version. Persists a validation run either way.
        </p>

        <form class="mt-4" @submit.prevent="handleValidate">
          <textarea
            v-model="validatePayloadText"
            rows="8"
            spellcheck="false"
            class="w-full rounded-md border border-slate-300 px-3 py-2 font-mono text-sm focus:border-slate-500 focus:outline-none"
          />
          <button
            type="submit"
            :disabled="isValidating"
            class="mt-2 rounded-md bg-slate-900 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
          >
            {{ isValidating ? 'Validating…' : 'Validate' }}
          </button>
        </form>

        <p v-if="validateError" class="mt-3 text-sm text-red-600">{{ validateError }}</p>

        <div v-if="validateResult" class="mt-4 rounded-md border border-slate-100 p-3">
          <span
            class="rounded-full px-2 py-0.5 text-xs font-medium"
            :class="validateResult.outcome === 'Valid' ? 'bg-emerald-100 text-emerald-800' : 'bg-red-100 text-red-800'"
          >
            {{ validateResult.outcome }}
          </span>
          <ul v-if="validateResult.errors.length > 0" class="mt-2 space-y-1 text-sm">
            <li v-for="(err, i) in validateResult.errors" :key="i" class="text-slate-700">
              <span class="font-mono text-xs text-slate-500">{{ err.path }}</span>
              — {{ err.message }}
              <span class="text-xs text-slate-400">({{ err.code }}{{ err.severity === 'Warning' ? ', warning' : '' }})</span>
            </li>
          </ul>
        </div>

        <button type="button" class="mt-4 text-sm text-slate-500 hover:text-slate-700" @click="toggleHistory">
          {{ isHistoryOpen ? 'Hide' : 'Show' }} validation history
        </button>

        <table v-if="isHistoryOpen" class="mt-3 w-full text-sm">
          <tbody>
            <tr v-if="validationRuns.length === 0">
              <td class="py-2 text-slate-500">No validation runs yet.</td>
            </tr>
            <tr v-for="run in validationRuns" :key="run.id" class="border-b border-slate-100 last:border-0">
              <td class="py-2">
                <span
                  class="rounded-full px-2 py-0.5 text-xs font-medium"
                  :class="run.outcome === 'Valid' ? 'bg-emerald-100 text-emerald-800' : 'bg-red-100 text-red-800'"
                >
                  {{ run.outcome }}
                </span>
              </td>
              <td class="py-2 text-slate-500">{{ run.errors.length }} {{ run.errors.length === 1 ? 'error' : 'errors' }}</td>
              <td class="py-2 text-right text-slate-400">{{ new Date(run.executedAt).toLocaleString() }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>

    <Modal v-if="addParentId" :title="`Add ${ATTACHMENT_LABELS[addAttachmentKind]}`" @close="addParentId = null">
      <form class="space-y-4" @submit.prevent="handleAttachNode">
        <div v-if="addAttachmentKind === 'ObjectProperty'">
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

        <div>
          <label for="edit-composition" class="block text-sm font-medium text-slate-700">
            Composition <span class="text-slate-400">(oneOf / anyOf / allOf / not)</span>
          </label>
          <select
            id="edit-composition"
            v-model="editComposition"
            class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
          >
            <option value="">None</option>
            <option v-for="kind in COMPOSITION_KINDS" :key="kind" :value="kind">{{ kind }}</option>
          </select>
        </div>

        <div>
          <label class="block text-sm font-medium text-slate-700">
            Reusable Component <span class="text-slate-400">(optional)</span>
          </label>
          <p v-if="editReferenceVersionId && !editReferenceComponentId" class="mt-1 text-xs text-slate-500">
            Currently references version <span class="font-mono">{{ editReferenceVersionId }}</span>. Pick a
            component below to point at a different one, or clear it.
          </p>
          <div class="mt-1 grid grid-cols-2 gap-3">
            <select
              v-model="editReferenceComponentId"
              class="rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
              @change="handleReferenceComponentChange"
            >
              <option value="">Select a component…</option>
              <option v-for="c in allComponents" :key="c.id" :value="c.id">{{ c.name }}</option>
            </select>
            <select
              v-if="editReferenceComponentId"
              v-model="editReferenceVersionId"
              class="rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
            >
              <option value="" disabled>
                {{
                  isLoadingReferenceVersions
                    ? 'Loading…'
                    : referenceVersions.length === 0
                      ? 'No published versions'
                      : 'Select version'
                }}
              </option>
              <option v-for="v in referenceVersions" :key="v.id" :value="v.id">{{ v.versionNumber }}</option>
            </select>
          </div>
          <button
            v-if="editReferenceVersionId"
            type="button"
            class="mt-1 text-xs text-slate-500 hover:text-red-600"
            @click="clearReference"
          >
            Clear reference
          </button>
        </div>

        <div>
          <label for="edit-local-definition-ref" class="block text-sm font-medium text-slate-700">
            Local Definition Reference <span class="text-slate-400">(optional)</span>
          </label>
          <select
            id="edit-local-definition-ref"
            v-model="editLocalDefinitionRef"
            class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
          >
            <option value="">None</option>
            <option v-for="d in version?.localDefinitions ?? []" :key="d.id" :value="d.id">{{ d.name }}</option>
          </select>
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

        <template v-if="editingNode.kind === 'Array'">
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label for="edit-min-items" class="block text-xs font-medium text-slate-500">Min items</label>
              <input
                id="edit-min-items"
                v-model.number="editMinItems"
                type="number"
                class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
              />
            </div>
            <div>
              <label for="edit-max-items" class="block text-xs font-medium text-slate-500">Max items</label>
              <input
                id="edit-max-items"
                v-model.number="editMaxItems"
                type="number"
                class="mt-1 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:border-slate-500 focus:outline-none"
              />
            </div>
          </div>
          <label class="flex items-center gap-2 text-sm text-slate-700">
            <input v-model="editUniqueItems" type="checkbox" />
            Require unique items
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

    <Modal
      v-if="reparentingNode"
      :title="`Move ${reparentingNode.propertyName ?? '(unnamed)'} to…`"
      @close="reparentingNode = null"
    >
      <form class="space-y-4" @submit.prevent="handleReparentNode">
        <div>
          <label for="reparent-target" class="block text-sm font-medium text-slate-700">New parent</label>
          <select
            id="reparent-target"
            v-model="reparentTargetParentId"
            required
            class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 font-mono text-sm focus:border-slate-500 focus:outline-none"
          >
            <option value="" disabled>Select a node…</option>
            <option v-for="option in reparentCandidates" :key="option.id" :value="option.id">{{ option.label }}</option>
          </select>
        </div>
        <div>
          <label for="reparent-kind" class="block text-sm font-medium text-slate-700">Attach as</label>
          <select
            id="reparent-kind"
            v-model="reparentAttachmentKind"
            class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
          >
            <option v-for="(label, kind) in ATTACHMENT_LABELS" :key="kind" :value="kind">{{ label }}</option>
          </select>
        </div>
        <div v-if="reparentAttachmentKind === 'ObjectProperty'">
          <label for="reparent-property-name" class="block text-sm font-medium text-slate-700">Property name</label>
          <input
            id="reparent-property-name"
            v-model="reparentPropertyName"
            type="text"
            required
            class="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
          />
        </div>
        <p v-if="reparentError" class="text-sm text-red-600">{{ reparentError }}</p>
        <button
          type="submit"
          :disabled="isReparenting || !reparentTargetParentId"
          class="w-full rounded-md bg-slate-900 px-3 py-2 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
        >
          {{ isReparenting ? 'Moving…' : 'Move' }}
        </button>
      </form>
    </Modal>
  </div>
</template>
