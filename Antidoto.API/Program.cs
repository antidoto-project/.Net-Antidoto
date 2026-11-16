using System.Text;
using Antidoto.API.Configuracoes;
using Antidoto.API.Data;
using Antidoto.API.Endpoints;
using Antidoto.API.Models;
using Antidoto.API.Observabilidade;
using Antidoto.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

// ── LOGS ESTRUTURADOS (Serilog) ───────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Versao", "1.0.0")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/antidoto-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // ── BANCO (Oracle + EF Core) ──────────────────────────────────────────────
    // Usuário e senha ficam no appsettings.json (ConnectionStrings:Oracle) — uso acadêmico
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseOracle(builder.Configuration.GetConnectionString("Oracle")));

    // ── JWT ───────────────────────────────────────────────────────────────────
    var jwt = builder.Configuration.GetSection("Jwt");
    var chave = jwt["Key"];
    if (string.IsNullOrWhiteSpace(chave) || chave.Length < 32)
        throw new InvalidOperationException(
            "Preencha Jwt:Key no appsettings.json com 32 caracteres ou mais.");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt["Issuer"],
                ValidAudience = jwt["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)),
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorizationBuilder()
        .AddPolicy("cidadao", p => p.RequireRole(Roles.Cidadao))
        .AddPolicy("gestor", p => p.RequireRole(Roles.Gestor));

    // ── SERVICES ──────────────────────────────────────────────────────────────
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IUnidadeService, UnidadeService>();
    builder.Services.AddScoped<ITriagemService, TriagemService>();
    builder.Services.AddScoped<IOcorrenciaService, OcorrenciaService>();
    builder.Services.AddScoped<IIdempotenciaService, IdempotenciaService>();

    // Serviço externo de IA (Python) com timeout
    builder.Services.AddHttpClient<IIaClient, IaClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Ia:BaseUrl"]!);
        client.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("Ia:TimeoutSegundos", 15));
    });

    // ── RECURSOS DA API ───────────────────────────────────────────────────────
    builder.Services.AddProblemDetails();
    builder.Services.AddAntidotoRateLimiting(builder.Configuration);
    builder.Services.AddAntidotoHealthChecks();
    builder.Services.AddAntidotoSwagger();

    // ── OPENTELEMETRY (traces e métricas) ─────────────────────────────────────
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(Telemetria.Nome, serviceVersion: "1.0.0"))
            .WithTracing(t => t
                .AddSource(Telemetria.Nome)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddConsoleExporter())
            .WithMetrics(m => m
                .AddMeter(Telemetria.Nome)
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddConsoleExporter());
    }

    var app = builder.Build();

    // ── CRIA AS TABELAS E O SEED AO SUBIR (aplica as migrations pendentes) ────
    if (!app.Environment.IsEnvironment("Testing"))
    {
        try
        {
            using var scope = app.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
            Log.Information("Banco Oracle atualizado");
        }
        catch (Exception ex)
        {
            // A API sobe mesmo assim; o /health/ready mostra o Oracle como Unhealthy
            Log.Error(ex, "Não foi possível conectar no Oracle. Confira ConnectionStrings:Oracle no appsettings.json");
        }
    }

    // ── PIPELINE ──────────────────────────────────────────────────────────────
    app.UseExceptionHandler();   // exceção não tratada → 500 em ProblemDetails
    app.UseStatusCodePages();    // 401/403/404 sem corpo → ProblemDetails
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
    {
        app.UseSwagger();
        app.UseSwaggerUI(o =>
        {
            o.SwaggerEndpoint("/swagger/v1/swagger.json", "Antídoto API v1");
            o.RoutePrefix = string.Empty; // Swagger na raiz
        });
    }

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    // ── ENDPOINTS ─────────────────────────────────────────────────────────────
    app.MapAntidotoHealthChecks();
    app.MapAuthEndpoints();
    app.MapSosEndpoints();
    app.MapUnidadeEndpoints();
    app.MapOcorrenciaEndpoints();
    app.MapEstatisticaEndpoints();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Antídoto API encerrada inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}

// Necessário para o WebApplicationFactory nos testes de integração
public partial class Program { }
