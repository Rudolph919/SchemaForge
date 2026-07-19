using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Commands.UpdateSchemaDefinitionDetails;

public sealed record UpdateSchemaDefinitionDetailsCommand(
    Guid SchemaDefinitionId, string Name, string? Description, IReadOnlyList<string> Tags) : ICommand<Result>;
