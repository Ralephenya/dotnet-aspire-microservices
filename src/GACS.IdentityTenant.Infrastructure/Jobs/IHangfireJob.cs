namespace GACS.IdentityTenant.Infrastructure.Jobs;

public interface IHangfireJob
{
    Task ExecuteAsync(CancellationToken ct = default);
}
