using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using SchemaForge.Domain.Schemas;

namespace SchemaForge.Application.Schemas.Validation;

// Dispatches to a purpose-built check per SchemaFormat value (Step 4 §4.2) rather than a single
// generic string match - a real date/email/UUID validator catches things a naive regex won't.
// Custom is deliberately always valid here: it's an open-ended, free-text format string with no
// fixed semantics SchemaForge itself can check.
internal static partial class SchemaFormatValidator
{
    public static bool IsValid(SchemaFormat format, string value) => format switch
    {
        SchemaFormat.Date => DateOnly.TryParse(value, out _) && DatePattern().IsMatch(value),
        SchemaFormat.DateTime => DateTimeOffset.TryParse(value, out _) && value.Contains('T'),
        SchemaFormat.Time => TimeOnly.TryParse(value, out _),
        SchemaFormat.Email => MailAddress.TryCreate(value, out _),
        SchemaFormat.Hostname => HostnamePattern().IsMatch(value),
        SchemaFormat.Ipv4 => IPAddress.TryParse(value, out var ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork,
        SchemaFormat.Ipv6 => IPAddress.TryParse(value, out var ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6,
        SchemaFormat.Uri => Uri.TryCreate(value, UriKind.Absolute, out _),
        SchemaFormat.UriReference => Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out _),
        SchemaFormat.Uuid => Guid.TryParse(value, out _),
        SchemaFormat.Custom => true,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown schema format."),
    };

    // DateOnly.TryParse alone is too permissive (accepts "1/2/2024") - Draft 2020-12's "date"
    // format is specifically RFC 3339 full-date, YYYY-MM-DD.
    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}$")]
    private static partial Regex DatePattern();

    [GeneratedRegex(@"^[a-zA-Z0-9]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$")]
    private static partial Regex HostnamePattern();
}
