using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Application.Schemas;
using SchemaForge.Application.Schemas.Validation;
using SchemaForge.Domain.Validation;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Validation.Commands.ValidateJsonPayload;

public sealed class ValidateJsonPayloadHandler(
    ISchemaVersionRepository schemaVersionRepository,
    ISchemaDefinitionRepository schemaDefinitionRepository,
    IValidationRunRepository validationRunRepository,
    ISchemaValidator schemaValidator,
    ITenantContext tenantContext,
    ICurrentUserContext currentUserContext)
    : IRequestHandler<ValidateJsonPayloadCommand, Result<ValidateJsonPayloadResult>>
{
    public async Task<Result<ValidateJsonPayloadResult>> Handle(
        ValidateJsonPayloadCommand request, CancellationToken cancellationToken)
    {
        var version = await schemaVersionRepository.GetByIdAsync(request.SchemaVersionId, cancellationToken);
        if (version is null)
        {
            return Result<ValidateJsonPayloadResult>.Failure(
                Error.NotFound("SchemaVersion.NotFound", "No such schema version."));
        }

        var schemaDefinition = await schemaDefinitionRepository.GetByIdAsync(version.SchemaDefinitionId, cancellationToken);
        if (schemaDefinition is null)
        {
            return Result<ValidateJsonPayloadResult>.Failure(
                Error.NotFound("SchemaDefinition.NotFound", "No such schema."));
        }

        var errors = schemaValidator.Validate(version.RootNode, version.LocalDefinitions, request.Payload);
        var hash = ComputeHash(request.Payload);

        var organizationId = tenantContext.CurrentTenantId!.Value;
        var userId = currentUserContext.UserId!.Value;

        var run = ValidationRun.Record(organizationId, schemaDefinition.ProjectId, version.Id, hash, errors, userId);
        await validationRunRepository.AddAsync(run, cancellationToken);

        return new ValidateJsonPayloadResult(run.Id, run.Outcome, errors);
    }

    // Hashes the canonical re-serialization, not raw request bytes - two payloads differing only
    // in insignificant whitespace should hash identically for dedup purposes (same reasoning as
    // JsonLiteral's canonicalization).
    private static string ComputeHash(JsonElement payload)
    {
        var canonicalJson = JsonSerializer.Serialize(payload);
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalJson));
        return Convert.ToHexStringLower(hashBytes);
    }
}
