using SchemaForge.Domain.Organizations;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Application.Organizations;

public interface IOrganizationRepository
{
    Task<bool> SlugExistsAsync(Slug slug, CancellationToken cancellationToken);

    Task AddAsync(Organization organization, CancellationToken cancellationToken);
}
