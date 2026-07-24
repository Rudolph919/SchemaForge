namespace SchemaForge.Contracts.V1.Auth;

public sealed record RefreshTokenResponse(
    string AccessToken, string RefreshToken, Guid UserId, Guid OrganizationId, string DisplayName);
