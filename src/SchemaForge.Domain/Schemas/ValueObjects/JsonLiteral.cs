using System.Text.Json;

namespace SchemaForge.Domain.Schemas.ValueObjects;

// Backs SchemaNode.Examples/DefaultValue/AllowedValues/ConstValue - these need to hold an
// arbitrary JSON value (a string, number, object, array, null...), not a fixed CLR type. Stored
// as canonical JSON text rather than a raw JsonElement specifically so value equality (two
// JsonLiteral instances are equal if they represent the same value) is just string equality, not
// a hand-rolled deep-structural comparison JsonElement doesn't give you for free.
public sealed record JsonLiteral
{
    public string RawJson { get; }

    private JsonLiteral(string rawJson) => RawJson = rawJson;

    public static JsonLiteral FromRawJson(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            throw new ArgumentException("Raw JSON cannot be empty.", nameof(rawJson));
        }

        // Round-trips through JsonDocument + re-serialize so differently-formatted-but-equivalent
        // input (extra whitespace, different property order... not property order, that's
        // preserved) normalizes to the same canonical text. JsonElement.GetRawText() alone is
        // NOT enough for this - it returns the original source slice verbatim, whitespace and
        // all, so two inputs differing only in formatting would canonicalize to different text
        // and compare unequal. Confirmed by a failing unit test before this fix.
        using var document = JsonDocument.Parse(rawJson);
        var canonical = JsonSerializer.Serialize(document.RootElement);

        return new JsonLiteral(canonical);
    }

    public static JsonLiteral FromValue<T>(T value) => new(JsonSerializer.Serialize(value));

    public JsonElement AsJsonElement() => JsonDocument.Parse(RawJson).RootElement;

    public override string ToString() => RawJson;
}
