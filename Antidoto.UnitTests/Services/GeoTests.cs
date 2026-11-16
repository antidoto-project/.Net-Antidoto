using Antidoto.API.Services;
using FluentAssertions;

namespace Antidoto.UnitTests.Services;

public class GeoTests
{
    [Fact]
    public void DistanciaKm_MesmoPonto_RetornaZero()
    {
        // Act
        var distancia = Geo.DistanciaKm(-23.55, -46.63, -23.55, -46.63);

        // Assert
        distancia.Should().Be(0);
    }

    [Fact]
    public void DistanciaKm_SaoPauloECampinas_RetornaCercaDe85Km()
    {
        // Arrange: Praça da Sé e centro de Campinas
        var (latSp, lngSp) = (-23.5503, -46.6339);
        var (latCps, lngCps) = (-22.9056, -47.0608);

        // Act
        var distancia = Geo.DistanciaKm(latSp, lngSp, latCps, lngCps);

        // Assert
        distancia.Should().BeApproximately(84, 3);
    }
}
