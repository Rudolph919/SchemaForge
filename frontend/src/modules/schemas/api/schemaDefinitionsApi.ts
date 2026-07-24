import { httpClient } from '@/shared/api/httpClient'
import type {
  CreateSchemaDefinitionRequest,
  CreateSchemaDefinitionResponse,
  SchemaDefinitionDetailResponse,
  SchemaDefinitionSummaryResponse,
  UpdateSchemaDefinitionDetailsRequest,
} from '@/types/schemas'

export const schemaDefinitionsApi = {
  listSchemas: (projectId: string) =>
    httpClient.get<SchemaDefinitionSummaryResponse[]>(`/api/v1/projects/${projectId}/schemas`),

  getSchema: (schemaDefinitionId: string) =>
    httpClient.getWithETag<SchemaDefinitionDetailResponse>(`/api/v1/schemas/${schemaDefinitionId}`),

  createSchema: (projectId: string, request: CreateSchemaDefinitionRequest, idempotencyKey: string) =>
    httpClient.post<CreateSchemaDefinitionResponse>(`/api/v1/projects/${projectId}/schemas`, request, idempotencyKey),

  updateSchemaDetails: (schemaDefinitionId: string, request: UpdateSchemaDefinitionDetailsRequest, ifMatch: string) =>
    httpClient.patch<void>(`/api/v1/schemas/${schemaDefinitionId}`, request, ifMatch),
}
