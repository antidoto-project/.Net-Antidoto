using Antidoto.API.DTOs;
using Antidoto.API.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Antidoto.API.Endpoints;

public static class EstatisticaEndpoints
{
    public static void MapEstatisticaEndpoints(this WebApplication app)
    {
        app.MapGroup("/api/estatisticas")
            .WithTags("Estatísticas")
            .RequireAuthorization("gestor")
            .RequireRateLimiting("usuario")
            .MapGet("/regiao", Regiao)
            .WithName("EstatisticasRegiao")
            .WithSummary("Ocorrências por grupo de animal e por mês (gestor)")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    private static async Task<Ok<EstatisticasResponse>> Regiao(IOcorrenciaService service) =>
        TypedResults.Ok(await service.EstatisticasAsync());
}
