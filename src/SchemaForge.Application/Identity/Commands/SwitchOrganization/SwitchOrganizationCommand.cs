using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Identity.Commands.SwitchOrganization;

// A command, not a query (same reasoning as Login): issuing the access token isn't a domain
// write, but persisting a fresh refresh token scoped to the newly-active org now is.
public sealed record SwitchOrganizationCommand(Guid OrganizationId) : ICommand<Result<SwitchOrganizationResult>>;

public sealed record SwitchOrganizationResult(
    string AccessToken, string RefreshToken, Guid OrganizationId, string DisplayName);
