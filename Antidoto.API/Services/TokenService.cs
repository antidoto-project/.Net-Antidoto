using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Antidoto.API.Models;
using Microsoft.IdentityModel.Tokens;

namespace Antidoto.API.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiraEm) GerarToken(Usuario usuario);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiraEm) GerarToken(Usuario usuario)
    {
        var jwt = _configuration.GetSection("Jwt");
        var expiraEm = DateTime.UtcNow.AddMinutes(jwt.GetValue("ExpiraEmMinutos", 60));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim(JwtRegisteredClaimNames.Name, usuario.Nome),
            new Claim("role", usuario.Role)
        };

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expiraEm,
            signingCredentials: new SigningCredentials(chave, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEm);
    }
}
