using SchemaForge.Application.Common.Messaging;
using SchemaForge.Application.Organizations;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Organizations.Queries.ListMembers;

public sealed record ListOrganizationMembersQuery : IQuery<Result<IReadOnlyList<OrganizationMemberSummary>>>;
