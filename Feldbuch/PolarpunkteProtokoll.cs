// Zweck:    Protokoll der Polarpunktberechnung: bereitet die Laufzeitdaten auf und lässt den
//           RtfProtokollGenerator daraus mit der Vorlage Polarpunkte_Protokoll.xml eine RTF-Datei
//           erzeugen (angelehnt an das Protokoll der Freien Stationierung).
//           Ausgabe: Polarpunkte_YYYY-MM-DD_HH-mm-ss.rtf im Projektverzeichnis.
// Hardware: keine
// Autor:    Johann Gerner

using System.Globalization;

namespace Feldbuch;

/// <summary>Anschlusspunkt-Zeile des Protokolls (NaN = nicht vorhanden).</summary>
public record PolarProtokollAnschluss(
    string PunktNr, bool Inaktiv, double StreckeH,
    double vWinkel_cc, double vQuer_mm, double vLaengs_mm, double vHoehe_mm, double GewichtRichtung);

/// <summary>Polarpunkt-Zeile des Protokolls; Status "neu" oder "ersetzt" (dR/dH = neu − alt, NaN bei "neu").</summary>
public record PolarProtokollPunkt(
    string PunktNr, double Hz, double V, double Strecke, double Zielhoehe,
    double ExzLaengs, double ExzQuer, double R, double H, double? Hoehe,
    string Status, double dR, double dH);

public record PolarProtokollDaten(
    string Standpunkt, double InstrHoehe, string Quelle, bool Lokal,
    double R, double H, double? Hoehe, double Orientierung_gon, double Massstab,
    string S0Info, string Gewichtung,
    IReadOnlyList<string> Hinweise,
    IReadOnlyList<PolarProtokollAnschluss> Anschluss,
    IReadOnlyList<PolarProtokollPunkt> Punkte);

public static class PolarpunkteProtokoll
{
    private const string VORLAGE_NAME   = "Polarpunkte_Protokoll.xml";
    private const string DATEI_MUSTER   = "Polarpunkte_*.rtf";
    private const string STRICH         = "-";
    private const string SIGNIERT       = "+0.0;-0.0;0.0";
    private const string SIGNIERT_M     = "+0.000;-0.000;0.000";
    private const int    PROZENT        = 100;
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    /// <summary>Ersetzt Vorlagen- und Ausgabeverzeichnis (nur für Tests).</summary>
    public static string? VerzeichnisUeberschreibung { get; set; }

    private static string Verzeichnis
        => VerzeichnisUeberschreibung
           ?? (ProjektManager.IstGeladen ? ProjektManager.ProjektVerzeichnis : AppPfade.Basis);

    /// <summary>Zuletzt erzeugtes Polarpunkte-Protokoll oder null.</summary>
    public static string? LetztesProtokoll()
    {
        try
        {
            return Directory.GetFiles(Verzeichnis, DATEI_MUSTER)
                            .OrderByDescending(f => f, StringComparer.Ordinal)
                            .FirstOrDefault();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log("PolarpunkteProtokoll.LetztesProtokoll", ex);
            return null;
        }
    }

    /// <summary>Schreibt das Protokoll; gibt den Dateipfad zurück, null wenn deaktiviert oder fehlgeschlagen.</summary>
    public static string? Schreiben(PolarProtokollDaten daten)
    {
        if (!ProjektManager.ProtokollAktiv) return null;

        string vorlage = Path.Combine(VerzeichnisUeberschreibung ?? AppPfade.Basis, VORLAGE_NAME);
        if (!File.Exists(vorlage))
        {
            MessageBox.Show($"Protokollvorlage nicht gefunden:\n{vorlage}",
                "Protokoll-Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        try
        {
            var jetzt = DateTime.Now;
            string ziel = Path.Combine(Verzeichnis, $"Polarpunkte_{jetzt:yyyy-MM-dd_HH-mm-ss}.rtf");
            var tabellen = new Dictionary<string, List<Dictionary<string, string>>>
            {
                ["Anschluss"] = BaueAnschlussZeilen(daten),
                ["Polar"]     = BauePolarZeilen(daten)
            };
            RtfProtokollGenerator.Schreiben(vorlage, BaueFelder(daten, jetzt), tabellen, ziel);
            return ziel;
        }
        catch (Exception ex)
        {
            ErrorLogger.Log("PolarpunkteProtokoll.Schreiben", ex);
            MessageBox.Show($"Protokoll konnte nicht geschrieben werden:\n{ex.Message}\n\nZiel: {Verzeichnis}",
                "Protokoll-Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }
    }

    // ── Einfache Felder ───────────────────────────────────────────────────────

    private static Dictionary<string, string> BaueFelder(PolarProtokollDaten d, DateTime zeitpunkt)
    {
        int neu     = d.Punkte.Count(p => p.Status == "neu");
        int ersetzt = d.Punkte.Count - neu;

        return new Dictionary<string, string>
        {
            ["Bearbeiter"]      = ProjektdatenManager.Bearbeiter,
            ["Zeitpunkt"]       = zeitpunkt.ToString("dd.MM.yyyy    HH:mm:ss"),
            ["Standpunktnr"]    = d.Standpunkt,
            ["IH"]              = d.InstrHoehe.ToString("F3", IC),
            ["Quelle"]          = d.Quelle,
            ["R"]               = d.R.ToString("F3", IC),
            ["H"]               = d.H.ToString("F3", IC),
            ["Hoehe"]           = d.Hoehe.HasValue ? d.Hoehe.Value.ToString("F3", IC) : STRICH,
            ["Orientierung"]    = d.Orientierung_gon.ToString("F4", IC),
            ["Massstab"]        = d.Massstab.ToString("F6", IC),
            ["S0Info"]          = d.S0Info,
            ["Gewichtung"]      = d.Gewichtung,
            ["Hinweise"]        = string.Join("\n", d.Hinweise.Select(h => "! " + h)),
            ["HatHinweise"]     = d.Hinweise.Count > 0 ? "1" : "0",
            ["HatAnschluss"]    = d.Anschluss.Count > 0 ? "1" : "0",
            ["Zusammenfassung"] = $"{d.Punkte.Count} Polarpunkt(e) in Neupunkte.json gespeichert: {neu} neu, {ersetzt} ersetzt."
        };
    }

    // ── Tabellenzeilen ────────────────────────────────────────────────────────

    private static string Wert(double w, string format)
        => double.IsNaN(w) ? STRICH : w.ToString(format, IC);

    private static List<Dictionary<string, string>> BaueAnschlussZeilen(PolarProtokollDaten d)
        => d.Anschluss.Select(a => new Dictionary<string, string>
        {
            ["PunktNr"]  = a.PunktNr + (a.Inaktiv ? "*" : ""),
            ["StreckeH"] = a.StreckeH.ToString("F3", IC),
            ["vWinkel"]  = Wert(a.vWinkel_cc, SIGNIERT),
            ["vQuer"]    = Wert(a.vQuer_mm, SIGNIERT),
            ["vLaengs"]  = Wert(a.vLaengs_mm, SIGNIERT),
            ["vHoehe"]   = Wert(a.vHoehe_mm, SIGNIERT),
            ["Gewicht"]  = double.IsNaN(a.GewichtRichtung)
                               ? STRICH : (a.GewichtRichtung * PROZENT).ToString("F1", IC) + "%"
        }).ToList();

    private static List<Dictionary<string, string>> BauePolarZeilen(PolarProtokollDaten d)
        => d.Punkte.Select(p => new Dictionary<string, string>
        {
            ["PunktNr"]   = p.PunktNr,
            ["HZ"]        = p.Hz.ToString("F4", IC),
            ["V"]         = p.V.ToString("F4", IC),
            ["Strecke"]   = p.Strecke.ToString("F3", IC),
            ["Zielhoehe"] = p.Zielhoehe != 0.0 ? p.Zielhoehe.ToString("F3", IC) : STRICH,
            ["ExzL"]      = p.ExzLaengs.ToString("F3", IC),
            ["ExzQ"]      = p.ExzQuer.ToString("F3", IC),
            ["R"]         = p.R.ToString("F3", IC),
            ["H"]         = p.H.ToString("F3", IC),
            ["Hoehe"]     = p.Hoehe.HasValue ? p.Hoehe.Value.ToString("F3", IC) : STRICH,
            ["Status"]    = p.Status,
            ["dR"]        = Wert(p.dR, SIGNIERT_M),
            ["dH"]        = Wert(p.dH, SIGNIERT_M)
        }).ToList();
}
