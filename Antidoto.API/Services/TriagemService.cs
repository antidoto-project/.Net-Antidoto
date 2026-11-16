using System.Diagnostics;
using Antidoto.API.DTOs;
using Antidoto.API.Observabilidade;

namespace Antidoto.API.Services;

public interface ITriagemService
{
    Task<TriagemResponse> TriarAsync(TriagemRequest request);
}

public class TriagemService : ITriagemService
{
    private readonly IIaClient _ia;
    private readonly IUnidadeService _unidades;
    private readonly ILogger<TriagemService> _logger;

    public TriagemService(IIaClient ia, IUnidadeService unidades, ILogger<TriagemService> logger)
    {
        _ia = ia;
        _unidades = unidades;
        _logger = logger;
    }

    public async Task<TriagemResponse> TriarAsync(TriagemRequest request)
    {
        var cronometro = Stopwatch.StartNew();
        using var activity = Telemetria.ActivitySource.StartActivity("Triagem");

        ResultadoTriagem resultado;
        string fonte;
        try
        {
            resultado = await _ia.TriarAsync(request);
            fonte = "ia";
        }
        catch (Exception ex)
        {
            // IA fora do ar ou lenta: decide pelas regras locais
            _logger.LogWarning(ex, "IA indisponível, usando regras locais para {GrupoAnimal}", request.GrupoAnimal);
            resultado = RegrasTriagem.Avaliar(request);
            fonte = "regras";
        }

        var unidades = await UnidadesProximasAsync(resultado.Soro, request);

        activity?.SetTag("triagem.fonte", fonte);
        activity?.SetTag("triagem.urgencia", resultado.Urgencia);
        Telemetria.Triagens.Add(1, new KeyValuePair<string, object?>("fonte", fonte));
        Telemetria.DuracaoTriagem.Record(cronometro.Elapsed.TotalMilliseconds);

        _logger.LogInformation("Triagem {Urgencia} pela fonte {Fonte} com {QtdUnidades} unidades",
            resultado.Urgencia, fonte, unidades.Count);

        return new TriagemResponse(resultado.Urgencia, resultado.Soro, resultado.Orientacao, fonte, unidades);
    }

    private async Task<List<UnidadeResponse>> UnidadesProximasAsync(string? soro, TriagemRequest request)
    {
        if (soro is null) return new List<UnidadeResponse>();

        try
        {
            var unidades = await _unidades.BuscarAsync(soro, request.Latitude, request.Longitude, 100);
            return unidades.Take(3).ToList();
        }
        catch (Exception ex)
        {
            // Mesmo sem banco, a pessoa recebe a urgência e a orientação
            _logger.LogError(ex, "Falha ao buscar unidades com {Soro}", soro);
            return new List<UnidadeResponse>();
        }
    }
}
