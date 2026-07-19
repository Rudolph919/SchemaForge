using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Application.Organizations;
using SchemaForge.Domain.Identity;
using SchemaForge.Domain.Organizations;
using SchemaForge.SharedKernel;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Application.Identity.Commands.RegisterUser;

public sealed class RegisterUserHandler(
    IUserRepository userRepository,
    IOrganizationRepository organizationRepository,
    IOrganizationMembershipRepository membershipRepository,
    IPasswordHasher passwordHasher,
    ITenantContext tenantContext)
    : IRequestHandler<RegisterUserCommand, Result<RegisterUserResult>>
{
    public async Task<Result<RegisterUserResult>> Handle(
        RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(request.Email);

        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            return Result<RegisterUserResult>.Failure(Error.Conflict(
                "User.EmailAlreadyRegistered", "An account with this email already exists."));
        }

        var slug = await GenerateUniqueSlugAsync(request.OrganizationName, cancellationToken);

        var user = User.Register(email, passwordHasher.Hash(request.Password), request.DisplayName);
        var organization = Organization.Create(request.OrganizationName, slug);
        var membership = OrganizationMembership.CreateOwner(organization.Id, user.Id);

        await userRepository.AddAsync(user, cancellationToken);
        await organizationRepository.AddAsync(organization, cancellationToken);
        await membershipRepository.AddAsync(membership, cancellationToken);

        // Bootstrapping: no JWT/ambient tenant exists yet during registration, so the handler
        // must tell the tenant context which org this write belongs to before TransactionBehavior
        // calls SaveChanges - otherwise this very first membership row could never legally be
        // inserted under either tenant-isolation layer (Infrastructure PR).
        tenantContext.SetTenant(organization.Id);

        return new RegisterUserResult(user.Id, organization.Id, organization.Slug.Value);
    }

    private async Task<Slug> GenerateUniqueSlugAsync(string organizationName, CancellationToken cancellationToken)
    {
        var baseSlug = Slugify(organizationName);

        if (!await organizationRepository.SlugExistsAsync(baseSlug, cancellationToken))
        {
            return baseSlug;
        }

        // Collision on an otherwise-plausible slug (e.g. two "Acme Corp" registrations) - append
        // a short random suffix rather than failing registration outright.
        var suffixed = Slug.Create($"{baseSlug.Value}-{Guid.NewGuid().ToString()[..8]}");
        return suffixed;
    }

    private static Slug Slugify(string organizationName)
    {
        var lowered = organizationName.Trim().ToLowerInvariant();
        var withHyphens = string.Concat(lowered.Select(c => char.IsLetterOrDigit(c) ? c : '-'));

        var collapsed = withHyphens;
        while (collapsed.Contains("--"))
        {
            collapsed = collapsed.Replace("--", "-");
        }

        collapsed = collapsed.Trim('-');

        return Slug.Create(collapsed.Length > 0 ? collapsed : Guid.NewGuid().ToString()[..8]);
    }
}
