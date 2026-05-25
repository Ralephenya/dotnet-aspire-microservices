using System.Data;

namespace GACS.IdentityTenant.Infrastructure.Data;

public interface IDapperConnectionFactory
{
    /// <summary>
    /// Opens a SQL connection and sets the TenantId session context so RLS filters apply.
    /// Always use this — never open a raw SqlConnection directly.
    /// </summary>
    Task<IDbConnection> CreateConnectionAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Opens a connection with RLS bypassed. Only for system-level jobs and admin operations.
    /// </summary>
    Task<IDbConnection> CreateElevatedConnectionAsync(CancellationToken ct = default);
}
