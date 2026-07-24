using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Identity.Commands.Logout;

public sealed class LogoutHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IRefreshTokenHasher refreshTokenHasher) : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = refreshTokenHasher.Hash(request.RefreshToken);
        var storedToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        // Revoking an unknown, already-revoked, or expired token is still success - the caller's
        // goal ("this token must not work anymore") is already true either way, and leaking which
        // case it was would just be another enumeration oracle.
        storedToken?.Revoke();

        return Result.Success();
    }
}
