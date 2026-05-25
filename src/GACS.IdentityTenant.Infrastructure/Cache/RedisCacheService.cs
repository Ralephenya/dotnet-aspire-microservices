using System.Text.Json;
using StackExchange.Redis;

namespace GACS.IdentityTenant.Infrastructure.Cache;

internal sealed class RedisCacheService(IConnectionMultiplexer redis) : IRedisCacheService
{
    public async Task<T?> GetAsync<T>(int database, string key, CancellationToken ct = default)
    {
        var db = redis.GetDatabase(database);
        var value = await db.StringGetAsync(key);
        return value.HasValue ? JsonSerializer.Deserialize<T>((string)value!) : default;
    }

    public async Task SetAsync<T>(int database, string key, T value, TimeSpan expiry, CancellationToken ct = default)
    {
        var db = redis.GetDatabase(database);
        var json = JsonSerializer.Serialize(value);
        await db.StringSetAsync(key, json, expiry);
    }

    public async Task RemoveAsync(int database, string key, CancellationToken ct = default)
    {
        var db = redis.GetDatabase(database);
        await db.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(int database, string key, CancellationToken ct = default)
    {
        var db = redis.GetDatabase(database);
        return await db.KeyExistsAsync(key);
    }

    public async Task IncrementAsync(int database, string key, CancellationToken ct = default)
    {
        var db = redis.GetDatabase(database);
        await db.StringIncrementAsync(key);
    }
}
