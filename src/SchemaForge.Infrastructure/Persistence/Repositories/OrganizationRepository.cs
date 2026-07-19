using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Organizations;
using SchemaForge.Domain.Organizations;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Infrastructure.Persistence.Repositories;

public sealed class OrganizationRepository(SchemaForgeDbContext dbContext) : IOrganizationRepository
{
    public Task<bool> SlugExistsAsync(Slug slug, CancellationToken cancellationToken) =>
        dbContext.Organizations.AnyAsync(o => o.Slug == slug, cancellationToken);

    public async Task AddAsync(Organization organization, CancellationToken cancellationToken) =>
        await dbContext.Organizations.AddAsync(organization, cancellationToken);
}
