using Microsoft.AspNetCore.Identity;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Domain.Identity;

namespace SchemaForge.Infrastructure.Security;

// Wraps ASP.NET Core Identity's PasswordHasher<T> - the standalone, secure (PBKDF2) hashing
// component from the shared framework, used without pulling in the rest of the Identity
// framework (UserManager/SignInManager/IdentityDbContext), which doesn't fit this project's
// custom Domain.User aggregate (Step 1's "self-hosted Identity" decision, reconciled at the
// implementation level - see the PR description).
public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(default!, password);

    public bool Verify(string password, string passwordHash) =>
        _hasher.VerifyHashedPassword(default!, passwordHash, password)
            is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
}
