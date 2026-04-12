namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// GnssMessung – Messdatenmodell für GNSS-Empfänger (NMEA 0183 / proprietär).
//
// Hinweis: GeoCOM (Leica TPS1200) unterstützt keine GNSS-Abfragen.
// GNSS-Empfänger senden NMEA 0183 über COM-Port (RS-232 oder BT-SPP).
//
// Relevante NMEA-Sätze:
//   GGA – Position, Höhe, Fixtyp, Satellitenzahl, HDOP
//   GSA – Satellitengeometrie, PDOP, HDOP, VDOP, Fixtyp
//   GST – Positionsgenauigkeit (RMS Lat/Lon/Alt in Metern)
//   VTG – (optional) Kurs und Geschwindigkeit
//
// GNSS-Fixtypen (aus NMEA GGA und GSA):
//   0 = NoFix          – keine Positionslösung
//   1 = GPS            – Standard-GPS (autonome Messung)
//   2 = DGPS           – Differentielle GPS-Korrektur (z.B. SAPOS DGNSS)
//   4 = RTK_Fixed      – RTK mit festem Ambiguity (cm-Genauigkeit)
//   5 = RTK_Float      – RTK mit schwebenden Ambiguities (dm-Genauigkeit)
//
// SAPOS-Dienste (Deutschland):
//   EPS  → DGPS    (≈ ±1 m)
//   HEPS → RTK     (≈ ±1–2 cm)
//   GPPS → (nachbearbeitet, kein Echtzeit-FixTyp)
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>Fix-Typ einer GNSS-Messung.</summary>
public enum GnssFixTyp
{
    NoFix    = 0,
    GPS      = 1,
    DGPS     = 2,   // inkl. SAPOS EPS / EGNOS
    RTK_Fixed = 4,  // SAPOS HEPS (cm-Genauigkeit)
    RTK_Float = 5,
    Unknown  = 9
}

/// <summary>Vollständiger GNSS-Messdatensatz aus NMEA-Empfang.</summary>
public class GnssMessung
{
    // ── Herkunft & Zeit ───────────────────────────────────────────────────────
    public DateTime  Zeitstempel { get; init; } = DateTime.Now;
    /// <summary>Quellformat: "NMEA" oder Gerätebezeichnung</summary>
    public string    Quelle      { get; set; }  = "NMEA";

    // ── Position ──────────────────────────────────────────────────────────────
    /// <summary>Geographische Breite [°, dezimal, positiv = Nord]</summary>
    public double?   Breite_deg  { get; set; }
    /// <summary>Geographische Länge [°, dezimal, positiv = Ost]</summary>
    public double?   Laenge_deg  { get; set; }
    /// <summary>Ellipsoidische Höhe oder Höhe über MSL [m] (aus GGA)</summary>
    public double?   Hoehe_m     { get; set; }

    // ── Genauigkeit ───────────────────────────────────────────────────────────
    /// <summary>Horizontale Genauigkeit RMS [m] (aus NMEA GST)</summary>
    public double?   HorizGenauigkeit_m { get; set; }
    /// <summary>Vertikale Genauigkeit RMS [m] (aus NMEA GST)</summary>
    public double?   VertGenauigkeit_m  { get; set; }
    /// <summary>HDOP – Horizontale Geometriefaktor (aus GGA/GSA)</summary>
    public double?   HDOP        { get; set; }
    /// <summary>PDOP – Positionsgeometriefaktor (aus GSA)</summary>
    public double?   PDOP        { get; set; }

    // ── Fixqualität ───────────────────────────────────────────────────────────
    /// <summary>Fix-Typ als Enum</summary>
    public GnssFixTyp FixTypEnum { get; set; } = GnssFixTyp.NoFix;

    /// <summary>Fix-Typ als lesbarer String für Protokoll.</summary>
    public string FixTyp => FixTypEnum switch
    {
        GnssFixTyp.GPS       => "GPS",
        GnssFixTyp.DGPS      => "DGPS",
        GnssFixTyp.RTK_Fixed => "RTK_Fixed",
        GnssFixTyp.RTK_Float => "RTK_Float",
        GnssFixTyp.NoFix     => "NoFix",
        _                    => "Unknown"
    };

    /// <summary>Anzahl der verwendeten Satelliten (aus GGA)</summary>
    public int?      Satelliten  { get; set; }

    // ── Qualitätsbewertung ────────────────────────────────────────────────────

    /// <summary>True wenn eine verwertbare Position vorliegt.</summary>
    public bool HatPosition => Breite_deg.HasValue && Laenge_deg.HasValue
                             && FixTypEnum != GnssFixTyp.NoFix;

    /// <summary>Lesbarer Qualitätshinweis für Statuszeile.</summary>
    public string QualitaetInfo()
    {
        if (!HatPosition) return "Kein Fix";
        string basis = $"{FixTyp}  Sat={Satelliten?.ToString() ?? "?"}";
        if (HorizGenauigkeit_m.HasValue)
            basis += $"  σH={HorizGenauigkeit_m.Value:F3} m";
        else if (HDOP.HasValue)
            basis += $"  HDOP={HDOP.Value:F1}";
        return basis;
    }
}
