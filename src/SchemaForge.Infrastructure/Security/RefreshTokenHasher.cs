using System.Security.Cryptography;
using System.Text;
using SchemaForge.Application.Common.Abstractions;

namespace SchemaForge.Infrastructure.Security;

// SHA-256, not IPasswordHasher's slow BCrypt - the raw token is already 256 bits of
// cryptographically random entropy (RandomNumberGenerator-sourced in the Application handlers),
// so a fast, deterministic hash is exactly what an equality lookup by hash needs; a slow KDF is
// only for defending low-entropy human-chosen secrets against offline brute force, which doesn't
// apply here.
public sealed class RefreshTokenHasher : IRefreshTokenHasher
{
    public string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
