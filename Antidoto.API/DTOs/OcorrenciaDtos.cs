using System.ComponentModel.DataAnnotations;

namespace Antidoto.API.DTOs;

public class OcorrenciaRequest
{
    [Required, GrupoAnimal]
    public string GrupoAnimal { get; set; } = string.Empty;

    [Required, Urgencia]
    public string Urgencia { get; set; } = string.Empty;

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    /// <summary>Data do acidente. Se não vier, usa o momento do registro.</summary>
    public DateTime? Data { get; set; }
}

public record OcorrenciaResponse(
    Guid Id, Guid UsuarioId, DateTime Data, string GrupoAnimal,
    string Urgencia, double Latitude, double Longitude, string Status);

public record PaginaResponse<T>(List<T> Itens, int Pagina, int Tamanho, int Total);

public record ContagemResponse(string Chave, int Quantidade);

public record EstatisticasResponse(int Total, List<ContagemResponse> PorGrupoAnimal, List<ContagemResponse> PorMes);
