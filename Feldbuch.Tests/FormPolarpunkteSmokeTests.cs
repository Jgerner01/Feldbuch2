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
