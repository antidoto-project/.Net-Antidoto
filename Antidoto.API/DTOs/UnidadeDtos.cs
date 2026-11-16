using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Antidoto.API.DTOs;

/// <summary>Filtros da busca GET /api/unidades (vêm da query string).</summary>
public class UnidadeBuscaRequest
{
    [FromQuery(Name = "soro"), Required, Soro]
    public string? Soro { get; set; }

    [FromQuery(Name = "lat"), Range(-90, 90)]
    public double Lat { get; set; }

    [FromQuery(Name = "lng"), Range(-180, 180)]
    public double Lng { get; set; }

    [FromQuery(Name = "raioKm"), Range(1, 100)]
    public double RaioKm { get; set; } = 50;
}

public class UnidadeRequest
{
    [Required, StringLength(200, MinimumLength = 3)]
    public string Nome { get; set; } = string.Empty;

    [Required, StringLength(300)]
    public string Endereco { get; set; } = string.Empty;

    [StringLength(20)]
    public string Telefone { get; set; } = string.Empty;

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    [Required, MinLength(1), Soro]
    public List<string> Soros { get; set; } = new();
}

public record UnidadeResponse(
    Guid Id, string Nome, string Endereco, string Telefone,
    double Latitude, double Longitude, List<string> Soros, double? DistanciaKm);
