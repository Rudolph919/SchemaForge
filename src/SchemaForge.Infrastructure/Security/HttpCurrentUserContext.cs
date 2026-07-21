using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using SchemaForge.Application.Common.Abstractions;

namespace SchemaForge.Infrastructure.Security;

// Parallel to HttpTenantContext, resolving from the JWT's "sub" claim instead of "org_id".
// Uses the same JwtRegisteredClaimNames source JwtTokenService writes with, not a hand-typed
// "sub" string, so creation and lookup can't silently drift. Worth verifying against a real
// authenticated request once the Api layer wires JWT bearer auth to these commands: ASP.NET
// Core's JWT handlers have historically remapped short claim names (sub -> a long XML claim URI)
// unless MapInboundClaims is false, which is the current default but hasn't been proven here yet.
public sealed class HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    private bool _resolved;
    private Guid? _userId;

    public Guid? UserId
    {
        get
        {
            if (_resolved) return _userId;

            var claimValue = httpContextAccessor.HttpContext?.User
                .FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            _userId = Guid.TryParse(claimValue, out var userId) ? userId : null;
            _resolved = true;

            return _userId;
        }
    }

    public void SetUser(Guid userId)
    {
        _userId = userId;
        _resolved = true;
    }
}
