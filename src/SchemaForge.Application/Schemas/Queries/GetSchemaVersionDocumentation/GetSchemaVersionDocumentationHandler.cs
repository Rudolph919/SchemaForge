using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Application.Schemas.Generation;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Queries.GetSchemaVersionDocumentation;

public sealed class GetSchemaVersionDocumentationHandler(
    ISchemaVersionRepository schemaVersionRepository, IEnumerable<IDocumentationRenderer> renderers, IDocumentationCache cache)
    : IRequestHandler<GetSchemaVersionDocumentationQuery, Result<string>>
{
    public async Task<Result<string>> Handle(GetSchemaVersionDocumentationQuery request, CancellationToken cancellationToken)
    {
        var renderer = renderers.FirstOrDefault(r => r.FormatKey == request.Format);
        if (renderer is null)
        {
            return Result<string>.Failure(Error.Validation(
                "SchemaDocumentation.UnknownFormat", $"Unknown documentation format '{request.Format}'."));
        }

        var version = await schemaVersionRepository.GetByIdAsync(request.SchemaVersionId, cancellationToken);
        if (version is null)
        {
            return Result<string>.Failure(Error.NotFound("SchemaVersion.NotFound", "No such schema version."));
        }

        // Only Published/Deprecated versions are genuinely immutable (Step 3 §2's "versions are
        // immutable after publish" - a Draft is explicitly still mutable). Caching a Draft's
        // documentation risks serving stale content after an edit; computing it fresh every time
        // is simpler than adding cache-invalidation-on-every-node-mutation for a case that
        // doesn't need caching's benefit anyway.
        if (version.Status == SchemaLifecycleStatus.Draft)
        {
            return await renderer.RenderAsync(version, cancellationToken);
        }

        var cacheKey = $"documentation:{request.SchemaVersionId}:{request.Format}";
        var cached = await cache.GetAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var rendered = await renderer.RenderAsync(version, cancellationToken);
        await cache.SetAsync(cacheKey, rendered, cancellationToken);

        return rendered;
    }
}
