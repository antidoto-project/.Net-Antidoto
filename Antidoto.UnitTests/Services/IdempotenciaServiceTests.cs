using Antidoto.API.Models;
using Antidoto.API.Services;
using FluentAssertions;

namespace Antidoto.UnitTests.Services;

public class IdempotenciaServiceTests
{
    private static readonly DateTime Agora = new(2026, 11, 12, 12, 0, 0, DateTimeKind.Utc);

    private static ChaveIdempotencia Registro(bool emProcessamento = false, int horasParaExpirar = 24) => new()
    {
        Chave = Guid.NewGuid(),
        UsuarioId = Guid.NewGuid(),
        HashCorpo = "HASH-A",
        EmProcessamento = emProcessamento,
        ExpiraEm = Agora.AddHours(horasParaExpirar)
    };

    [Fact]
    public void Avaliar_ChaveNova_RetornaNova()
    {
        // Act
        var situacao = IdempotenciaService.Avaliar(null, "HASH-A", Agora);

        // Assert
        situacao.Should().Be(SituacaoChave.Nova);
    }

    [Fact]
    public void Avaliar_MesmaChaveMesmoCorpo_RetornaRepetida()
    {
        // Act
        var situacao = IdempotenciaService.Avaliar(Registro(), "HASH-A", Agora);

        // Assert
        situacao.Should().Be(SituacaoChave.Repetida);
    }

    [Fact]
    public void Avaliar_MesmaChaveCorpoDiferente_RetornaCorpoDiferente()
    {
        // Act
        var situacao = IdempotenciaService.Avaliar(Registro(), "HASH-B", Agora);

        // Assert
        situacao.Should().Be(SituacaoChave.CorpoDiferente);
    }

    [Fact]
    public void Avaliar_ChaveAindaProcessando_RetornaEmProcessamento()
    {
        // Act
        var situacao = IdempotenciaService.Avaliar(Registro(emProcessamento: true), "HASH-A", Agora);

        // Assert
        situacao.Should().Be(SituacaoChave.EmProcessamento);
    }

    [Fact]
    public void Avaliar_ChaveExpirada_RetornaNova()
    {
        // Act
        var situacao = IdempotenciaService.Avaliar(Registro(horasParaExpirar: -1), "HASH-B", Agora);

        // Assert
        situacao.Should().Be(SituacaoChave.Nova);
    }
}
