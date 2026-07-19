// Mirrors SchemaForge.Contracts/V1/Organizations.

export type OrganizationRole = 'Owner' | 'Admin' | 'Member'
export type MembershipStatus = 'Invited' | 'Active' | 'Revoked'

export interface InviteMemberRequest {
  email: string
  role: OrganizationRole
}

export interface InviteMemberResponse {
  membershipId: string
}

export interface ChangeMemberRoleRequest {
  newRole: OrganizationRole
}

export interface OrganizationMemberResponse {
  membershipId: string
  userId: string
  email: string
  displayName: string
  role: OrganizationRole
  status: MembershipStatus
}

export interface MembershipResponse {
  membershipId: string
  organizationId: string
  organizationName: string
  organizationSlug: string
  role: OrganizationRole
  status: MembershipStatus
}
