using SchemaForge.Domain.Identity;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Application.Identity;

public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(EmailAddress email, CancellationToken cancellationToken);

    Task<User?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);
}
