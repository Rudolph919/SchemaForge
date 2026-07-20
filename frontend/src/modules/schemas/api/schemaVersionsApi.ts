import { httpClient } from '@/shared/api/httpClient'
import type {
  AddSchemaNodeRequest,
  AddSchemaNodeResponse,
  CreateSchemaVersionRequest,
  CreateSchemaVersionResponse,
  MoveSchemaNodeRequest,
  SchemaDiffResponse,
  SchemaVersionDetailResponse,
  SchemaVersionSummaryResponse,
  UpdateSchemaNodeRequest,
  VersionBumpKind,
} from '@/types/schemas'
import type { ValidateJsonPayloadResponse, ValidationRunSummaryResponse } from '@/types/validation'

export const EXPORT_FORMATS = ['json-schema', 'openapi', 'typescript', 'csharp'] as const
export type ExportFormat = (typeof EXPORT_FORMATS)[number]

export const DOCUMENTATION_FORMATS = ['html', 'markdown', 'json'] as const
export type DocumentationFormat = (typeof DOCUMENTATION_FORMATS)[number]

export const schemaVersionsApi = {
  listVersions: (schemaDefinitionId: string) =>
    httpClient.get<SchemaVersionSummaryResponse[]>(`/api/v1/schemas/${schemaDefinitionId}/versions`),

  createVersion: (schemaDefinitionId: string, request: CreateSchemaVersionRequest) =>
    httpClient.post<CreateSchemaVersionResponse>(`/api/v1/schemas/${schemaDefinitionId}/versions`, request),

  getVersion: (schemaVersionId: string) =>
    httpClient.get<SchemaVersionDetailResponse>(`/api/v1/schema-versions/${schemaVersionId}`),

  addNode: (schemaVersionId: string, request: AddSchemaNodeRequest) =>
    httpClient.post<AddSchemaNodeResponse>(`/api/v1/schema-versions/${schemaVersionId}/nodes`, request),

  updateNode: (schemaVersionId: string, nodeId: string, request: UpdateSchemaNodeRequest) =>
    httpClient.patch<void>(`/api/v1/schema-versions/${schemaVersionId}/nodes/${nodeId}`, request),

  removeNode: (schemaVersionId: string, nodeId: string) =>
    httpClient.delete<void>(`/api/v1/schema-versions/${schemaVersionId}/nodes/${nodeId}`),

  moveNode: (schemaVersionId: string, nodeId: string, request: MoveSchemaNodeRequest) =>
    httpClient.post<void>(`/api/v1/schema-versions/${schemaVersionId}/nodes/${nodeId}/move`, request),

  publish: (schemaVersionId: string) => httpClient.post<void>(`/api/v1/schema-versions/${schemaVersionId}/publish`),

  deprecate: (schemaVersionId: string) =>
    httpClient.post<void>(`/api/v1/schema-versions/${schemaVersionId}/deprecate`),

  validate: (schemaVersionId: string, payload: unknown) =>
    httpClient.post<ValidateJsonPayloadResponse>(`/api/v1/schema-versions/${schemaVersionId}/validate`, payload),

  listValidationRuns: (schemaVersionId: string) =>
    httpClient.get<ValidationRunSummaryResponse[]>(`/api/v1/schema-versions/${schemaVersionId}/validation-runs`),

  export: (schemaVersionId: string, format: ExportFormat) =>
    httpClient.getText(`/api/v1/schema-versions/${schemaVersionId}/export?format=${format}`),

  documentation: (schemaVersionId: string, format: DocumentationFormat) =>
    httpClient.getText(`/api/v1/schema-versions/${schemaVersionId}/documentation?format=${format}`),

  diff: (schemaVersionId: string, against: string) =>
    httpClient.get<SchemaDiffResponse>(`/api/v1/schema-versions/${schemaVersionId}/diff?against=${against}`),

  // Raw JSON Schema document as the request body - httpClient.post JSON.stringifies whatever is
  // passed as `document`, matching the backend's [FromBody] JsonElement pattern.
  importSchema: (schemaDefinitionId: string, document: unknown, bumpKind: VersionBumpKind, changeSummary: string | null) =>
    httpClient.post<CreateSchemaVersionResponse>(
      `/api/v1/schemas/${schemaDefinitionId}/import?bumpKind=${bumpKind}${changeSummary ? `&changeSummary=${encodeURIComponent(changeSummary)}` : ''}`,
      document,
    ),
}
