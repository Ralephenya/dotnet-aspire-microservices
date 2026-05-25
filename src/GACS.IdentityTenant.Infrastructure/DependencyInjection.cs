using GACS.IdentityTenant.Infrastructure.Cache;
using GACS.IdentityTenant.Infrastructure.Data;
using GACS.IdentityTenant.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace GACS.IdentityTenant.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string redisConnectionString)
    {
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddScoped<IDapperConnectionFactory, DapperConnectionFactory>();
        services.AddScoped<IRedisCacheService, RedisCacheService>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();

        return services;
    }
}
