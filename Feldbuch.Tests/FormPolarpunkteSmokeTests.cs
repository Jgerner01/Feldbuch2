// Zweck:    Rauchtest der Polarpunkt-Maske: Formular wird erzeugt, Messdaten werden per Reflection
//           eingesetzt, Anschlusspunkte/Polarpunkte werden berechnet und angezeigt.
//           Keine Dialoge, kein Zugriff auf das echte Projektverzeichnis.
// Hardware: keine
// Autor:    Johann Gerner

using System.Reflection;
using Feldbuch;
using Xunit;

namespace Feldbuch.Tests;

[Collection("Konfiguration")]
public class FormPolarpunkteSmokeTests : IDisposable
{
    private const double GON2RAD = Math.PI / 200.0;
    private const double R_STAND = 1000.0;
    private const double H_STAND = 2000.0;
    private const double O_GON   = 20.0;
    private const double FERN_M  = 100.0;
    private const double TOL_M   = 0.001;

    public FormPolarpunkteSmokeTests() => Zuruecksetzen();
    public void Dispose()             => Zuruecksetzen();

    private static void Zuruecksetzen()
    {
        string rp = Path.Combine(Path.GetTempPath(), $"feldbuch-rp-{Guid.NewGuid():N}.xml");
        RechenparameterManager.Initialize(rp);
        File.Delete(rp);
        string ip = Path.Combine(Path.GetTempPath(), $"feldbuch-ip-{Guid.NewGuid():N}.json");
        ImportPunkteManager.Initialize(ip);
        File.Delete(ip);
        string fp = Path.Combine(Path.GetTempPath(), $"feldbuch-fp-{Guid.NewGuid():N}.json");
        FeldbuchpunkteManager.Initialize(fp);
    }

    private static T Feld<T>(object o, string name)
        => (T)o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(o)!;

    private static void Setze(object o, string name, object wert)
        => o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(o, wert);

    private static string Zeile(string nr, double hz, double s, double eL, double eQ, string typ)
        => $"{nr}; {hz:F4}; 100.0000; {s:F3}; 1.600; ; 2026-10-30 10:00; {eL:F3}; {eQ:F3}; SP01; {typ}"
            .Replace(',', '.');

    private static string[] Messdatei(double eLaengs) =>
    [
        "# METADATA", "StandpunktNr: SP01", "InstrumentenHoehe: 1.500", "---",
        "# DATENTYP", "Messdaten", "# DATEN",
        "PunktNr; HZ_gon; V_gon; Strecke_m; Zielhoehe_m; Code; Zeitstempel; ExzLaengs_m; ExzQuer_m; StandpunktNr; Typ",
        Zeile("A1", Norm(0.0   - O_GON), FERN_M, 0, 0, "AP"),
        Zeile("A2", Norm(130.0 - O_GON), FERN_M, 0, 0, "AP"),
        Zeile("A3", Norm(270.0 - O_GON), FERN_M, 0, 0, "AP"),
        Zeile("7",  Norm(100.0 - O_GON), 50.0, eLaengs, 1.0, "NP")
    ];

    private static double Norm(double gon) => (gon % 400.0 + 400.0) % 400.0;

    private static void AnschlusspunkteImportieren()
    {
        ImportPunkt P(string nr, double t) => new()
        {
            PunktNr = nr,
            R = R_STAND + FERN_M * Math.Sin(t * GON2RAD),
            H = H_STAND + FERN_M * Math.Cos(t * GON2RAD)
        };
        ImportPunkteManager.AddRange([P("A1", 0.0), P("A2", 130.0), P("A3", 270.0)]);
    }

    private static T InSta<T>(Func<T> aktion)
    {
        T? ergebnis = default;
        Exception? fehler = null;
        var thread = new Thread(() =>
        {
            try { ergebnis = aktion(); }
            catch (Exception ex) { fehler = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (fehler != null) throw new Exception("Fehler im STA-Thread: " + fehler, fehler);
        return ergebnis!;
    }

    private static FormPolarpunkte MaskeMitDaten(double eLaengs)
    {
        AnschlusspunkteImportieren();
        var form = new FormPolarpunkte();
        var cmb  = Feld<ComboBox>(form, "cmbStandpunkt");

        Setze(form, "_daten", PolarMessdatenLeser.Parse(Messdatei(eLaengs)));
        cmb.Items.Add("SP01");
        cmb.SelectedIndex = 0;                  // löst die Berechnung aus
        return form;
    }

    [Fact]
    public void Maske_BerechnetPolarpunktMitExzentrizitaet()
    {
        var (r, h, hinweisSichtbar, gewicht) = InSta(() =>
        {
            using var form = MaskeMitDaten(1.0);
            var polar = Feld<DataGridView>(form, "dgvPolar");
            var anschluss = Feld<DataGridView>(form, "dgvAnschluss");
            return (
                double.Parse(polar.Rows[0].Cells["pR"].Value!.ToString()!, System.Globalization.CultureInfo.InvariantCulture),
                double.Parse(polar.Rows[0].Cells["pH"].Value!.ToString()!, System.Globalization.CultureInfo.InvariantCulture),
                Feld<Label>(form, "lblHinweise").Visible,
                anschluss.Rows[0].Cells["Gewicht"].Value?.ToString() ?? "");
        });

        // t = 100 gon (Ost): 50 m + eL 1 m -> R + 51, eQ 1 m rechts -> H − 1
        Assert.Equal(R_STAND + 51.0, r, TOL_M);
        Assert.Equal(H_STAND - 1.0, h, TOL_M);
        Assert.False(hinweisSichtbar);            // 3 Anschlusspunkte, m = 1, Exzentrizität <= 2 m
        Assert.Contains("%", gewicht);
    }

    [Fact]
    public void Maske_ZeigtHinweisBeiGrosserExzentrizitaet()
    {
        string hinweis = InSta(() =>
        {
            using var form = MaskeMitDaten(2.5);
            return Feld<Label>(form, "lblHinweise").Text;
        });

        Assert.Contains("Exzentrizität", hinweis);
        Assert.Contains("7", hinweis);
    }

    [Fact]
    public void Maske_AnschlussBeobachtungAbschalten_RechnetNeuOhneFehler()
    {
        string ergebnisText = InSta(() =>
        {
            using var form = MaskeMitDaten(0.0);
            var anschluss = Feld<DataGridView>(form, "dgvAnschluss");
            anschluss.Rows[0].Cells["colAktivStr"].Value = false;
            return Feld<Label>(form, "lblErgebnis").Text;
        });

        Assert.Contains("Orientierung", ergebnisText);
    }

    [Fact]
    public void Maske_Massstab_ErzeugtHinweisUndVerschiebtStrecke()
    {
        RechenparameterManager.Params.PolarMassstabModus = MassstabModus.Vorgegeben;
        RechenparameterManager.Params.PolarMassstabWert  = 0.9996;

        var (r, hinweis) = InSta(() =>
        {
            using var form = MaskeMitDaten(0.0);
            var polar = Feld<DataGridView>(form, "dgvPolar");
            return (double.Parse(polar.Rows[0].Cells["pR"].Value!.ToString()!, System.Globalization.CultureInfo.InvariantCulture),
                    Feld<Label>(form, "lblHinweise").Text);
        });

        Assert.Equal(R_STAND + 50.0 * 0.9996, r, TOL_M);
        Assert.Contains("0.999600", hinweis);
    }

    [Theory]
    [InlineData(150)]      // kleiner als beide Teilbereiche zusammen (Regression: SplitterDistance-Ausnahme beim Öffnen)
    [InlineData(400)]
    [InlineData(900)]
    public void Maske_LaesstSichBeiJederFensterhoeheAnzeigen(int hoehe)
    {
        bool sichtbar = InSta(() =>
        {
            using var form = new FormPolarpunkte();
            form.StartPosition = FormStartPosition.Manual;
            form.ClientSize    = new Size(1180, hoehe);
            form.Show();
            Application.DoEvents();
            form.ClientSize = new Size(1180, hoehe + 120);
            Application.DoEvents();
            bool ok = form.Visible;
            form.Close();
            return ok;
        });
        Assert.True(sichtbar);
    }

    private static void KlickSpeichern(FormPolarpunkte form)
        => typeof(FormPolarpunkte)
            .GetMethod("btnSpeichern_Click", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(form, [null, EventArgs.Empty]);

    private static void LegeNeupunktAn(string nr, string sp, double r, double h)
        => NeupunkteManager.HinzufuegenOderErsetzen(
            new NeupunktRohdaten  { PunktNr = nr, StandpunktNr = sp },
            new NeupunktErgebnis  { PunktNr = nr, StandpunktNr = sp, R = r, H = h });

    /// <summary>Temporäres Protokollverzeichnis mit Vorlage (die Testumgebung hat keine EXE-Vorlage).</summary>
    private static string ProtokollVerzeichnisVorbereiten()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"feldbuch-pr-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Polarpunkte_Protokoll.xml"),
                  Path.Combine(dir, "Polarpunkte_Protokoll.xml"));
        PolarpunkteProtokoll.VerzeichnisUeberschreibung = dir;
        return dir;
    }

    private static string NeuerTempPfad(string prefix)
        => Path.Combine(Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}.json");

    [Fact]
    public void Speichern_FragtBeiStarkerAbweichungNachUndSchreibtProtokoll()
    {
        RechenparameterManager.Params.Berechnung3D = false;      // sonst blockiert der Hinweisdialog (Station ohne Höhe)
        string np = NeuerTempPfad("feldbuch-np");
        NeupunkteManager.Initialize(np);
        LegeNeupunktAn("7", "SP01", 1.0, 2.0);
        string ausgabe = ProtokollVerzeichnisVorbereiten();

        string? frage = null;
        try
        {
            string status = InSta(() =>
            {
                using var form = MaskeMitDaten(0.0);
                form.RueckfrageUeberschreiben = t => { frage = t; return true; };
                KlickSpeichern(form);
                return Feld<Label>(form, "lblStatus").Text;
            });

            var punkt = NeupunkteManager.Koordinaten.Single(k => k.PunktNr == "7");
            Assert.Equal(R_STAND + 50.0, punkt.R, TOL_M);
            Assert.Equal(NeupunkteManager.QUELLE_POLAR, punkt.Quelle);
            Assert.Contains("1 ersetzt", status);
            Assert.NotNull(frage);
            Assert.Contains("7", frage);

            string? protokoll = PolarpunkteProtokoll.LetztesProtokoll();
            Assert.NotNull(protokoll);
            string rtf = File.ReadAllText(protokoll!);
            Assert.Contains("ersetzt", rtf);
            Assert.Contains("Polarpunkte", rtf);
        }
        finally
        {
            PolarpunkteProtokoll.VerzeichnisUeberschreibung = null;
            Directory.Delete(ausgabe, true);
            File.Delete(np);
        }
    }

    [Fact]
    public void Speichern_BrichtBeiVerneinterRueckfrageAbUndLaesstPunktUnveraendert()
    {
        RechenparameterManager.Params.Berechnung3D = false;
        string np = NeuerTempPfad("feldbuch-np");
        NeupunkteManager.Initialize(np);
        LegeNeupunktAn("7", "SP01", 1.0, 2.0);

        try
        {
            string status = InSta(() =>
            {
                using var form = MaskeMitDaten(0.0);
                form.RueckfrageUeberschreiben = _ => false;
                KlickSpeichern(form);
                return Feld<Label>(form, "lblStatus").Text;
            });

            Assert.Equal(1.0, NeupunkteManager.Koordinaten.Single(k => k.PunktNr == "7").R, TOL_M);
            Assert.Contains("abgebrochen", status);
        }
        finally { File.Delete(np); }
    }

    [Fact]
    public void Speichern_UeberschreibtNichtDenGleichnamigenPunktEinesAnderenStandpunkts()
    {
        RechenparameterManager.Params.Berechnung3D = false;
        string np = NeuerTempPfad("feldbuch-np");
        NeupunkteManager.Initialize(np);
        LegeNeupunktAn("7", "SP02", 1.0, 2.0);
        string ausgabe = ProtokollVerzeichnisVorbereiten();

        bool gefragt = false;
        try
        {
            string status = InSta(() =>
            {
                using var form = MaskeMitDaten(0.0);
                form.RueckfrageUeberschreiben = _ => { gefragt = true; return true; };
                KlickSpeichern(form);
                return Feld<Label>(form, "lblStatus").Text;
            });

            Assert.False(gefragt);
            Assert.Contains("1 neu", status);
            Assert.Equal(2, NeupunkteManager.Koordinaten.Count(k => k.PunktNr == "7"));
            Assert.Equal(1.0, NeupunkteManager.FindeKoordinate("7", "SP02")!.R, TOL_M);
            Assert.Equal(R_STAND + 50.0, NeupunkteManager.FindeKoordinate("7", "SP01")!.R, TOL_M);
        }
        finally
        {
            PolarpunkteProtokoll.VerzeichnisUeberschreibung = null;
            Directory.Delete(ausgabe, true);
            File.Delete(np);
        }
    }

    [Fact]
    public void Maske_ZeigtHinweisBeiAbweichungZumFestpunkt()
    {
        string fb = NeuerTempPfad("feldbuch-fb");
        FeldbuchpunkteManager.Initialize(fb);
        FeldbuchpunkteManager.AddOrUpdate(new FeldbuchPunkt
        {
            PunktNr = "7", Typ = FestpunktUebernahme.TYP_NEUPUNKT, R = R_STAND + 51.0, H = H_STAND - 1.0     // Messpunkt liegt bei R+50 / H-1 (eQ = 1 m)
        });

        try
        {
            string hinweis = InSta(() =>
            {
                using var form = MaskeMitDaten(0.0);
                return Feld<Label>(form, "lblHinweise").Text;
            });

            Assert.Contains("Festpunkt", hinweis);
            Assert.Contains("7 (1.000 m)", hinweis);
        }
        finally { File.Delete(fb); }
    }

    [Fact]
    public void Maske_OhneProjekt_ErzeugtLeereMaske()
    {
        string status = InSta(() =>
        {
            using var form = new FormPolarpunkte();
            return Feld<Label>(form, "lblStatus").Text;
        });
        Assert.False(string.IsNullOrEmpty(status));
    }
}
