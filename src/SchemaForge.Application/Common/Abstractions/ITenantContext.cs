namespace SchemaForge.Application.Common.Abstractions;

// Null (no ambient tenant) is a legitimate state - unauthenticated requests like registration,
// and any pre-auth flow, have no tenant yet. The EF Core global query filter and the RLS session
// variable both treat null as "fail closed", not "bypass filtering" - see the DbContext for why
// that's structurally safe rather than convention-based.
//
// SetTenant exists for bootstrapping: registration creates a brand-new Organization and its
// first OrganizationMembership in the same request, before any JWT (and therefore no ambient
// tenant) exists. The handler calls SetTenant(newOrganizationId) right before SaveChanges so
// both the query filter and the RLS session variable see the org that write actually belongs
// to - otherwise that very first membership row could never legally be inserted at all.
public interface ITenantContext
{
    Guid? CurrentTenantId { get; }

    void SetTenant(Guid organizationId);
}
