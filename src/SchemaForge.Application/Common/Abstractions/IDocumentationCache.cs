namespace SchemaForge.Application.Common.Abstractions;

// A port, not a direct IDistributedCache dependency - same reasoning as IFileStorage/IJobDispatcher
// (Step 1 §8/§9): Application shouldn't need a package reference to a specific caching technology
// just to express "cache this rendered documentation." Backed by Redis today (Step 6 §2.4).
public interface IDocumentationCache
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken);

    Task SetAsync(string key, string value, CancellationToken cancellationToken);
}
