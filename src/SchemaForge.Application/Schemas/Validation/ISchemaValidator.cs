using System.Text.Json;
using SchemaForge.Domain.Schemas;
using SchemaForge.Domain.Schemas.ValueObjects;

namespace SchemaForge.Application.Schemas.Validation;

// Validates a JSON payload directly against a SchemaNode tree - SchemaForge's own structured
// model is the source of truth (Step 2 §2), so this is a native interpreter over that model, not
// a wrapper around an off-the-shelf JSON Schema validator working from a compiled export. It has
// to exist independently of the (later, Phase 4) JSON Schema exporter, since /validate needs to
// work the moment a SchemaVersion exists, not only once export/import is built.
public interface ISchemaValidator
{
    IReadOnlyList<ValidationError> Validate(
        SchemaNode rootNode, IReadOnlyList<LocalDefinition> localDefinitions, JsonElement payload);
}
