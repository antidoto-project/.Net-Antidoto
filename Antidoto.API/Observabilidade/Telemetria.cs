using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Antidoto.API.Observabilidade;

/// <summary>Traces e métricas próprios da API, exportados pelo OpenTelemetry.</summary>
public static class Telemetria
{
    public const string Nome = "Antidoto.API";

    public static readonly ActivitySource ActivitySource = new(Nome);
    private static readonly Meter Meter = new(Nome);

    /// <summary>Counter: total de triagens, com a tag "fonte" (ia ou regras).</summary>
    public static readonly Counter<long> Triagens = Meter.CreateCounter<long>("antidoto_triagens_total");

    /// <summary>Counter: total de ocorrências criadas.</summary>
    public static readonly Counter<long> OcorrenciasCriadas = Meter.CreateCounter<long>("antidoto_ocorrencias_total");

    /// <summary>Histogram: tempo de cada triagem em milissegundos.</summary>
    public static readonly Histogram<double> DuracaoTriagem =
        Meter.CreateHistogram<double>("antidoto_triagem_duracao", unit: "ms");
}
