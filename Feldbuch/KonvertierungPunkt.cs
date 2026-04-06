namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// Datensatz für die Konvertierungstabelle
// Entspricht Muster.csv + Punktcode + Bemerkung
// ──────────────────────────────────────────────────────────────────────────────
public class KonvertierungPunkt
{
    public string PunktNr    { get; set; } = "";
    public string Typ        { get; set; } = "";
    public double R          { get; set; }   // Rechtswert (Easting)
    public double H          { get; set; }   // Hochwert (Northing)
    public double Hoehe      { get; set; }   // Höhe [m]
    public double HZ         { get; set; }   // Horizontalrichtung [gon]
    public double V          { get; set; }   // Zenitwinkel [gon]
    public double Strecke    { get; set; }   // Schrägstrecke [m]
    public double Zielhoehe  { get; set; }   // Zielhöhe [m]
    public string Punktcode  { get; set; } = "";
    public string Bemerkung  { get; set; } = "";
}
