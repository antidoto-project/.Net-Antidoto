using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace Antidoto.API.Configuracoes;

public static class RateLimitingConfig
{
    /// <summary>
    /// 4 políticas. Os limites ficam no appsettings (seção RateLimiting) para os testes usarem outros valores.
    /// Ao estourar: 429 em ProblemDetails com o header Retry-After.
    /// </summary>
    public static IServiceCollection AddAntidotoRateLimiting(this IServiceCollection services, IConfiguration config)
    {
        var limites = config.GetSection("RateLimiting");
        var janela = TimeSpan.FromSeconds(limites.GetValue("JanelaSegundos", 60));

        services.AddRateLimiter(options =>
        {
            // sos: cada chamada aciona a IA, que é cara → janela deslizante por IP
            options.AddPolicy("sos", http => RateLimitPartition.GetSlidingWindowLimiter(Ip(http), _ =>
                new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = limites.GetValue("Sos", 10),
                    Window = janela,
                    SegmentsPerWindow = 6
                }));

            // auth: evita tentativa de senha em massa → janela fixa por IP
            options.AddPolicy("auth", http => RateLimitPartition.GetFixedWindowLimiter(Ip(http), _ =>
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limites.GetValue("Auth", 5),
                    Window = janela
                }));

            // consulta: leitura barata, sem abuso → token bucket por IP
            options.AddPolicy("consulta", http => RateLimitPartition.GetTokenBucketLimiter(Ip(http), _ =>
                new TokenBucketRateLimiterOptions
                {
                    TokenLimit = limites.GetValue("Consulta", 60),
                    TokensPerPeriod = limites.GetValue("Consulta", 60),
                    ReplenishmentPeriod = janela
                }));

            // usuario: limite justo por pessoa logada → janela fixa pelo id do token (sub)
            options.AddPolicy("usuario", http => RateLimitPartition.GetFixedWindowLimiter(
                http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Ip(http), _ =>
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limites.GetValue("Usuario", 30),
                    Window = janela
                }));

            options.OnRejected = async (context, ct) =>
            {
                var segundos = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
                    ? (int)Math.Ceiling(retry.TotalSeconds)
                    : (int)janela.TotalSeconds;

                var response = context.HttpContext.Response;
                response.StatusCode = StatusCodes.Status429TooManyRequests;
                response.Headers.RetryAfter = segundos.ToString();

                await response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Muitas requisições",
                    Detail = $"Limite atingido. Tente de novo em {segundos} segundos.",
                    Instance = context.HttpContext.Request.Path
                }, options: null, contentType: "application/problem+json", cancellationToken: ct);
            };
        });

        return services;
    }

    private static string Ip(HttpContext http) =>
        http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
}
