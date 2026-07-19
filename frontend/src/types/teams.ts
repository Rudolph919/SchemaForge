// Mirrors SchemaForge.Contracts/V1/Teams.

export interface CreateTeamRequest {
  name: string
  description: string | null
}

export interface CreateTeamResponse {
  teamId: string
}

export interface UpdateTeamDetailsRequest {
  name: string
  description: string | null
}

export interface AddTeamMemberRequest {
  userId: string
}

export interface TeamSummaryResponse {
  id: string
  name: string
  description: string | null
  memberCount: number
}

export interface TeamMemberResponse {
  userId: string
  joinedAt: string
}

export interface TeamDetailResponse {
  id: string
  name: string
  description: string | null
  members: TeamMemberResponse[]
}
