using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Identity;

// Not tenant-owned like TenantOwnedAggregateRoot's other aggregates - refresh/logout are
// anonymous endpoints gated purely by possessing the raw token, so lookups happen before any
// ambient org context exists to key an EF Core global query filter off. OrganizationId here just
// records which org the paired access token was minted for, so a refresh reissues into that same
// org rather than re-resolving a user's membership from scratch.
public sealed class RefreshToken : AuditableEntity<Guid>
{
    // Fixed security policy, not per-deployment config like JwtSettings.AccessTokenExpiryMinutes -
    // how long an idle session survives is a product decision, not something an operator tunes.
    private const int ExpiryDays = 30;

    public Guid UserId { get; private set; }

    public Guid OrganizationId { get; private set; }

    // SHA-256 of the raw token actually handed to the client - only the hash is ever persisted,
    // so a database leak alone can't be replayed as a working refresh token (mirrors why
    // User.PasswordHash, never the password itself, is what's stored).
    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    // Set when this token was consumed by a refresh and replaced by a new one - populated for
    // rotation-chain/reuse-detection bookkeeping, not read by any authorization check.
    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;

    private RefreshToken() { } // EF Core materialization

    private RefreshToken(Guid id, Guid userId, Guid organizationId, string tokenHash, DateTimeOffset expiresAt)
        : base(id)
    {
        UserId = userId;
        OrganizationId = organizationId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    public static RefreshToken Issue(Guid userId, Guid organizationId, string tokenHash) =>
        new(Guid.NewGuid(), userId, organizationId, tokenHash, DateTimeOffset.UtcNow.AddDays(ExpiryDays));

    public void Revoke(Guid? replacedByTokenId = null)
    {
        RevokedAt = DateTimeOffset.UtcNow;
        ReplacedByTokenId = replacedByTokenId;
    }
}
