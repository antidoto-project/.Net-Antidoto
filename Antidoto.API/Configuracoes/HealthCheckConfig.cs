using System.Text.Json;
using Antidoto.API.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Antidoto.API.Configuracoes;

public static class HealthCheckConfig
{
    public static IServiceCollection AddAntidotoHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            // Liveness: a aplicação está viva?
            .AddCheck("api", () => HealthCheckResult.Healthy("API respondendo."), tags: new[] { "live" })
            // Startup: terminou de inicializar?
            .AddCheck<StartupHealthCheck>("startup", tags: new[] { "startup" })
            // Readiness: está pronta para receber tráfego?
            .AddCheck<OracleHealthCheck>("oracle", HealthStatus.Unhealthy, new[] { "ready" })
            .AddCheck<IaHealthCheck>("ia-antidoto", HealthStatus.Degraded, new[] { "ready" });
        return services;
    }

    public static void MapAntidotoHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health", Opcoes(_ => true));
        app.MapHealthChecks("/health/live", Opcoes(c => c.Tags.Contains("live")));
        app.MapHealthChecks("/health/startup", Opcoes(c => c.Tags.Contains("startup")));
        app.MapHealthChecks("/health/ready", Opcoes(c => c.Tags.Contains("ready")));
    }

    private static HealthCheckOptions Opcoes(Func<HealthCheckRegistration, bool> filtro) => new()
    {
        Predicate = filtro,
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = $"{e.Value.Duration.TotalMilliseconds:0.0}ms"
                }),
                totalDuration = $"{report.TotalDuration.TotalMilliseconds:0.0}ms"
            }));
        }
    };
}
