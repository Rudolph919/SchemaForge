import { httpClient } from '@/shared/api/httpClient'
import type {
  CreateProjectRequest,
  CreateProjectResponse,
  ProjectDetailResponse,
  ProjectSummaryResponse,
  UpdateProjectDetailsRequest,
} from '@/types/projects'

export const projectsApi = {
  listProjects: () => httpClient.get<ProjectSummaryResponse[]>('/api/v1/projects'),

  getProject: (projectId: string) => httpClient.getWithETag<ProjectDetailResponse>(`/api/v1/projects/${projectId}`),

  createProject: (request: CreateProjectRequest, idempotencyKey: string) =>
    httpClient.post<CreateProjectResponse>('/api/v1/projects', request, idempotencyKey),

  updateProjectDetails: (projectId: string, request: UpdateProjectDetailsRequest, ifMatch: string) =>
    httpClient.put<void>(`/api/v1/projects/${projectId}`, request, ifMatch),

  archiveProject: (projectId: string) => httpClient.post<void>(`/api/v1/projects/${projectId}/archive`),

  reactivateProject: (projectId: string) => httpClient.post<void>(`/api/v1/projects/${projectId}/reactivate`),
}
