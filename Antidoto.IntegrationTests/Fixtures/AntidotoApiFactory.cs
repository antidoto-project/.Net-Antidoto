using System.Net.Http.Headers;
using Antidoto.API.Data;
using Antidoto.API.Models;
using Antidoto.API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Antidoto.IntegrationTests.Fixtures;

/// <summary>
/// Sobe a API em memória: troca o Oracle pelo EF Core InMemory e a IA por uma falsa.
/// </summary>
public class AntidotoApiFactory : WebApplicationFactory<Program>
{
    public FakeIaClient Ia { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting entra antes do Program.cs ler a configuração
        builder.UseSetting("Jwt:Key", "chave-de-teste-antidoto-com-mais-de-32-caracteres");
        builder.UseSetting("RateLimiting:Sos", "1000");
        builder.UseSetting("RateLimiting:Auth", "1000");
        builder.UseSetting("RateLimiting:Consulta", "1000");
        builder.UseSetting("RateLimiting:Usuario", "1000");

        builder.ConfigureServices(services =>
        {
            // Remove o Oracle
            var doBanco = services.Where(d => d.ServiceType.FullName != null &&
                (d.ServiceType.FullName.Contains("DbContext") || d.ServiceType.FullName.Contains("EntityFramework")))
                .ToList();
            foreach (var d in doBanco) services.Remove(d);

            // Banco em memória (nome fixo por factory para os dados persistirem entre requisições)
            var nomeBanco = "AntidotoTestes_" + Guid.NewGuid();
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(nomeBanco));

            // IA falsa
            services.RemoveAll<IIaClient>();
            services.AddSingleton<IIaClient>(Ia);

            services.AddLogging(l => l.ClearProviders());
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Cria o banco com o seed (gestor + unidades)
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        return host;
    }

    /// <summary>Cliente já autenticado com um JWT do usuário e da role informados.</summary>
    public HttpClient CriarCliente(string role, Guid? usuarioId = null)
    {
        var client = CreateClient();
        var usuario = new Usuario { Id = usuarioId ?? Guid.NewGuid(), Nome = "Teste", Email = "teste@antidoto.com", Role = role };
        var (token, _) = Services.CreateScope().ServiceProvider.GetRequiredService<ITokenService>().GerarToken(usuario);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
