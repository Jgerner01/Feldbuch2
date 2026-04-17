namespace Feldbuch;

using System.Globalization;

// ══════════════════════════════════════════════════════════════════════════════
// SokkiaSDRParser  –  Parser für Sokkia SDR33-Format (SET-Tachymeter-Reihe)
//
// Das SDR33-Format ist ein ASCII-Format mit festen Feldlängen:
//   Recordtyp (2 Zeichen) + Sequenznr (4 Zeichen) + Datenfelder
//
//   Recordtyp 00: Job-Header (Datum, Uhrzeit, Instrumentname)
//   Recordtyp 02: Winkel + Strecke (Hz, V, SD, [HD, dH])
//   Recordtyp 08: Koordinaten (N, E, Z + Punktname)
//   Recordtyp 10: Punktname
//   Recordtyp 13: Messparameter (Prismenkonstante, Zielhöhe, etc.)
//
// Winkeleinheit: Grad (dezimal) – wird in Gon umgerechnet
// Streckeneinheit: Meter
// ══════════════════════════════════════════════════════════════════════════════
public class SokkiaSDRParser : ITachymeterDatenParser
{
    public string FormatName        => "Sokkia SDR";
    public string FormatBeschreibung => "Sokkia SDR33-Format (SET-Reihe) – ASCII-Festfelder";

    public bool KannVerarbeiten(string zeile)
    {
        if (string.IsNullOrEmpty(zeile) || zeile.Length < 6) return false;
        var typ = zeile.Length >= 2 ? zeile[..2] : "";
        return typ is "00" or "02" or "08" or "10" or "13";
    }

    public TachymeterMessung? ParseZeile(string zeile)
    {
        if (string.IsNullOrWhiteSpace(zeile) || zeile.Length < 2) return null;
        var typ = zeile[..2];
        return typ switch
        {
            "02" => ParseRecord02(zeile),
            "08" => ParseRecord08(zeile),
            "00" => ParseRecord00(zeile),
            "13" => ParseRecord13(zeile),
            _    => null
        };
    }

    public IEnumerable<TachymeterMessung> ParseMehrere(IEnumerable<string> zeilen)
    {
        foreach (var z in zeilen)
        {
            var m = ParseZeile(z);
            if (m != null) yield return m;
        }
    }

    // ── Record 02: Winkel + Strecke ───────────────────────────────────────────
    // Pos  0-1:  RecordTyp "02"
    // Pos  2-5:  Sequenznummer (4 Ziffern)
    // Pos  6-15: Hz-Winkel (±ddd.ddddd, Grad dezimal)
    // Pos 16-25: V-Winkel  (±ddd.ddddd, Grad dezimal)
    // Pos 26-35: Schrägdistanz (±ddddd.dddd, m) – leer wenn nur Winkel
    // Pos 36-45: Horizontaldistanz (optional)
    // Pos 46-55: Höhenunterschied (optional)
    private static TachymeterMessung? ParseRecord02(string zeile)
    {
        if (zeile.Length < 16) return null;
        try
        {
            double? hzDeg = ParseFeld(zeile, 6, 10);
            double? vDeg  = ParseFeld(zeile, 16, 10);
            double? sd    = zeile.Length >= 36 ? ParseFeld(zeile, 26, 10) : null;
            double? hd    = zeile.Length >= 46 ? ParseFeld(zeile, 36, 10) : null;
            double? dh    = zeile.Length >= 56 ? ParseFeld(zeile, 46, 10) : null;

            if (!hzDeg.HasValue && !vDeg.HasValue) return null;

            // Grad → Gon (1° = 400/360 gon)
            double? hzGon = hzDeg.HasValue ? hzDeg.Value * 400.0 / 360.0 : null;
            double? vGon  = vDeg.HasValue  ? vDeg.Value  * 400.0 / 360.0 : null;

            return new TachymeterMessung
            {
                Quelle              = "Sokkia SDR",
                Rohdaten            = zeile,
                Typ                 = sd.HasValue ? MessungsTyp.Vollmessung : MessungsTyp.Winkel,
                Hz_gon              = hzGon,
                V_gon               = vGon,
                Schraegstrecke_m    = sd,
                Horizontalstrecke_m = hd,
                Hoehenunterschied_m = dh
            };
        }
        catch { return null; }
    }

    // ── Record 08: Koordinaten ────────────────────────────────────────────────
    // Enthält Punktnummer gefolgt von drei Koordinatenfeldern.
    // Format kann je nach SDR-Softwareversion variieren → flexibles Parsen.
    private static TachymeterMessung? ParseRecord08(string zeile)
    {
        if (zeile.Length < 12) return null;
        try
        {
            // Felder ab Position 6 (nach RecType + SeqNr) durch Whitespace trennen
            var felder = zeile[6..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (felder.Length < 3) return null;

            // Letzten 3 Felder sind immer N, E, Z
            if (!double.TryParse(felder[^3], NumberStyles.Float, CultureInfo.InvariantCulture, out var n)) return null;
            if (!double.TryParse(felder[^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var e)) return null;
            if (!double.TryParse(felder[^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var h)) return null;

            string punktNr = felder.Length > 3 ? felder[0] : "";

            return new TachymeterMessung
            {
                Quelle   = "Sokkia SDR",
                Rohdaten = zeile,
                Typ      = MessungsTyp.Koordinate,
                PunktNr  = punktNr,
                N_m      = n,
                E_m      = e,
                H_m      = h
            };
        }
        catch { return null; }
    }

    // ── Record 00: Job-Header ─────────────────────────────────────────────────
    private static TachymeterMessung ParseRecord00(string zeile) =>
        new()
        {
            Quelle    = "Sokkia SDR",
            Rohdaten  = zeile,
            Typ       = MessungsTyp.Status,
            Bemerkung = zeile.Length > 6 ? zeile[6..].Trim() : "Job-Header"
        };

    // ── Record 13: Messparameter ──────────────────────────────────────────────
    // Enthält Prismenkonstante, Zielhöhe, Atmosphärische Korrektur
    private static TachymeterMessung? ParseRecord13(string zeile)
    {
        // Flexibles Parsen: Werte nach Position 6 extrahieren
        if (zeile.Length < 7) return null;
        return new TachymeterMessung
        {
            Quelle    = "Sokkia SDR",
            Rohdaten  = zeile,
            Typ       = MessungsTyp.Status,
            Bemerkung = $"Messparameter: {zeile[6..].Trim()}"
        };
    }

    // ── Hilfsmethode: Festes Feld parsen ──────────────────────────────────────
    private static double? ParseFeld(string zeile, int start, int laenge)
    {
        if (zeile.Length < start + laenge) return null;
        var s = zeile.Substring(start, laenge).Trim();
        if (string.IsNullOrEmpty(s)) return null;
        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
    }
}
