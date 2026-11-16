using System.Net;
using Antidoto.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;

namespace Antidoto.IntegrationTests;

[Collection("Api")]
public class InfraEndpointsTests
{
    private readonly AntidotoApiFactory _factory;

    public InfraEndpointsTests(AntidotoApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/startup")]
    [InlineData("/swagger/v1/swagger.json")]
    public async Task Get_EndpointDeInfra_RetornaStatus200(string url)
    {
        // Act
        var resposta = await _factory.CreateClient().GetAsync(url);

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_IaForaDoAr_RetornaDegraded()
    {
        // Arrange
        _factory.Ia.Online = false;

        // Act
        var corpo = await _factory.CreateClient().GetStringAsync("/health");

        // Assert
        corpo.Should().Contain("\"status\":\"Degraded\"");
    }

    [Fact]
    public async Task Login_PassandoDoLimite_RetornaStatus429ComRetryAfter()
    {
        // Arrange: API separada com limite de 2 logins por minuto
        await using var factory = _factory.WithWebHostBuilder(b => b.UseSetting("RateLimiting:Auth", "2"));
        var client = factory.CreateClient();
        var login = new { email = "x@x.com", senha = "qualquer" };

        // Act
        HttpResponseMessage resposta = null!;
        for (var i = 0; i < 3; i++)
            resposta = await client.PostAsync("/api/auth/login", System.Net.Http.Json.JsonContent.Create(login));

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        resposta.Headers.Contains("Retry-After").Should().BeTrue();
    }
}
