using Antidoto.API.DTOs;
using Antidoto.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Antidoto.UnitTests.Services;

public class TriagemServiceTests
{
    private readonly Mock<IIaClient> _ia = new();
    private readonly Mock<IUnidadeService> _unidades = new();
    private readonly TriagemService _service;

    public TriagemServiceTests()
    {
        _unidades
            .Setup(u => u.BuscarAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .ReturnsAsync(Enumerable.Range(1, 5)
                .Select(i => new UnidadeResponse(Guid.NewGuid(), $"Unidade {i}", "", "", 0, 0, new(), i))
                .ToList());

        _service = new TriagemService(_ia.Object, _unidades.Object, NullLogger<TriagemService>.Instance);
    }

    private static TriagemRequest Pedido(string grupo, params string[] sintomas) =>
        new() { GrupoAnimal = grupo, Sintomas = sintomas.ToList(), Latitude = -23.55, Longitude = -46.63 };

    [Fact]
    public async Task TriarAsync_IaRespondendo_RetornaResultadoDaIaComFonteIa()
    {
        // Arrange
        _ia.Setup(i => i.TriarAsync(It.IsAny<TriagemRequest>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new ResultadoTriagem("ALTA", "CROTALICO", "Orientação da IA"));

        // Act
        var resposta = await _service.TriarAsync(Pedido("CASCAVEL"));

        // Assert
        resposta.Fonte.Should().Be("ia");
        resposta.Soro.Should().Be("CROTALICO");
        resposta.Orientacao.Should().Be("Orientação da IA");
        _ia.Verify(i => i.TriarAsync(It.IsAny<TriagemRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TriarAsync_IaForaDoAr_UsaRegrasLocaisComFonteRegras()
    {
        // Arrange
        _ia.Setup(i => i.TriarAsync(It.IsAny<TriagemRequest>(), It.IsAny<CancellationToken>()))
           .ThrowsAsync(new HttpRequestException("IA fora do ar"));

        // Act
        var resposta = await _service.TriarAsync(Pedido("JARARACA"));

        // Assert
        resposta.Fonte.Should().Be("regras");
        resposta.Soro.Should().Be("BOTROPICO");
        resposta.Urgencia.Should().Be("ALTA");
    }

    [Fact]
    public async Task TriarAsync_ComSoro_RetornaNoMaximo3Unidades()
    {
        // Arrange
        _ia.Setup(i => i.TriarAsync(It.IsAny<TriagemRequest>(), It.IsAny<CancellationToken>()))
           .ThrowsAsync(new TaskCanceledException());

        // Act
        var resposta = await _service.TriarAsync(Pedido("ESCORPIAO"));

        // Assert
        resposta.Unidades.Should().HaveCount(3);
        _unidades.Verify(u => u.BuscarAsync("ESCORPIONICO", -23.55, -46.63, 100), Times.Once);
    }

    [Fact]
    public async Task TriarAsync_AnimalDesconhecido_NaoBuscaUnidades()
    {
        // Arrange
        _ia.Setup(i => i.TriarAsync(It.IsAny<TriagemRequest>(), It.IsAny<CancellationToken>()))
           .ThrowsAsync(new HttpRequestException());

        // Act
        var resposta = await _service.TriarAsync(Pedido("DESCONHECIDO"));

        // Assert
        resposta.Soro.Should().BeNull();
        resposta.Unidades.Should().BeEmpty();
        _unidades.Verify(u => u.BuscarAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()),
            Times.Never);
    }

    [Fact]
    public async Task TriarAsync_BancoForaDoAr_RetornaOrientacaoSemUnidades()
    {
        // Arrange
        _ia.Setup(i => i.TriarAsync(It.IsAny<TriagemRequest>(), It.IsAny<CancellationToken>()))
           .ThrowsAsync(new HttpRequestException());
        _unidades
            .Setup(u => u.BuscarAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .ThrowsAsync(new InvalidOperationException("Oracle fora do ar"));

        // Act
        var resposta = await _service.TriarAsync(Pedido("CORAL", "VISAO_DUPLA"));

        // Assert
        resposta.Urgencia.Should().Be("CRITICA");
        resposta.Orientacao.Should().NotBeNullOrEmpty();
        resposta.Unidades.Should().BeEmpty();
    }
}
