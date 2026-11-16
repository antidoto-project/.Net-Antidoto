using Antidoto.API.Data;
using Antidoto.API.DTOs;
using Antidoto.API.Models;
using Antidoto.API.Observabilidade;
using Microsoft.EntityFrameworkCore;

namespace Antidoto.API.Services;

public interface IOcorrenciaService
{
    Task<OcorrenciaResponse> CriarAsync(Guid usuarioId, OcorrenciaRequest request);
    Task<PaginaResponse<OcorrenciaResponse>> ListarDoUsuarioAsync(Guid usuarioId, int pagina, int tamanho);
    Task<OcorrenciaResponse?> ObterAsync(Guid id);
    Task<EstatisticasResponse> EstatisticasAsync();
}

public class OcorrenciaService : IOcorrenciaService
{
    private readonly AppDbContext _db;
    private readonly ILogger<OcorrenciaService> _logger;

    public OcorrenciaService(AppDbContext db, ILogger<OcorrenciaService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<OcorrenciaResponse> CriarAsync(Guid usuarioId, OcorrenciaRequest request)
    {
        var ocorrencia = new Ocorrencia
        {
            UsuarioId = usuarioId,
            Data = request.Data ?? DateTime.UtcNow,
            GrupoAnimal = request.GrupoAnimal.ToUpperInvariant(),
            Urgencia = request.Urgencia.ToUpperInvariant(),
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        _db.Ocorrencias.Add(ocorrencia);
        await _db.SaveChangesAsync();

        Telemetria.OcorrenciasCriadas.Add(1);
        _logger.LogInformation("Ocorrência {OcorrenciaId} criada pelo usuário {UsuarioId}", ocorrencia.Id, usuarioId);
        return ParaResponse(ocorrencia);
    }

    public async Task<PaginaResponse<OcorrenciaResponse>> ListarDoUsuarioAsync(Guid usuarioId, int pagina, int tamanho)
    {
        pagina = Math.Max(pagina, 1);
        tamanho = Math.Clamp(tamanho, 1, 50);

        var query = _db.Ocorrencias.AsNoTracking().Where(o => o.UsuarioId == usuarioId);
        var total = await query.CountAsync();
        var itens = await query
            .OrderByDescending(o => o.Data)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync();

        return new PaginaResponse<OcorrenciaResponse>(itens.Select(ParaResponse).ToList(), pagina, tamanho, total);
    }

    public async Task<OcorrenciaResponse?> ObterAsync(Guid id)
    {
        var ocorrencia = await _db.Ocorrencias.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        return ocorrencia is null ? null : ParaResponse(ocorrencia);
    }

    public async Task<EstatisticasResponse> EstatisticasAsync()
    {
        var ocorrencias = await _db.Ocorrencias.AsNoTracking()
            .Select(o => new { o.GrupoAnimal, o.Data })
            .ToListAsync();

        var porGrupo = ocorrencias
            .GroupBy(o => o.GrupoAnimal)
            .Select(g => new ContagemResponse(g.Key, g.Count()))
            .OrderByDescending(c => c.Quantidade)
            .ToList();

        var porMes = ocorrencias
            .GroupBy(o => o.Data.ToString("yyyy-MM"))
            .Select(g => new ContagemResponse(g.Key, g.Count()))
            .OrderBy(c => c.Chave)
            .ToList();

        return new EstatisticasResponse(ocorrencias.Count, porGrupo, porMes);
    }

    private static OcorrenciaResponse ParaResponse(Ocorrencia o) =>
        new(o.Id, o.UsuarioId, o.Data, o.GrupoAnimal, o.Urgencia, o.Latitude, o.Longitude, o.Status);
}
