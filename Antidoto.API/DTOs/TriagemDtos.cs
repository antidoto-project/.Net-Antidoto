using System.ComponentModel.DataAnnotations;

namespace Antidoto.API.DTOs;

public class TriagemRequest
{
    /// <summary>JARARACA, CASCAVEL, SURUCUCU, CORAL, ESCORPIAO, ARMADEIRA, ARANHA_MARROM, LAGARTA ou DESCONHECIDO.</summary>
    [Required, GrupoAnimal]
    public string GrupoAnimal { get; set; } = string.Empty;

    /// <summary>Ex.: DOR_LOCAL, INCHACO, FALTA_DE_AR, VISAO_DUPLA, URINA_ESCURA, SANGRAMENTO, DESMAIO.</summary>
    public List<string> Sintomas { get; set; } = new();

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }
}

/// <summary>Resposta da IA ou das regras locais.</summary>
public record ResultadoTriagem(string Urgencia, string? Soro, string Orientacao);

public record TriagemResponse(
    string Urgencia, string? Soro, string Orientacao, string Fonte, List<UnidadeResponse> Unidades);
