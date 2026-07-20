import { httpClient } from '@/shared/api/httpClient'
import type {
  AddSchemaNodeRequest,
  AddSchemaNodeResponse,
  CreateSchemaVersionRequest,
  CreateSchemaVersionResponse,
  MoveSchemaNodeRequest,
  SchemaVersionDetailResponse,
  SchemaVersionSummaryResponse,
  UpdateSchemaNodeRequest,
} from '@/types/schemas'

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
}
