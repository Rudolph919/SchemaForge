using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Commands.RemoveSchemaNode;

public sealed class RemoveSchemaNodeHandler(ISchemaVersionRepository schemaVersionRepository)
    : IRequestHandler<RemoveSchemaNodeCommand, Result>
{
    public async Task<Result> Handle(RemoveSchemaNodeCommand request, CancellationToken cancellationToken)
    {
        var version = await schemaVersionRepository.GetByIdAsync(request.SchemaVersionId, cancellationToken);
        if (version is null)
        {
            return Result.Failure(Error.NotFound("SchemaVersion.NotFound", "No such schema version."));
        }

        var result = version.RemoveNode(request.NodeId);
        if (result.IsSuccess)
        {
            schemaVersionRepository.ApplyExpectedVersion(version, request.ExpectedVersion);
        }

        return result;
    }
}
