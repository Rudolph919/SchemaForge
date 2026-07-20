using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Commands.UpdateComponentDefinitionDetails;

public sealed record UpdateComponentDefinitionDetailsCommand(Guid ComponentDefinitionId, string Name, string? Description)
    : ICommand<Result>;
