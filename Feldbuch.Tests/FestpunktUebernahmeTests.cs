// Zweck:    Tests der Übernahme gemessener Neupunkte als Festpunkte (Feldbuchpunkte.json).
// Hardware: keine
// Autor:    Johann Gerner

using Feldbuch;
using Xunit;

namespace Feldbuch.Tests;

[Collection("Konfiguration")]
public class FestpunktUebernahmeTests : IDisposable
{
    private const double R_TEST = 1234.567;
    private const double H_TEST = 2345.678;
    private const double TOL_M  = 1e-9;

    private readonly string _fbPfad = Path.Combine(Path.GetTempPath(), $"feldbuch-fb-{Guid.NewGuid():N}.json");
    private readonly string _npPfad = Path.Combine(Path.GetTempPath(), $"feldbuch-np-{Guid.NewGuid():N}.json");

    public FestpunktUebernahmeTests()
    {
        FeldbuchpunkteManager.Initialize(_fbPfad);
        NeupunkteManager.Initialize(_npPfad);
    }

    public void Dispose()
    {
        File.Delete(_fbPfad);
        File.Delete(_npPfad);
    }

    private static NeupunktErgebnis Np(string nr, double r, double h) => new()
        { PunktNr = nr, R = r, H = h, Hoehe = 10.0, Ist3D = true };

    [Fact]
    public void Uebernehme_LegtFestpunktAnUndSuchbarMachen()
    {
        var erg = FestpunktUebernahme.Uebernehme([Np("N1", R_TEST, H_TEST)]);

        Assert.Equal(1, erg.Neu);
        Assert.Equal(0, erg.Ersetzt);
        var gefunden = AnschlusspunktSuche.Finde("N1");
        Assert.NotNull(gefunden);
        Assert.Equal(R_TEST, gefunden!.Value.R, TOL_M);
        Assert.Equal(H_TEST, gefunden.Value.H, TOL_M);
        Assert.Equal(FestpunktUebernahme.QUELLE_LOKAL, FeldbuchpunkteManager.Punkte[0].Quelle);
    }

    [Fact]
    public void Uebernehme_ErsetztVorhandenenFestpunkt()
    {
        FestpunktUebernahme.Uebernehme([Np("N1", R_TEST, H_TEST)]);
        var erg = FestpunktUebernahme.Uebernehme([Np("N1", R_TEST + 1.0, H_TEST)]);

        Assert.Equal(0, erg.Neu);
        Assert.Equal(1, erg.Ersetzt);
        Assert.Single(FeldbuchpunkteManager.Punkte);
        Assert.Equal(R_TEST + 1.0, FeldbuchpunkteManager.Punkte[0].R, TOL_M);
    }

    [Fact]
    public void Kandidaten_LassenPunkteOhneKoordinatenAus()
    {
        NeupunkteManager.HinzufuegenOderErsetzen(
            new NeupunktRohdaten { PunktNr = "A" }, Np("A", R_TEST, H_TEST));
        NeupunkteManager.HinzufuegenOderErsetzen(
            new NeupunktRohdaten { PunktNr = "B" }, Np("B", 0.0, 0.0));

        var nummern = FestpunktUebernahme.Kandidaten().Select(e => e.PunktNr).ToList();
        Assert.Equal(["A"], nummern);
    }

    [Fact]
    public void Neupunkte_GleicheNummerAufVerschiedenenStandpunkten_BleibenGetrennt()
    {
        void Lege(string sp, double r) => NeupunkteManager.HinzufuegenOderErsetzen(
            new NeupunktRohdaten { PunktNr = "7", StandpunktNr = sp },
            new NeupunktErgebnis { PunktNr = "7", StandpunktNr = sp, R = r, H = 0.0 });

        Lege("SP1", 1.0);
        Lege("SP2", 3.0);
        Lege("SP1", 5.0);                         // ersetzt nur den Punkt von SP1

        Assert.Equal(2, NeupunkteManager.Koordinaten.Count);
        Assert.Equal(2, NeupunkteManager.Rohdaten.Count);
        Assert.Equal(5.0, NeupunkteManager.FindeKoordinate("7", "SP1")!.R, TOL_M);
        Assert.Equal(3.0, NeupunkteManager.FindeKoordinate("7", "SP2")!.R, TOL_M);

        NeupunkteManager.RemoveWhere(r => r.PunktNr == "7" && r.StandpunktNr == "SP1");
        Assert.Single(NeupunkteManager.Koordinaten);
        Assert.NotNull(NeupunkteManager.FindeKoordinate("7", "SP2"));
    }

    [Fact]
    public void Abstand_IstPythagoras()
        => Assert.Equal(5.0, PolarpunktRechner.Abstand(3.0, -4.0), TOL_M);
}
