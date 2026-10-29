// Zweck:    Tests der Neupunktberechnung im lokalen System (Standpunkt 0/0/0,
//           Orientierung 0 gon) des DXF-Viewers.
// Hardware: keine
// Autor:    Johann Gerner

using Feldbuch;
using Xunit;

namespace Feldbuch.Tests;

public class LokaleStationTests
{
    private const double TOLERANZ_M   = 1e-9;
    private const double ZENIT_HORIZ  = 100.0;
    private const double STRECKE_M    = 10.0;

    private static StationierungsErgebnis LokaleStation() => new()
    {
        R = 0, H = 0, Hoehe = 0, Orientierung_gon = 0, Massstab = 1,
        Konvergiert = true, Berechnung3D = true, IstLokalesSystem = true
    };

    private static NeupunktRohdaten Rohdaten(double hz, double v, double s, double zh = 0) => new()
    {
        PunktNr = "1", Hz_gon = hz, V_gon = v, Strecke_m = s, Zielhoehe_m = zh
    };

    [Theory]
    [InlineData(0.0,   0.0,  STRECKE_M)]    // Norden
    [InlineData(100.0, STRECKE_M, 0.0)]     // Osten
    [InlineData(200.0, 0.0, -STRECKE_M)]    // Süden
    [InlineData(300.0, -STRECKE_M, 0.0)]    // Westen
    public void Himmelsrichtungen_ErgebenErwarteteKoordinaten(double hz, double r, double h)
    {
        var erg = NeupunktRechner.Berechnen(Rohdaten(hz, ZENIT_HORIZ, STRECKE_M), LokaleStation());

        Assert.NotNull(erg);
        Assert.Equal(r, erg!.R, TOLERANZ_M);
        Assert.Equal(h, erg.H, TOLERANZ_M);
    }

    [Fact]
    public void LokalesSystem_LiefertHoehe_AuchBeiStandpunktHoeheNull()
    {
        var erg = NeupunktRechner.Berechnen(Rohdaten(0, ZENIT_HORIZ, STRECKE_M), LokaleStation());

        Assert.NotNull(erg);
        Assert.True(erg!.Ist3D);
    }

    [Fact]
    public void OhneLokalesSystem_BleibtHoeheNullBeiStandpunktHoeheNull_Zweidimensional()
    {
        var station = LokaleStation();
        station.IstLokalesSystem = false;

        var erg = NeupunktRechner.Berechnen(Rohdaten(0, ZENIT_HORIZ, STRECKE_M), station);

        Assert.NotNull(erg);
        Assert.False(erg!.Ist3D);
    }

    [Fact]
    public void OhneStrecke_LiefertNull()
    {
        Assert.Null(NeupunktRechner.Berechnen(Rohdaten(0, ZENIT_HORIZ, 0), LokaleStation()));
    }
}
