namespace SchemaForge.Contracts.V1.Auth;

public sealed record RegisterResponse(Guid UserId, Guid OrganizationId, string OrganizationSlug);
