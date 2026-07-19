namespace SchemaForge.Contracts.V1.Auth;

public sealed record RegisterRequest(string Email, string Password, string DisplayName, string OrganizationName);
