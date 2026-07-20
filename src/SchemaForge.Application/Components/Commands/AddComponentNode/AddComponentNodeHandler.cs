using MediatR;
using SchemaForge.Application.Schemas;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Commands.AddComponentNode;

public sealed class AddComponentNodeHandler(IComponentVersionRepository componentVersionRepository)
    : IRequestHandler<AddComponentNodeCommand, Result<AddComponentNodeResult>>
{
    public async Task<Result<AddComponentNodeResult>> Handle(AddComponentNodeCommand request, CancellationToken cancellationToken)
    {
        var version = await componentVersionRepository.GetByIdAsync(request.ComponentVersionId, cancellationToken);
        if (version is null)
        {
            return Result<AddComponentNodeResult>.Failure(Error.NotFound("ComponentVersion.NotFound", "No such component version."));
        }

        var result = request.AttachmentKind switch
        {
            NodeAttachmentKind.ObjectProperty =>
                version.AddObjectProperty(request.ParentNodeId, request.PropertyName!, request.Kind),
            NodeAttachmentKind.ArrayPrefixItem => version.AddArrayPrefixItem(request.ParentNodeId, request.Kind),
            NodeAttachmentKind.ArrayItems => version.SetArrayItemsNode(request.ParentNodeId, request.Kind),
            NodeAttachmentKind.CompositionBranch => version.AddCompositionBranch(request.ParentNodeId, request.Kind),
            NodeAttachmentKind.ConditionalIf =>
                version.SetConditionalNode(request.ParentNodeId, ConditionalSlot.If, request.Kind),
            NodeAttachmentKind.ConditionalThen =>
                version.SetConditionalNode(request.ParentNodeId, ConditionalSlot.Then, request.Kind),
            NodeAttachmentKind.ConditionalElse =>
                version.SetConditionalNode(request.ParentNodeId, ConditionalSlot.Else, request.Kind),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.AttachmentKind, "Unknown attachment kind."),
        };

        return result.IsFailure
            ? Result<AddComponentNodeResult>.Failure(result.Error)
            : new AddComponentNodeResult(result.Value);
    }
}
