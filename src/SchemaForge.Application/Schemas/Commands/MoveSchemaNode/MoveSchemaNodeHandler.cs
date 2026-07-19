using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Commands.MoveSchemaNode;

public sealed class MoveSchemaNodeHandler(ISchemaVersionRepository schemaVersionRepository)
    : IRequestHandler<MoveSchemaNodeCommand, Result>
{
    public async Task<Result> Handle(MoveSchemaNodeCommand request, CancellationToken cancellationToken)
    {
        var version = await schemaVersionRepository.GetByIdAsync(request.SchemaVersionId, cancellationToken);
        if (version is null)
        {
            return Result.Failure(Error.NotFound("SchemaVersion.NotFound", "No such schema version."));
        }

        return version.MoveNode(request.NodeId, request.NewOrder);
    }
}
