using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Organizations.Queries.ListMyMemberships;

// Powers both the frontend's organization switcher (Status == Active entries) and a "pending
// invitations" list (Status == Invited entries) - one query, the caller filters by status client-
// side rather than needing two near-identical endpoints.
public sealed record ListMyMembershipsQuery : IQuery<Result<IReadOnlyList<MembershipWithOrganizationSummary>>>;
