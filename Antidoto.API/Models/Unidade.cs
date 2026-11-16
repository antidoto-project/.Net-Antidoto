namespace Antidoto.API.Models;

/// <summary>Unidade de saúde que aplica soro antiveneno.</summary>
public class Unidade
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>Soros disponíveis separados por ";" (ex.: "BOTROPICO;CROTALICO").</summary>
    public string Soros { get; set; } = string.Empty;

    public List<string> ListaDeSoros() =>
        Soros.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

    public bool TemSoro(string soro) =>
        ListaDeSoros().Contains(soro, StringComparer.OrdinalIgnoreCase);
}
