using System.ComponentModel.DataAnnotations;

namespace Antidoto.API.DTOs;

public class RegistrarRequest
{
    [Required, StringLength(150, MinimumLength = 3)]
    public string Nome { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6)]
    public string Senha { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Senha { get; set; } = string.Empty;
}

public record UsuarioResponse(Guid Id, string Nome, string Email, string Role);

public record LoginResponse(string Token, DateTime ExpiraEm, string Role);
