using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Commands.RemoveComponentNode;

public sealed record RemoveComponentNodeCommand(Guid ComponentVersionId, Guid NodeId, uint ExpectedVersion) : ICommand<Result>;
