namespace Antidoto.API.Models;

/// <summary>Valores aceitos pela API para grupo do animal, soro e urgência.</summary>
public static class Catalogo
{
    public static readonly Dictionary<string, string?> SoroPorGrupo = new()
    {
        ["JARARACA"] = "BOTROPICO",
        ["CASCAVEL"] = "CROTALICO",
        ["SURUCUCU"] = "LAQUETICO",
        ["CORAL"] = "ELAPIDICO",
        ["ESCORPIAO"] = "ESCORPIONICO",
        ["ARMADEIRA"] = "ARACNIDICO",
        ["ARANHA_MARROM"] = "LOXOSCELICO",
        ["LAGARTA"] = "LONOMICO",
        ["DESCONHECIDO"] = null
    };

    public static readonly string[] GruposAnimal = SoroPorGrupo.Keys.ToArray();

    public static readonly string[] Soros =
    {
        "BOTROPICO", "CROTALICO", "LAQUETICO", "ELAPIDICO",
        "ESCORPIONICO", "ARACNIDICO", "LOXOSCELICO", "LONOMICO"
    };

    public static readonly string[] Urgencias = { "BAIXA", "MEDIA", "ALTA", "CRITICA" };

    /// <summary>Sintomas que sempre tornam o caso crítico.</summary>
    public static readonly string[] SinaisGraves =
    {
        "FALTA_DE_AR", "VISAO_DUPLA", "PALPEBRA_CAIDA", "URINA_ESCURA",
        "SANGRAMENTO", "DESMAIO", "VOMITOS"
    };
}
