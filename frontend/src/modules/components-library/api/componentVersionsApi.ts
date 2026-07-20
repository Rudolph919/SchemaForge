import { httpClient } from '@/shared/api/httpClient'
import type {
  ComponentVersionDetailResponse,
  ComponentVersionSummaryResponse,
  CreateComponentVersionRequest,
  CreateComponentVersionResponse,
} from '@/types/components'
import type {
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

  createVersion: (componentDefinitionId: string, request: CreateComponentVersionRequest) =>
    httpClient.post<CreateComponentVersionResponse>(`/api/v1/components/${componentDefinitionId}/versions`, request),

  getVersion: (componentVersionId: string) =>
    httpClient.get<ComponentVersionDetailResponse>(`/api/v1/component-versions/${componentVersionId}`),

  addNode: (componentVersionId: string, request: AddSchemaNodeRequest) =>
    httpClient.post<AddSchemaNodeResponse>(`/api/v1/component-versions/${componentVersionId}/nodes`, request),

  updateNode: (componentVersionId: string, nodeId: string, request: UpdateSchemaNodeRequest) =>
    httpClient.patch<void>(`/api/v1/component-versions/${componentVersionId}/nodes/${nodeId}`, request),

  removeNode: (componentVersionId: string, nodeId: string) =>
    httpClient.delete<void>(`/api/v1/component-versions/${componentVersionId}/nodes/${nodeId}`),

  moveNode: (componentVersionId: string, nodeId: string, request: MoveSchemaNodeRequest) =>
    httpClient.post<void>(`/api/v1/component-versions/${componentVersionId}/nodes/${nodeId}/move`, request),

  publish: (componentVersionId: string) =>
    httpClient.post<void>(`/api/v1/component-versions/${componentVersionId}/publish`),

  deprecate: (componentVersionId: string) =>
    httpClient.post<void>(`/api/v1/component-versions/${componentVersionId}/deprecate`),
}
