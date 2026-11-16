namespace Antidoto.API.Models;

/// <summary>
/// Guarda o resultado do POST /api/ocorrencias para cada Idempotency-Key.
/// Vale por usuário e por 24 horas.
/// </summary>
public class ChaveIdempotencia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid Chave { get; set; }
    public Guid UsuarioId { get; set; }
    public string HashCorpo { get; set; } = string.Empty;
    public bool EmProcessamento { get; set; } = true;
    public int? StatusCode { get; set; }
    public string? RespostaJson { get; set; }
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    public DateTime ExpiraEm { get; set; } = DateTime.UtcNow.AddHours(24);
}
