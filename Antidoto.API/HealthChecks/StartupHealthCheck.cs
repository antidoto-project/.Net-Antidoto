using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Antidoto.API.HealthChecks;

/// <summary>Startup: a aplicação terminou de inicializar?</summary>
public class StartupHealthCheck : IHealthCheck
{
    private readonly IHostApplicationLifetime _lifetime;

    public StartupHealthCheck(IHostApplicationLifetime lifetime)
    {
        _lifetime = lifetime;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        return Task.FromResult(_lifetime.ApplicationStarted.IsCancellationRequested
            ? HealthCheckResult.Healthy("Inicialização concluída.")
            : HealthCheckResult.Unhealthy("Aplicação ainda inicializando."));
    }
}
