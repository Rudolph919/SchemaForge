using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Domain.Identity;
using SchemaForge.Domain.Organizations;

namespace SchemaForge.Infrastructure.Security;

// Access tokens only - refresh token issuance/rotation lives in RefreshTokenHasher plus the
// RefreshToken aggregate/repository (Step 6 §2.1's /auth/refresh), which don't need a signing key
// or any of this class's JWT machinery.
public sealed class JwtTokenService(IOptions<JwtSettings> jwtSettings) : IJwtTokenService
{
    private readonly JwtSettings _settings = jwtSettings.Value;

    public string GenerateAccessToken(User user, Guid organizationId, OrganizationRole role)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            Expires = DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes),
            SigningCredentials = credentials,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = user.Email.Value,
                [JwtRegisteredClaimNames.Name] = user.DisplayName,
                // "org_id" is what HttpTenantContext.ResolveFromClaims reads to establish the
                // ambient tenant for the rest of the request.
                ["org_id"] = organizationId.ToString(),
                ["role"] = role.ToString()
            }
        };

        var handler = new JsonWebTokenHandler();

        return handler.CreateToken(descriptor);
    }
}
