using Antidoto.API.Data;
using Antidoto.API.DTOs;
using Antidoto.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Antidoto.API.Services;

public interface IAuthService
{
    /// <summary>Cria um cidadão. Retorna null se o e-mail já existe.</summary>
    Task<UsuarioResponse?> RegistrarAsync(RegistrarRequest request);

    /// <summary>Retorna null se e-mail ou senha estiverem errados.</summary>
    Task<LoginResponse?> LoginAsync(LoginRequest request);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, ITokenService tokenService, ILogger<AuthService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<UsuarioResponse?> RegistrarAsync(RegistrarRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Usuarios.AnyAsync(u => u.Email == email))
            return null;

        // O registro público só cria cidadão; o gestor vem do seed
        var usuario = new Usuario
        {
            Nome = request.Nome.Trim(),
            Email = email,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha),
            Role = Roles.Cidadao
        };

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Usuário {UsuarioId} registrado", usuario.Id);
        return new UsuarioResponse(usuario.Id, usuario.Nome, usuario.Email, usuario.Role);
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(request.Senha, usuario.SenhaHash))
        {
            _logger.LogWarning("Login inválido para {Email}", email);
            return null;
        }

        var (token, expiraEm) = _tokenService.GerarToken(usuario);
        return new LoginResponse(token, expiraEm, usuario.Role);
    }
}
