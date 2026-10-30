namespace Feldbuch;

using System.Globalization;
using System.Text;

// ──────────────────────────────────────────────────────────────────────────────
// MessdatenCSV – Schreibt Messungen in {ProjektName}-dat.csv.
//
// Format gemäß CLAUDE.md / Muster-dat.csv:
//
//   # METADATA
//   Projekt: {name}
//   Sensor: {tachymeter}
//   Bearbeiter:
//   Datum: yyyy-MM-dd
//   StandpunktNr: {nr}
//   InstrumentenHoehe: {ih}
//   StandpunktCode:
//   ---
//   # DATENTYP
//   Messdaten
//   # DATEN
//   PunktNr; HZ_gon; V_gon; Strecke_m; Zielhoehe_m; Code; Zeitstempel;
//     ExzLaengs_m; ExzQuer_m; StandpunktNr; Typ
//   {wert1}; {wert2}; ...
//
// Zusatzspalten (seit v1.9.8):
//   ExzLaengs_m / ExzQuer_m  Exzentrizität des Zielpunkts [m] (siehe PolarpunktRechner)
//   StandpunktNr             Standpunkt, von dem aus gemessen wurde
//   Typ                      AP = Anschlusspunkt, NP = Neupunkt
// Alte Dateien mit 7 Spalten werden beim Schreiben automatisch aufgerüstet.
//
// Standpunktwechsel / Neuberechnung während der Session:
//   # STATIONIERUNG: SP={nr}  IH={ih}  Ori={ori}  s0={s0}  n={n}
//   (reine Kommentarzeile – keine Daten)
//
// Existiert die Datei bereits mit altem (falschem) Format:
//   → Sicherungskopie anlegen, neue Datei mit korrektem Header erstellen.
// ──────────────────────────────────────────────────────────────────────────────
public static class MessdatenCSV
{
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    private const string SpaltenzeileAlt =
        "PunktNr; HZ_gon; V_gon; Strecke_m; Zielhoehe_m; Code; Zeitstempel";

    private const string Spaltenzeile =
        SpaltenzeileAlt + "; ExzLaengs_m; ExzQuer_m; StandpunktNr; Typ";

    /// <summary>Zusatzwerte für Datenzeilen alter Dateien: Exzentrizität 0, Standpunkt und Typ leer.</summary>
    private const string ZusatzwerteAlteZeile = "; 0.000; 0.000; ; ";

    /// <summary>Typ-Kennzeichen in der Spalte „Typ“.</summary>
    public const string TYP_ANSCHLUSSPUNKT = "AP";
    public const string TYP_NEUPUNKT       = "NP";

    /// <summary>Index der ersten Zusatzspalte (ExzLaengs_m) in einer Datenzeile.</summary>
    private const int SPALTE_EXZ_LAENGS = 7;
    private const int SPALTE_EXZ_QUER   = 8;
    private const int SPALTENANZAHL     = 11;

    /// <summary>Pfad zur Messdaten-Datei (basierend auf aktuellem Projekt).</summary>
    public static string Pfad =>
        ProjektManager.GetPfad(ProjektManager.ProjektName + "-dat.csv");

    // ── Neupunktmessung ───────────────────────────────────────────────────────

    /// <summary>
    /// Schreibt eine Neupunktmessung in die CSV-Datei.
    /// Legt die Datei an (mit vollständigem METADATA-Header) wenn sie noch nicht
    /// existiert oder ein veraltetes Format hat.
    /// </summary>
    public static void SchreibeNeupunktmessung(
        NeupunktRohdaten rohdaten,
        string           standpunktNr,
        double           instrHoehe)
    {
        SicherstelleDatei(standpunktNr, instrHoehe);

        string zeile = string.Join("; ",
            rohdaten.PunktNr,
            rohdaten.Hz_gon     .ToString("F4", IC),
            rohdaten.V_gon      .ToString("F4", IC),
            rohdaten.Strecke_m  .ToString("F3", IC),
            rohdaten.Zielhoehe_m.ToString("F3", IC),
            rohdaten.Code,
            rohdaten.Zeitstempel.ToString("yyyy-MM-dd HH:mm"),
            rohdaten.ExzLaengs_m.ToString("F3", IC),
            rohdaten.ExzQuer_m  .ToString("F3", IC),
            standpunktNr,
            TYP_NEUPUNKT);

        AppendZeile(zeile);
    }

    // ── Stationierungsmessung ─────────────────────────────────────────────────

    /// <summary>
    /// Schreibt eine Stationierungsmessung (Anschlusspunkt) in die CSV-Datei.
    /// Legt die Datei an (mit vollständigem METADATA-Header) wenn sie noch nicht
    /// existiert oder ein veraltetes Format hat.
    /// </summary>
    public static void SchreibeStationierungsmessung(
        StationierungsPunkt punkt,
        string              standpunktNr,
        double              instrHoehe,
        DateTime            zeitstempel)
    {
        SicherstelleDatei(standpunktNr, instrHoehe);

        string zeile = string.Join("; ",
            punkt.PunktNr,
            punkt.HZ      .ToString("F4", IC),
            punkt.V       .ToString("F4", IC),
            punkt.Strecke .ToString("F3", IC),
            punkt.Zielhoehe.ToString("F3", IC),
            "",
            zeitstempel.ToString("yyyy-MM-dd HH:mm"),
            0.0.ToString("F3", IC),
            0.0.ToString("F3", IC),
            standpunktNr,
            TYP_ANSCHLUSSPUNKT);

        AppendZeile(zeile);
    }

    // ── Stationierungsergebnis-Kommentar ──────────────────────────────────────

    /// <summary>
    /// Schreibt eine Kommentarzeile nach Neuberechnung der Stationierung.
    /// </summary>
    public static void SchreibeStandpunktinfo(
        string                 standpunktNr,
        double                 instrHoehe,
        StationierungsErgebnis ergebnis)
    {
        SicherstelleDatei(standpunktNr, instrHoehe);

        string zeile = string.Format(IC,
            "# STATIONIERUNG: SP={0}  IH={1:F3} m  R={2:F3}  H={3:F3}  " +
            "Hoehe={4:F3}  Ori={5:F4} gon  s0={6:F2} mm  n={7}",
            standpunktNr, instrHoehe,
            ergebnis.R, ergebnis.H, ergebnis.Hoehe,
            ergebnis.Orientierung_gon, ergebnis.s0_mm, ergebnis.Redundanz);
        if (RechenparameterManager.SigmaFaktor > 1)
            zeile += string.Format(IC, "  {0}s0={1:F2} mm",
                RechenparameterManager.SigmaFaktor,
                RechenparameterManager.SigmaWert(ergebnis.s0_mm));

        AppendZeile(zeile);
        ProtokollManager.Log("STANDPKT", zeile.TrimStart('#', ' '));
    }

    // ── Interne Helfer ────────────────────────────────────────────────────────

    /// <summary>
    /// Stellt sicher dass die Datei mit korrektem METADATA-Header existiert.
    /// Existiert sie bereits mit falschem Format → Sicherungskopie + Neuanlage.
    /// </summary>
    private static void SicherstelleDatei(string standpunktNr, double instrHoehe)
    {
        string pfad = Pfad;

        if (File.Exists(pfad))
        {
            // Format prüfen: erste Zeile muss "# METADATA" sein
            try
            {
                string ersteLine = File.ReadLines(pfad, Encoding.UTF8)
                                       .FirstOrDefault()?.Trim() ?? "";
                if (ersteLine == "# METADATA")
                {
                    MigriereSpalten(pfad);              // 7-Spalten-Format aufrüsten
                    return;                             // Format korrekt
                }
            }
            catch { return; }

            // Veraltetes Format: Sicherungskopie anlegen
            try
            {
                string dir    = Path.GetDirectoryName(pfad) ?? "";
                string basis  = Path.GetFileNameWithoutExtension(pfad);
                string backup = Path.Combine(dir,
                    $"{basis}_backup_{DateTime.Now:yyyyMMdd-HHmmss}.csv");
                File.Move(pfad, backup);
                ErrorLogger.Log("MessdatenCSV.SicherstelleDatei",
                    new Exception($"Veraltetes Format gesichert nach: {Path.GetFileName(backup)}"));
            }
            catch { return; }
            // Fällt durch → neue Datei anlegen
        }

        // Neue Datei mit METADATA-Header anlegen
        string sensor = ProjektManager.TachymeterModell == TachymeterModell.Manuell
            ? ""
            : ProjektManager.TachymeterModell.ToString();

        var sb = new StringBuilder();
        sb.AppendLine("# METADATA");
        sb.AppendLine($"Projekt: {ProjektManager.ProjektName}");
        sb.AppendLine($"Sensor: {sensor}");
        sb.AppendLine("Bearbeiter: ");
        sb.AppendLine($"Datum: {DateTime.Today:yyyy-MM-dd}");
        sb.AppendLine($"StandpunktNr: {standpunktNr}");
        sb.AppendLine($"InstrumentenHoehe: {instrHoehe.ToString("F3", IC)}");
        sb.AppendLine("StandpunktCode: ");
        sb.AppendLine("---");
        sb.AppendLine("# DATENTYP");
        sb.AppendLine("Messdaten");
        sb.AppendLine("# DATEN");
        sb.Append(Spaltenzeile);   // kein AppendLine – AppendZeile hängt \r\n an

        File.WriteAllText(pfad, sb.ToString() + Environment.NewLine, Encoding.UTF8);
    }

    // ── Migration / Rückschreiben ─────────────────────────────────────────────

    /// <summary>
    /// Rüstet eine Datei mit alter 7-Spalten-Zeile auf das 11-Spalten-Format auf
    /// (Sicherungskopie, Spaltenzeile ersetzen, Datenzeilen auffüllen).
    /// Gibt true zurück, wenn die Datei geändert wurde.
    /// </summary>
    public static bool MigriereSpalten(string pfad)
    {
        try
        {
            string[] zeilen = File.ReadAllLines(pfad, Encoding.UTF8);
            int idx = Array.FindIndex(zeilen, z => z.Trim() == SpaltenzeileAlt);
            if (idx < 0) return false;

            string dir    = Path.GetDirectoryName(pfad) ?? "";
            string basis  = Path.GetFileNameWithoutExtension(pfad);
            string backup = Path.Combine(dir, $"{basis}_backup_{DateTime.Now:yyyyMMdd-HHmmss}.csv");
            File.Copy(pfad, backup, overwrite: true);

            zeilen[idx] = Spaltenzeile;
            for (int i = idx + 1; i < zeilen.Length; i++)
            {
                string z = zeilen[i];
                if (string.IsNullOrWhiteSpace(z) || z.TrimStart().StartsWith('#')) continue;
                zeilen[i] = z.TrimEnd() + ZusatzwerteAlteZeile;
            }
            File.WriteAllLines(pfad, zeilen, Encoding.UTF8);
            return true;
        }
        catch (Exception ex)
        {
            ErrorLogger.Log("MessdatenCSV.MigriereSpalten", ex);
            return false;
        }
    }

    /// <summary>
    /// Schreibt geänderte Exzentrizitäten in die angegebenen Datenzeilen (0-basierter Zeilenindex der Datei,
    /// wie von <see cref="PolarMessdatenLeser"/> geliefert).
    /// </summary>
    public static void SchreibeExzentrizitaeten(
        string pfad, IEnumerable<(int ZeilenIndex, double ExzLaengs, double ExzQuer)> aenderungen)
    {
        MigriereSpalten(pfad);
        string[] zeilen = File.ReadAllLines(pfad, Encoding.UTF8);

        foreach (var (index, eL, eQ) in aenderungen)
        {
            if (index < 0 || index >= zeilen.Length) continue;
            var teile = zeilen[index].Split(';').Select(t => t.Trim()).ToList();
            while (teile.Count < SPALTENANZAHL) teile.Add("");
            teile[SPALTE_EXZ_LAENGS] = eL.ToString("F3", IC);
            teile[SPALTE_EXZ_QUER]   = eQ.ToString("F3", IC);
            zeilen[index] = string.Join("; ", teile);
        }
        File.WriteAllLines(pfad, zeilen, Encoding.UTF8);
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
