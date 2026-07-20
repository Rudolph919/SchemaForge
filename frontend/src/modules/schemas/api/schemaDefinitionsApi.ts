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
    httpClient.get<SchemaDefinitionDetailResponse>(`/api/v1/schemas/${schemaDefinitionId}`),

  createSchema: (projectId: string, request: CreateSchemaDefinitionRequest) =>
    httpClient.post<CreateSchemaDefinitionResponse>(`/api/v1/projects/${projectId}/schemas`, request),

  updateSchemaDetails: (schemaDefinitionId: string, request: UpdateSchemaDefinitionDetailsRequest) =>
    httpClient.patch<void>(`/api/v1/schemas/${schemaDefinitionId}`, request),
}
