namespace SchemaForge.Contracts.V1.Auth;

public sealed record SwitchOrganizationResponse(string AccessToken, Guid OrganizationId, string DisplayName);
