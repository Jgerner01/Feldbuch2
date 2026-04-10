namespace Feldbuch;

using System.Globalization;
using System.Text;

// ──────────────────────────────────────────────────────────────────────────────
// MessdatenCSV – Anhängen aller Messungen in {ProjektName}-dat.csv.
//
// Format: Append-only CSV, Excel-kompatibel (Semikolon-getrennt).
//
// Kopfzeile beim ersten Schreiben:
//   # Projekt: {name}
//   # Erstellt: {datum}
//   Datum;Uhrzeit;StandpunktNr;InstrHoehe_m;PunktNr;Typ;Code;Hz_gon;V_gon;Strecke_m;Zielhoehe_m
//
// Standpunkt-Kommentarzeile bei Standpunktwechsel:
//   # SP: {nr}  IH: {hoehe}  R: {r}  H: {h}  Hoehe: {h3d}  Ori: {ori}  s0: {s0}
//
// Jede Messung = eine Datenzeile.
// ──────────────────────────────────────────────────────────────────────────────
public static class MessdatenCSV
{
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    private const string Header =
        "Datum;Uhrzeit;StandpunktNr;InstrHoehe_m;PunktNr;Typ;Code;" +
        "Hz_gon;V_gon;Strecke_m;Zielhoehe_m";

    /// <summary>Pfad zur Messdaten-Datei (basierend auf aktuellem Projekt).</summary>
    public static string Pfad =>
        ProjektManager.GetPfad(ProjektManager.ProjektName + "-dat.csv");

    // ── Messung schreiben ─────────────────────────────────────────────────────

    /// <summary>
    /// Schreibt eine Stationierungsmessung (Anschlusspunkt) in die CSV-Datei.
    /// Legt die Datei an (mit Header) wenn sie noch nicht existiert.
    /// </summary>
    public static void SchreibeStationierungsmessung(
        StationierungsPunkt punkt,
        string              standpunktNr,
        double              instrHoehe,
        DateTime            zeitstempel)
    {
        SchreibeZeile(
            zeitstempel, standpunktNr, instrHoehe,
            punkt.PunktNr, "Stationierung", "",
            punkt.HZ, punkt.V, punkt.Strecke, punkt.Zielhoehe);
    }

    /// <summary>
    /// Schreibt eine Neupunktmessung in die CSV-Datei.
    /// </summary>
    public static void SchreibeNeupunktmessung(
        NeupunktRohdaten rohdaten,
        string           standpunktNr,
        double           instrHoehe)
    {
        SchreibeZeile(
            rohdaten.Zeitstempel, standpunktNr, instrHoehe,
            rohdaten.PunktNr, "Neupunkt", rohdaten.Code,
            rohdaten.Hz_gon, rohdaten.V_gon, rohdaten.Strecke_m, rohdaten.Zielhoehe_m);
    }

    // ── Standpunkt-Kommentarzeile ─────────────────────────────────────────────

    /// <summary>
    /// Schreibt eine Kommentarzeile mit den aktuellen Standpunktdaten.
    /// Wird nach Neuberechnung der Stationierung eingefügt.
    /// </summary>
    public static void SchreibeStandpunktinfo(
        string                 standpunktNr,
        double                 instrHoehe,
        StationierungsErgebnis ergebnis)
    {
        SicherstelleDatei();
        string zeile = string.Format(IC,
            "# SP: {0}  IH: {1:F3} m  R: {2:F3}  H: {3:F3}  Hoehe: {4:F3}  Ori: {5:F4} gon  s0: {6:F2} mm  n={7}",
            standpunktNr, instrHoehe,
            ergebnis.R, ergebnis.H, ergebnis.Hoehe,
            ergebnis.Orientierung_gon, ergebnis.s0_mm, ergebnis.Redundanz);

        AppendZeile(zeile);
        ProtokollManager.Log("STANDPKT", zeile.TrimStart('#', ' '));
    }

    // ── Interne Helfer ────────────────────────────────────────────────────────

    private static void SchreibeZeile(
        DateTime zeitstempel,
        string   standpunktNr,
        double   instrHoehe,
        string   punktNr,
        string   typ,
        string   code,
        double   hz, double v, double strecke, double zh)
    {
        SicherstelleDatei();

        string zeile = string.Join(";",
            zeitstempel.ToString("yyyy-MM-dd"),
            zeitstempel.ToString("HH:mm:ss"),
            standpunktNr,
            instrHoehe.ToString("F3", IC),
            punktNr,
            typ,
            code,
            hz    .ToString("F4", IC),
            v     .ToString("F4", IC),
            strecke.ToString("F3", IC),
            zh    .ToString("F3", IC));

        AppendZeile(zeile);
    }

    private static void SicherstelleDatei()
    {
        string pfad = Pfad;
        if (File.Exists(pfad)) return;

        // Neue Datei anlegen
        var sb = new StringBuilder();
        sb.AppendLine($"# Projekt: {ProjektManager.ProjektName}");
        sb.AppendLine($"# Erstellt: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("# Format: Feldbuch Messdaten");
        sb.AppendLine(Header);

        File.WriteAllText(pfad, sb.ToString(), Encoding.UTF8);
    }

    private static void AppendZeile(string zeile)
    {
        try
        {
            File.AppendAllText(Pfad, zeile + Environment.NewLine, Encoding.UTF8);
        }
        catch (Exception ex) { ErrorLogger.Log("MessdatenCSV.AppendZeile", ex); }
    }
}
