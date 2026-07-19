// Mirrors SchemaForge.Contracts/V1/Projects.

export type ProjectStatus = 'Active' | 'Archived'

export interface CreateProjectRequest {
  name: string
  description: string | null
}

export interface CreateProjectResponse {
  projectId: string
}

export interface UpdateProjectDetailsRequest {
  name: string
  description: string | null
}

export interface ProjectSummaryResponse {
  id: string
  name: string
  description: string | null
  status: ProjectStatus
}

export interface ProjectDetailResponse {
  id: string
  name: string
  description: string | null
  status: ProjectStatus
}
