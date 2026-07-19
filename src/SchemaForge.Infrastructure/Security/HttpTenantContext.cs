using Microsoft.AspNetCore.Http;
using SchemaForge.Application.Common.Abstractions;

namespace SchemaForge.Infrastructure.Security;

// Scoped per-request. Resolves lazily from the "org_id" JWT claim on first access, cached for
// the rest of the request; SetTenant lets a handler override it explicitly for pre-auth
// bootstrapping flows (registration) where there's no JWT yet to resolve from.
public sealed class HttpTenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    private bool _resolved;
    private Guid? _currentTenantId;

    public Guid? CurrentTenantId
    {
        get
        {
            if (_resolved) return _currentTenantId;

            _currentTenantId = ResolveFromClaims();
            _resolved = true;

            return _currentTenantId;
        }
    }

    public void SetTenant(Guid organizationId)
    {
        _currentTenantId = organizationId;
        _resolved = true;
    }

    private Guid? ResolveFromClaims()
    {
        var claimValue = httpContextAccessor.HttpContext?.User.FindFirst("org_id")?.Value;

        return Guid.TryParse(claimValue, out var tenantId) ? tenantId : null;
    }
}
