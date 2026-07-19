namespace SchemaForge.Contracts.V1.Organizations;

public sealed record OrganizationMemberResponse(
    Guid MembershipId,
    Guid UserId,
    string Email,
    string DisplayName,
    OrganizationRole Role,
    MembershipStatus Status);
