using Antidoto.API.DTOs;
using Antidoto.API.Services;

namespace Antidoto.IntegrationTests.Fixtures;

/// <summary>IA falsa: cada teste liga ou desliga com <see cref="Online"/>.</summary>
public class FakeIaClient : IIaClient
{
    public bool Online { get; set; }

    public Task<ResultadoTriagem> TriarAsync(TriagemRequest request, CancellationToken ct = default) =>
        Online
            ? Task.FromResult(new ResultadoTriagem("ALTA", "BOTROPICO", "Resposta da IA falsa"))
            : throw new HttpRequestException("IA fora do ar");

    public Task<bool> EstaOnlineAsync(CancellationToken ct = default) => Task.FromResult(Online);
}
