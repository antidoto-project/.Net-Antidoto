namespace Antidoto.API.Services;

public static class Geo
{
    private const double RaioDaTerraKm = 6371;

    /// <summary>Distância em km entre dois pontos (fórmula de Haversine).</summary>
    public static double DistanciaKm(double lat1, double lng1, double lat2, double lng2)
    {
        var dLat = ParaRadianos(lat2 - lat1);
        var dLng = ParaRadianos(lng2 - lng1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ParaRadianos(lat1)) * Math.Cos(ParaRadianos(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        return RaioDaTerraKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ParaRadianos(double graus) => graus * Math.PI / 180;
}
