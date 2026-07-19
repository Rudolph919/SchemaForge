using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Organizations;
using SchemaForge.Domain.Organizations;

namespace SchemaForge.Infrastructure.Persistence.Repositories;

public sealed class OrganizationOwnershipGuard(SchemaForgeDbContext dbContext) : IOrganizationOwnershipGuard
{
    public Task<bool> HasAnotherActiveOwnerAsync(
        Guid organizationId, Guid membershipIdBeingChanged, CancellationToken cancellationToken) =>
        dbContext.OrganizationMemberships.AnyAsync(
            m => m.OrganizationId == organizationId
                && m.Id != membershipIdBeingChanged
                && m.Role == OrganizationRole.Owner
                && m.Status == MembershipStatus.Active,
            cancellationToken);
}
