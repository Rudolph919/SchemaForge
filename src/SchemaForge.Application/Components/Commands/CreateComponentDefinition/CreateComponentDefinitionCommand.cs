using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Commands.CreateComponentDefinition;

public sealed record CreateComponentDefinitionCommand(string Name, string? Description)
    : ICommand<Result<CreateComponentDefinitionResult>>;

public sealed record CreateComponentDefinitionResult(Guid ComponentDefinitionId);
