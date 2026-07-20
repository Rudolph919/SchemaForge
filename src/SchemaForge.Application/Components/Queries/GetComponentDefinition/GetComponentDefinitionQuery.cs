using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Queries.GetComponentDefinition;

public sealed record GetComponentDefinitionQuery(Guid ComponentDefinitionId) : IQuery<Result<ComponentDefinitionDetail>>;

public sealed record ComponentDefinitionDetail(Guid Id, Guid OrganizationId, string Name, string? Description);
