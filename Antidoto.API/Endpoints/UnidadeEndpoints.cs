using Antidoto.API.DTOs;
using Antidoto.API.Filters;
using Antidoto.API.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Antidoto.API.Endpoints;

public static class UnidadeEndpoints
{
    public static void MapUnidadeEndpoints(this WebApplication app)
    {
        var publico = app.MapGroup("/api/unidades")
            .WithTags("Unidades")
            .RequireRateLimiting("consulta")
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        publico.MapGet("/", Buscar)
            .AddEndpointFilter<ValidacaoFilter<UnidadeBuscaRequest>>()
            .WithName("BuscarUnidades")
            .WithSummary("Unidades com o soro dentro do raio, da mais próxima para a mais distante")
            .ProducesValidationProblem();

        publico.MapGet("/{id:guid}", Obter)
            .WithName("ObterUnidade")
            .WithSummary("Detalhe de uma unidade");

        var gestor = app.MapGroup("/api/unidades")
            .WithTags("Unidades")
            .RequireAuthorization("gestor")
            .RequireRateLimiting("usuario")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        gestor.MapPost("/", Criar)
            .AddEndpointFilter<ValidacaoFilter<UnidadeRequest>>()
            .WithName("CriarUnidade")
            .WithSummary("Cadastra uma unidade com seus soros (gestor)")
            .ProducesValidationProblem();

        gestor.MapPut("/{id:guid}", Atualizar)
            .AddEndpointFilter<ValidacaoFilter<UnidadeRequest>>()
            .WithName("AtualizarUnidade")
            .WithSummary("Atualiza uma unidade e seus soros (gestor)")
            .ProducesValidationProblem();
    }

    private static async Task<Ok<List<UnidadeResponse>>> Buscar(
        [AsParameters] UnidadeBuscaRequest busca, IUnidadeService service) =>
        TypedResults.Ok(await service.BuscarAsync(busca.Soro!.ToUpperInvariant(), busca.Lat, busca.Lng, busca.RaioKm));

    private static async Task<Results<Ok<UnidadeResponse>, NotFound>> Obter(Guid id, IUnidadeService service)
    {
        var unidade = await service.ObterAsync(id);
        return unidade is null ? TypedResults.NotFound() : TypedResults.Ok(unidade);
    }

    private static async Task<Created<UnidadeResponse>> Criar(UnidadeRequest request, IUnidadeService service)
    {
        var unidade = await service.CriarAsync(request);
        return TypedResults.Created($"/api/unidades/{unidade.Id}", unidade);
    }

    private static async Task<Results<Ok<UnidadeResponse>, NotFound>> Atualizar(
        Guid id, UnidadeRequest request, IUnidadeService service)
    {
        var unidade = await service.AtualizarAsync(id, request);
        return unidade is null ? TypedResults.NotFound() : TypedResults.Ok(unidade);
    }
}
