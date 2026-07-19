using SchemaForge.Application.Common.Messaging;
using SchemaForge.Application.Organizations;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Organizations.Queries.ListTeams;

public sealed record ListTeamsQuery : IQuery<Result<IReadOnlyList<TeamSummary>>>;
