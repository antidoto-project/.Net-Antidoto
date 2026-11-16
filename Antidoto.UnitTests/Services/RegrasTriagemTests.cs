using Antidoto.API.DTOs;
using Antidoto.API.Services;
using FluentAssertions;

namespace Antidoto.UnitTests.Services;

public class RegrasTriagemTests
{
    [Theory]
    [InlineData("JARARACA", "BOTROPICO")]
    [InlineData("CASCAVEL", "CROTALICO")]
    [InlineData("SURUCUCU", "LAQUETICO")]
    [InlineData("CORAL", "ELAPIDICO")]
    [InlineData("ESCORPIAO", "ESCORPIONICO")]
    [InlineData("ARMADEIRA", "ARACNIDICO")]
    [InlineData("ARANHA_MARROM", "LOXOSCELICO")]
    [InlineData("LAGARTA", "LONOMICO")]
    public void Avaliar_GrupoAnimal_IndicaOSoroCerto(string grupo, string soroEsperado)
    {
        // Arrange
        var pedido = new TriagemRequest { GrupoAnimal = grupo };

        // Act
        var resultado = RegrasTriagem.Avaliar(pedido);

        // Assert
        resultado.Soro.Should().Be(soroEsperado);
    }

    [Theory]
    [InlineData("FALTA_DE_AR")]
    [InlineData("visao_dupla")]
    [InlineData("URINA_ESCURA")]
    public void Avaliar_SinalGrave_RetornaUrgenciaCritica(string sintoma)
    {
        // Arrange
        var pedido = new TriagemRequest { GrupoAnimal = "ESCORPIAO", Sintomas = new() { "DOR_LOCAL", sintoma } };

        // Act
        var resultado = RegrasTriagem.Avaliar(pedido);

        // Assert
        resultado.Urgencia.Should().Be("CRITICA");
        resultado.Orientacao.Should().Contain("192");
    }

    [Fact]
    public void Avaliar_EscorpiaoSemSinalGrave_RetornaUrgenciaMedia()
    {
        // Arrange
        var pedido = new TriagemRequest { GrupoAnimal = "ESCORPIAO", Sintomas = new() { "DOR_LOCAL" } };

        // Act
        var resultado = RegrasTriagem.Avaliar(pedido);

        // Assert
        resultado.Urgencia.Should().Be("MEDIA");
    }
}
