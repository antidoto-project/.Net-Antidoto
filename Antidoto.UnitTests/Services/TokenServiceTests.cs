using Antidoto.API.Models;
using Antidoto.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;

namespace Antidoto.UnitTests.Services;

public class TokenServiceTests
{
    [Fact]
    public void GerarToken_Usuario_CarregaSubERole()
    {
        // Arrange
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "Antidoto.API",
            ["Jwt:Audience"] = "Antidoto.Clientes",
            ["Jwt:Key"] = "chave-de-teste-com-mais-de-32-caracteres!!",
            ["Jwt:ExpiraEmMinutos"] = "60"
        }).Build();
        var usuario = new Usuario { Nome = "Ana", Email = "ana@email.com", Role = Roles.Gestor };

        // Act
        var (token, expiraEm) = new TokenService(config).GerarToken(usuario);

        // Assert
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Subject.Should().Be(usuario.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == Roles.Gestor);
        expiraEm.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromMinutes(1));
    }
}
