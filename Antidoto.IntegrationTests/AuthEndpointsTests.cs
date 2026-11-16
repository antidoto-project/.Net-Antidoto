using System.Net;
using System.Net.Http.Json;
using Antidoto.API.Data;
using Antidoto.API.DTOs;
using Antidoto.IntegrationTests.Fixtures;
using FluentAssertions;

namespace Antidoto.IntegrationTests;

[Collection("Api")]
public class AuthEndpointsTests
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(AntidotoApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Registrar_DadosValidos_RetornaStatus201ComRoleCidadao()
    {
        // Arrange
        var pedido = new { nome = "Maria Silva", email = $"{Guid.NewGuid()}@email.com", senha = "senha123" };

        // Act
        var resposta = await _client.PostAsJsonAsync("/api/auth/registrar", pedido);

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        var usuario = await resposta.Content.ReadFromJsonAsync<UsuarioResponse>();
        usuario!.Role.Should().Be("cidadao");
    }

    [Fact]
    public async Task Registrar_EmailRepetido_RetornaStatus409()
    {
        // Arrange
        var pedido = new { nome = "João", email = $"{Guid.NewGuid()}@email.com", senha = "senha123" };
        await _client.PostAsJsonAsync("/api/auth/registrar", pedido);

        // Act
        var resposta = await _client.PostAsJsonAsync("/api/auth/registrar", pedido);

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Registrar_DadosInvalidos_RetornaStatus400ComOsCampos()
    {
        // Arrange
        var pedido = new { nome = "A", email = "nao-e-email", senha = "1" };

        // Act
        var resposta = await _client.PostAsJsonAsync("/api/auth/registrar", pedido);

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await resposta.Content.ReadAsStringAsync();
        corpo.Should().Contain("Nome").And.Contain("Email").And.Contain("Senha");
    }

    [Fact]
    public async Task Login_GestorDoSeed_RetornaStatus200ComToken()
    {
        // Act
        var resposta = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = AppDbContext.GestorEmail, senha = AppDbContext.GestorSenha });

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = await resposta.Content.ReadFromJsonAsync<LoginResponse>();
        login!.Token.Should().NotBeNullOrEmpty();
        login.Role.Should().Be("gestor");
    }

    [Fact]
    public async Task Login_SenhaErrada_RetornaStatus401()
    {
        // Act
        var resposta = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = AppDbContext.GestorEmail, senha = "errada" });

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_JsonMalFormado_RetornaStatus400()
    {
        // Arrange
        var corpo = new StringContent("{ \"email\": \"a@a.com\", \"senha\": \"123\" }\"", System.Text.Encoding.UTF8, "application/json");

        // Act
        var resposta = await _client.PostAsync("/api/auth/login", corpo);

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
