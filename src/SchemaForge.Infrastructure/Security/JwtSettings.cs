namespace SchemaForge.Infrastructure.Security;

public sealed class JwtSettings
{
    public required string SigningKey { get; init; }

    public string Issuer { get; init; } = "SchemaForge";

    public string Audience { get; init; } = "SchemaForge";

    public int AccessTokenExpiryMinutes { get; init; } = 60;
}
