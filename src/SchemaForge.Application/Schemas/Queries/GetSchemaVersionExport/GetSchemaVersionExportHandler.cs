using MediatR;
using SchemaForge.Application.Schemas.Generation;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Queries.GetSchemaVersionExport;

public sealed class GetSchemaVersionExportHandler(
    ISchemaVersionRepository schemaVersionRepository, IEnumerable<ISchemaExporter> exporters)
    : IRequestHandler<GetSchemaVersionExportQuery, Result<string>>
{
    public async Task<Result<string>> Handle(GetSchemaVersionExportQuery request, CancellationToken cancellationToken)
    {
        var version = await schemaVersionRepository.GetByIdAsync(request.SchemaVersionId, cancellationToken);
        if (version is null)
        {
            return Result<string>.Failure(Error.NotFound("SchemaVersion.NotFound", "No such schema version."));
        }

        var exporter = exporters.FirstOrDefault(e => e.FormatKey == request.Format);
        if (exporter is null)
        {
            return Result<string>.Failure(Error.Validation(
                "SchemaExport.UnknownFormat", $"Unknown export format '{request.Format}'."));
        }

        var content = await exporter.ExportAsync(version, cancellationToken);

        return content;
    }
}
