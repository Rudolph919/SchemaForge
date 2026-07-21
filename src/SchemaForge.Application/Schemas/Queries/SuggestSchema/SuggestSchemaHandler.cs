using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Application.Workspaces;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Queries.SuggestSchema;

public sealed class SuggestSchemaHandler(
    ISourceDocumentRepository sourceDocumentRepository, ISchemaSuggestionProvider suggestionProvider)
    : IRequestHandler<SuggestSchemaQuery, Result<SchemaSuggestion>>
{
    public async Task<Result<SchemaSuggestion>> Handle(SuggestSchemaQuery request, CancellationToken cancellationToken)
    {
        var document = await sourceDocumentRepository.GetByIdAsync(request.SourceDocumentId, cancellationToken);
        if (document is null)
        {
            return Result<SchemaSuggestion>.Failure(Error.NotFound("SourceDocument.NotFound", "No such document."));
        }

        return await suggestionProvider.SuggestAsync(document, cancellationToken);
    }
}
