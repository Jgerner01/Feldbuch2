// Zweck:    Tests des dat.csv-Lesers (alte/neue Spalten), der Migration von 7 auf 11 Spalten,
//           des Rückschreibens der Exzentrizitäten und der KOR-CSV-Ausgabe.
// Hardware: keine
// Autor:    Johann Gerner

using Feldbuch;
using Xunit;

namespace Feldbuch.Tests;

public class PolarMessdatenTests
{
    private const double TOLERANZ = 1e-9;
    private const int    ANZAHL_SPALTEN_NEU = 11;

    private static readonly string[] AltDatei =
    [
        "# METADATA",
        "Projekt: Test",
        "Sensor: LeicaTS06",
        "Bearbeiter: ",
        "Datum: 2026-10-30",
        "StandpunktNr: SP01",
        "InstrumentenHoehe: 1.500",
        "StandpunktCode: ",
        "---",
        "# DATENTYP",
        "Messdaten",
        "# DATEN",
        "PunktNr; HZ_gon; V_gon; Strecke_m; Zielhoehe_m; Code; Zeitstempel",
        "1001; 55.4833; 98.8039; 111.822; 1.600; ; 2026-10-30 10:00",
        "1002; 135.0000; 100.7882; 113.146; 1.600; ; 2026-10-30 10:01",
        "# STATIONIERUNG: SP=SP01  IH=1.500 m  R=1.000  H=2.000  Hoehe=0.000  Ori=3.0000 gon  s0=1.00 mm  n=2",
        "7; 10.0000; 100.0000; 25.000; 1.600; 030; 2026-10-30 10:05"
    ];

    private static readonly string[] NeuDatei =
    [
        "# METADATA",
        "StandpunktNr: SP01",
        "InstrumentenHoehe: 1.500",
        "---",
        "# DATENTYP",
        "Messdaten",
        "# DATEN",
        "PunktNr; HZ_gon; V_gon; Strecke_m; Zielhoehe_m; Code; Zeitstempel; ExzLaengs_m; ExzQuer_m; StandpunktNr; Typ",
        "1001; 55.4833; 98.8039; 111.822; 1.600; ; 2026-10-30 10:00; 0.000; 0.000; SP01; AP",
        "7; 10.0000; 100.0000; 25.000; 1.600; 030; 2026-10-30 10:05; 1.250; -0.500; SP02; NP"
    ];

    // ── Leser ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Leser_NeuesFormat_LiestExzentrizitaetStandpunktUndTyp()
    {
        var daten = PolarMessdatenLeser.Parse(NeuDatei);

        Assert.Equal(2, daten.Messungen.Count);
        var np = daten.Messungen[1];
        Assert.Equal("7", np.PunktNr);
        Assert.Equal(1.25, np.ExzLaengs_m, TOLERANZ);
        Assert.Equal(-0.5, np.ExzQuer_m, TOLERANZ);
        Assert.Equal("SP02", np.StandpunktNr);
        Assert.Equal(MessdatenCSV.TYP_NEUPUNKT, np.Typ);
        Assert.False(np.TypGeschaetzt);
        Assert.True(daten.Messungen[0].IstAnschlusspunkt);
        Assert.Equal(["SP01", "SP02"], daten.Standpunkte);
        Assert.Equal(1.5, daten.KopfInstrHoehe, TOLERANZ);
    }

    [Fact]
    public void Leser_AlteDatei_ExzentrizitaetNull_TypAusKoordinaten()
    {
        var daten = PolarMessdatenLeser.Parse(AltDatei, nr => nr.StartsWith("100"));

        Assert.Equal(3, daten.Messungen.Count);
        Assert.All(daten.Messungen, m => Assert.Equal(0.0, m.ExzLaengs_m));
        Assert.All(daten.Messungen, m => Assert.True(m.TypGeschaetzt));
        Assert.True(daten.Messungen[0].IstAnschlusspunkt);
        Assert.True(daten.Messungen[1].IstAnschlusspunkt);
        Assert.False(daten.Messungen[2].IstAnschlusspunkt);
        Assert.All(daten.Messungen, m => Assert.Equal("SP01", m.StandpunktNr));
    }

    [Fact]
    public void Leser_AlteDatei_OhneKoordinaten_AllesNeupunkte()
    {
        var daten = PolarMessdatenLeser.Parse(AltDatei);
        Assert.All(daten.Messungen, m => Assert.False(m.IstAnschlusspunkt));
    }

    [Fact]
    public void Leser_ZeilenindexZeigtAufDateizeile()
    {
        var daten = PolarMessdatenLeser.Parse(AltDatei);
        Assert.StartsWith("1001", AltDatei[daten.Messungen[0].ZeilenIndex]);
        Assert.StartsWith("7;",  AltDatei[daten.Messungen[2].ZeilenIndex]);
    }

    // ── Migration und Rückschreiben ───────────────────────────────────────────

    private static string TempDatei() => Path.Combine(Path.GetTempPath(), $"feldbuch-dat-{Guid.NewGuid():N}-dat.csv");

    private static void Aufraeumen(string pfad)
    {
        File.Delete(pfad);
        string dir = Path.GetDirectoryName(pfad)!;
        foreach (var b in Directory.GetFiles(dir, Path.GetFileNameWithoutExtension(pfad) + "_backup_*.csv"))
            File.Delete(b);
    }

    [Fact]
    public void Migration_RuestetAltDateiAuf_MitSicherungskopie()
    {
        string pfad = TempDatei();
        File.WriteAllLines(pfad, AltDatei);
        try
        {
            Assert.True(MessdatenCSV.MigriereSpalten(pfad));

            var daten = PolarMessdatenLeser.Lese(pfad);
            Assert.Equal(3, daten.Messungen.Count);
            string[] zeilen = File.ReadAllLines(pfad);
            Assert.Contains("ExzLaengs_m; ExzQuer_m; StandpunktNr; Typ", string.Join("\n", zeilen));
            Assert.All(zeilen.Where(z => z.StartsWith("1001") || z.StartsWith("7;")),
                z => Assert.Equal(ANZAHL_SPALTEN_NEU, z.Split(';').Length));
            Assert.Single(Directory.GetFiles(Path.GetDirectoryName(pfad)!,
                Path.GetFileNameWithoutExtension(pfad) + "_backup_*.csv"));
            Assert.False(MessdatenCSV.MigriereSpalten(pfad));   // zweiter Lauf: nichts mehr zu tun
        }
        finally { Aufraeumen(pfad); }
    }

    [Fact]
    public void SchreibeExzentrizitaeten_AendertNurDieAngegebeneZeile()
    {
        string pfad = TempDatei();
        File.WriteAllLines(pfad, AltDatei);
        try
        {
            var vorher = PolarMessdatenLeser.Parse(AltDatei);
            int idx = vorher.Messungen[2].ZeilenIndex;

            MessdatenCSV.SchreibeExzentrizitaeten(pfad, [(idx, 2.5, -0.125)]);

            var nachher = PolarMessdatenLeser.Lese(pfad);
            Assert.Equal(2.5, nachher.Messungen[2].ExzLaengs_m, TOLERANZ);
            Assert.Equal(-0.125, nachher.Messungen[2].ExzQuer_m, TOLERANZ);
            Assert.Equal(0.0, nachher.Messungen[0].ExzLaengs_m, TOLERANZ);
            Assert.Equal(vorher.Messungen[2].Hz_gon, nachher.Messungen[2].Hz_gon, TOLERANZ);
            Assert.Equal("030", nachher.Messungen[2].Code);
        }
        finally { Aufraeumen(pfad); }
    }

    // ── KOR-CSV ───────────────────────────────────────────────────────────────

    private static string TempKor() => Path.Combine(Path.GetTempPath(), $"feldbuch-{Guid.NewGuid():N}-kor.csv");

    [Fact]
    public void KorCsv_Format_EntsprichtMusterkor()
    {
        string text = KorCsvSchreiber.Erzeuge("Test", "LeicaTS06", "2026-10-30",
            [new KorPunkt("7", 803213.0634, 5400842.7541, 452.3456, "030")]);

        string[] z = text.Replace("\r\n", "\n").Split('\n');
        Assert.Equal("# METADATA", z[0]);
        Assert.Equal("Projekt: Test", z[1]);
        Assert.Equal("# DATENTYP", z[6]);
        Assert.Equal("Koordinaten", z[7]);
        Assert.Equal("# DATEN", z[8]);
        Assert.Equal("PunktNr; X; Y; Z; Code", z[9]);
        Assert.Equal("7; 803213.063; 5400842.754; 452.346; 030", z[10]);
    }

    [Fact]
    public void KorCsv_2D_LasstHoeheLeer()
    {
        string text = KorCsvSchreiber.Erzeuge("T", "", "2026-10-30", [new KorPunkt("1", 10, 20, null, "")]);
        Assert.Contains("1; 10.000; 20.000; ; ", text);
    }

    [Fact]
    public void KorCsv_VorhandeneDatei_ErsetztGleicheNummernUndBehaeltAndere()
    {
        string pfad = TempKor();
        try
        {
            KorCsvSchreiber.Schreibe(pfad, "T", "", [new KorPunkt("1", 1, 1, 1, "a"), new KorPunkt("2", 2, 2, 2, "b")], false);
            int n = KorCsvSchreiber.Schreibe(pfad, "T", "", [new KorPunkt("2", 20, 20, null, "neu"), new KorPunkt("3", 3, 3, 3, "c")], true);

            var punkte = KorCsvSchreiber.LesePunkte(pfad);
            Assert.Equal(3, n);
            Assert.Equal(["1", "2", "3"], punkte.Select(p => p.PunktNr));
            Assert.Equal(20.0, punkte[1].R, TOLERANZ);
            Assert.Null(punkte[1].Hoehe);
            Assert.Equal("neu", punkte[1].Code);
            Assert.Equal(1.0, punkte[0].R, TOLERANZ);
        }
        finally { File.Delete(pfad); }
    }

    [Fact]
    public void KorCsv_OhneErhalten_UeberschreibtDatei()
    {
        string pfad = TempKor();
        try
        {
            KorCsvSchreiber.Schreibe(pfad, "T", "", [new KorPunkt("1", 1, 1, 1, "")], false);
            KorCsvSchreiber.Schreibe(pfad, "T", "", [new KorPunkt("9", 9, 9, 9, "")], false);
            Assert.Equal(["9"], KorCsvSchreiber.LesePunkte(pfad).Select(p => p.PunktNr));
        }
        finally { File.Delete(pfad); }
    }
}
