using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Commands.RemoveLocalDefinition;

public sealed record RemoveLocalDefinitionCommand(Guid SchemaVersionId, Guid LocalDefinitionId, uint ExpectedVersion)
    : ICommand<Result>;
