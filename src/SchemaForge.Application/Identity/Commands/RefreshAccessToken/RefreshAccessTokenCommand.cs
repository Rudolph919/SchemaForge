using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Identity.Commands.RefreshAccessToken;

// A command, unlike Login/SwitchOrganization: rotating the refresh token is a real write (the
// old row is revoked, a new one persisted), so it needs TransactionBehavior's SaveChanges wrap.
public sealed record RefreshAccessTokenCommand(string RefreshToken) : ICommand<Result<RefreshAccessTokenResult>>;

public sealed record RefreshAccessTokenResult(
    string AccessToken, string RefreshToken, Guid UserId, Guid OrganizationId, string DisplayName);
