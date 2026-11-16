using Antidoto.API.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Antidoto.API.HealthChecks;

/// <summary>Serviço externo de IA. Fora do ar = Degraded, porque o SOS segue pelas regras locais.</summary>
public class IaHealthCheck : IHealthCheck
{
    private readonly IIaClient _ia;

    public IaHealthCheck(IIaClient ia)
    {
        _ia = ia;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        return await _ia.EstaOnlineAsync(ct)
            ? HealthCheckResult.Healthy("Antídoto IA respondendo.")
            : HealthCheckResult.Degraded("Antídoto IA fora do ar. Triagem usando regras locais.");
    }
}
