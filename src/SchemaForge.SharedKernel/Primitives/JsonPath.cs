namespace SchemaForge.SharedKernel.Primitives;

public sealed record JsonPath
{
    public string Value { get; }

    private JsonPath(string value) => Value = value;

    public static JsonPath Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("JSON path cannot be empty.", nameof(value));

        if (!value.StartsWith('$'))
            throw new ArgumentException("JSON path must start with '$'.", nameof(value));

        return new JsonPath(value);
    }

    public static JsonPath Root => new("$");

    public override string ToString() => Value;
}
