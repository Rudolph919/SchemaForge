using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Commands.MoveComponentNode;

public sealed record MoveComponentNodeCommand(Guid ComponentVersionId, Guid NodeId, int NewOrder) : ICommand<Result>;
