using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Queries.GetSchemaDefinition;

public sealed record GetSchemaDefinitionQuery(Guid SchemaDefinitionId) : IQuery<Result<SchemaDefinitionDetail>>;

public sealed record SchemaDefinitionDetail(
    Guid Id, Guid ProjectId, string Name, string? Description, IReadOnlyList<string> Tags);
