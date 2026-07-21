using Microsoft.AspNetCore.Mvc;

namespace SchemaForge.Api.Common;

// Step 6 §1.5: every mutable resource exposes an ETag response header (backed by Postgres's
// xmin, surfaced as a plain uint via IHasRowVersion) and requires If-Match on PATCH/DELETE.
public static class ConcurrencyExtensions
{
    public static void SetETag(this HttpResponse response, uint rowVersion) =>
        response.Headers.ETag = $"\"{rowVersion}\"";

    // ETags are quoted per RFC 9110 ("12345"), but tolerate an unquoted value too - some HTTP
    // clients/tools normalize this away, and there's no ambiguity risk in accepting either form.
    public static bool TryGetIfMatch(this HttpRequest request, out uint expectedVersion)
    {
        expectedVersion = 0;

        if (!request.Headers.TryGetValue("If-Match", out var values))
        {
            return false;
        }

        var raw = values.ToString().Trim().Trim('"');
        return uint.TryParse(raw, out expectedVersion);
    }

    // 428, not 400/409 - this is specifically "you didn't tell me what version you expected,"
    // distinct from "you expected the wrong version" (409, surfaced via
    // TransactionBehavior/Error.Conflict once EF's own concurrency check fires).
    public static IActionResult PreconditionRequired() =>
        new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status428PreconditionRequired,
            Title = "Precondition Required",
            Detail = "An If-Match header carrying this resource's current ETag is required for this request.",
        })
        {
            StatusCode = StatusCodes.Status428PreconditionRequired,
            ContentTypes = { "application/problem+json" },
        };
}
