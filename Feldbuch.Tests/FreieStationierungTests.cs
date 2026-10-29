// Zweck:    Referenztests des Rechenkerns Freie Stationierung mit synthetischen,
//           fehlerfreien Beobachtungen aus einer bekannten Station.
// Hardware: keine
// Autor:    Johann Gerner

using Feldbuch;
using Xunit;

namespace Feldbuch.Tests;

public class FreieStationierungTests
{
    private const double GON2RAD          = Math.PI / 200.0;
    private const double STATION_R        = 4_500_100.000;
    private const double STATION_H        = 5_400_200.000;
    private const double ORIENTIERUNG_GON = 37.5;
    private const double ZENIT_GON        = 100.0;
    private const double TOLERANZ_M       = 1e-6;
    private const double TOLERANZ_S0_MM   = 0.01;
    private const double TOLERANZ_GON     = 1e-6;

    // Erzeugt fehlerfreie Beobachtungen (Hz, V, Strecke) zu einem Festpunkt.
    private static StationierungsPunkt Beobachte(string nr, double r, double h)
    {
        double dR = r - STATION_R;
        double dH = h - STATION_H;
        double azimutGon = Math.Atan2(dR, dH) / GON2RAD;
        double hz = ((azimutGon - ORIENTIERUNG_GON) % 400.0 + 400.0) % 400.0;
        return new StationierungsPunkt
        {
            PunktNr = nr, R = r, H = h,
            HZ = hz, V = ZENIT_GON,
            Strecke = Math.Sqrt(dR * dR + dH * dH)
        };
    }

    [Fact]
    public void DreiPunkte_FehlerfreieDaten_LiefertStationUndNullResiduen()
    {
        var punkte = new List<StationierungsPunkt>
        {
            Beobachte("A", STATION_R + 40.0, STATION_H + 25.0),
            Beobachte("B", STATION_R - 30.0, STATION_H + 55.0),
            Beobachte("C", STATION_R + 10.0, STATION_H - 45.0),
            Beobachte("D", STATION_R - 50.0, STATION_H - 20.0),
        };

        var erg = FreieStationierungRechner.Berechnen(
            punkte, 1.5, freierMassstab: false, berechnung3D: false);

        Assert.True(erg.Konvergiert);
        Assert.Equal(STATION_R, erg.R, TOLERANZ_M);
        Assert.Equal(STATION_H, erg.H, TOLERANZ_M);
        Assert.Equal(ORIENTIERUNG_GON, erg.Orientierung_gon, TOLERANZ_GON);
        Assert.True(erg.s0_mm < TOLERANZ_S0_MM);
        Assert.Equal(punkte.Count * 2 - 3, erg.Redundanz);
    }

    [Fact]
    public void FreierMassstab_FehlerfreieDaten_LiefertMassstabEins()
    {
        var punkte = new List<StationierungsPunkt>
        {
            Beobachte("A", STATION_R + 40.0, STATION_H + 25.0),
            Beobachte("B", STATION_R - 30.0, STATION_H + 55.0),
            Beobachte("C", STATION_R + 10.0, STATION_H - 45.0),
        };

        var erg = FreieStationierungRechner.Berechnen(
            punkte, 1.5, freierMassstab: true, berechnung3D: false);

        Assert.Equal(1.0, erg.Massstab, 1e-8);
        Assert.Equal(STATION_R, erg.R, TOLERANZ_M);
        Assert.Equal(STATION_H, erg.H, TOLERANZ_M);
    }

    [Fact]
    public void ZweiPunkte_Direktloesung_HatRedundanzNull()
    {
        var punkte = new List<StationierungsPunkt>
        {
            Beobachte("A", STATION_R + 40.0, STATION_H + 25.0),
            Beobachte("B", STATION_R - 30.0, STATION_H + 55.0),
        };

        var erg = FreieStationierungRechner.Berechnen(
            punkte, 1.5, freierMassstab: false, berechnung3D: false);

        Assert.Equal(0, erg.Redundanz);
        Assert.Equal(STATION_R, erg.R, TOLERANZ_M);
        Assert.Equal(STATION_H, erg.H, TOLERANZ_M);
    }

    [Fact]
    public void GrobfehlerInStrecke_ErhoehtS0()
    {
        var punkte = new List<StationierungsPunkt>
        {
            Beobachte("A", STATION_R + 40.0, STATION_H + 25.0),
            Beobachte("B", STATION_R - 30.0, STATION_H + 55.0),
            Beobachte("C", STATION_R + 10.0, STATION_H - 45.0),
            Beobachte("D", STATION_R - 50.0, STATION_H - 20.0),
        };
        punkte[0].Strecke += 0.050;

        var erg = FreieStationierungRechner.Berechnen(
            punkte, 1.5, freierMassstab: false, berechnung3D: false);

        Assert.True(erg.s0_mm > 1.0);
    }

    [Fact]
    public void ZuWenigPunkte_WirftException()
    {
        var punkte = new List<StationierungsPunkt>
        {
            Beobachte("A", STATION_R + 40.0, STATION_H + 25.0),
        };

        Assert.Throws<InvalidOperationException>(
            () => FreieStationierungRechner.Berechnen(punkte, 1.5));
    }

    [Fact]
    public void OhneAktiveRichtung_WirftException()
    {
        var punkte = new List<StationierungsPunkt>
        {
            Beobachte("A", STATION_R + 40.0, STATION_H + 25.0),
            Beobachte("B", STATION_R - 30.0, STATION_H + 55.0),
            Beobachte("C", STATION_R + 10.0, STATION_H - 45.0),
        };

        Assert.Throws<InvalidOperationException>(() =>
            FreieStationierungRechner.Berechnen(
                punkte, 1.5, aktivRichtung: new[] { false, false, false }));
    }
}
