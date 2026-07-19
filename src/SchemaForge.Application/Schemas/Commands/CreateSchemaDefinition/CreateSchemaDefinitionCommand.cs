using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Commands.CreateSchemaDefinition;

public sealed record CreateSchemaDefinitionCommand(Guid ProjectId, string Name, string? Description)
    : ICommand<Result<CreateSchemaDefinitionResult>>;

public sealed record CreateSchemaDefinitionResult(Guid SchemaDefinitionId);
