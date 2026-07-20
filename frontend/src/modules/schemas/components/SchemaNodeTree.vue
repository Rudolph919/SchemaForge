<script setup lang="ts">
import { computed } from 'vue'
import type { SchemaNodeResponse } from '@/types/schemas'

const props = defineProps<{
  node: SchemaNodeResponse
  siblings: SchemaNodeResponse[]
  depth: number
  editable: boolean
}>()

const emit = defineEmits<{
  addProperty: [parentNodeId: string]
  editNode: [node: SchemaNodeResponse]
  removeNode: [node: SchemaNodeResponse]
  moveNode: [node: SchemaNodeResponse, direction: 'up' | 'down', siblings: SchemaNodeResponse[]]
}>()

const sortedProperties = computed(() => [...props.node.properties].sort((a, b) => a.order - b.order))

const sortedSiblings = computed(() => [...props.siblings].sort((a, b) => a.order - b.order))
const siblingIndex = computed(() => sortedSiblings.value.findIndex((n) => n.id === props.node.id))
const isRoot = computed(() => props.depth === 0)

function kindSummary(node: SchemaNodeResponse): string {
  switch (node.kind) {
    case 'String':
      return node.stringConstraints?.pattern ? `String (pattern: ${node.stringConstraints.pattern})` : 'String'
    case 'Number':
    case 'Integer': {
      const c = node.numericConstraints
      if (!c || (c.minimum == null && c.maximum == null)) return node.kind
      return `${node.kind} (${c.minimum ?? '-∞'}..${c.maximum ?? '∞'})`
    }
    case 'Object':
      return `Object (${node.properties.length} ${node.properties.length === 1 ? 'property' : 'properties'})`
    default:
      return node.kind ?? 'Unspecified'
  }
}
</script>

<template>
  <div class="rounded-md border border-slate-200 bg-white">
    <div class="flex items-center justify-between gap-2 px-3 py-2">
      <div class="min-w-0">
        <div class="flex items-center gap-2">
          <span v-if="node.propertyName" class="font-mono text-sm font-medium text-slate-900">
            {{ node.propertyName }}
          </span>
          <span v-else class="text-sm font-medium text-slate-400 italic">(root)</span>
          <span class="rounded bg-slate-100 px-1.5 py-0.5 text-xs text-slate-600">{{ kindSummary(node) }}</span>
          <span v-if="node.isRequiredByParent" class="text-xs text-amber-700">required</span>
          <span v-if="node.isNullable" class="text-xs text-slate-400">nullable</span>
        </div>
        <p v-if="node.description" class="mt-0.5 truncate text-xs text-slate-500">{{ node.description }}</p>
      </div>

      <div v-if="editable" class="flex shrink-0 items-center gap-1">
        <template v-if="!isRoot">
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
        <button
          type="button"
          class="rounded px-2 py-0.5 text-xs text-slate-600 hover:bg-slate-100"
          @click="emit('editNode', node)"
        >
          Edit
        </button>
        <button
          v-if="node.kind === 'Object'"
          type="button"
          class="rounded px-2 py-0.5 text-xs text-slate-600 hover:bg-slate-100"
          @click="emit('addProperty', node.id)"
        >
          + Property
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

    <div v-if="node.kind === 'Object' && sortedProperties.length > 0" class="space-y-2 border-t border-slate-100 p-2 pl-6">
      <SchemaNodeTree
        v-for="child in sortedProperties"
        :key="child.id"
        :node="child"
        :siblings="node.properties"
        :depth="depth + 1"
        :editable="editable"
        @add-property="(id) => emit('addProperty', id)"
        @edit-node="(n) => emit('editNode', n)"
        @remove-node="(n) => emit('removeNode', n)"
        @move-node="(n, dir, sibs) => emit('moveNode', n, dir, sibs)"
      />
    </div>
  </div>
</template>
