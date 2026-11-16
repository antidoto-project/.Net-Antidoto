using System.ComponentModel.DataAnnotations;
using Antidoto.API.Models;

namespace Antidoto.API.DTOs;

/// <summary>Aceita só valores de uma lista do <see cref="Catalogo"/> (ignora maiúsculas/minúsculas).</summary>
public abstract class ValorDaListaAttribute : ValidationAttribute
{
    protected abstract string[] Permitidos { get; }

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is null) return ValidationResult.Success; // [Required] cuida do obrigatório

        var valores = value is IEnumerable<string> lista ? lista : new[] { value.ToString()! };
        var invalidos = valores.Where(v => !Permitidos.Contains(v, StringComparer.OrdinalIgnoreCase)).ToList();

        return invalidos.Count == 0
            ? ValidationResult.Success
            : new ValidationResult(
                $"Valor inválido: {string.Join(", ", invalidos)}. Aceitos: {string.Join(", ", Permitidos)}.",
                new[] { context.MemberName! });
    }
}

public class GrupoAnimalAttribute : ValorDaListaAttribute
{
    protected override string[] Permitidos => Catalogo.GruposAnimal;
}

public class SoroAttribute : ValorDaListaAttribute
{
    protected override string[] Permitidos => Catalogo.Soros;
}

public class UrgenciaAttribute : ValorDaListaAttribute
{
    protected override string[] Permitidos => Catalogo.Urgencias;
}
