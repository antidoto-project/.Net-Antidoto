using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Antidoto.API.Services;

namespace Antidoto.API.Filters;

/// <summary>
/// Idempotência do POST /api/ocorrencias pelo header Idempotency-Key (UUID).
/// Mesma chave + mesmo corpo devolve a resposta salva, sem criar de novo.
/// </summary>
public class IdempotenciaFilter<T> : IEndpointFilter where T : class
{
    public const string Header = "Idempotency-Key";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IIdempotenciaService _service;

    public IdempotenciaFilter(IIdempotenciaService service)
    {
        _service = service;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;

        if (!Guid.TryParse(http.Request.Headers[Header], out var chave))
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Idempotency-Key obrigatório",
                detail: $"Envie o header {Header} com um UUID.");

        var usuarioId = Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var dto = context.Arguments.OfType<T>().First();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dto, Json))));

        var (situacao, registro) = await _service.IniciarAsync(chave, usuarioId, hash);

        switch (situacao)
        {
            case SituacaoChave.Repetida:
                http.Response.Headers["Idempotency-Replayed"] = "true";
                return Results.Text(registro!.RespostaJson, "application/json", Encoding.UTF8, registro.StatusCode);

            case SituacaoChave.CorpoDiferente:
                return TypedResults.Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "Idempotency-Key já usada com outro corpo");

            case SituacaoChave.EmProcessamento:
                return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict,
                    title: "Requisição com esta Idempotency-Key ainda em processamento");
        }

        try
        {
            var resultado = await next(context);

            // Results<A, B> embrulha o resultado real
            var real = resultado is INestedHttpResult nested ? nested.Result : resultado;
            if (real is IStatusCodeHttpResult { StatusCode: >= 200 and < 300 } ok && real is IValueHttpResult valor)
                await _service.ConcluirAsync(registro!, ok.StatusCode!.Value, JsonSerializer.Serialize(valor.Value, Json));
            else
                await _service.DescartarAsync(registro!);

            return resultado;
        }
        catch
        {
            await _service.DescartarAsync(registro!);
            throw;
        }
    }
}
