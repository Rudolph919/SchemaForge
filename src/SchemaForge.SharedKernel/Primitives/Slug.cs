using System.Text.RegularExpressions;

namespace SchemaForge.SharedKernel.Primitives;

public sealed partial record Slug
{
    public string Value { get; }

    private Slug(string value) => Value = value;

    public static Slug Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Slug cannot be empty.", nameof(value));

        if (!SlugPattern().IsMatch(value))
            throw new ArgumentException(
                "Slug must be lowercase, alphanumeric, and hyphen-separated (e.g. 'acme-corp').", nameof(value));

        return new Slug(value);
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
