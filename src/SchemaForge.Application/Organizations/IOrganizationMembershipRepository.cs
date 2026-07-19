using SchemaForge.Domain.Organizations;

namespace SchemaForge.Application.Organizations;

public interface IOrganizationMembershipRepository
{
    // Normal tenant-scoped lookup (the EF Core filter applies) - for an org admin acting on a
    // membership within their OWN current organization (role change, revoke). Returns null if
    // the id doesn't belong to the caller's org, which is exactly the isolation guarantee wanted
    // here: an id from a different org should be indistinguishable from a nonexistent one.
    Task<OrganizationMembership?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    // Deliberately NOT the same method as GetByIdAsync: a user accepting their own pending
    // invitation doesn't have that org as their ambient tenant yet (that's the point of
    // accepting it), so this bypasses the tenant filter via the same app.current_user_id RLS
    // exception GetFirstByUserIdAsync uses - but filters by userId directly in the query itself,
    // not as a post-load check, so the database only ever returns rows that are actually this
    // user's to begin with.
    Task<OrganizationMembership?> GetByIdForUserAsync(
        Guid membershipId, Guid userId, CancellationToken cancellationToken);

    // Login has no tenant context yet - discovering which org a user belongs to is exactly what
    // it needs to do before one exists. See the Infrastructure implementation and the migration
    // that added the app.current_user_id RLS exception for the full reasoning (Step 0's
    // integration-tests PR).
    Task<OrganizationMembership?> GetFirstByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    // Same self-lookup mechanism as GetFirstByUserIdAsync, generalized to "all my memberships
    // across every org, with enough Organization detail to render a switcher/pending-invites
    // list" - a user's own membership list is exactly the kind of query that has to cross tenant
    // boundaries by design, the same way login's self-lookup does.
    Task<IReadOnlyList<MembershipWithOrganizationSummary>> GetAllByUserIdAsync(
        Guid userId, CancellationToken cancellationToken);

    Task<bool> ExistsForUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken);

    // Ambient-tenant-scoped (the EF Core query filter handles "which org" implicitly) - lists
    // every membership of the caller's current organization, any status, for admin visibility
    // (Step 6's membership-listing endpoint). Returns an already-joined projection (with User
    // details) rather than raw OrganizationMembership entities, matching Step 1 §3's "lean
    // projection, not the full aggregate" guidance for listing queries - avoids an N+1 lookup
    // the query handler would otherwise need to do per membership.
    Task<IReadOnlyList<OrganizationMemberSummary>> GetAllForCurrentOrganizationAsync(
        CancellationToken cancellationToken);

    Task AddAsync(OrganizationMembership membership, CancellationToken cancellationToken);
}

public sealed record MembershipWithOrganizationSummary(
    Guid MembershipId,
    Guid OrganizationId,
    string OrganizationName,
    string OrganizationSlug,
    OrganizationRole Role,
    MembershipStatus Status);

public sealed record OrganizationMemberSummary(
    Guid MembershipId,
    Guid UserId,
    string Email,
    string DisplayName,
    OrganizationRole Role,
    MembershipStatus Status);
