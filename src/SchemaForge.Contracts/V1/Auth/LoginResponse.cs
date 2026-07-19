namespace SchemaForge.Contracts.V1.Auth;

public sealed record LoginResponse(string AccessToken, Guid UserId, Guid OrganizationId, string DisplayName);
