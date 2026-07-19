using MediatR;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Commands.AddSchemaNode;

public sealed class AddSchemaNodeHandler(ISchemaVersionRepository schemaVersionRepository)
    : IRequestHandler<AddSchemaNodeCommand, Result<AddSchemaNodeResult>>
{
    public async Task<Result<AddSchemaNodeResult>> Handle(AddSchemaNodeCommand request, CancellationToken cancellationToken)
    {
        var version = await schemaVersionRepository.GetByIdAsync(request.SchemaVersionId, cancellationToken);
        if (version is null)
        {
            return Result<AddSchemaNodeResult>.Failure(Error.NotFound("SchemaVersion.NotFound", "No such schema version."));
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
            ? Result<AddSchemaNodeResult>.Failure(result.Error)
            : new AddSchemaNodeResult(result.Value);
    }
}
