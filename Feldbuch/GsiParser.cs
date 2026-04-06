namespace Feldbuch;

using System.Globalization;
using System.Text;

// ──────────────────────────────────────────────────────────────────────────────
// GSI-Parser  – Leica GEO Serial Interface (GSI-8 und GSI-16)
//
// Referenz: https://totalopenstation.readthedocs.io/en/latest/input_formats/if_leica_gsi.html
//
// Blockstruktur pro Token:
//   GSI-8 : WI(2) + Info(4) + Sign(1) + Data(8)  = 15 Zeichen
//   GSI-16: WI(2) + Info(4) + Sign(1) + Data(16) = 23 Zeichen
//
// Word-Index (WI) Codes:
//   11       Punktnummer (alphanumerisch)
//   21       Horizontalrichtung Hz [gon]
//   22       Zenitwinkel V [gon]
//   31       Schrägstrecke [m]
//   32       Horizontalstrecke [m]
//   33       Höhenunterschied [m]
//   41-49    Code/Kommentar-Felder
//   71-79    Attribute
//   81       Rechtswert (Easting) [m]
//   82       Hochwert (Northing) [m]
//   83       Höhe (Elevation) [m]
//   84       Standpunkt Rechtswert [m]
//   85       Standpunkt Hochwert [m]
//   86       Standpunkt Höhe [m]
//   87       Zielhöhe [m]
//   88       Instrumenthöhe [m]
//
// Einheiten-Code (letztes Zeichen des Info-Felds):
//   0  Meter, ÷1000      (1mm)
//   1  Fuß,   ÷1000
//   2  Gon,   ÷100000
//   3  Grad dezimal, ÷100000
//   4  Grad DMS (Sonderfall)
//   5  Mil,   ÷100000
//   6  Meter, ÷10000     (0,1mm)
//   7  Fuß,   ÷10000
//   8  Meter, ÷100000    (0,01mm)
// ──────────────────────────────────────────────────────────────────────────────
public static class GsiParser
{
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    public static List<KonvertierungPunkt> Parse(string path)
    {
        var lines = File.ReadAllLines(path, Encoding.Latin1);
        var result = new List<KonvertierungPunkt>();

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var punkt = ParseLine(line);
            if (punkt != null)
                result.Add(punkt);
        }

        return result;
    }

    private static KonvertierungPunkt? ParseLine(string line)
    {
        // Token durch Leerzeichen trennen
        var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return null;

        // GSI-8 (15 Zeichen) oder GSI-16 (23 Zeichen) erkennen
        bool isGsi16 = tokens.Any(t => t.Length >= 23);

        var p = new KonvertierungPunkt();
        var bemerkungParts = new List<string>();

        foreach (var token in tokens)
        {
            if (token.Length < 15) continue;   // zu kurz → kein gültiger Block

            string wiStr  = token.Substring(0, 2);
            string info   = token.Substring(2, 4);
            char   sign   = token[6];
            string data   = token.Substring(7);   // 8 oder 16 Zeichen

            if (!int.TryParse(wiStr, out int wi)) continue;

            char unitCode = info[3];   // letztes Zeichen = Einheitencode

            // ── WI 11: Punktnummer ────────────────────────────────────────────
            if (wi == 11)
            {
                // alphanumerische Punktnummer: führende Nullen entfernen, aber Text erhalten
                string raw = data.TrimEnd();
                // Versuche als Ganzzahl → keine führenden Nullen
                if (long.TryParse(raw, out long pnNum))
                    p.PunktNr = pnNum.ToString();
                else
                    p.PunktNr = raw.TrimStart('0');
                continue;
            }

            // ── Numerischen Wert dekodieren ───────────────────────────────────
            if (!long.TryParse(data.Trim(), out long rawValue)) continue;
            if (sign == '-') rawValue = -rawValue;

            double value = Dekodiere(rawValue, unitCode);

            switch (wi)
            {
                case 21: p.HZ       = value; break;
                case 22: p.V        = value; break;
                case 31: p.Strecke  = value; break;
                case 32: p.Strecke  = p.Strecke == 0 ? value : p.Strecke; break;
                case 33: bemerkungParts.Add($"Δh={value:F3}m"); break;
                case 81: p.R        = value; break;
                case 82: p.H        = value; break;
                case 83: p.Hoehe    = value; break;
                case 84: bemerkungParts.Add($"STP-R={value:F3}m"); break;
                case 85: bemerkungParts.Add($"STP-H={value:F3}m"); break;
                case 86: bemerkungParts.Add($"STP-Hoehe={value:F3}m"); break;
                case 87: p.Zielhoehe = value; break;
                case 88: bemerkungParts.Add($"Inst-h={value:F3}m"); break;
                case int c when c >= 41 && c <= 49:
                    // Code/Kommentar: ersten Codeblock als Punktcode, Rest als Bemerkung
                    string codeText = data.TrimEnd().TrimStart('0');
                    if (wi == 41 && string.IsNullOrEmpty(p.Punktcode))
                        p.Punktcode = codeText;
                    else if (!string.IsNullOrEmpty(codeText))
                        bemerkungParts.Add($"WI{wi}={codeText}");
                    break;
                case int c when c >= 71 && c <= 79:
                    string attrText = data.TrimEnd().TrimStart('0');
                    if (!string.IsNullOrEmpty(attrText))
                        bemerkungParts.Add($"Attr{wi}={attrText}");
                    break;
            }
        }

        if (bemerkungParts.Count > 0)
            p.Bemerkung = string.Join(", ", bemerkungParts);

        p.Typ = "Import-GSI";

        // Zeile nur übernehmen wenn mindestens Koordinaten oder Messungen vorhanden
        bool hatKoord   = p.R != 0 || p.H != 0;
        bool hatMessung = p.HZ != 0 || p.V != 0 || p.Strecke != 0;
        if (!hatKoord && !hatMessung && string.IsNullOrEmpty(p.PunktNr))
            return null;

        return p;
    }

    private static double Dekodiere(long rawValue, char unitCode) => unitCode switch
    {
        '0' => rawValue / 1000.0,        // Meter,  1mm
        '1' => rawValue / 1000.0,        // Fuß,    1/1000 ft
        '2' => rawValue / 100000.0,      // Gon
        '3' => rawValue / 100000.0,      // Grad dezimal
        '4' => DekodiereGradDMS(rawValue), // Grad DMS
        '5' => rawValue / 100000.0,      // Mil
        '6' => rawValue / 10000.0,       // Meter,  0,1mm
        '7' => rawValue / 10000.0,       // Fuß,    0,1/1000 ft
        '8' => rawValue / 100000.0,      // Meter,  0,01mm
        _   => rawValue / 1000.0
    };

    // DMS-Format: DDMMSS.SSSSS → Dezimalgrad
    private static double DekodiereGradDMS(long rawValue)
    {
        double v = rawValue / 100000.0;
        double sec = v % 100;
        long   rem = (long)(v / 100);
        double min = rem % 100;
        double deg = rem / 100;
        return deg + min / 60.0 + sec / 3600.0;
    }
}
