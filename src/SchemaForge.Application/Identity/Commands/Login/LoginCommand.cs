using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Identity.Commands.Login;

// A command, not a query, despite looking like a pure read (Step 6 §2.1): issuing the access
// token itself still isn't a domain write, but persisting the paired refresh token row now is,
// so this needs TransactionBehavior's SaveChanges wrap the same as any other mutating command.
public sealed record LoginCommand(string Email, string Password) : ICommand<Result<LoginResult>>;

public sealed record LoginResult(
    string AccessToken, string RefreshToken, Guid UserId, Guid OrganizationId, string DisplayName);
