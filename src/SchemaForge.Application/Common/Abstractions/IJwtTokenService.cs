using SchemaForge.Domain.Identity;
using SchemaForge.Domain.Organizations;

namespace SchemaForge.Application.Common.Abstractions;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, Guid organizationId, OrganizationRole role);
}
