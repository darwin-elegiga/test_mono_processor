using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using VPay.Ols.Processor.Data.Connection;

namespace VPay.Ols.Processor.Data.Sql.Health;

public sealed class SqlServerSystemHealthCheck : IHealthCheck
{
    private readonly IDataConnection<SqlConnection> _connection;

    public SqlServerSystemHealthCheck(IDataConnection<SqlConnection> connection)
    {
        _connection = connection;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        bool canConnect = await _connection.CanConnectAsync(cancellationToken).ConfigureAwait(false);
        return canConnect ? HealthCheckResult.Healthy("Connection Succeeded") : HealthCheckResult.Unhealthy("Connection Failed");
    }
}
