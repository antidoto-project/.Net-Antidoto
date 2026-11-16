namespace Antidoto.API.Models;

public class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.Cidadao;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

public static class Roles
{
    public const string Cidadao = "cidadao";
    public const string Gestor = "gestor";
}
