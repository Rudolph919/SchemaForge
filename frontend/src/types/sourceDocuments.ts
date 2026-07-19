// Mirrors SchemaForge.Contracts/V1/SourceDocuments.

export interface UploadSourceDocumentResponse {
  documentId: string
}

export interface SourceDocumentResponse {
  id: string
  fileName: string
  contentType: string
  sizeBytes: number
  createdAt: string
}
