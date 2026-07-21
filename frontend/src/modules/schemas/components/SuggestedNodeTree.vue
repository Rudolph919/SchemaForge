<script setup lang="ts">
import type { SuggestedNodeResponse } from '@/types/schemas'

const props = defineProps<{
  node: SuggestedNodeResponse
  acceptedIds: Set<string>
  depth: number
  parentAccepted: boolean
}>()

const emit = defineEmits<{
  toggle: [nodeId: string]
}>()

// A rejected node's children have no accepted parent to attach to once materialized (the
// backend prunes the whole subtree), so their checkboxes are disabled here too rather than
// letting the reviewer pick an inconsistent combination.
function toggle(nodeId: string) {
  if (props.parentAccepted) {
    emit('toggle', nodeId)
  }
}
</script>

<template>
  <div>
    <label
      class="flex items-center gap-2 rounded-md px-2 py-1 text-sm"
      :class="parentAccepted ? 'hover:bg-slate-50' : 'opacity-40'"
      :style="{ paddingLeft: `${depth * 1.25 + 0.5}rem` }"
    >
      <input
        type="checkbox"
        :checked="acceptedIds.has(node.id)"
        :disabled="!parentAccepted"
        @change="toggle(node.id)"
      />
      <span v-if="node.propertyName" class="font-mono font-medium text-slate-900">{{ node.propertyName }}</span>
      <span v-else class="text-slate-400 italic">(root)</span>
      <span class="rounded bg-slate-100 px-1.5 py-0.5 text-xs text-slate-600">{{ node.kind }}</span>
      <span class="text-xs text-slate-400">{{ Math.round(node.confidence * 100) }}% confidence</span>
      <span v-if="node.description" class="truncate text-xs text-slate-500">{{ node.description }}</span>
    </label>

    <SuggestedNodeTree
      v-for="child in node.children"
      :key="child.id"
      :node="child"
      :accepted-ids="acceptedIds"
      :depth="depth + 1"
      :parent-accepted="parentAccepted && acceptedIds.has(node.id)"
      @toggle="(id) => emit('toggle', id)"
    />
  </div>
</template>
