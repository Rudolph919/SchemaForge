import { httpClient } from '@/shared/api/httpClient'
import type {
  AddLocalDefinitionRequest,
  AddLocalDefinitionResponse,
  AddSchemaNodeRequest,
  AddSchemaNodeResponse,
  CreateDraftFromSuggestionRequest,
  CreateDraftFromSuggestionResponse,
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

  createVersion: (schemaDefinitionId: string, request: CreateSchemaVersionRequest, idempotencyKey: string) =>
    httpClient.post<CreateSchemaVersionResponse>(`/api/v1/schemas/${schemaDefinitionId}/versions`, request, idempotencyKey),

  getVersion: (schemaVersionId: string) =>
    httpClient.getWithETag<SchemaVersionDetailResponse>(`/api/v1/schema-versions/${schemaVersionId}`),

  addNode: (schemaVersionId: string, request: AddSchemaNodeRequest) =>
    httpClient.post<AddSchemaNodeResponse>(`/api/v1/schema-versions/${schemaVersionId}/nodes`, request),

  updateNode: (schemaVersionId: string, nodeId: string, request: UpdateSchemaNodeRequest, ifMatch: string) =>
    httpClient.patch<void>(`/api/v1/schema-versions/${schemaVersionId}/nodes/${nodeId}`, request, ifMatch),

  removeNode: (schemaVersionId: string, nodeId: string, ifMatch: string) =>
    httpClient.delete<void>(`/api/v1/schema-versions/${schemaVersionId}/nodes/${nodeId}`, ifMatch),

  addLocalDefinition: (schemaVersionId: string, request: AddLocalDefinitionRequest) =>
    httpClient.post<AddLocalDefinitionResponse>(`/api/v1/schema-versions/${schemaVersionId}/local-definitions`, request),

  removeLocalDefinition: (schemaVersionId: string, localDefinitionId: string, ifMatch: string) =>
    httpClient.delete<void>(
      `/api/v1/schema-versions/${schemaVersionId}/local-definitions/${localDefinitionId}`,
      ifMatch,
    ),

  moveNode: (schemaVersionId: string, nodeId: string, request: MoveSchemaNodeRequest) =>
    httpClient.post<void>(`/api/v1/schema-versions/${schemaVersionId}/nodes/${nodeId}/move`, request),

  publish: (schemaVersionId: string, idempotencyKey: string) =>
    httpClient.post<void>(`/api/v1/schema-versions/${schemaVersionId}/publish`, undefined, idempotencyKey),

  deprecate: (schemaVersionId: string, idempotencyKey: string) =>
    httpClient.post<void>(`/api/v1/schema-versions/${schemaVersionId}/deprecate`, undefined, idempotencyKey),

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
  importSchema: (
    schemaDefinitionId: string,
    document: unknown,
    bumpKind: VersionBumpKind,
    changeSummary: string | null,
    idempotencyKey: string,
  ) =>
    httpClient.post<CreateSchemaVersionResponse>(
      `/api/v1/schemas/${schemaDefinitionId}/import?bumpKind=${bumpKind}${changeSummary ? `&changeSummary=${encodeURIComponent(changeSummary)}` : ''}`,
      document,
      idempotencyKey,
    ),

  createDraftFromSuggestion: (
    schemaDefinitionId: string,
    request: CreateDraftFromSuggestionRequest,
    idempotencyKey: string,
  ) =>
    httpClient.post<CreateDraftFromSuggestionResponse>(
      `/api/v1/schemas/${schemaDefinitionId}/versions/from-suggestion`,
      request,
      idempotencyKey,
    ),
}
