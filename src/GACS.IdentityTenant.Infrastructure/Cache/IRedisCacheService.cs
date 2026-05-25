namespace GACS.IdentityTenant.Infrastructure.Cache;

public interface IRedisCacheService
{
    Task<T?> GetAsync<T>(int database, string key, CancellationToken ct = default);
    Task SetAsync<T>(int database, string key, T value, TimeSpan expiry, CancellationToken ct = default);
    Task RemoveAsync(int database, string key, CancellationToken ct = default);
    Task<bool> ExistsAsync(int database, string key, CancellationToken ct = default);
    Task IncrementAsync(int database, string key, CancellationToken ct = default);
}
