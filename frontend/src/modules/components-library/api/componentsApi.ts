import { httpClient } from '@/shared/api/httpClient'
import type {
  ComponentDefinitionDetailResponse,
  ComponentDefinitionSummaryResponse,
  CreateComponentDefinitionRequest,
  CreateComponentDefinitionResponse,
  UpdateComponentDefinitionDetailsRequest,
} from '@/types/components'

export const componentsApi = {
  listComponents: () => httpClient.get<ComponentDefinitionSummaryResponse[]>('/api/v1/components'),

  getComponent: (componentDefinitionId: string) =>
    httpClient.get<ComponentDefinitionDetailResponse>(`/api/v1/components/${componentDefinitionId}`),

  createComponent: (request: CreateComponentDefinitionRequest) =>
    httpClient.post<CreateComponentDefinitionResponse>('/api/v1/components', request),

  updateComponentDetails: (componentDefinitionId: string, request: UpdateComponentDefinitionDetailsRequest) =>
    httpClient.patch<void>(`/api/v1/components/${componentDefinitionId}`, request),
}
