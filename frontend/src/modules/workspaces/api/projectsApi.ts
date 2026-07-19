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

  getProject: (projectId: string) => httpClient.get<ProjectDetailResponse>(`/api/v1/projects/${projectId}`),

  createProject: (request: CreateProjectRequest) =>
    httpClient.post<CreateProjectResponse>('/api/v1/projects', request),

  updateProjectDetails: (projectId: string, request: UpdateProjectDetailsRequest) =>
    httpClient.put<void>(`/api/v1/projects/${projectId}`, request),

  archiveProject: (projectId: string) => httpClient.post<void>(`/api/v1/projects/${projectId}/archive`),

  reactivateProject: (projectId: string) => httpClient.post<void>(`/api/v1/projects/${projectId}/reactivate`),
}
