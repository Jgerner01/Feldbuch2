namespace Feldbuch;

using System.Globalization;
using System.Text;

// ──────────────────────────────────────────────────────────────────────────────
// MessdatenProtokoll – Lückenloses Tagesprotokoll aller Messungen.
//
// Dateiname:  Data-protocol-{yyyy-MM-dd}.csv  im Projektverzeichnis.
// Verhalten:  Append-only – niemals überschrieben.
//             Datumswechsel → neue Datei automatisch.
//             Jede Messung wird sofort gespeichert, unabhängig vom Ergebnis.
//
// Tachymeter-Spalten:
//   Timestamp, StationNr, InstrHeight_m, PointNr, Code, Method,
//   EDM_Mode, PrismConst_mm, Atm_PPM, Pressure_mbar, Temp_C,
//   CrossInclination_mgon, LengthInclination_mgon, Compensator_OK,
//   Hz_gon, V_gon, SlopeDist_m, TargetHeight_m, Source
//
// GNSS-Spalten (leer bei Tachymeter-Messung):
//   GNSS_Latitude, GNSS_Longitude, GNSS_Height_m,
//   GNSS_HorizAccuracy_m, GNSS_VertAccuracy_m,
//   GNSS_FixType, GNSS_Satellites, GNSS_HDOP, GNSS_PDOP
//
// FixType: "NoFix" | "GPS" | "DGPS" | "RTK_Fixed" | "RTK_Float" | "SAPOS"
// ──────────────────────────────────────────────────────────────────────────────
public static class MessdatenProtokoll
{
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    // Kompensator-Messbereich TPS1200: ±10' ≈ ±185 mgon
    private const double KompMaxMgon = 200.0;

    private const string Spaltenzeile =
        // Allgemein
        "Timestamp; StationNr; InstrHeight_m; PointNr; Code; Method; " +
        // Tachymeter
        "EDM_Mode; PrismConst_mm; Atm_PPM; Pressure_mbar; Temp_C; " +
        "CrossInclination_mgon; LengthInclination_mgon; Compensator_OK; " +
        "Hz_gon; V_gon; SlopeDist_m; TargetHeight_m; Source; " +
        // GNSS
        "GNSS_Latitude; GNSS_Longitude; GNSS_Height_m; " +
        "GNSS_HorizAccuracy_m; GNSS_VertAccuracy_m; " +
        "GNSS_FixType; GNSS_Satellites; GNSS_HDOP; GNSS_PDOP";

    // ── Tachymeter-Messung ────────────────────────────────────────────────────

    /// <summary>
    /// Schreibt einen Tachymeter-Messeintrag ins Tagesprotokoll.
    /// GNSS-Spalten bleiben leer.
    /// </summary>
    public static void Schreibe(
        string             messmethode,
        string             standpunktNr,
        double             instrHoehe,
        string             punktNr,
        string             code,
        TachymeterMessung  messung)
    {
        try
        {
            string pfad = TagespfadFuer(messung.Zeitstempel);
            SicherstelleDatei(pfad, messung.Zeitstempel);

            string zeile = string.Join("; ",
                // Allgemein
                messung.Zeitstempel.ToString("yyyy-MM-dd HH:mm:ss"),
                standpunktNr,
                instrHoehe.ToString("F3", IC),
                punktNr,
                code,
                messmethode,
                // Tachymeter
                FormatEdmModus(messung.EdmModus),
                FormatNullable(messung.Prismenkonstante_mm, "F1"),
                FormatNullable(messung.Atm_PPM,             "F2"),
                FormatNullable(messung.Atm_Druck_mbar,      "F1"),
                FormatNullable(messung.Atm_TempTrock_C,     "F1"),
                FormatNeigung(messung.KreuzNeigung_rad),
                FormatNeigung(messung.LaengsNeigung_rad),
                FormatKompensatorOK(messung.KreuzNeigung_rad, messung.LaengsNeigung_rad),
                FormatNullable(messung.Hz_gon,              "F4"),
                FormatNullable(messung.V_gon,               "F4"),
                FormatNullable(messung.Schraegstrecke_m,    "F3"),
                FormatNullable(messung.Zielhoehe_m,         "F3"),
                messung.Quelle,
                // GNSS – leer bei Tachymeter
                "", "", "", "", "", "", "", "", "");

            File.AppendAllText(pfad, zeile + Environment.NewLine, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            ErrorLogger.Log("MessdatenProtokoll.Schreibe", ex);
        }
    }

    // ── GNSS-Messung ──────────────────────────────────────────────────────────

    /// <summary>
    /// Schreibt einen GNSS-Messeintrag ins Tagesprotokoll.
    /// Tachymeter-Spalten bleiben leer.
    /// </summary>
    public static void SchreibeGnss(
        string      standpunktNr,
        string      punktNr,
        string      code,
        GnssMessung gnss)
    {
        // Nur speichern wenn als GNSS-Gerät konfiguriert
        if (!ProjektManager.IstGnssGeraet) return;

        try
        {
            string pfad = TagespfadFuer(gnss.Zeitstempel);
            SicherstelleDatei(pfad, gnss.Zeitstempel);

            string zeile = string.Join("; ",
                // Allgemein
                gnss.Zeitstempel.ToString("yyyy-MM-dd HH:mm:ss"),
                standpunktNr,
                "",           // InstrHeight – bei GNSS nicht relevant
                punktNr,
                code,
                "GNSS",
                // Tachymeter – leer
                "", "", "", "", "", "", "", "", "", "", "", "", "",
                gnss.Quelle,
                // GNSS
                FormatNullable(gnss.Breite_deg,       "F8"),
                FormatNullable(gnss.Laenge_deg,        "F8"),
                FormatNullable(gnss.Hoehe_m,           "F3"),
                FormatNullable(gnss.HorizGenauigkeit_m,"F3"),
                FormatNullable(gnss.VertGenauigkeit_m, "F3"),
                gnss.FixTyp,
                gnss.Satelliten.HasValue
                    ? gnss.Satelliten.Value.ToString(IC) : "",
                FormatNullable(gnss.HDOP, "F2"),
                FormatNullable(gnss.PDOP, "F2"));

            File.AppendAllText(pfad, zeile + Environment.NewLine, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            ErrorLogger.Log("MessdatenProtokoll.SchreibeGnss", ex);
        }
    }

    // ── Interne Helfer ────────────────────────────────────────────────────────

    private static string TagespfadFuer(DateTime zeitstempel) =>
        ProjektManager.GetPfad($"Data-protocol-{zeitstempel:yyyy-MM-dd}.csv");

    private static void SicherstelleDatei(string pfad, DateTime zeitstempel)
    {
        if (File.Exists(pfad)) return;

        var sb = new StringBuilder();
        sb.AppendLine($"# Data Protocol: {ProjektManager.ProjektName}");
        sb.AppendLine($"# Date: {zeitstempel:yyyy-MM-dd}");
        sb.AppendLine("# This file is created daily and never overwritten.");
        sb.AppendLine("# All measurements are recorded without gaps.");
        sb.Append(Spaltenzeile);

        File.WriteAllText(pfad, sb.ToString() + Environment.NewLine, Encoding.UTF8);
    }

    // ── Formatierungshilfen ───────────────────────────────────────────────────

    private static string FormatNullable(double? wert, string format) =>
        wert.HasValue ? wert.Value.ToString(format, IC) : "";

    private static string FormatEdmModus(MessungsEdmModus modus) => modus switch
    {
        MessungsEdmModus.Prisma       => "Prisma",
        MessungsEdmModus.Folie        => "Folie",
        MessungsEdmModus.Reflektorlos => "Reflektorlos",
        _                             => ""
    };

    private static string FormatNeigung(double? rad)
    {
        if (!rad.HasValue) return "";
        double mgon = rad.Value * 200000.0 / Math.PI;
        return mgon.ToString("F1", IC);
    }

    private static string FormatKompensatorOK(double? kreuz, double? laengs)
    {
        if (!kreuz.HasValue || !laengs.HasValue) return "";
        double kreuzMgon  = Math.Abs(kreuz.Value  * 200000.0 / Math.PI);
        double laengsMgon = Math.Abs(laengs.Value * 200000.0 / Math.PI);
        return (kreuzMgon <= KompMaxMgon && laengsMgon <= KompMaxMgon) ? "Yes" : "No";
    }
}
