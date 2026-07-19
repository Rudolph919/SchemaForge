import { httpClient } from '@/shared/api/httpClient'
import type {
  ChangeMemberRoleRequest,
  InviteMemberRequest,
  InviteMemberResponse,
  MembershipResponse,
  OrganizationMemberResponse,
} from '@/types/organizations'

export const organizationsApi = {
  listMembers: () => httpClient.get<OrganizationMemberResponse[]>('/api/v1/organizations/members'),

  inviteMember: (request: InviteMemberRequest) =>
    httpClient.post<InviteMemberResponse>('/api/v1/organizations/members/invite', request),

  acceptInvitation: (membershipId: string) =>
    httpClient.post<void>(`/api/v1/organizations/members/${membershipId}/accept`),

  changeMemberRole: (membershipId: string, request: ChangeMemberRoleRequest) =>
    httpClient.put<void>(`/api/v1/organizations/members/${membershipId}/role`, request),

  revokeMember: (membershipId: string) =>
    httpClient.delete<void>(`/api/v1/organizations/members/${membershipId}`),

  listMyMemberships: () => httpClient.get<MembershipResponse[]>('/api/v1/me/memberships'),
}
