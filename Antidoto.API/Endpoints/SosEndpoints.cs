using Antidoto.API.DTOs;
using Antidoto.API.Filters;
using Antidoto.API.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Antidoto.API.Endpoints;

public static class SosEndpoints
{
    public static void MapSosEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/sos")
            .WithTags("SOS")
            .RequireRateLimiting("sos")
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/triagem", Triar)
            .AddEndpointFilter<ValidacaoFilter<TriagemRequest>>()
            .WithName("Triagem")
            .WithSummary("Classifica a urgência, indica o soro e as 3 unidades mais próximas (público)")
            .ProducesValidationProblem();
    }

    private static async Task<Ok<TriagemResponse>> Triar(TriagemRequest request, ITriagemService service) =>
        TypedResults.Ok(await service.TriarAsync(request));
}
