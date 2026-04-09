namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// NeupunktRechner – Berechnung von Neupunktkoordinaten aus polaren Messungen.
//
// Koordinatensystem: Rechtssystem (R = Rechtswert/Easting, H = Hochwert/Northing)
// Winkeleinheit: gon (Neugrad, 0..400, im Uhrzeigersinn)
// Höhenberechnung: aus Schrägstrecke + Zenitwinkel + Instrumentenhöhe − Zielhöhe
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>Rohdaten einer Neupunkt-Messung (für spätere Neuberechnung).</summary>
public class NeupunktRohdaten
{
    public string   PunktNr      { get; set; } = "";
    public string   Code         { get; set; } = "";
    public double   Hz_gon       { get; set; }
    public double   V_gon        { get; set; }
    public double   Strecke_m    { get; set; }
    public double   Zielhoehe_m  { get; set; }
    public DateTime Zeitstempel  { get; set; }
    public string   StandpunktNr { get; set; } = "";
}

/// <summary>Berechnetes Ergebnis einer Neupunktmessung.</summary>
public class NeupunktErgebnis
{
    public string   PunktNr      { get; set; } = "";
    public string   Code         { get; set; } = "";
    public double   R            { get; set; }   // Rechtswert (Easting)
    public double   H            { get; set; }   // Hochwert  (Northing)
    public double   Hoehe        { get; set; }   // Höhe [m]  (0 wenn 2D)
    public bool     Ist3D        { get; set; }
    /// <summary>Orientierte Horizontalrichtung [gon].</summary>
    public double   HzOrientiert { get; set; }
    public double   HzRoh        { get; set; }   // unorientierte Messung
    public double   V_gon        { get; set; }
    public double   Strecke_m    { get; set; }
    public double   Zielhoehe_m  { get; set; }
    public DateTime Zeitstempel  { get; set; }
    public string   StandpunktNr { get; set; } = "";
    /// <summary>Verwendete Orientierung [gon] bei der Berechnung.</summary>
    public double   VerwendeteOrientierung { get; set; }
}

public static class NeupunktRechner
{
    private const double GON2RAD = Math.PI / 200.0;

    // ── Einzelberechnung ─────────────────────────────────────────────────────

    /// <summary>
    /// Berechnet die Koordinaten eines Neupunktes aus einer Polaremessung
    /// und dem aktuellen Stationierungsergebnis.
    /// Gibt null zurück wenn Pflichtdaten fehlen.
    /// </summary>
    public static NeupunktErgebnis? Berechnen(
        NeupunktRohdaten rohdaten,
        StationierungsErgebnis station)
    {
        if (rohdaten.Strecke_m <= 0) return null;

        double hz      = rohdaten.Hz_gon;
        double v       = rohdaten.V_gon;
        double strecke = rohdaten.Strecke_m;
        double zh      = rohdaten.Zielhoehe_m;

        // Orientierte Richtung [gon, 0..400]
        double hzOri = (hz + station.Orientierung_gon) % 400.0;
        if (hzOri < 0) hzOri += 400.0;

        // Horizontaldistanz: D_h = S * sin(V)
        double dHoriz = strecke * Math.Sin(v * GON2RAD);

        // Lagekoordinaten (Rechtssystem: R = E, H = N)
        double dR = dHoriz * Math.Sin(hzOri * GON2RAD);
        double dH = dHoriz * Math.Cos(hzOri * GON2RAD);

        double r = station.R + dR;
        double h = station.H + dH;

        // Höhe (nur bei 3D-Stationierung mit gültiger Standpunkthöhe)
        bool ist3D  = station.Berechnung3D && station.Hoehe != 0;
        double hoehe = 0.0;
        if (ist3D)
        {
            // Δh_Instr_zu_Ziel = S * cos(V) + ih − zh
            double instrHoehe = StationsdatenManager.InstrumentenHoehe;
            double dHoehe     = strecke * Math.Cos(v * GON2RAD) + instrHoehe - zh;
            hoehe             = station.Hoehe + dHoehe;
        }

        return new NeupunktErgebnis
        {
            PunktNr               = rohdaten.PunktNr,
            Code                  = rohdaten.Code,
            R                     = r,
            H                     = h,
            Hoehe                 = hoehe,
            Ist3D                 = ist3D,
            HzOrientiert          = hzOri,
            HzRoh                 = hz,
            V_gon                 = v,
            Strecke_m             = strecke,
            Zielhoehe_m           = zh,
            Zeitstempel           = rohdaten.Zeitstempel,
            StandpunktNr          = rohdaten.StandpunktNr,
            VerwendeteOrientierung = station.Orientierung_gon
        };
    }

    /// <summary>
    /// Berechnet aus einer TachymeterMessung direkt ein Ergebnis
    /// (ohne vorherige Rohdaten-Zwischenstufe).
    /// </summary>
    public static NeupunktErgebnis? BerechnenAusMess(
        TachymeterMessung messung,
        StationierungsErgebnis station,
        string standpunktNr,
        string punktNr,
        string code)
    {
        if (!messung.Hz_gon.HasValue || !messung.V_gon.HasValue ||
            !messung.Schraegstrecke_m.HasValue)
            return null;

        var rohdaten = new NeupunktRohdaten
        {
            PunktNr      = punktNr,
            Code         = code,
            Hz_gon       = messung.Hz_gon.Value,
            V_gon        = messung.V_gon.Value,
            Strecke_m    = messung.Schraegstrecke_m.Value,
            Zielhoehe_m  = messung.Zielhoehe_m ?? 0.0,
            Zeitstempel  = messung.Zeitstempel,
            StandpunktNr = standpunktNr
        };

        return Berechnen(rohdaten, station);
    }

    // ── Massenberechnung (Neuberechnung nach Stationierungsänderung) ──────────

    /// <summary>
    /// Berechnet alle Neupunkte eines Standpunktes mit aktuellen Stationierungsdaten neu.
    /// </summary>
    public static List<NeupunktErgebnis> BerechneAlle(
        IEnumerable<NeupunktRohdaten> rohdaten,
        StationierungsErgebnis station,
        string standpunktNr)
    {
        var ergebnisse = new List<NeupunktErgebnis>();
        foreach (var rd in rohdaten.Where(r => r.StandpunktNr == standpunktNr))
        {
            var e = Berechnen(rd, station);
            if (e != null) ergebnisse.Add(e);
        }
        return ergebnisse;
    }
}
