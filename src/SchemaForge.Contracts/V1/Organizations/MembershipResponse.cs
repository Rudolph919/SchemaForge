namespace SchemaForge.Contracts.V1.Organizations;

public sealed record MembershipResponse(
    Guid MembershipId,
    Guid OrganizationId,
    string OrganizationName,
    string OrganizationSlug,
    OrganizationRole Role,
    MembershipStatus Status);
