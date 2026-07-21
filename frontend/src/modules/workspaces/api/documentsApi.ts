import { httpClient } from '@/shared/api/httpClient'
import type { SourceDocumentResponse, UploadSourceDocumentResponse } from '@/types/sourceDocuments'
import type { SchemaSuggestionResponse } from '@/types/schemas'

export const documentsApi = {
  listDocuments: (projectId: string) =>
    httpClient.get<SourceDocumentResponse[]>(`/api/v1/projects/${projectId}/documents`),

  uploadDocument: (projectId: string, file: File) =>
    httpClient.upload<UploadSourceDocumentResponse>(`/api/v1/projects/${projectId}/documents`, file),

  deleteDocument: (documentId: string) => httpClient.delete<void>(`/api/v1/documents/${documentId}`),

  suggestSchema: (documentId: string) =>
    httpClient.post<SchemaSuggestionResponse>(`/api/v1/documents/${documentId}/suggest-schema`),
}
