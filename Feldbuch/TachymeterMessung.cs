namespace Feldbuch;

// ══════════════════════════════════════════════════════════════════════════════
// TachymeterMessung  –  universelles Messdaten-Modell
//
// Dieses Modell nimmt alle Messwerte auf, die von irgendeinem Tachymeter-
// Format (GeoCOM, GSI-8, GSI-16, …) geliefert werden können.
// Nicht belegte Felder bleiben null.
//
// Interne Einheiten (einheitlich für alle Formate):
//   Winkel       → gon  (Neugrad, 0..400)
//   Strecken     → m    (Meter)
//   Prismenkons. → mm   (Millimeter)
//   Koordinaten  → m    (Meter)
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>Art der Messung / des Datensatzes.</summary>
public enum MessungsTyp
{
    Unbekannt,
    Winkel,         // nur Hz + V (keine Distanz)
    Vollmessung,    // Hz + V + Schrägdistanz
    Koordinate,     // direkte XYZ-Koordinaten
    Status,         // Gerätestatus, Bestätigung, Ping-Antwort
    EdmModusInfo,   // EDM-Modus-Rückmeldung
    AtmKorrektur,   // Atmosphärische Korrekturdaten (Lambda, Druck, Temp, PPM)
    Fehler          // Fehlermeldung / ungültiger Returncode
}

/// <summary>EDM-Messmodus (Reflektortyp).</summary>
public enum MessungsEdmModus
{
    Unbekannt,
    Prisma,         // Standardprisma / Reflektor
    Folie,          // Reflektorfolie / Kleintarget
    Reflektorlos    // RL – kein Reflektor
}

/// <summary>
/// Vollständiger Messdatensatz, unabhängig vom Quellformat.
/// Alle optionalen Felder sind nullable; nur vorhandene Werte werden gesetzt.
/// </summary>
public class TachymeterMessung
{
    // ── Herkunft & Zeit ───────────────────────────────────────────────────────
    /// <summary>Zeitstempel der Verarbeitung.</summary>
    public DateTime      Zeitstempel        { get; init; } = DateTime.Now;
    /// <summary>Quellformat: "GeoCOM", "GSI-8", "GSI-16", …</summary>
    public string        Quelle             { get; set; }  = "";
    /// <summary>Unveränderter Rohdatenstring (für Debugging).</summary>
    public string        Rohdaten           { get; set; }  = "";
    /// <summary>Art der enthaltenen Messdaten.</summary>
    public MessungsTyp   Typ                { get; set; }  = MessungsTyp.Unbekannt;

    // ── Punktidentifikation ───────────────────────────────────────────────────
    public string        PunktNr            { get; set; }  = "";
    public string        Punktcode          { get; set; }  = "";

    // ── Richtungswinkel ───────────────────────────────────────────────────────
    /// <summary>Horizontalrichtung [gon, 0..400].</summary>
    public double?       Hz_gon             { get; set; }
    /// <summary>Zenitwinkel [gon, 0=Zenit, 100=horizontal, 200=Nadir].</summary>
    public double?       V_gon              { get; set; }

    // ── Strecken ──────────────────────────────────────────────────────────────
    /// <summary>Schrägdistanz [m].</summary>
    public double?       Schraegstrecke_m   { get; set; }
    /// <summary>Horizontaldistanz [m].</summary>
    public double?       Horizontalstrecke_m{ get; set; }
    /// <summary>Höhenunterschied Δh [m].</summary>
    public double?       Hoehenunterschied_m{ get; set; }

    // ── Koordinaten Zielpunkt ─────────────────────────────────────────────────
    public double?       E_m                { get; set; }   // Rechtswert
    public double?       N_m                { get; set; }   // Hochwert
    public double?       H_m                { get; set; }   // Höhe

    // ── Standpunkt ────────────────────────────────────────────────────────────
    public double?       StandpunktE_m      { get; set; }
    public double?       StandpunktN_m      { get; set; }
    public double?       StandpunktH_m      { get; set; }

    // ── Aufstellungsparameter ─────────────────────────────────────────────────
    /// <summary>Zielhöhe (Reflektorhöhe) [m].</summary>
    public double?       Zielhoehe_m        { get; set; }
    /// <summary>Instrumenthöhe [m].</summary>
    public double?       Instrumenthoehe_m  { get; set; }
    /// <summary>Prismenkonstante [mm].</summary>
    public double?       Prismenkonstante_mm{ get; set; }

    // ── Gerätestatus ──────────────────────────────────────────────────────────
    /// <summary>Aktueller EDM-Modus.</summary>
    public MessungsEdmModus EdmModus        { get; set; }  = MessungsEdmModus.Unbekannt;
    /// <summary>GeoCOM Returncode (GRC), 0 = OK.</summary>
    public int?          ReturnCode         { get; set; }
    /// <summary>Roher EDM-Moduswert (z. B. GeoCOM-Ganzzahl).</summary>
    public int?          EdmModusRoh        { get; set; }

    // ── Qualitätskennzahlen ───────────────────────────────────────────────────
    /// <summary>Winkelgenauigkeit [cc = Zentigrade].</summary>
    public double?       WinkelGenauigkeit_cc   { get; set; }
    /// <summary>Streckengenauigkeit [mm].</summary>
    public double?       StreckenGenauigkeit_mm { get; set; }

    // ── Kompensatordaten (GeoCOM TMC_GetAngle1, RPC 2003) ─────────────────────
    /// <summary>Querachsenneigung (CrossIncline) [rad].</summary>
    public double?       KreuzNeigung_rad       { get; set; }
    /// <summary>Längsachsenneigung (LengthIncline) [rad].</summary>
    public double?       LaengsNeigung_rad      { get; set; }

    // ── Atmosphärische Korrekturdaten (GeoCOM TMC_GetAtmCorr, RPC 2029) ───────
    /// <summary>Wellenlänge des EDM [m] (z. B. 6.58e-7 für roten Laser).</summary>
    public double?       Atm_Lambda_m           { get; set; }
    /// <summary>Luftdruck [mbar].</summary>
    public double?       Atm_Druck_mbar         { get; set; }
    /// <summary>Trockentemperatur [°C].</summary>
    public double?       Atm_TempTrock_C        { get; set; }
    /// <summary>Feuchttemperatur [°C].</summary>
    public double?       Atm_TempFeucht_C       { get; set; }
    /// <summary>Berechneter PPM-Wert (Barrel-Formel). Positiv = Verkürzt.</summary>
    public double?       Atm_PPM                { get; set; }

    // ── Sonstiges ─────────────────────────────────────────────────────────────
    public string        Bemerkung          { get; set; }  = "";

    // ── Abgeleitete Eigenschaften ─────────────────────────────────────────────
    /// <summary>True wenn ReturnCode vorhanden und != 0.</summary>
    public bool IstFehler => ReturnCode.HasValue && ReturnCode.Value != 0;

    /// <summary>Gibt an ob Winkeldaten vorhanden sind.</summary>
    public bool HatWinkel => Hz_gon.HasValue || V_gon.HasValue;

    /// <summary>Gibt an ob eine vollständige Polar-Messung vorliegt.</summary>
    public bool IstVollmessung => Hz_gon.HasValue && V_gon.HasValue && Schraegstrecke_m.HasValue;

    // ── Konvertierung → KonvertierungPunkt (Kompatibilität mit bestehendem Code) ──
    /// <summary>Wandelt die Messung in einen KonvertierungPunkt um (für Import/Export).</summary>
    public KonvertierungPunkt ZuKonvertierungPunkt() => new()
    {
        PunktNr   = PunktNr,
        Typ       = Quelle,
        R         = E_m          ?? 0,
        H         = N_m          ?? 0,
        Hoehe     = H_m          ?? 0,
        HZ        = Hz_gon       ?? 0,
        V         = V_gon        ?? 0,
        Strecke   = Schraegstrecke_m ?? 0,
        Zielhoehe = Zielhoehe_m  ?? 0,
        Punktcode = Punktcode,
        Bemerkung = Bemerkung
    };

    // ── Lesbare Darstellung ───────────────────────────────────────────────────
    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"[{Quelle}] {Typ}");
        if (!string.IsNullOrEmpty(PunktNr)) sb.Append($"  Pkt:{PunktNr}");
        if (Hz_gon.HasValue)               sb.Append($"  Hz:{Hz_gon.Value:F4} gon");
        if (V_gon.HasValue)                sb.Append($"  V:{V_gon.Value:F4} gon");
        if (Schraegstrecke_m.HasValue)     sb.Append($"  D:{Schraegstrecke_m.Value:F3} m");
        if (Horizontalstrecke_m.HasValue)  sb.Append($"  Dh:{Horizontalstrecke_m.Value:F3} m");
        if (Hoehenunterschied_m.HasValue)  sb.Append($"  Δh:{Hoehenunterschied_m.Value:F3} m");
        if (E_m.HasValue)                  sb.Append($"  E:{E_m.Value:F3}");
        if (N_m.HasValue)                  sb.Append($"  N:{N_m.Value:F3}");
        if (H_m.HasValue)                  sb.Append($"  H:{H_m.Value:F3}");
        if (Prismenkonstante_mm.HasValue)  sb.Append($"  PC:{Prismenkonstante_mm.Value:F1} mm");
        if (EdmModus != MessungsEdmModus.Unbekannt) sb.Append($"  EDM:{EdmModus}");
        if (IstFehler)                     sb.Append($"  !!GRC={ReturnCode}");
        if (!string.IsNullOrEmpty(Bemerkung)) sb.Append($"  ({Bemerkung})");
        return sb.ToString();
    }
}
