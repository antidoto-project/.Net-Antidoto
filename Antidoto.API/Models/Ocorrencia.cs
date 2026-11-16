namespace Antidoto.API.Models;

/// <summary>Acidente com animal peçonhento registrado por um cidadão.</summary>
public class Ocorrencia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public DateTime Data { get; set; }
    public string GrupoAnimal { get; set; } = string.Empty;
    public string Urgencia { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Status { get; set; } = "REGISTRADA";
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
