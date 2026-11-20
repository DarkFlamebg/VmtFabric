using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace VmtFabric.Api.Data;

/// <summary>Comprueba que SQL Server responde. No expone detalles de la conexión.</summary>
public partial class DatabaseHealthCheck(VmtFabricDbContext db, ILogger<DatabaseHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogSinConexion(logger, ex);
            return HealthCheckResult.Unhealthy();
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Health check: no se pudo conectar a SQL Server")]
    private static partial void LogSinConexion(ILogger logger, Exception ex);
}
