using SchemaForge.Domain.Identity;

namespace SchemaForge.Application.Identity;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    // Reuse-detection sweep (Step 6 §2.1): when a rotated-out token is replayed, every other
    // still-active token this user holds gets killed, forcing a fresh login everywhere.
    Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken);
}
