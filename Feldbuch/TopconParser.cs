namespace Feldbuch;

using System.Globalization;

// ══════════════════════════════════════════════════════════════════════════════
// TopconParser  –  Parser für Topcon GTS/GPT-Tachymeter-Ausgabe
//
// Topcon GTS/GPT-Reihe gibt nach Messtrigger (CR) folgende Zeilen aus:
//   Variante A (Fernsteuerungsmodus, alle Werte):
//     "SS-H:ddd.dddd V:ddd.dddd D: ddddd.dddd"
//     "H:" = Hz-Winkel (Grad dezimal)
//     "V:" = Vertikal-Winkel (Grad dezimal, zentrisch = 90°)
//     "D:" = Schrägdistanz (m)
//   Variante B (nur Winkel):
//     "H ddd.dddd V ddd.dddd"
//
// Winkel: Grad (dezimal) – wird in Gon umgerechnet.
// Strecke: Meter.
// ══════════════════════════════════════════════════════════════════════════════
public class TopconParser : ITachymeterDatenParser
{
    public string FormatName        => "Topcon";
    public string FormatBeschreibung => "Topcon GTS/GPT-Reihe – Fernsteuerungsformat (CR-Trigger)";

    public bool KannVerarbeiten(string zeile)
    {
        if (string.IsNullOrWhiteSpace(zeile)) return false;
        var z = zeile.TrimStart();
        // Variante A: beginnt mit "SS-"
        if (z.StartsWith("SS-", StringComparison.OrdinalIgnoreCase)) return true;
        // Variante B/C: enthält "H:" und "V:"
        if (z.Contains("H:", StringComparison.OrdinalIgnoreCase)
            && z.Contains("V:", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    public TachymeterMessung? ParseZeile(string zeile)
    {
        if (!KannVerarbeiten(zeile)) return null;
        try
        {
            double? hzDeg = ExtrahiereWert(zeile, "H:");
            double? vDeg  = ExtrahiereWert(zeile, "V:");
            double? sd    = ExtrahiereWert(zeile, "D:");

            if (!hzDeg.HasValue && !vDeg.HasValue) return null;

            // Grad → Gon
            double? hzGon = hzDeg.HasValue ? hzDeg.Value * 400.0 / 360.0 : null;
            double? vGon  = vDeg.HasValue  ? vDeg.Value  * 400.0 / 360.0 : null;

            return new TachymeterMessung
            {
                Quelle           = "Topcon",
                Rohdaten         = zeile,
                Typ              = sd.HasValue ? MessungsTyp.Vollmessung : MessungsTyp.Winkel,
                Hz_gon           = hzGon,
                V_gon            = vGon,
                Schraegstrecke_m = sd
            };
        }
        catch { return null; }
    }

    public IEnumerable<TachymeterMessung> ParseMehrere(IEnumerable<string> zeilen)
    {
        foreach (var z in zeilen)
        {
            var m = ParseZeile(z);
            if (m != null) yield return m;
        }
    }

    // ── Hilfsmethode: Wert nach Schlüssel extrahieren ─────────────────────────
    private static double? ExtrahiereWert(string zeile, string schluessel)
    {
        int idx = zeile.IndexOf(schluessel, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;

        int start = idx + schluessel.Length;
        while (start < zeile.Length && zeile[start] == ' ') start++;

        int end = start;
        while (end < zeile.Length &&
               (char.IsDigit(zeile[end]) || zeile[end] == '.' || zeile[end] == '-' || zeile[end] == '+'))
            end++;

        if (end <= start) return null;
        return double.TryParse(zeile[start..end], NumberStyles.Float,
            CultureInfo.InvariantCulture, out var v) ? v : null;
    }
}
