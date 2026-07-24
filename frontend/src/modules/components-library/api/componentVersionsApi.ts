import { httpClient } from '@/shared/api/httpClient'
import type {
  ComponentVersionDetailResponse,
  ComponentVersionSummaryResponse,
  CreateComponentVersionRequest,
  CreateComponentVersionResponse,
} from '@/types/components'
import type {
  AddLocalDefinitionRequest,
  AddLocalDefinitionResponse,
  AddSchemaNodeRequest,
  AddSchemaNodeResponse,
  MoveSchemaNodeRequest,
  UpdateSchemaNodeRequest,
} from '@/types/schemas'

// Reuses AddSchemaNodeRequest/Response, UpdateSchemaNodeRequest, and MoveSchemaNodeRequest
// directly from types/schemas.ts - identical wire shape (mirrors the backend Api's own reuse of
// these Contracts types for component-version node endpoints). No /validate endpoints -
// ValidationRun is Schema-specific.
export const componentVersionsApi = {
  listVersions: (componentDefinitionId: string) =>
    httpClient.get<ComponentVersionSummaryResponse[]>(`/api/v1/components/${componentDefinitionId}/versions`),

  createVersion: (componentDefinitionId: string, request: CreateComponentVersionRequest, idempotencyKey: string) =>
    httpClient.post<CreateComponentVersionResponse>(
      `/api/v1/components/${componentDefinitionId}/versions`,
      request,
      idempotencyKey,
    ),

  getVersion: (componentVersionId: string) =>
    httpClient.getWithETag<ComponentVersionDetailResponse>(`/api/v1/component-versions/${componentVersionId}`),

  addNode: (componentVersionId: string, request: AddSchemaNodeRequest) =>
    httpClient.post<AddSchemaNodeResponse>(`/api/v1/component-versions/${componentVersionId}/nodes`, request),

  updateNode: (componentVersionId: string, nodeId: string, request: UpdateSchemaNodeRequest, ifMatch: string) =>
    httpClient.patch<void>(`/api/v1/component-versions/${componentVersionId}/nodes/${nodeId}`, request, ifMatch),

  removeNode: (componentVersionId: string, nodeId: string, ifMatch: string) =>
    httpClient.delete<void>(`/api/v1/component-versions/${componentVersionId}/nodes/${nodeId}`, ifMatch),

  addLocalDefinition: (componentVersionId: string, request: AddLocalDefinitionRequest) =>
    httpClient.post<AddLocalDefinitionResponse>(
      `/api/v1/component-versions/${componentVersionId}/local-definitions`,
      request,
    ),

  removeLocalDefinition: (componentVersionId: string, localDefinitionId: string, ifMatch: string) =>
    httpClient.delete<void>(
      `/api/v1/component-versions/${componentVersionId}/local-definitions/${localDefinitionId}`,
      ifMatch,
    ),

  moveNode: (componentVersionId: string, nodeId: string, request: MoveSchemaNodeRequest) =>
    httpClient.post<void>(`/api/v1/component-versions/${componentVersionId}/nodes/${nodeId}/move`, request),

  publish: (componentVersionId: string, idempotencyKey: string) =>
    httpClient.post<void>(`/api/v1/component-versions/${componentVersionId}/publish`, undefined, idempotencyKey),

  deprecate: (componentVersionId: string, idempotencyKey: string) =>
    httpClient.post<void>(`/api/v1/component-versions/${componentVersionId}/deprecate`, undefined, idempotencyKey),
}
