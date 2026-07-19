using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Identity.Commands.SwitchOrganization;

// A query, not a command (same reasoning as Login): issuing a token isn't a domain write, so it
// shouldn't trigger TransactionBehavior's SaveChanges wrap.
public sealed record SwitchOrganizationQuery(Guid OrganizationId) : IQuery<Result<SwitchOrganizationResult>>;

public sealed record SwitchOrganizationResult(string AccessToken, Guid OrganizationId, string DisplayName);
