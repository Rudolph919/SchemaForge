using SchemaForge.Application.Common.Messaging;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Commands.AddComponentLocalDefinition;

public sealed record AddComponentLocalDefinitionCommand(Guid ComponentVersionId, string Name, NodeKind? RootKind)
    : ICommand<Result<AddComponentLocalDefinitionResult>>;

public sealed record AddComponentLocalDefinitionResult(Guid LocalDefinitionId);
