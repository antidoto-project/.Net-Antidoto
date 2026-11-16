using Antidoto.API.DTOs;
using Antidoto.API.Models;

namespace Antidoto.API.Services;

/// <summary>Regras locais usadas quando a IA não responde. O SOS nunca fica sem resposta.</summary>
public static class RegrasTriagem
{
    private const string OrientacaoBase =
        "Lave o local com água e sabão, mantenha a pessoa calma e deitada. " +
        "Não faça torniquete, não corte, não chupe o local e não passe nada na picada. " +
        "Vá agora para a unidade indicada.";

    public static ResultadoTriagem Avaliar(TriagemRequest request)
    {
        var grupo = request.GrupoAnimal.ToUpperInvariant();
        var soro = Catalogo.SoroPorGrupo.GetValueOrDefault(grupo);

        var temSinalGrave = request.Sintomas.Any(s =>
            Catalogo.SinaisGraves.Contains(s, StringComparer.OrdinalIgnoreCase));

        var urgencia = temSinalGrave ? "CRITICA" : grupo switch
        {
            "JARARACA" or "CASCAVEL" or "SURUCUCU" or "CORAL" or "DESCONHECIDO" => "ALTA",
            "ARMADEIRA" or "ARANHA_MARROM" or "LAGARTA" or "ESCORPIAO" => "MEDIA",
            _ => "ALTA"
        };

        var orientacao = urgencia == "CRITICA"
            ? "Ligue 192 (SAMU) agora. " + OrientacaoBase
            : OrientacaoBase;

        return new ResultadoTriagem(urgencia, soro, orientacao);
    }
}
