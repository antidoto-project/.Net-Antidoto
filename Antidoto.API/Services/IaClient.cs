using Antidoto.API.DTOs;

namespace Antidoto.API.Services;

/// <summary>Cliente do serviço externo Antídoto IA (Python).</summary>
public interface IIaClient
{
    /// <summary>Lança exceção se a IA estiver fora do ar, lenta ou responder erro.</summary>
    Task<ResultadoTriagem> TriarAsync(TriagemRequest request, CancellationToken ct = default);

    Task<bool> EstaOnlineAsync(CancellationToken ct = default);
}

public class IaClient : IIaClient
{
    private readonly HttpClient _http;

    public IaClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ResultadoTriagem> TriarAsync(TriagemRequest request, CancellationToken ct = default)
    {
        var resposta = await _http.PostAsJsonAsync("/triagem", request, ct);
        resposta.EnsureSuccessStatusCode();

        return await resposta.Content.ReadFromJsonAsync<ResultadoTriagem>(ct)
               ?? throw new InvalidOperationException("Resposta vazia da IA.");
    }

    public async Task<bool> EstaOnlineAsync(CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            var resposta = await _http.GetAsync("/health", cts.Token);
            return resposta.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
