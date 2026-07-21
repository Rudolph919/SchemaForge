using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.JsonWebTokens;

namespace SchemaForge.Api.Middleware;

// Step 6 §1.6: a client retry (timeout, network blip) of a POST carrying the same
// Idempotency-Key header replays the original response instead of re-executing the side effect.
// Backed by Redis (the same IDistributedCache instance the documentation cache already uses),
// not a new dependency.
//
// Runs after routing has selected an endpoint and after authentication (needs both
// HttpContext.GetEndpoint() for the [Idempotent] marker and the "sub" claim to scope the cache
// key per user), so it's registered between UseAuthorization() and MapControllers().
//
// Known simplification, acceptable for this project's scope: no distributed lock against two
// truly concurrent requests carrying the same key racing each other - both could execute before
// either finishes caching a response. A production system protecting a "charge a card" endpoint
// would want a short-lived lock too; this protects the realistic case the doc names (a client
// retrying after a timeout), not adversarial concurrent duplication.
public sealed class IdempotencyKeyMiddleware(RequestDelegate next)
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public async Task InvokeAsync(HttpContext context, IDistributedCache cache)
    {
        var endpoint = context.GetEndpoint();
        var isIdempotent = endpoint?.Metadata.GetMetadata<IdempotentAttribute>() is not null;

        if (!isIdempotent
            || !HttpMethods.IsPost(context.Request.Method)
            || !context.Request.Headers.TryGetValue(IdempotencyKeyHeader, out var keyValues)
            || string.IsNullOrWhiteSpace(keyValues.ToString()))
        {
            await next(context);
            return;
        }

        var userId = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? "anonymous";
        var cacheKey = $"idempotency:{userId}:{keyValues}";

        var cachedBytes = await cache.GetAsync(cacheKey, context.RequestAborted);
        if (cachedBytes is not null)
        {
            var cached = JsonSerializer.Deserialize<CachedResponse>(cachedBytes)!;
            context.Response.StatusCode = cached.StatusCode;
            context.Response.ContentType = cached.ContentType;
            await context.Response.WriteAsync(cached.Body, Encoding.UTF8, context.RequestAborted);
            return;
        }

        var originalBody = context.Response.Body;
        await using var capturedBody = new MemoryStream();
        context.Response.Body = capturedBody;

        try
        {
            await next(context);
        }
        finally
        {
            context.Response.Body = originalBody;
        }

        capturedBody.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(capturedBody).ReadToEndAsync(context.RequestAborted);

        // Only a genuinely successful side effect is worth replaying - caching a failure would
        // mean a client that fixes the underlying problem and retries with the same key is stuck
        // replaying the old error forever instead of getting a fresh attempt.
        if (context.Response.StatusCode is >= 200 and < 300)
        {
            var toCache = new CachedResponse(context.Response.StatusCode, context.Response.ContentType, body);
            await cache.SetAsync(
                cacheKey,
                JsonSerializer.SerializeToUtf8Bytes(toCache),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration },
                context.RequestAborted);
        }

        capturedBody.Seek(0, SeekOrigin.Begin);
        await capturedBody.CopyToAsync(originalBody, context.RequestAborted);
    }

    private sealed record CachedResponse(int StatusCode, string? ContentType, string Body);
}
