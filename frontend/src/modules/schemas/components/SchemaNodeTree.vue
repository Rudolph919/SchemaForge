<script setup lang="ts">
import { computed } from 'vue'
import type { NodeAttachmentKind, SchemaNodeResponse } from '@/types/schemas'

const props = defineProps<{
  node: SchemaNodeResponse
  siblings: SchemaNodeResponse[]
  depth: number
  editable: boolean
  slotLabel?: string
}>()

const emit = defineEmits<{
  attachNode: [parentNodeId: string, attachmentKind: NodeAttachmentKind]
  editNode: [node: SchemaNodeResponse]
  removeNode: [node: SchemaNodeResponse]
  moveNode: [node: SchemaNodeResponse, direction: 'up' | 'down', siblings: SchemaNodeResponse[]]
}>()

const sortedProperties = computed(() => [...props.node.properties].sort((a, b) => a.order - b.order))
const sortedPrefixItems = computed(() => [...props.node.prefixItems].sort((a, b) => a.order - b.order))
const sortedBranches = computed(() => [...props.node.compositionBranches].sort((a, b) => a.order - b.order))

const sortedSiblings = computed(() => [...props.siblings].sort((a, b) => a.order - b.order))
const siblingIndex = computed(() => sortedSiblings.value.findIndex((n) => n.id === props.node.id))
const isRoot = computed(() => props.depth === 0)
const canReorder = computed(() => props.depth > 0 && !props.slotLabel)

function kindSummary(node: SchemaNodeResponse): string {
  const parts: string[] = []
  switch (node.kind) {
    case 'String':
      parts.push(node.stringConstraints?.pattern ? `String (pattern: ${node.stringConstraints.pattern})` : 'String')
      break
    case 'Number':
    case 'Integer': {
      const c = node.numericConstraints
      parts.push(!c || (c.minimum == null && c.maximum == null) ? node.kind : `${node.kind} (${c.minimum ?? '-∞'}..${c.maximum ?? '∞'})`)
      break
    }
    case 'Object':
      parts.push(`Object (${node.properties.length} ${node.properties.length === 1 ? 'property' : 'properties'})`)
      break
    case 'Array':
      parts.push(node.itemsNode ? 'Array of ' + (node.itemsNode.kind ?? 'unspecified') : 'Array (no items set)')
      break
    default:
      parts.push(node.kind ?? 'Unspecified')
  }
  if (node.composition) {
    parts.push(`${node.composition} (${node.compositionBranches.length})`)
  }
  return parts.join(' · ')
}
</script>

<template>
  <div class="rounded-md border border-slate-200 bg-white">
    <div class="flex items-center justify-between gap-2 px-3 py-2">
      <div class="min-w-0">
        <div class="flex items-center gap-2">
          <span v-if="slotLabel" class="rounded bg-indigo-50 px-1.5 py-0.5 text-xs font-medium text-indigo-700">
            {{ slotLabel }}
          </span>
          <span v-if="node.propertyName" class="font-mono text-sm font-medium text-slate-900">
            {{ node.propertyName }}
          </span>
          <span v-else-if="!slotLabel" class="text-sm font-medium text-slate-400 italic">(root)</span>
          <span class="rounded bg-slate-100 px-1.5 py-0.5 text-xs text-slate-600">{{ kindSummary(node) }}</span>
          <span v-if="node.isRequiredByParent" class="text-xs text-amber-700">required</span>
          <span v-if="node.isNullable" class="text-xs text-slate-400">nullable</span>
        </div>
        <p v-if="node.description" class="mt-0.5 truncate text-xs text-slate-500">{{ node.description }}</p>
      </div>

      <div v-if="editable" class="flex shrink-0 flex-wrap items-center justify-end gap-1">
        <template v-if="canReorder">
          <button
            type="button"
            :disabled="siblingIndex <= 0"
            class="rounded px-1.5 py-0.5 text-xs text-slate-500 hover:bg-slate-100 disabled:opacity-30"
            title="Move up"
            @click="emit('moveNode', node, 'up', siblings)"
          >
            ↑
          </button>
          <button
            type="button"
            :disabled="siblingIndex === -1 || siblingIndex >= sortedSiblings.length - 1"
            class="rounded px-1.5 py-0.5 text-xs text-slate-500 hover:bg-slate-100 disabled:opacity-30"
            title="Move down"
            @click="emit('moveNode', node, 'down', siblings)"
          >
            ↓
          </button>
        </template>
        <button type="button" class="rounded px-2 py-0.5 text-xs text-slate-600 hover:bg-slate-100" @click="emit('editNode', node)">
          Edit
        </button>
        <button
          v-if="node.kind === 'Object'"
          type="button"
          class="rounded px-2 py-0.5 text-xs text-slate-600 hover:bg-slate-100"
          @click="emit('attachNode', node.id, 'ObjectProperty')"
        >
          + Property
        </button>
        <template v-if="node.kind === 'Array'">
          <button
            type="button"
            class="rounded px-2 py-0.5 text-xs text-slate-600 hover:bg-slate-100"
            @click="emit('attachNode', node.id, 'ArrayItems')"
          >
            {{ node.itemsNode ? 'Replace Items' : 'Set Items' }}
          </button>
          <button
            type="button"
            class="rounded px-2 py-0.5 text-xs text-slate-600 hover:bg-slate-100"
            @click="emit('attachNode', node.id, 'ArrayPrefixItem')"
          >
            + Prefix Item
          </button>
        </template>
        <button
          v-if="node.composition"
          type="button"
          class="rounded px-2 py-0.5 text-xs text-slate-600 hover:bg-slate-100"
          @click="emit('attachNode', node.id, 'CompositionBranch')"
        >
          + Branch
        </button>
        <button
          type="button"
          class="rounded px-2 py-0.5 text-xs text-slate-600 hover:bg-slate-100"
          @click="emit('attachNode', node.id, 'ConditionalIf')"
        >
          {{ node.ifNode ? 'Replace If' : 'Set If' }}
        </button>
        <button
          type="button"
          class="rounded px-2 py-0.5 text-xs text-slate-600 hover:bg-slate-100"
          @click="emit('attachNode', node.id, 'ConditionalThen')"
        >
          {{ node.thenNode ? 'Replace Then' : 'Set Then' }}
        </button>
        <button
          type="button"
          class="rounded px-2 py-0.5 text-xs text-slate-600 hover:bg-slate-100"
          @click="emit('attachNode', node.id, 'ConditionalElse')"
        >
          {{ node.elseNode ? 'Replace Else' : 'Set Else' }}
        </button>
        <button
          v-if="!isRoot"
          type="button"
          class="rounded px-2 py-0.5 text-xs text-red-500 hover:bg-red-50"
          @click="emit('removeNode', node)"
        >
          Remove
        </button>
      </div>
    </div>

    <div
      v-if="
        sortedProperties.length > 0 ||
        sortedPrefixItems.length > 0 ||
        sortedBranches.length > 0 ||
        node.itemsNode ||
        node.ifNode ||
        node.thenNode ||
        node.elseNode
      "
      class="space-y-2 border-t border-slate-100 p-2 pl-6"
    >
      <SchemaNodeTree
        v-for="child in sortedProperties"
        :key="child.id"
        :node="child"
        :siblings="node.properties"
        :depth="depth + 1"
        :editable="editable"
        @attach-node="(id, kind) => emit('attachNode', id, kind)"
        @edit-node="(n) => emit('editNode', n)"
        @remove-node="(n) => emit('removeNode', n)"
        @move-node="(n, dir, sibs) => emit('moveNode', n, dir, sibs)"
      />
      <SchemaNodeTree
        v-for="(child, i) in sortedPrefixItems"
        :key="child.id"
        :node="child"
        :siblings="node.prefixItems"
        :depth="depth + 1"
        :editable="editable"
        :slot-label="`[${i}]`"
        @attach-node="(id, kind) => emit('attachNode', id, kind)"
        @edit-node="(n) => emit('editNode', n)"
        @remove-node="(n) => emit('removeNode', n)"
        @move-node="(n, dir, sibs) => emit('moveNode', n, dir, sibs)"
      />
      <SchemaNodeTree
        v-if="node.itemsNode"
        :node="node.itemsNode"
        :siblings="[]"
        :depth="depth + 1"
        :editable="editable"
        slot-label="items"
        @attach-node="(id, kind) => emit('attachNode', id, kind)"
        @edit-node="(n) => emit('editNode', n)"
        @remove-node="(n) => emit('removeNode', n)"
        @move-node="(n, dir, sibs) => emit('moveNode', n, dir, sibs)"
      />
      <SchemaNodeTree
        v-for="(child, i) in sortedBranches"
        :key="child.id"
        :node="child"
        :siblings="node.compositionBranches"
        :depth="depth + 1"
        :editable="editable"
        :slot-label="`branch ${i}`"
        @attach-node="(id, kind) => emit('attachNode', id, kind)"
        @edit-node="(n) => emit('editNode', n)"
        @remove-node="(n) => emit('removeNode', n)"
        @move-node="(n, dir, sibs) => emit('moveNode', n, dir, sibs)"
      />
      <SchemaNodeTree
        v-if="node.ifNode"
        :node="node.ifNode"
        :siblings="[]"
        :depth="depth + 1"
        :editable="editable"
        slot-label="if"
        @attach-node="(id, kind) => emit('attachNode', id, kind)"
        @edit-node="(n) => emit('editNode', n)"
        @remove-node="(n) => emit('removeNode', n)"
        @move-node="(n, dir, sibs) => emit('moveNode', n, dir, sibs)"
      />
      <SchemaNodeTree
        v-if="node.thenNode"
        :node="node.thenNode"
        :siblings="[]"
        :depth="depth + 1"
        :editable="editable"
        slot-label="then"
        @attach-node="(id, kind) => emit('attachNode', id, kind)"
        @edit-node="(n) => emit('editNode', n)"
        @remove-node="(n) => emit('removeNode', n)"
        @move-node="(n, dir, sibs) => emit('moveNode', n, dir, sibs)"
      />
      <SchemaNodeTree
        v-if="node.elseNode"
        :node="node.elseNode"
        :siblings="[]"
        :depth="depth + 1"
        :editable="editable"
        slot-label="else"
        @attach-node="(id, kind) => emit('attachNode', id, kind)"
        @edit-node="(n) => emit('editNode', n)"
        @remove-node="(n) => emit('removeNode', n)"
        @move-node="(n, dir, sibs) => emit('moveNode', n, dir, sibs)"
      />
    </div>
  </div>
</template>
