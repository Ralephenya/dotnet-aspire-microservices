using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace GACS.IdentityTenant.Infrastructure.Data;

internal sealed class DapperConnectionFactory(IConfiguration configuration) : IDapperConnectionFactory
{
    private readonly string _connectionString =
        configuration.GetConnectionString("gacsdb")
        ?? throw new InvalidOperationException("Connection string 'gacsdb' is not configured.");

    public async Task<IDbConnection> CreateConnectionAsync(Guid tenantId, CancellationToken ct = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        // TODO: Set RLS session context so SQL Server Row Level Security filters by TenantId
        // await connection.ExecuteAsync(
        //     "EXEC sp_set_session_context @key = N'TenantId', @value = @tenantId",
        //     new { tenantId });

        return connection;
    }

    public async Task<IDbConnection> CreateElevatedConnectionAsync(CancellationToken ct = default)
    {
        // TODO: Use elevated connection string from Key Vault that bypasses RLS
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
