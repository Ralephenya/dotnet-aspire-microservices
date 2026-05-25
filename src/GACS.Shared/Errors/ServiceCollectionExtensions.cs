using Microsoft.Extensions.DependencyInjection;

namespace GACS.Shared.Errors;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGacsExceptionHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }
}
