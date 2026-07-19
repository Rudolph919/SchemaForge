using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Commands.UpdateSchemaNode;

public sealed class UpdateSchemaNodeHandler(ISchemaVersionRepository schemaVersionRepository)
    : IRequestHandler<UpdateSchemaNodeCommand, Result>
{
    public async Task<Result> Handle(UpdateSchemaNodeCommand request, CancellationToken cancellationToken)
    {
        var version = await schemaVersionRepository.GetByIdAsync(request.SchemaVersionId, cancellationToken);
        if (version is null)
        {
            return Result.Failure(Error.NotFound("SchemaVersion.NotFound", "No such schema version."));
        }

        return version.UpdateNode(request.NodeId, request.Content);
    }
}
