using SchemaForge.Application.Common.Messaging;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Commands.UpdateComponentNode;

public sealed record UpdateComponentNodeCommand(
    Guid ComponentVersionId, Guid NodeId, SchemaNodeContent Content, uint ExpectedVersion) : ICommand<Result>;
