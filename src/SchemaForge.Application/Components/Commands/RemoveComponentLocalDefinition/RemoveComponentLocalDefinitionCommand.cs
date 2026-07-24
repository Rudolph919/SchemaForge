using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Commands.RemoveComponentLocalDefinition;

public sealed record RemoveComponentLocalDefinitionCommand(
    Guid ComponentVersionId, Guid LocalDefinitionId, uint ExpectedVersion) : ICommand<Result>;
