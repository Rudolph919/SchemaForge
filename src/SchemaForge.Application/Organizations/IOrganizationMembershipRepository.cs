using SchemaForge.Domain.Organizations;

namespace SchemaForge.Application.Organizations;

public interface IOrganizationMembershipRepository
{
    // Login issues a token scoped to the user's first membership - a deliberate simplification
    // for this walking-skeleton slice. A freshly registered user has exactly one; a real
    // multi-organization "which org am I acting as" selection is Phase 1 scope (Step 10), not
    // something the walking skeleton needs to solve.
    Task<OrganizationMembership?> GetFirstByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task AddAsync(OrganizationMembership membership, CancellationToken cancellationToken);
}
