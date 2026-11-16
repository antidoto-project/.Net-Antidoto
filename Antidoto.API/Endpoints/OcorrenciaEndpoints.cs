using System.Security.Claims;
using Antidoto.API.DTOs;
using Antidoto.API.Filters;
using Antidoto.API.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Antidoto.API.Endpoints;

public static class OcorrenciaEndpoints
{
    public static void MapOcorrenciaEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/ocorrencias")
            .WithTags("Ocorrências")
            .RequireRateLimiting("usuario")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/", Criar)
            .RequireAuthorization("cidadao")
            .AddEndpointFilter<ValidacaoFilter<OcorrenciaRequest>>()
            .AddEndpointFilter<IdempotenciaFilter<OcorrenciaRequest>>()
            .WithName("CriarOcorrencia")
            .WithSummary("Registra uma ocorrência (exige o header Idempotency-Key)")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/minhas", ListarMinhas)
            .RequireAuthorization("cidadao")
            .WithName("MinhasOcorrencias")
            .WithSummary("Ocorrências do usuário logado, paginadas");

        group.MapGet("/{id:guid}", Obter)
            .RequireAuthorization()
            .WithName("ObterOcorrencia")
            .WithSummary("Detalhe de uma ocorrência (dono ou gestor)")
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<Created<OcorrenciaResponse>> Criar(
        OcorrenciaRequest request, ClaimsPrincipal user, IOcorrenciaService service)
    {
        var ocorrencia = await service.CriarAsync(user.Id(), request);
        return TypedResults.Created($"/api/ocorrencias/{ocorrencia.Id}", ocorrencia);
    }

    private static async Task<Ok<PaginaResponse<OcorrenciaResponse>>> ListarMinhas(
        ClaimsPrincipal user, IOcorrenciaService service, int pagina = 1, int tamanho = 10) =>
        TypedResults.Ok(await service.ListarDoUsuarioAsync(user.Id(), pagina, tamanho));

    private static async Task<Results<Ok<OcorrenciaResponse>, NotFound, ForbidHttpResult>> Obter(
        Guid id, ClaimsPrincipal user, IOcorrenciaService service)
    {
        var ocorrencia = await service.ObterAsync(id);
        if (ocorrencia is null) return TypedResults.NotFound();
        if (ocorrencia.UsuarioId != user.Id() && !user.EhGestor()) return TypedResults.Forbid();
        return TypedResults.Ok(ocorrencia);
    }
}
