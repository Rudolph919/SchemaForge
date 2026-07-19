using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Identity.Commands.RegisterUser;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string DisplayName,
    string OrganizationName) : ICommand<Result<RegisterUserResult>>;

public sealed record RegisterUserResult(Guid UserId, Guid OrganizationId, string OrganizationSlug);
