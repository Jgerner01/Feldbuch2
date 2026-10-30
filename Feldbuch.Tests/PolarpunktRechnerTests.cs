// Zweck:    Tests der Polarpunktberechnung: Lage mit Exzentrizitäten und Maßstab, Maßstabsermittlung
//           nach Rechenparameter, Orientierung aus einem Anschlussziel, Gewichtung bei nahen Zielen,
//           Rechenparameter-Datei und Schutz vor Überschreiben durch den DXF-Viewer.
// Hardware: keine
// Autor:    Johann Gerner

using Feldbuch;
using Xunit;

namespace Feldbuch.Tests;

[Collection("Konfiguration")]
public class PolarpunktRechnerTests : IDisposable
{
    private const double TOLERANZ_M  = 1e-9;
    private const double ZENIT_HORIZ = 100.0;
    private const double GON2RAD     = Math.PI / 200.0;
    private const double RAD2CC      = 2_000_000.0 / Math.PI;
    private const double MASSSTAB_UTM = 0.9996;

    public PolarpunktRechnerTests() => ParameterZuruecksetzen();
    public void Dispose()           => ParameterZuruecksetzen();

    private static void ParameterZuruecksetzen()
    {
        string pfad = Path.Combine(Path.GetTempPath(), $"feldbuch-rp-{Guid.NewGuid():N}.xml");
        RechenparameterManager.Initialize(pfad);   // Datei fehlt -> Standardwerte
        File.Delete(pfad);
    }

    private static StationierungsErgebnis Station(double r = 0, double h = 0, double o = 0, double m = 1) => new()
    {
        R = r, H = h, Hoehe = 0, Orientierung_gon = o, Massstab = m,
        Konvergiert = true, Berechnung3D = true, IstLokalesSystem = true
    };

    // ── Lage / Exzentrizität ──────────────────────────────────────────────────

    [Theory]
    [InlineData(0.0,    0.0, 50.0,  0.0)]    // ohne Exzentrizität
    [InlineData(1.0,    0.0, 51.0,  0.0)]    // weiter weg
    [InlineData(-1.0,   0.0, 49.0,  0.0)]    // näher
    [InlineData(0.0,    1.0, 50.0, -1.0)]    // rechts von Ost = Süd
    [InlineData(0.0,   -1.0, 50.0,  1.0)]    // links von Ost = Nord
    public void Lage_Richtung_Ost_Exzentrizitaet(double eL, double eQ, double r, double h)
    {
        var (rr, hh) = PolarpunktRechner.Lage(0, 0, 100.0, 50.0, 1.0, eL, eQ);
        Assert.Equal(r, rr, TOLERANZ_M);
        Assert.Equal(h, hh, TOLERANZ_M);
    }

    [Fact]
    public void Lage_Richtung_Nord_QuerRechts_IstOst()
    {
        var (r, h) = PolarpunktRechner.Lage(10, 20, 0.0, 30.0, 1.0, 0.0, 2.0);
        Assert.Equal(12.0, r, TOLERANZ_M);
        Assert.Equal(50.0, h, TOLERANZ_M);
    }

    [Fact]
    public void Lage_Massstab_WirktNurAufStrecke_NichtAufExzentrizitaet()
    {
        var (r, h) = PolarpunktRechner.Lage(0, 0, 100.0, 50.0, MASSSTAB_UTM, 1.0, 1.0);
        Assert.Equal(50.0 * MASSSTAB_UTM + 1.0, r, TOLERANZ_M);
        Assert.Equal(-1.0, h, TOLERANZ_M);
    }

    [Theory]
    [InlineData(2.0,    0.0, false)]
    [InlineData(2.001,  0.0, true)]
    [InlineData(0.0,   -2.5, true)]
    [InlineData(-2.0,   2.0, false)]
    public void ExzentrizitaetGross_GrenzeZweiMeter(double eL, double eQ, bool erwartet)
        => Assert.Equal(erwartet, PolarpunktRechner.ExzentrizitaetGross(eL, eQ));

    // ── NeupunktRechner mit Exzentrizität / Maßstab / Höhe ────────────────────

    [Fact]
    public void NeupunktRechner_OrientierungExzentrizitaetUndMassstab()
    {
        var roh = new NeupunktRohdaten
        {
            PunktNr = "1", Hz_gon = 80.0, V_gon = ZENIT_HORIZ, Strecke_m = 50.0,
            ExzLaengs_m = 1.0, ExzQuer_m = 1.0
        };
        var erg = NeupunktRechner.Berechnen(roh, Station(o: 20.0), MASSSTAB_UTM);

        Assert.NotNull(erg);
        Assert.Equal(50.0 * MASSSTAB_UTM + 1.0, erg!.R, TOLERANZ_M);   // t = 100 gon
        Assert.Equal(-1.0, erg.H, TOLERANZ_M);
        Assert.Equal(MASSSTAB_UTM, erg.VerwendeterMassstab, TOLERANZ_M);
        Assert.Equal(1.0, erg.ExzLaengs_m, TOLERANZ_M);
    }

    [Fact]
    public void NeupunktRechner_Hoehe_UnabhaengigVonMassstabUndExzentrizitaet()
    {
        var roh = new NeupunktRohdaten { PunktNr = "1", Hz_gon = 0, V_gon = 90.0, Strecke_m = 40.0, Zielhoehe_m = 1.5 };
        var ohne = NeupunktRechner.Berechnen(roh, Station(), 1.0, 1.6);

        roh.ExzLaengs_m = 1.5; roh.ExzQuer_m = -0.7;
        var mit = NeupunktRechner.Berechnen(roh, Station(), MASSSTAB_UTM, 1.6);

        Assert.True(ohne!.Ist3D);
        Assert.Equal(ohne.Hoehe, mit!.Hoehe, TOLERANZ_M);
    }

    [Fact]
    public void NeupunktRechner_2D_WennRechenparameterOhne3D()
    {
        RechenparameterManager.Params.Berechnung3D = false;
        var roh = new NeupunktRohdaten { PunktNr = "1", Hz_gon = 0, V_gon = 90.0, Strecke_m = 40.0 };

        var erg = NeupunktRechner.Berechnen(roh, Station());

        Assert.False(erg!.Ist3D);
        Assert.Equal(0.0, erg.Hoehe);
    }

    [Fact]
    public void NeupunktRechner_StandardParameter_Massstab1()
    {
        var roh = new NeupunktRohdaten { PunktNr = "1", Hz_gon = 100, V_gon = ZENIT_HORIZ, Strecke_m = 25.0 };
        var erg = NeupunktRechner.Berechnen(roh, Station(m: 0.9990));   // Station-Maßstab wird im Standard nicht angewandt

        Assert.Equal(25.0, erg!.R, TOLERANZ_M);
        Assert.Equal(1.0, erg.VerwendeterMassstab);
    }

    // ── Maßstabsermittlung ────────────────────────────────────────────────────

    [Fact]
    public void Massstab_Standard_IstEinsOhneHinweis()
    {
        var m = PolarpunktRechner.ErmittleMassstab(new Rechenparameter(), Station());
        Assert.Equal(1.0, m.Wert);
        Assert.Equal("", m.Hinweis);
    }

    [Fact]
    public void Massstab_Vorgegeben_LiefertWertUndHinweis()
    {
        var p = new Rechenparameter { PolarMassstabModus = MassstabModus.Vorgegeben, PolarMassstabWert = MASSSTAB_UTM };
        var m = PolarpunktRechner.ErmittleMassstab(p, Station());

        Assert.Equal(MASSSTAB_UTM, m.Wert);
        Assert.Contains("0.999600", m.Hinweis);
    }

    [Fact]
    public void Massstab_VorgegebenOhneAbweichung_KeinHinweis()
    {
        var p = new Rechenparameter { PolarMassstabModus = MassstabModus.Vorgegeben, PolarMassstabWert = 1.0 };
        Assert.Equal("", PolarpunktRechner.ErmittleMassstab(p, Station()).Hinweis);
    }

    [Fact]
    public void Massstab_Unplausibel_FaelltAufEinsZurueck()
    {
        var p = new Rechenparameter { PolarMassstabModus = MassstabModus.Vorgegeben, PolarMassstabWert = 2.0 };
        var m = PolarpunktRechner.ErmittleMassstab(p, Station());

        Assert.Equal(1.0, m.Wert);
        Assert.NotEqual("", m.Hinweis);
    }

    [Fact]
    public void Massstab_Berechnet_NutztStationsmassstab()
    {
        var p = new Rechenparameter { PolarMassstabModus = MassstabModus.Berechnet };
        var station = new StationierungsErgebnis { Massstab = 0.9998, Konvergiert = true };

        var m = PolarpunktRechner.ErmittleMassstab(p, station);

        Assert.Equal(0.9998, m.Wert);
        Assert.Contains("berechnet", m.Hinweis);
    }

    [Fact]
    public void Massstab_Berechnet_LokalesSystem_RueckfallMitHinweis()
    {
        var p = new Rechenparameter { PolarMassstabModus = MassstabModus.Berechnet };
        var m = PolarpunktRechner.ErmittleMassstab(p, Station());

        Assert.Equal(1.0, m.Wert);
        Assert.Contains("nicht berechenbar", m.Hinweis);
    }

    // ── Orientierung aus einem Anschlussziel ──────────────────────────────────

    [Fact]
    public void EinZiel_OrientierungAusBekanntemStandpunkt()
    {
        var ziel = new StationierungsPunkt
        {
            PunktNr = "A", R = 100, H = 0, Hoehe = 0, HZ = 50.0, V = ZENIT_HORIZ, Strecke = 100.0
        };

        var erg = PolarpunktRechner.OrientierungAusEinemZiel(0, 0, 0, ziel, 1.5, true, true, false, false);

        Assert.NotNull(erg);
        Assert.Equal(50.0, erg!.Orientierung_gon, 1e-9);      // t = 100 gon, Hz = 50 gon
        Assert.Equal(1.0, erg.Massstab, 1e-12);
        Assert.Equal(0, erg.Redundanz);
        Assert.Single(erg.Residuen);
        Assert.True(double.IsNaN(erg.Residuen[0].vQuer_mm));
        Assert.Equal(0.0, erg.Residuen[0].vStrecke_mm, 1e-6);
    }

    [Fact]
    public void EinZiel_MassstabAusStrecke()
    {
        var ziel = new StationierungsPunkt
        {
            PunktNr = "A", R = 0, H = 99.96, HZ = 0.0, V = ZENIT_HORIZ, Strecke = 100.0
        };

        var erg = PolarpunktRechner.OrientierungAusEinemZiel(0, 0, 0, ziel, 0, true, true, false, false);

        Assert.Equal(MASSSTAB_UTM, erg!.Massstab, 1e-12);
        Assert.Equal(40.0, erg.Residuen[0].vStrecke_mm, 1e-6);   // gemessen 40 mm länger als Koordinaten
    }

    [Fact]
    public void EinZiel_OhneAktiveRichtung_KeineOrientierung()
    {
        var ziel = new StationierungsPunkt { PunktNr = "A", R = 10, H = 10, HZ = 1, V = ZENIT_HORIZ, Strecke = 14 };
        Assert.Null(PolarpunktRechner.OrientierungAusEinemZiel(0, 0, 0, ziel, 0, false, true, false, false));
    }

    [Fact]
    public void EinZiel_Hoehe_AusZielpunkt()
    {
        var ziel = new StationierungsPunkt
        {
            PunktNr = "A", R = 0, H = 100, Hoehe = 450.0, HZ = 0, V = ZENIT_HORIZ, Strecke = 100.0, Zielhoehe = 1.5
        };

        var erg = PolarpunktRechner.OrientierungAusEinemZiel(0, 0, 0, ziel, 1.6, true, true, true, true);

        Assert.True(erg!.Berechnung3D);
        Assert.Equal(450.0 + 1.5 - 1.6 - 100.0 * Math.Cos(ZENIT_HORIZ * GON2RAD), erg.Hoehe, 1e-9);
    }

    // ── Gewichtung bei nahen Zielen ───────────────────────────────────────────

    private const double STAND_R = 1000.0;
    private const double STAND_H = 2000.0;
    private const double O_WAHR  = 20.0;       // gon
    private const double NAH_M   = 5.0;
    private const double FERN_M  = 100.0;
    private const double LOT_MM  = 3.0;        // Lotfehler am nahen Ziel

    private static StationierungsPunkt Ziel(string nr, double tGon, double dist, double hzFehlerGon = 0)
    {
        double t = tGon * GON2RAD;
        return new StationierungsPunkt
        {
            PunktNr = nr,
            R = STAND_R + dist * Math.Sin(t),
            H = STAND_H + dist * Math.Cos(t),
            HZ = (tGon - O_WAHR + 400.0) % 400.0 + hzFehlerGon,
            V = ZENIT_HORIZ, Strecke = dist
        };
    }

    private static List<StationierungsPunkt> PunkteMitNahziel()
    {
        double fehlerGon = LOT_MM / 1000.0 / NAH_M / GON2RAD;
        return new List<StationierungsPunkt>
        {
            Ziel("F1",   0.0, FERN_M),
            Ziel("F2", 130.0, FERN_M),
            Ziel("F3", 270.0, FERN_M),
            Ziel("N1", 200.0, NAH_M, fehlerGon)
        };
    }

    // Ungünstige Geometrie: zwei ferne Ziele und ein nahes mit Lotfehler
    private static double OrientierungsfehlerCC(double zielZentrierung_mm)
    {
        var punkte = PunkteMitNahziel().Where(p => p.PunktNr != "F3").ToList();
        var erg = FreieStationierungRechner.Berechnen(
            punkte, 1.5, berechnung3D: false,
            zentrierfehlerZiel_mm: zielZentrierung_mm);
        return Math.Abs(erg.Orientierung_gon - O_WAHR) * 10000.0;   // gon -> cc
    }

    private const double MIN_STOERUNG_CC       = 5.0;    // ohne Gewichtung messbare Verfälschung
    private const double MAX_RESTANTEIL_MIT_GEW = 0.6;    // mit Gewichtung höchstens dieser Anteil

    [Fact]
    public void Gewichtung_VerkleinertDenEinflussEinesNahenZiels()
    {
        double ohne = OrientierungsfehlerCC(0.0);
        double mit  = OrientierungsfehlerCC(Rechenparameter.ZENTRIERFEHLER_ZIEL_STANDARD_MM);

        Assert.True(ohne > MIN_STOERUNG_CC, $"ohne Gewichtung muss das nahe Ziel stören (war {ohne:F1} cc)");
        Assert.True(mit < ohne * MAX_RESTANTEIL_MIT_GEW,
            $"mit Gewichtung deutlich kleiner (ohne {ohne:F1} cc, mit {mit:F1} cc)");
    }

    [Fact]
    public void Gewichtung_ZentrierfehlerNull_LiefertBisherigesErgebnis()
    {
        var a = FreieStationierungRechner.Berechnen(PunkteMitNahziel(), 1.5, berechnung3D: false);
        var b = FreieStationierungRechner.Berechnen(PunkteMitNahziel(), 1.5, berechnung3D: false,
                    zentrierfehlerZiel_mm: 0.0, zentrierfehlerStandpunkt_mm: 0.0);

        Assert.Equal(a.Orientierung_gon, b.Orientierung_gon, 1e-12);
        Assert.Equal(a.R, b.R, 1e-12);
        Assert.Equal(a.s0_mm, b.s0_mm, 1e-12);
    }

    [Fact]
    public void Gewichtung_RelativesGewichtFaelltMitKleinerStrecke()
    {
        var erg = FreieStationierungRechner.Berechnen(PunkteMitNahziel(), 1.5, berechnung3D: false,
                    zentrierfehlerZiel_mm: Rechenparameter.ZENTRIERFEHLER_ZIEL_STANDARD_MM);

        double fern = erg.Residuen.First(r => r.PunktNr == "F1").GewichtRichtung;
        double nah  = erg.Residuen.First(r => r.PunktNr == "N1").GewichtRichtung;

        Assert.InRange(fern, 0.99, 1.0);
        Assert.InRange(nah, 0.0, 0.05);
        Assert.Equal(1.0, erg.Residuen.Max(r => r.GewichtRichtung), 1e-12);
    }

    [Fact]
    public void Gewichtung_Aus_AlleGewichteGleich()
    {
        var erg = FreieStationierungRechner.Berechnen(PunkteMitNahziel(), 1.5, berechnung3D: false);
        Assert.All(erg.Residuen, r => Assert.Equal(1.0, r.GewichtRichtung, 1e-9));
    }

    [Fact]
    public void Gewichtung_ZweiPunkte_Unwirksam()
    {
        var punkte = PunkteMitNahziel().Take(2).ToList();
        var erg = FreieStationierungRechner.Berechnen(punkte, 1.5, berechnung3D: false,
                    zentrierfehlerZiel_mm: Rechenparameter.ZENTRIERFEHLER_ZIEL_STANDARD_MM);
        Assert.All(erg.Residuen, r => Assert.True(double.IsNaN(r.GewichtRichtung)));
    }

    [Fact]
    public void Gewichtung_Hoehe_BleibtUnveraendert()
    {
        var punkte = PunkteMitNahziel();
        for (int i = 0; i < punkte.Count; i++) punkte[i].Hoehe = 400.0 + i * 0.002;

        var ohne = FreieStationierungRechner.Berechnen(punkte, 1.5, berechnung3D: true);
        var mit  = FreieStationierungRechner.Berechnen(punkte, 1.5, berechnung3D: true, zentrierfehlerZiel_mm: 2.0);

        Assert.Equal(ohne.Hoehe, mit.Hoehe, 1e-9);
    }

    // ── Rechenparameter-Datei ─────────────────────────────────────────────────

    [Fact]
    public void Rechenparameter_AlteDateiOhnePolarElemente_LiefertStandardwerte()
    {
        string pfad = Path.Combine(Path.GetTempPath(), $"feldbuch-rp-{Guid.NewGuid():N}.xml");
        File.WriteAllText(pfad,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?><RechenparameterFreieStationierung>" +
            "<FehlergrenzCC_Winkel>20.0</FehlergrenzCC_Winkel><SigmaFaktor>2</SigmaFaktor>" +
            "</RechenparameterFreieStationierung>");
        try
        {
            RechenparameterManager.Initialize(pfad);
            var p = RechenparameterManager.Params;

            Assert.Equal(MassstabModus.Eins, p.PolarMassstabModus);
            Assert.Equal(1.0, p.PolarMassstabWert);
            Assert.True(p.GewichtungNaheZiele);
            Assert.Equal(2.0, p.ZentrierfehlerZiel_mm);
            Assert.Equal(0.0, p.ZentrierfehlerStandpunkt_mm);
            Assert.Equal(2, p.SigmaFaktor);
        }
        finally { File.Delete(pfad); }
    }

    [Fact]
    public void Rechenparameter_SpeichernUndLaden_Rundlauf()
    {
        string pfad = Path.Combine(Path.GetTempPath(), $"feldbuch-rp-{Guid.NewGuid():N}.xml");
        try
        {
            RechenparameterManager.Initialize(pfad);
            var p = RechenparameterManager.Params;
            p.PolarMassstabModus   = MassstabModus.Vorgegeben;
            p.PolarMassstabWert    = MASSSTAB_UTM;
            p.PolarMassstabQuelle  = "UTM";
            p.GewichtungNaheZiele  = false;
            p.ZentrierfehlerZiel_mm = 1.5;
            p.ZentrierfehlerStandpunkt_mm = 0.5;
            RechenparameterManager.Save();

            RechenparameterManager.Initialize(pfad);
            var q = RechenparameterManager.Params;

            Assert.Equal(MassstabModus.Vorgegeben, q.PolarMassstabModus);
            Assert.Equal(MASSSTAB_UTM, q.PolarMassstabWert, 1e-9);
            Assert.Equal("UTM", q.PolarMassstabQuelle);
            Assert.False(q.GewichtungNaheZiele);
            Assert.Equal(1.5, q.ZentrierfehlerZiel_mm);
            Assert.Equal(0.5, q.ZentrierfehlerStandpunkt_mm);
        }
        finally { File.Delete(pfad); }
    }

    // ── Polar-Punkte werden vom DXF-Viewer nicht überschrieben ────────────────

    [Fact]
    public void BerechneNeu_LaesstPolarPunkteUnveraendert()
    {
        string pfad = Path.Combine(Path.GetTempPath(), $"feldbuch-np-{Guid.NewGuid():N}.json");
        try
        {
            NeupunkteManager.Initialize(pfad);

            var polarRoh = new NeupunktRohdaten { PunktNr = "P1", StandpunktNr = "SP", Hz_gon = 100, V_gon = ZENIT_HORIZ, Strecke_m = 10 };
            var polarErg = NeupunktRechner.Berechnen(polarRoh, Station(o: 0), 1.0)!;
            polarErg.StandpunktNr = "SP";
            polarErg.Quelle       = NeupunkteManager.QUELLE_POLAR;
            NeupunkteManager.HinzufuegenOderErsetzen(polarRoh, polarErg);

            var viewerRoh = new NeupunktRohdaten { PunktNr = "V1", StandpunktNr = "SP", Hz_gon = 100, V_gon = ZENIT_HORIZ, Strecke_m = 10 };
            NeupunkteManager.HinzufuegenOderErsetzen(viewerRoh, NeupunktRechner.Berechnen(viewerRoh, Station(o: 0), 1.0)!);

            NeupunkteManager.BerechneNeu(Station(o: 50), "SP");   // andere Orientierung

            var p1 = NeupunkteManager.Koordinaten.First(k => k.PunktNr == "P1");
            var v1 = NeupunkteManager.Koordinaten.First(k => k.PunktNr == "V1");
            Assert.Equal(polarErg.R, p1.R, TOLERANZ_M);          // unverändert
            Assert.NotEqual(polarErg.R, v1.R, 3);                // neu berechnet
        }
        finally { File.Delete(pfad); }
    }
}
