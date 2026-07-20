using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Application.Schemas.Generation;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Application.Schemas.Commands.ImportSchemaVersion;

// Creates a new Draft the same way CreateSchemaVersionHandler does (same one-draft-at-a-time and
// version-bump rules - a small amount of duplication accepted here rather than forcing the two
// handlers to share a helper for what's otherwise a genuinely different command), then populates
// it via JsonSchemaImporter instead of leaving it empty for a human to build up node by node.
public sealed class ImportSchemaVersionHandler(
    ISchemaDefinitionRepository schemaDefinitionRepository,
    ISchemaVersionRepository schemaVersionRepository,
    IJsonSchemaImporter importer,
    ITenantContext tenantContext)
    : IRequestHandler<ImportSchemaVersionCommand, Result<ImportSchemaVersionResult>>
{
    public async Task<Result<ImportSchemaVersionResult>> Handle(
        ImportSchemaVersionCommand request, CancellationToken cancellationToken)
    {
        var schemaDefinition = await schemaDefinitionRepository.GetByIdAsync(request.SchemaDefinitionId, cancellationToken);
        if (schemaDefinition is null)
        {
            return Result<ImportSchemaVersionResult>.Failure(Error.NotFound("SchemaDefinition.NotFound", "No such schema."));
        }

        if (await schemaVersionRepository.HasDraftAsync(request.SchemaDefinitionId, cancellationToken))
        {
            return Result<ImportSchemaVersionResult>.Failure(Error.Conflict(
                "SchemaVersion.DraftAlreadyExists",
                "This schema already has a Draft version - publish or deprecate it before importing another."));
        }

        var latest = await schemaVersionRepository.GetLatestVersionNumberAsync(request.SchemaDefinitionId, cancellationToken);
        var nextVersion = latest is null ? SemVer.Initial : request.BumpKind switch
        {
            VersionBumpKind.Major => latest.NextMajor(),
            VersionBumpKind.Minor => latest.NextMinor(),
            VersionBumpKind.Patch => latest.NextPatch(),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.BumpKind, "Unknown version bump kind."),
        };

        var organizationId = tenantContext.CurrentTenantId!.Value;
        var version = SchemaVersion.CreateDraft(organizationId, request.SchemaDefinitionId, nextVersion, request.ChangeSummary);

        var importResult = importer.Import(version, request.SchemaDocument);
        if (importResult.IsFailure)
        {
            return Result<ImportSchemaVersionResult>.Failure(importResult.Error);
        }

        await schemaVersionRepository.AddAsync(version, cancellationToken);

        return new ImportSchemaVersionResult(version.Id, nextVersion);
    }
}
