using Antidoto.API.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Antidoto.API.HealthChecks;

/// <summary>Readiness: sem banco a API não consegue atender.</summary>
public class OracleHealthCheck : IHealthCheck
{
    private readonly AppDbContext _db;

    public OracleHealthCheck(AppDbContext db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            return await _db.Database.CanConnectAsync(ct)
                ? HealthCheckResult.Healthy("Oracle acessível.")
                : HealthCheckResult.Unhealthy("Oracle inacessível.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Oracle inacessível.", ex);
        }
    }
}
