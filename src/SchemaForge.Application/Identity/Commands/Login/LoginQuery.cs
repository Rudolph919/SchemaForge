using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Identity.Commands.Login;

// A query, not a command, despite POST at the API layer (Step 6 §2.1): it doesn't mutate any
// persisted state (issuing a token isn't a domain write), so it shouldn't trigger
// TransactionBehavior's SaveChanges wrap. The REST verb is about idempotency/caching semantics,
// not the CQRS categorization - they don't have to match 1:1.
public sealed record LoginQuery(string Email, string Password) : IQuery<Result<LoginResult>>;

public sealed record LoginResult(string AccessToken, Guid UserId, Guid OrganizationId, string DisplayName);
