<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { projectsApi } from '@/modules/workspaces/api/projectsApi'
import { documentsApi } from '@/modules/workspaces/api/documentsApi'
import { ApiError } from '@/shared/api/httpClient'
import type { ProjectDetailResponse } from '@/types/projects'
import type { SourceDocumentResponse } from '@/types/sourceDocuments'

const route = useRoute()
const router = useRouter()
const projectId = computed(() => route.params.id as string)

const project = ref<ProjectDetailResponse | null>(null)
const documents = ref<SourceDocumentResponse[]>([])
const loadError = ref<string | null>(null)

const isEditing = ref(false)
const editName = ref('')
const editDescription = ref('')
const editError = ref<string | null>(null)
const isSaving = ref(false)

const isUploading = ref(false)
const uploadError = ref<string | null>(null)
const fileInput = ref<HTMLInputElement | null>(null)

async function load() {
  loadError.value = null
  try {
    const [projectResponse, documentsResponse] = await Promise.all([
      projectsApi.getProject(projectId.value),
      documentsApi.listDocuments(projectId.value),
    ])
    project.value = projectResponse
    documents.value = documentsResponse
  } catch (error) {
    loadError.value = error instanceof ApiError ? error.message : 'Could not load project.'
  }
}

function startEditing() {
  if (!project.value) return
  editName.value = project.value.name
  editDescription.value = project.value.description ?? ''
  editError.value = null
  isEditing.value = true
}

async function handleSaveDetails() {
  editError.value = null
  isSaving.value = true
  try {
    await projectsApi.updateProjectDetails(projectId.value, {
      name: editName.value,
      description: editDescription.value || null,
    })
    isEditing.value = false
    await load()
  } catch (error) {
    editError.value = error instanceof ApiError ? error.message : 'Could not save changes.'
  } finally {
    isSaving.value = false
  }
}

async function handleToggleArchive() {
  if (!project.value) return
  try {
    if (project.value.status === 'Active') {
      await projectsApi.archiveProject(projectId.value)
    } else {
      await projectsApi.reactivateProject(projectId.value)
    }
    await load()
  } catch (error) {
    loadError.value = error instanceof ApiError ? error.message : 'Could not update status.'
  }
}

async function handleFileChange(event: Event) {
  const file = (event.target as HTMLInputElement).files?.[0]
  if (!file) return

  uploadError.value = null
  isUploading.value = true
  try {
    await documentsApi.uploadDocument(projectId.value, file)
    documents.value = await documentsApi.listDocuments(projectId.value)
  } catch (error) {
    uploadError.value = error instanceof ApiError ? error.message : 'Could not upload document.'
  } finally {
    isUploading.value = false
    if (fileInput.value) fileInput.value.value = ''
  }
}

async function handleDeleteDocument(documentId: string) {
  try {
    await documentsApi.deleteDocument(documentId)
    documents.value = documents.value.filter((d) => d.id !== documentId)
  } catch (error) {
    uploadError.value = error instanceof ApiError ? error.message : 'Could not delete document.'
  }
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

onMounted(load)
</script>

<template>
  <div>
    <button type="button" class="text-sm text-slate-500 hover:text-slate-700" @click="router.push('/projects')">
      ← Back to Projects
    </button>

    <p v-if="loadError" class="mt-4 text-sm text-red-600">{{ loadError }}</p>

    <template v-if="project">
      <div class="mt-4 rounded-lg border border-slate-200 bg-white p-6">
        <div v-if="!isEditing" class="flex items-start justify-between">
          <div>
            <div class="flex items-center gap-2">
              <h1 class="text-lg font-semibold text-slate-900">{{ project.name }}</h1>
              <span
                class="rounded-full px-2 py-0.5 text-xs font-medium"
                :class="project.status === 'Active' ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-100 text-slate-600'"
              >
                {{ project.status }}
              </span>
            </div>
            <p v-if="project.description" class="mt-1 text-sm text-slate-500">{{ project.description }}</p>
          </div>
          <div class="flex shrink-0 gap-2">
            <button
              type="button"
              class="rounded-md border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50"
              @click="startEditing"
            >
              Edit
            </button>
            <button
              type="button"
              class="rounded-md border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50"
              @click="handleToggleArchive"
            >
              {{ project.status === 'Active' ? 'Archive' : 'Reactivate' }}
            </button>
            <router-link
              :to="`/projects/${projectId}/schemas`"
              class="rounded-md bg-slate-900 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-800"
            >
              Schemas
            </router-link>
          </div>
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
          <h2 class="text-base font-semibold text-slate-900">Source Documents</h2>
          <label
            class="cursor-pointer rounded-md bg-slate-900 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-800"
            :class="{ 'opacity-50': isUploading }"
          >
            {{ isUploading ? 'Uploading…' : 'Upload' }}
            <input ref="fileInput" type="file" class="hidden" :disabled="isUploading" @change="handleFileChange" />
          </label>
        </div>

        <p v-if="uploadError" class="mt-2 text-sm text-red-600">{{ uploadError }}</p>
        <p v-if="documents.length === 0" class="mt-4 text-sm text-slate-500">No documents uploaded yet.</p>

        <table v-else class="mt-4 w-full text-sm">
          <thead>
            <tr class="border-b border-slate-200 text-left text-slate-500">
              <th class="pb-2 font-medium">File</th>
              <th class="pb-2 font-medium">Type</th>
              <th class="pb-2 font-medium">Size</th>
              <th class="pb-2"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="doc in documents" :key="doc.id" class="border-b border-slate-100 last:border-0">
              <td class="py-2 text-slate-900">{{ doc.fileName }}</td>
              <td class="py-2 text-slate-500">{{ doc.contentType }}</td>
              <td class="py-2 text-slate-500">{{ formatSize(doc.sizeBytes) }}</td>
              <td class="py-2 text-right">
                <button type="button" class="text-slate-400 hover:text-red-600" @click="handleDeleteDocument(doc.id)">
                  Delete
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>
  </div>
</template>
