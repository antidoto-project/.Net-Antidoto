using System.Net;
using System.Net.Http.Json;
using Antidoto.API.Data;
using Antidoto.API.DTOs;
using Antidoto.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Antidoto.IntegrationTests;

[Collection("Api")]
public class OcorrenciaEndpointsTests
{
    private readonly AntidotoApiFactory _factory;

    public OcorrenciaEndpointsTests(AntidotoApiFactory factory)
    {
        _factory = factory;
    }

    private static readonly object Ocorrencia =
        new { grupoAnimal = "JARARACA", urgencia = "ALTA", latitude = -23.55, longitude = -46.63 };

    private static HttpRequestMessage Post(object corpo, Guid? chave)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/ocorrencias") { Content = JsonContent.Create(corpo) };
        if (chave.HasValue) request.Headers.Add("Idempotency-Key", chave.Value.ToString());
        return request;
    }

    [Fact]
    public async Task CriarOcorrencia_SemToken_RetornaStatus401()
    {
        // Act
        var resposta = await _factory.CreateClient().SendAsync(Post(Ocorrencia, Guid.NewGuid()));

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CriarOcorrencia_SemIdempotencyKey_RetornaStatus400()
    {
        // Act
        var resposta = await _factory.CriarCliente("cidadao").SendAsync(Post(Ocorrencia, null));

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CriarOcorrencia_MesmaChaveDuasVezes_RetornaMesmoIdECriaUmaVez()
    {
        // Arrange
        var cliente = _factory.CriarCliente("cidadao");
        var chave = Guid.NewGuid();

        // Act
        var primeira = await cliente.SendAsync(Post(Ocorrencia, chave));
        var segunda = await cliente.SendAsync(Post(Ocorrencia, chave));

        // Assert
        primeira.StatusCode.Should().Be(HttpStatusCode.Created);
        segunda.StatusCode.Should().Be(HttpStatusCode.Created);
        segunda.Headers.GetValues("Idempotency-Replayed").Should().Contain("true");

        var id1 = (await primeira.Content.ReadFromJsonAsync<OcorrenciaResponse>())!.Id;
        var id2 = (await segunda.Content.ReadFromJsonAsync<OcorrenciaResponse>())!.Id;
        id2.Should().Be(id1);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Ocorrencias.Count(o => o.Id == id1).Should().Be(1);
    }

    [Fact]
    public async Task CriarOcorrencia_MesmaChaveOutroCorpo_RetornaStatus422()
    {
        // Arrange
        var cliente = _factory.CriarCliente("cidadao");
        var chave = Guid.NewGuid();
        await cliente.SendAsync(Post(Ocorrencia, chave));
        var outroCorpo = new { grupoAnimal = "CASCAVEL", urgencia = "ALTA", latitude = -23.55, longitude = -46.63 };

        // Act
        var resposta = await cliente.SendAsync(Post(outroCorpo, chave));

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ObterOcorrencia_DeOutroUsuario_RetornaStatus403()
    {
        // Arrange
        var dono = _factory.CriarCliente("cidadao");
        var criada = await dono.SendAsync(Post(Ocorrencia, Guid.NewGuid()));
        var id = (await criada.Content.ReadFromJsonAsync<OcorrenciaResponse>())!.Id;

        // Act
        var resposta = await _factory.CriarCliente("cidadao").GetAsync($"/api/ocorrencias/{id}");

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ObterOcorrencia_Gestor_RetornaStatus200()
    {
        // Arrange
        var dono = _factory.CriarCliente("cidadao");
        var criada = await dono.SendAsync(Post(Ocorrencia, Guid.NewGuid()));
        var id = (await criada.Content.ReadFromJsonAsync<OcorrenciaResponse>())!.Id;

        // Act
        var resposta = await _factory.CriarCliente("gestor").GetAsync($"/api/ocorrencias/{id}");

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MinhasOcorrencias_UsuarioComDuas_RetornaSoAsDele()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var cliente = _factory.CriarCliente("cidadao", usuarioId);
        await cliente.SendAsync(Post(Ocorrencia, Guid.NewGuid()));
        await cliente.SendAsync(Post(Ocorrencia, Guid.NewGuid()));

        // Act
        var pagina = await cliente.GetFromJsonAsync<PaginaResponse<OcorrenciaResponse>>("/api/ocorrencias/minhas");

        // Assert
        pagina!.Total.Should().Be(2);
        pagina.Itens.Should().OnlyContain(o => o.UsuarioId == usuarioId);
    }

    [Fact]
    public async Task Estatisticas_Cidadao_RetornaStatus403()
    {
        // Act
        var resposta = await _factory.CriarCliente("cidadao").GetAsync("/api/estatisticas/regiao");

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Estatisticas_Gestor_RetornaStatus200()
    {
        // Act
        var resposta = await _factory.CriarCliente("gestor").GetAsync("/api/estatisticas/regiao");

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
