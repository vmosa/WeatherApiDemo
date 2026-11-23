using Microsoft.Extensions.Caching.Distributed;

namespace WeatherApiDemo.Tests.Support;

/// <summary>Behaves like an unreachable Redis server.</summary>
internal sealed class ThrowingDistributedCache : IDistributedCache
{
    public byte[]? Get(string key) => throw new InvalidOperationException("cache down");

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
        throw new InvalidOperationException("cache down");

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
        throw new InvalidOperationException("cache down");

    public Task SetAsync(
        string key,
        byte[] value,
        DistributedCacheEntryOptions options,
        CancellationToken token = default) =>
        throw new InvalidOperationException("cache down");

    public void Refresh(string key) => throw new InvalidOperationException("cache down");

    public Task RefreshAsync(string key, CancellationToken token = default) =>
        throw new InvalidOperationException("cache down");

    public void Remove(string key) => throw new InvalidOperationException("cache down");

    public Task RemoveAsync(string key, CancellationToken token = default) =>
        throw new InvalidOperationException("cache down");
}
