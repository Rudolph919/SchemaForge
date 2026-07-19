using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Organizations;
using SchemaForge.Domain.Organizations;

namespace SchemaForge.Infrastructure.Persistence.Repositories;

public sealed class OrganizationMembershipRepository(SchemaForgeDbContext dbContext)
    : IOrganizationMembershipRepository
{
    // Login has no tenant context yet - discovering which org(s) a user belongs to is exactly
    // what it needs to do before one exists. Neither isolation layer can use the normal
    // "match the ambient tenant" rule here: the EF Core filter would block everything (no
    // ambient tenant to match), and IgnoreQueryFilters() alone isn't enough, because the
    // Postgres RLS policy would independently block it too (its USING clause also compares
    // against the same absent tenant setting). The actual fix lives at both layers: RLS gained
    // a second, narrower USING branch (see the migration) allowing a row to be read if
    // app.current_user_id matches its user_id - safe specifically because that value is only
    // ever set here, after LoginHandler has already verified the password, never from
    // caller-supplied input. Set transaction-scoped (true), matching the tenant-context pattern
    // in SchemaForgeDbContext, so it can't leak across a pooled connection's next use.
    public async Task<OrganizationMembership?> GetFirstByUserIdAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('app.current_user_id', {userId.ToString()}, true)", cancellationToken);

        var membership = await dbContext.OrganizationMemberships
            .IgnoreQueryFilters()
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return membership;
    }

    public async Task AddAsync(OrganizationMembership membership, CancellationToken cancellationToken) =>
        await dbContext.OrganizationMemberships.AddAsync(membership, cancellationToken);
}
