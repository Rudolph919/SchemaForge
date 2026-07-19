import { httpClient } from '@/shared/api/httpClient'
import type {
  AddTeamMemberRequest,
  CreateTeamRequest,
  CreateTeamResponse,
  TeamDetailResponse,
  TeamSummaryResponse,
  UpdateTeamDetailsRequest,
} from '@/types/teams'

export const teamsApi = {
  listTeams: () => httpClient.get<TeamSummaryResponse[]>('/api/v1/teams'),

  getTeam: (teamId: string) => httpClient.get<TeamDetailResponse>(`/api/v1/teams/${teamId}`),

  createTeam: (request: CreateTeamRequest) => httpClient.post<CreateTeamResponse>('/api/v1/teams', request),

  updateTeamDetails: (teamId: string, request: UpdateTeamDetailsRequest) =>
    httpClient.put<void>(`/api/v1/teams/${teamId}`, request),

  addTeamMember: (teamId: string, request: AddTeamMemberRequest) =>
    httpClient.post<void>(`/api/v1/teams/${teamId}/members`, request),

  removeTeamMember: (teamId: string, userId: string) =>
    httpClient.delete<void>(`/api/v1/teams/${teamId}/members/${userId}`),
}
