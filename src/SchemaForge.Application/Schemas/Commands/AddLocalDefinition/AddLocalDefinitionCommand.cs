using SchemaForge.Application.Common.Messaging;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Commands.AddLocalDefinition;

public sealed record AddLocalDefinitionCommand(Guid SchemaVersionId, string Name, NodeKind? RootKind)
    : ICommand<Result<AddLocalDefinitionResult>>;

public sealed record AddLocalDefinitionResult(Guid LocalDefinitionId);
