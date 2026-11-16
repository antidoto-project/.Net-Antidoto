using Antidoto.API.Data;
using Antidoto.API.DTOs;
using Antidoto.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Antidoto.API.Services;

public interface IUnidadeService
{
    /// <summary>Unidades com o soro dentro do raio, da mais próxima para a mais distante.</summary>
    Task<List<UnidadeResponse>> BuscarAsync(string soro, double lat, double lng, double raioKm);
    Task<UnidadeResponse?> ObterAsync(Guid id);
    Task<UnidadeResponse> CriarAsync(UnidadeRequest request);
    Task<UnidadeResponse?> AtualizarAsync(Guid id, UnidadeRequest request);
}

public class UnidadeService : IUnidadeService
{
    private readonly AppDbContext _db;

    public UnidadeService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<UnidadeResponse>> BuscarAsync(string soro, double lat, double lng, double raioKm)
    {
        var unidades = await _db.Unidades.AsNoTracking().ToListAsync();

        return unidades
            .Where(u => u.TemSoro(soro))
            .Select(u => ParaResponse(u, Math.Round(Geo.DistanciaKm(lat, lng, u.Latitude, u.Longitude), 1)))
            .Where(u => u.DistanciaKm <= raioKm)
            .OrderBy(u => u.DistanciaKm)
            .ToList();
    }

    public async Task<UnidadeResponse?> ObterAsync(Guid id)
    {
        var unidade = await _db.Unidades.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        return unidade is null ? null : ParaResponse(unidade, null);
    }

    public async Task<UnidadeResponse> CriarAsync(UnidadeRequest request)
    {
        var unidade = new Unidade();
        Preencher(unidade, request);

        _db.Unidades.Add(unidade);
        await _db.SaveChangesAsync();
        return ParaResponse(unidade, null);
    }

    public async Task<UnidadeResponse?> AtualizarAsync(Guid id, UnidadeRequest request)
    {
        var unidade = await _db.Unidades.FirstOrDefaultAsync(u => u.Id == id);
        if (unidade is null) return null;

        Preencher(unidade, request);
        await _db.SaveChangesAsync();
        return ParaResponse(unidade, null);
    }

    private static void Preencher(Unidade unidade, UnidadeRequest request)
    {
        unidade.Nome = request.Nome.Trim();
        unidade.Endereco = request.Endereco.Trim();
        unidade.Telefone = request.Telefone.Trim();
        unidade.Latitude = request.Latitude;
        unidade.Longitude = request.Longitude;
        unidade.Soros = string.Join(';', request.Soros.Select(s => s.ToUpperInvariant()).Distinct());
    }

    private static UnidadeResponse ParaResponse(Unidade u, double? distanciaKm) =>
        new(u.Id, u.Nome, u.Endereco, u.Telefone, u.Latitude, u.Longitude, u.ListaDeSoros(), distanciaKm);
}
