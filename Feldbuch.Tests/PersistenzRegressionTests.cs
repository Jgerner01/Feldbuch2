using System.Reflection;
using Feldbuch;
using Xunit;

namespace Feldbuch.Tests;

[Collection("Konfiguration")]
public sealed class PersistenzRegressionTests : IDisposable
{
    private readonly string ordner = Path.Combine(Path.GetTempPath(), "feldbuch-regression-" + Guid.NewGuid().ToString("N"));
    private readonly string alterProjektName = ProjektManager.ProjektName;
    private readonly string alterProjektPfad = ProjektManager.ProjektVerzeichnis;
    private readonly bool autoBackup = ProjektManager.AutoBackup;

    public PersistenzRegressionTests()
    {
        Directory.CreateDirectory(ordner);
        Projekt("Test", ordner);
        ProjektManager.AutoBackup = false;
        StationsdatenManager.Zuruecksetzen();
    }

    private static void Projekt(string name, string pfad)
    {
        // Einstellungen des Benutzers werden in diesen Tests nicht geschrieben.
        typeof(ProjektManager).GetProperty(nameof(ProjektManager.ProjektName))!.SetValue(null, name);
        typeof(ProjektManager).GetProperty(nameof(ProjektManager.ProjektVerzeichnis))!.SetValue(null, pfad);
    }

    private string Pfad(string name) => Path.Combine(ordner, name);

    private static List<StationierungsPunkt> Punkte() =>
    [
        new() { PunktNr = "A", R = 100, H = 0, HZ = 100, V = 100, Strecke = 100 },
        new() { PunktNr = "B", R = 0, H = 100, HZ = 0, V = 100, Strecke = 100 }
    ];

    [Fact]
    public void Zweipunktstation_MitNichtBestimmtenResiduen_WirdGespeichertUndGeladen()
    {
        var punkte = Punkte();
        var ergebnis = FreieStationierungRechner.Berechnen(punkte, 0, berechnung3D: false);
        StationsdatenManager.SetzeErgebnisManuell("SP1", 0, punkte, ergebnis);
        StationsdatenManager.Zuruecksetzen();
        StationsdatenManager.Laden("SP1");
        Assert.True(StationsdatenManager.HatGueltigeStation);
        Assert.Equal(ergebnis.R, StationsdatenManager.AktuellesErgebnis!.R);
        Assert.True(double.IsNaN(StationsdatenManager.AktuellesErgebnis.Residuen[0].GewichtRichtung));
        Assert.True(double.IsNaN(StationsdatenManager.AktuellesErgebnis.Residuen[0].vHoehe_mm));
    }

    [Theory]
    [InlineData("{ kein json")]
    [InlineData("null")]
    [InlineData("[null]")]
    [InlineData("[{\"R\":1,\"H\":2}]")]
    public void DefekterPunktIndex_WirdBeiseitegelegtUndNeuGeschrieben(string inhalt)
    {
        string pfad = Pfad("Test-PunktIndex.json");
        File.WriteAllText(pfad, inhalt);

        Assert.Null(DxfPunktIndexManager.Laden(pfad));

        string sicherung = Assert.Single(Directory.GetFiles(ordner, "Test-PunktIndex.json.defekt-*"));
        Assert.Equal(inhalt, File.ReadAllText(sicherung));

        DxfPunktIndexManager.Speichern([new PunktEintrag("1", 10, 20)], pfad);
        Assert.Equal("1", Assert.Single(DxfPunktIndexManager.Laden(pfad)!).PunktNr);
    }

    [Fact]
    public void NichtLesbarerPunktIndex_BleibtErhaltenUndGesperrt()
    {
        string pfad = Pfad("Test-PunktIndex.json");
        DxfPunktIndexManager.Speichern([new PunktEintrag("7", 1, 2)], pfad);

        using (new FileStream(pfad, FileMode.Open, FileAccess.Read, FileShare.None))
            Assert.Null(DxfPunktIndexManager.Laden(pfad));

        Assert.Throws<IOException>(() => DxfPunktIndexManager.Speichern([], pfad));
        Assert.Empty(Directory.GetFiles(ordner, "*.defekt-*"));
        Assert.Equal("7", Assert.Single(DxfPunktIndexManager.Laden(pfad)!).PunktNr);
    }

    [Theory]
    [InlineData("")]
    [InlineData("<FeldbuchEinstellungen><abgebrochen")]
    [InlineData("<AndereDatei />")]
    public void DefekteEinstellungen_WerdenBeiseitegelegtUndSpeichernBleibtMoeglich(string inhalt)
    {
        string pfad = Pfad("Einstellungen.xml");
        File.WriteAllText(pfad, inhalt);

        ProjektManager.Laden(pfad);

        Assert.False(File.Exists(pfad));
        string sicherung = Assert.Single(Directory.GetFiles(ordner, "Einstellungen.xml.defekt-*"));
        Assert.Equal(inhalt, File.ReadAllText(sicherung));
        DateiSpeicher.SchreibeText(pfad, "<FeldbuchEinstellungen />");
        Assert.True(File.Exists(pfad));
    }

    [Fact]
    public void NichtLesbareEinstellungen_BleibenErhaltenUndGesperrtBisZumNeuenLaden()
    {
        string pfad = Pfad("Einstellungen.xml");
        File.WriteAllText(pfad, "kein xml");

        using (new FileStream(pfad, FileMode.Open, FileAccess.Read, FileShare.None))
            ProjektManager.Laden(pfad);

        Assert.Equal("kein xml", File.ReadAllText(pfad));
        Assert.Throws<IOException>(() => DateiSpeicher.SchreibeText(pfad, "neu"));

        ProjektManager.Laden(pfad);
        DateiSpeicher.SchreibeText(pfad, "neu");
        Assert.Equal("neu", File.ReadAllText(pfad));
    }

    [Theory]
    [InlineData("")]
    [InlineData("<RechenparameterFreieStationierung><abgebrochen")]
    [InlineData("<AndereDatei />")]
    public void DefekteRechenparameter_WerdenBeiseitegelegtUndNeuAngelegt(string inhalt)
    {
        var typ = typeof(RechenparameterManager);
        var feldPfad = typ.GetField("_path", BindingFlags.NonPublic | BindingFlags.Static)!;
        var feldParams = typ.GetField("_params", BindingFlags.NonPublic | BindingFlags.Static)!;
        object? alterPfad = feldPfad.GetValue(null), alteParams = feldParams.GetValue(null);
        try
        {
            string pfad = Pfad("Freie-Stationierung.xml");
            File.WriteAllText(pfad, inhalt);

            RechenparameterManager.Initialize(pfad);

            Assert.Equal(new Rechenparameter().FehlergrenzCC_Winkel, RechenparameterManager.Params.FehlergrenzCC_Winkel);
            string sicherung = Assert.Single(Directory.GetFiles(ordner, "Freie-Stationierung.xml.defekt-*"));
            Assert.Equal(inhalt, File.ReadAllText(sicherung));

            RechenparameterManager.Params.FehlergrenzCC_Winkel = 33;
            RechenparameterManager.Save();
            RechenparameterManager.Initialize(pfad);
            Assert.Equal(33, RechenparameterManager.Params.FehlergrenzCC_Winkel);
        }
        finally
        {
            feldPfad.SetValue(null, alterPfad);
            feldParams.SetValue(null, alteParams);
        }
    }

    [Fact]
    public void Projektwechsel_OhneStandpunkt_VerwirftAlteStationUndAltenSchreibpfad()
    {
        var punkte = Punkte();
        StationsdatenManager.SetzeErgebnisManuell("ALT", 0, punkte,
            FreieStationierungRechner.Berechnen(punkte, 0));
        string vorher = File.ReadAllText(Pfad("Station-ALT.json"));
        string neu = Pfad("Neu");
        Directory.CreateDirectory(neu);
        Projekt("Neu", neu);
        ProjektdatenManager.Initialize(Path.Combine(neu, "Projektdaten.csv"));
        StationsdatenManager.LadeAktuellenStandpunkt();
        Assert.False(StationsdatenManager.HatGueltigeStation);
        Assert.Equal("", StationsdatenManager.StandpunktNr);
        StationsdatenManager.InstrumentenHoehe = 7;
        Assert.Equal(vorher, File.ReadAllText(Pfad("Station-ALT.json")));
        Projekt("Test", ordner);
        StationsdatenManager.Laden("ALT");
        Assert.True(StationsdatenManager.HatGueltigeStation);
    }

    [Theory]
    [InlineData("{defekt")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"Punkte\":null}")]
    public void DefektePunktdatei_BleibtErhaltenUndWirdNachReparaturWiederFreigegeben(string defekt)
    {
        string pfad = Pfad("Feldbuchpunkte.json");
        File.WriteAllText(pfad, defekt);
        FeldbuchpunkteManager.Initialize(pfad);
        Assert.Throws<IOException>(() => FeldbuchpunkteManager.AddOrUpdate(new() { PunktNr = "NEU" }));
        Assert.Empty(FeldbuchpunkteManager.Punkte);
        Assert.Equal(defekt, File.ReadAllText(pfad));
        File.WriteAllText(pfad, "{\"Punkte\":[]}");
        FeldbuchpunkteManager.Initialize(pfad);
        FeldbuchpunkteManager.AddOrUpdate(new() { PunktNr = "NEU" });
        Assert.Contains("NEU", File.ReadAllText(pfad));
    }

    [Fact]
    public void DefekteNeupunktStationsUndImportdateien_BleibenGesperrt()
    {
        string np = Pfad("Neupunkte.json"), station = Pfad("Station-KAPUTT.json"), import = Pfad("Import.json");
        foreach (string pfad in new[] { np, station, import }) File.WriteAllText(pfad, "{defekt");
        NeupunkteManager.Initialize(np);
        Assert.Throws<IOException>(() => NeupunkteManager.HinzufuegenOderErsetzen(new(), new()));
        StationsdatenManager.Laden("KAPUTT");
        Assert.Throws<IOException>(() => StationsdatenManager.NeuerStandpunkt("KAPUTT", 1.5));
        ImportPunkteManager.Initialize(import);
        Assert.Throws<IOException>(() => ImportPunkteManager.AddRange([new() { PunktNr = "NEU" }]));
        foreach (string pfad in new[] { np, station, import }) Assert.Equal("{defekt", File.ReadAllText(pfad));
    }

    [Theory]
    [InlineData("A,B")]
    [InlineData("Er sagt \"Hallo\"")]
    [InlineData("Zeile 1\r\nZeile 2")]
    [InlineData("")]
    [InlineData("  Leerzeichen  ")]
    public void Projektdaten_CsvRundlauf_ErhaeltAlleFelder(string wert)
    {
        string pfad = Pfad("Projektdaten.csv");
        string alterBearbeiter = ProjektdatenManager.Bearbeiter;
        try
        {
            ProjektdatenManager.Bearbeiter = "Test, \"Name\"";
            ProjektdatenManager.Initialize(pfad);
            ProjektdatenManager.SetValue("Kat,egorie", "Para\nMeter", wert);
            ProjektdatenManager.Initialize(pfad);
            Assert.Equal(wert, ProjektdatenManager.GetValue("Kat,egorie", "Para\nMeter"));
            Assert.Equal("Test, \"Name\"", ProjektdatenManager.GetAll().Last().Bearbeiter);
        }
        finally { ProjektdatenManager.Bearbeiter = alterBearbeiter; }
    }

    [Fact]
    public void AutoBackup_EnthaeltDieVorherigeDateiversion()
    {
        ProjektManager.AutoBackup = true;
        string pfad = Pfad("daten.txt");
        DateiSpeicher.SchreibeText(pfad, "alt");
        DateiSpeicher.SchreibeText(pfad, "neu");
        Assert.Equal("neu", File.ReadAllText(pfad));
        Assert.Equal("alt", File.ReadAllText(Assert.Single(Directory.GetFiles(Pfad("Backup")))));
    }

    [Fact]
    public void DefekteCsvUndRechenparameter_WerdenNichtUeberschrieben()
    {
        string csv = Pfad("Projektdaten.csv"), xml = Pfad("Rechenparameter.xml");
        File.WriteAllText(csv, "Datum,Uhrzeit,Bearbeiter,Kategorie,Parameter,Wert\nzu,wenig");
        File.WriteAllText(xml, "<defekt");
        string originalCsv = File.ReadAllText(csv);
        ProjektdatenManager.Initialize(csv);
        Assert.Throws<IOException>(() => ProjektdatenManager.SetValue("Test", "Wert", "neu"));
        RechenparameterManager.Initialize(xml);
        RechenparameterManager.Save();
        Assert.Equal(originalCsv, File.ReadAllText(csv));
        string sicherung = Assert.Single(Directory.GetFiles(ordner, "Rechenparameter.xml.defekt-*"));
        Assert.Equal("<defekt", File.ReadAllText(sicherung));
    }

    [Fact]
    public void PunktSpeicherfehler_WirdAnAufruferWeitergereicht()
    {
        string pfad = Pfad("Feldbuchpunkte.json");
        FeldbuchpunkteManager.Initialize(pfad);
        FeldbuchpunkteManager.AddOrUpdate(new() { PunktNr = "ALT" });
        using (File.Open(pfad, FileMode.Open, FileAccess.Read, FileShare.None))
            Assert.Throws<IOException>(() => FeldbuchpunkteManager.AddOrUpdate(new() { PunktNr = "NEU" }));
        Assert.DoesNotContain("NEU", File.ReadAllText(pfad));
        Assert.Equal("ALT", Assert.Single(FeldbuchpunkteManager.Punkte).PunktNr);
    }

    [Fact]
    public void FehlgeschlageneDateiersetzung_ErhaeltOriginalUndMeldetFehler()
    {
        string pfad = Pfad("daten.txt");
        File.WriteAllText(pfad, "Original");
        using (File.Open(pfad, FileMode.Open, FileAccess.Read, FileShare.None))
            Assert.Throws<IOException>(() => DateiSpeicher.SchreibeText(pfad, "ersetzt"));
        Assert.Equal("Original", File.ReadAllText(pfad));
        Assert.Empty(Directory.GetFiles(ordner, "*.tmp"));
    }

    [Fact]
    public void NichtKonvergierteOderNichtEndlicheStation_IstNichtGueltig()
    {
        var station = StationsdatenManager.AktiveStation;
        station.Messungen = Punkte();
        station.Ergebnis = new() { Konvergiert = false, Massstab = 1 };
        Assert.False(StationsdatenManager.HatGueltigeStation);
        station.Ergebnis.Konvergiert = true;
        Assert.True(StationsdatenManager.HatGueltigeStation);
        station.Ergebnis.R = double.NaN;
        Assert.False(StationsdatenManager.HatGueltigeStation);
        station.Ergebnis.R = 0;
        station.Messungen[1].PunktNr = "A";
        Assert.False(StationsdatenManager.HatGueltigeStation);
    }

    [Fact]
    public void WiderspruechlicheHoehen_Liefern2DmitWarnung()
    {
        var punkte = Punkte();
        punkte[1].Hoehe = 10;
        var ergebnis = FreieStationierungRechner.Berechnen(punkte, 0);
        Assert.False(ergebnis.Berechnung3D);
        Assert.Contains("Höhenabweichung", ergebnis.WarnungHoehe);
    }

    public void Dispose()
    {
        StationsdatenManager.Zuruecksetzen();
        Projekt(alterProjektName, alterProjektPfad);
        ProjektManager.AutoBackup = autoBackup;
        Directory.Delete(ordner, recursive: true);
    }
}
