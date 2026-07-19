using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Application.Schemas.Commands.CreateSchemaVersion;

public sealed class CreateSchemaVersionHandler(
    ISchemaDefinitionRepository schemaDefinitionRepository,
    ISchemaVersionRepository schemaVersionRepository,
    ITenantContext tenantContext)
    : IRequestHandler<CreateSchemaVersionCommand, Result<CreateSchemaVersionResult>>
{
    public async Task<Result<CreateSchemaVersionResult>> Handle(
        CreateSchemaVersionCommand request, CancellationToken cancellationToken)
    {
        var schemaDefinition = await schemaDefinitionRepository.GetByIdAsync(request.SchemaDefinitionId, cancellationToken);
        if (schemaDefinition is null)
        {
            return Result<CreateSchemaVersionResult>.Failure(
                Error.NotFound("SchemaDefinition.NotFound", "No such schema."));
        }

        // Fast, friendly pre-flight check (Step 3 §4) - the partial unique index on
        // (schema_definition_id) WHERE status = 'Draft' is the actual concurrency-safe guarantee
        // if two requests race past this check at the same time.
        if (await schemaVersionRepository.HasDraftAsync(request.SchemaDefinitionId, cancellationToken))
        {
            return Result<CreateSchemaVersionResult>.Failure(Error.Conflict(
                "SchemaVersion.DraftAlreadyExists",
                "This schema already has a Draft version - publish or deprecate it before creating another."));
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
        await schemaVersionRepository.AddAsync(version, cancellationToken);

        return new CreateSchemaVersionResult(version.Id, nextVersion);
    }
}
