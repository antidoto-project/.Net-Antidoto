using System.ComponentModel.DataAnnotations;

namespace Antidoto.API.Filters;

/// <summary>
/// Minimal API não valida DataAnnotations sozinha no .NET 9.
/// Este filtro valida o DTO do tipo T e devolve 400 com os campos do erro.
/// </summary>
public class ValidacaoFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var dto = context.Arguments.OfType<T>().FirstOrDefault();
        if (dto is null)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["body"] = new[] { "Corpo da requisição ausente ou inválido." }
            });

        var erros = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, new ValidationContext(dto), erros, validateAllProperties: true))
        {
            var porCampo = erros
                .SelectMany(e => e.MemberNames.DefaultIfEmpty(""), (e, campo) => new { campo, e.ErrorMessage })
                .GroupBy(x => x.campo)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage ?? "Inválido").ToArray());

            return TypedResults.ValidationProblem(porCampo);
        }

        return await next(context);
    }
}
