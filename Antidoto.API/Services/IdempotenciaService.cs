using Antidoto.API.Data;
using Antidoto.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Antidoto.API.Services;

public enum SituacaoChave
{
    /// <summary>Chave nova (ou expirada): processa a requisição.</summary>
    Nova,
    /// <summary>Mesma chave e mesmo corpo já concluídos: devolve a resposta salva.</summary>
    Repetida,
    /// <summary>Mesma chave com corpo diferente: 422.</summary>
    CorpoDiferente,
    /// <summary>Mesma chave ainda sendo processada: 409.</summary>
    EmProcessamento
}

public interface IIdempotenciaService
{
    Task<(SituacaoChave Situacao, ChaveIdempotencia? Registro)> IniciarAsync(Guid chave, Guid usuarioId, string hashCorpo);
    Task ConcluirAsync(ChaveIdempotencia registro, int statusCode, string respostaJson);
    Task DescartarAsync(ChaveIdempotencia registro);
}

public class IdempotenciaService : IIdempotenciaService
{
    private readonly AppDbContext _db;

    public IdempotenciaService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Regra pura (sem banco), testada nos testes unitários.</summary>
    public static SituacaoChave Avaliar(ChaveIdempotencia? existente, string hashCorpo, DateTime agora)
    {
        if (existente is null || existente.ExpiraEm <= agora) return SituacaoChave.Nova;
        if (existente.HashCorpo != hashCorpo) return SituacaoChave.CorpoDiferente;
        if (existente.EmProcessamento) return SituacaoChave.EmProcessamento;
        return SituacaoChave.Repetida;
    }

    public async Task<(SituacaoChave Situacao, ChaveIdempotencia? Registro)> IniciarAsync(
        Guid chave, Guid usuarioId, string hashCorpo)
    {
        var existente = await _db.ChavesIdempotencia
            .FirstOrDefaultAsync(c => c.Chave == chave && c.UsuarioId == usuarioId);

        var situacao = Avaliar(existente, hashCorpo, DateTime.UtcNow);
        if (situacao != SituacaoChave.Nova)
            return (situacao, existente);

        if (existente is not null) // expirada: começa de novo
            _db.ChavesIdempotencia.Remove(existente);

        var registro = new ChaveIdempotencia { Chave = chave, UsuarioId = usuarioId, HashCorpo = hashCorpo };
        _db.ChavesIdempotencia.Add(registro);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Outra requisição com a mesma chave gravou antes (índice único)
            _db.Entry(registro).State = EntityState.Detached;
            return (SituacaoChave.EmProcessamento, null);
        }

        return (SituacaoChave.Nova, registro);
    }

    public async Task ConcluirAsync(ChaveIdempotencia registro, int statusCode, string respostaJson)
    {
        registro.EmProcessamento = false;
        registro.StatusCode = statusCode;
        registro.RespostaJson = respostaJson;
        await _db.SaveChangesAsync();
    }

    public async Task DescartarAsync(ChaveIdempotencia registro)
    {
        // A requisição falhou: libera a chave para o cliente tentar de novo
        _db.ChavesIdempotencia.Remove(registro);
        await _db.SaveChangesAsync();
    }
}
