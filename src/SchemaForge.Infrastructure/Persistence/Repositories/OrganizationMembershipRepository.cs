using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Organizations;
using SchemaForge.Domain.Organizations;

namespace SchemaForge.Infrastructure.Persistence.Repositories;

public sealed class OrganizationMembershipRepository(SchemaForgeDbContext dbContext)
    : IOrganizationMembershipRepository
{
    public Task<OrganizationMembership?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.OrganizationMemberships.SingleOrDefaultAsync(m => m.Id == id, cancellationToken);

    // Same self-lookup mechanism as GetFirstByUserIdAsync (see below) but filters by the target
    // membership id AND userId directly in the query, not as a post-load ownership check - the
    // database itself never returns a row that isn't this user's to begin with.
    public async Task<OrganizationMembership?> GetByIdForUserAsync(
        Guid membershipId, Guid userId, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await SetCurrentUserSessionVariableAsync(userId, cancellationToken);

        var membership = await dbContext.OrganizationMemberships
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(m => m.Id == membershipId && m.UserId == userId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return membership;
    }

    // Login has no tenant context yet - discovering which org a user belongs to is exactly what
    // it needs to do before one exists. See the migration that added the app.current_user_id RLS
    // exception for the full reasoning (Step 0's integration-tests PR).
    public async Task<OrganizationMembership?> GetFirstByUserIdAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await SetCurrentUserSessionVariableAsync(userId, cancellationToken);

        var membership = await dbContext.OrganizationMemberships
            .IgnoreQueryFilters()
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return membership;
    }

    public async Task<IReadOnlyList<MembershipWithOrganizationSummary>> GetAllByUserIdAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await SetCurrentUserSessionVariableAsync(userId, cancellationToken);

        var memberships = await dbContext.OrganizationMemberships
            .IgnoreQueryFilters()
            .Where(m => m.UserId == userId)
            .Join(
                dbContext.Organizations, // Organization isn't tenant-scoped (Step 0's Domain PR), no filter to ignore here
                m => m.OrganizationId,
                o => o.Id,
                (m, o) => new MembershipWithOrganizationSummary(
                    m.Id, o.Id, o.Name, o.Slug.Value, m.Role, m.Status))
            .ToListAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return memberships;
    }

    public Task<bool> ExistsForUserAsync(
        Guid organizationId, Guid userId, CancellationToken cancellationToken) =>
        dbContext.OrganizationMemberships.AnyAsync(
            m => m.OrganizationId == organizationId && m.UserId == userId, cancellationToken);

    public Task<bool> IsActiveMemberAsync(
        Guid organizationId, Guid userId, CancellationToken cancellationToken) =>
        dbContext.OrganizationMemberships.AnyAsync(
            m => m.OrganizationId == organizationId
                && m.UserId == userId
                && m.Status == MembershipStatus.Active,
            cancellationToken);

    public async Task<IReadOnlyList<OrganizationMemberSummary>> GetAllForCurrentOrganizationAsync(
        CancellationToken cancellationToken) =>
        await dbContext.OrganizationMemberships
            .Join(
                dbContext.Users,
                m => m.UserId,
                u => u.Id,
                (m, u) => new OrganizationMemberSummary(
                    m.Id, u.Id, u.Email.Value, u.DisplayName, m.Role, m.Status))
            .ToListAsync(cancellationToken);

    public async Task AddAsync(OrganizationMembership membership, CancellationToken cancellationToken) =>
        await dbContext.OrganizationMemberships.AddAsync(membership, cancellationToken);

    private async Task SetCurrentUserSessionVariableAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('app.current_user_id', {userId.ToString()}, true)", cancellationToken);
}
