using Antidoto.API.DTOs;
using Antidoto.API.Filters;
using Antidoto.API.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Antidoto.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .RequireRateLimiting("auth")
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/registrar", Registrar)
            .AddEndpointFilter<ValidacaoFilter<RegistrarRequest>>()
            .WithName("Registrar")
            .WithSummary("Cria uma conta de cidadão")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", Login)
            .AddEndpointFilter<ValidacaoFilter<LoginRequest>>()
            .WithName("Login")
            .WithSummary("Autentica e devolve o token JWT")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<Results<Created<UsuarioResponse>, ProblemHttpResult>> Registrar(
        RegistrarRequest request, IAuthService service)
    {
        var usuario = await service.RegistrarAsync(request);
        return usuario is null
            ? TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "E-mail já cadastrado")
            : TypedResults.Created($"/api/usuarios/{usuario.Id}", usuario);
    }

    private static async Task<Results<Ok<LoginResponse>, ProblemHttpResult>> Login(
        LoginRequest request, IAuthService service)
    {
        var login = await service.LoginAsync(request);
        return login is null
            ? TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "E-mail ou senha inválidos")
            : TypedResults.Ok(login);
    }
}
