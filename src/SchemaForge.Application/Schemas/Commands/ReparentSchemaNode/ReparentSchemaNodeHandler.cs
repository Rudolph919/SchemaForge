using MediatR;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Commands.ReparentSchemaNode;

public sealed class ReparentSchemaNodeHandler(ISchemaVersionRepository schemaVersionRepository)
    : IRequestHandler<ReparentSchemaNodeCommand, Result>
{
    public async Task<Result> Handle(ReparentSchemaNodeCommand request, CancellationToken cancellationToken)
    {
        var version = await schemaVersionRepository.GetByIdAsync(request.SchemaVersionId, cancellationToken);
        if (version is null)
        {
            return Result.Failure(Error.NotFound("SchemaVersion.NotFound", "No such schema version."));
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
