namespace SchemaForge.Contracts.V1.Auth;

public sealed record SwitchOrganizationResponse(
    string AccessToken, string RefreshToken, Guid OrganizationId, string DisplayName);
