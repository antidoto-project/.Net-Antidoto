using System.Net;
using System.Net.Http.Json;
using Antidoto.API.DTOs;
using Antidoto.IntegrationTests.Fixtures;
using FluentAssertions;

namespace Antidoto.IntegrationTests;

[Collection("Api")]
public class SosEUnidadeEndpointsTests
{
    private readonly AntidotoApiFactory _factory;
    private readonly HttpClient _client;

    public SosEUnidadeEndpointsTests(AntidotoApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static object PedidoSp(string grupo) =>
        new { grupoAnimal = grupo, sintomas = new[] { "DOR_LOCAL" }, latitude = -23.55, longitude = -46.63 };

    [Fact]
    public async Task Triagem_IaForaDoAr_RetornaStatus200ComFonteRegras()
    {
        // Arrange
        _factory.Ia.Online = false;

        // Act
        var resposta = await _client.PostAsJsonAsync("/api/sos/triagem", PedidoSp("CASCAVEL"));

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var triagem = await resposta.Content.ReadFromJsonAsync<TriagemResponse>();
        triagem!.Fonte.Should().Be("regras");
        triagem.Soro.Should().Be("CROTALICO");
        triagem.Unidades.Should().NotBeEmpty().And.HaveCountLessThanOrEqualTo(3);
    }

    [Fact]
    public async Task Triagem_GrupoInvalido_RetornaStatus400()
    {
        // Act
        var resposta = await _client.PostAsJsonAsync("/api/sos/triagem", PedidoSp("DRAGAO"));

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BuscarUnidades_SoroERaioValidos_RetornaOrdenadasPorDistancia()
    {
        // Act
        var unidades = await _client.GetFromJsonAsync<List<UnidadeResponse>>(
            "/api/unidades?soro=BOTROPICO&lat=-23.55&lng=-46.63&raioKm=100");

        // Assert
        unidades.Should().NotBeEmpty();
        unidades!.Should().OnlyContain(u => u.Soros.Contains("BOTROPICO") && u.DistanciaKm <= 100);
        unidades.Select(u => u.DistanciaKm).Should().BeInAscendingOrder();
    }

    [Theory]
    [InlineData("/api/unidades?soro=BOTROPICO&lat=-23.55&lng=-46.63&raioKm=500")]
    [InlineData("/api/unidades?soro=BOTROPICO&lat=95&lng=-46.63")]
    [InlineData("/api/unidades?soro=XPTO&lat=-23.55&lng=-46.63")]
    public async Task BuscarUnidades_ParametroInvalido_RetornaStatus400(string url)
    {
        // Act
        var resposta = await _client.GetAsync(url);

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ObterUnidade_IdInexistente_RetornaStatus404()
    {
        // Act
        var resposta = await _client.GetAsync($"/api/unidades/{Guid.NewGuid()}");

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CriarUnidade_Cidadao_RetornaStatus403()
    {
        // Arrange
        var cliente = _factory.CriarCliente("cidadao");
        var unidade = new { nome = "UBS Teste", endereco = "Rua A, 1", telefone = "", latitude = -23.5, longitude = -46.6, soros = new[] { "BOTROPICO" } };

        // Act
        var resposta = await cliente.PostAsJsonAsync("/api/unidades", unidade);

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CriarUnidade_Gestor_RetornaStatus201()
    {
        // Arrange
        var cliente = _factory.CriarCliente("gestor");
        var unidade = new { nome = "UBS Teste", endereco = "Rua A, 1", telefone = "", latitude = -23.5, longitude = -46.6, soros = new[] { "BOTROPICO" } };

        // Act
        var resposta = await cliente.PostAsJsonAsync("/api/unidades", unidade);

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
