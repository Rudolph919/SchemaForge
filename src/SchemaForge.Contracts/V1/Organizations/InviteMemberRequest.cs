namespace SchemaForge.Contracts.V1.Organizations;

public sealed record InviteMemberRequest(string Email, OrganizationRole Role);
