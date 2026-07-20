using Microsoft.Extensions.Caching.Distributed;
using SchemaForge.Application.Common.Abstractions;

namespace SchemaForge.Infrastructure.Caching;

public sealed class RedisDocumentationCache(IDistributedCache cache) : IDocumentationCache
{
    public Task<string?> GetAsync(string key, CancellationToken cancellationToken) => cache.GetStringAsync(key, cancellationToken);

    public Task SetAsync(string key, string value, CancellationToken cancellationToken) => cache.SetStringAsync(key, value, cancellationToken);
}
