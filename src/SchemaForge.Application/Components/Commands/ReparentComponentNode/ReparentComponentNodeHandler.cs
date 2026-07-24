using MediatR;
using SchemaForge.Application.Schemas;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Commands.ReparentComponentNode;

public sealed class ReparentComponentNodeHandler(IComponentVersionRepository componentVersionRepository)
    : IRequestHandler<ReparentComponentNodeCommand, Result>
{
    public async Task<Result> Handle(ReparentComponentNodeCommand request, CancellationToken cancellationToken)
    {
        var version = await componentVersionRepository.GetByIdAsync(request.ComponentVersionId, cancellationToken);
        if (version is null)
        {
            return Result.Failure(Error.NotFound("ComponentVersion.NotFound", "No such component version."));
        }

        return request.AttachmentKind switch
        {
            NodeAttachmentKind.ObjectProperty =>
                version.ReparentNodeAsObjectProperty(request.NodeId, request.NewParentNodeId, request.PropertyName!),
            NodeAttachmentKind.ArrayPrefixItem => version.ReparentNodeAsArrayPrefixItem(request.NodeId, request.NewParentNodeId),
            NodeAttachmentKind.ArrayItems => version.ReparentNodeAsArrayItems(request.NodeId, request.NewParentNodeId),
            NodeAttachmentKind.CompositionBranch => version.ReparentNodeAsCompositionBranch(request.NodeId, request.NewParentNodeId),
            NodeAttachmentKind.ConditionalIf =>
                version.ReparentNodeAsConditionalNode(request.NodeId, request.NewParentNodeId, ConditionalSlot.If),
            NodeAttachmentKind.ConditionalThen =>
                version.ReparentNodeAsConditionalNode(request.NodeId, request.NewParentNodeId, ConditionalSlot.Then),
            NodeAttachmentKind.ConditionalElse =>
                version.ReparentNodeAsConditionalNode(request.NodeId, request.NewParentNodeId, ConditionalSlot.Else),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.AttachmentKind, "Unknown attachment kind."),
        };
    }
}
