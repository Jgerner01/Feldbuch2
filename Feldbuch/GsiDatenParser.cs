namespace Feldbuch;

using System.Globalization;
using System.Text;

// ══════════════════════════════════════════════════════════════════════════════
// GsiDatenParser  –  Leica GSI-8 und GSI-16 als ITachymeterDatenParser
//
// Implementiert das universelle Parser-Interface für GSI-Daten.
// Kann Einzel-Zeilen (z. B. aus serieller Übertragung) und komplette
// Dateien verarbeiten.
//
// GSI-Blockstruktur:
//   GSI-8 : WI(2) + Info(4) + Sign(1) + Data(8)  = 15 Zeichen/Token
//   GSI-16: WI(2) + Info(4) + Sign(1) + Data(16) = 23 Zeichen/Token
//
// Wichtige Word-Index (WI)-Codes:
//   11      Punktnummer
//   21      Hz-Richtung [gon]
//   22      Zenitwinkel [gon]
//   31      Schrägdistanz [m]
//   32      Horizontaldistanz [m]
//   33      Höhenunterschied [m]
//   41-49   Code/Kommentar
//   71-79   Attribute
//   81-83   Zielpunkt E, N, H [m]
//   84-86   Standpunkt E, N, H [m]
//   87      Zielhöhe [m]
//   88      Instrumenthöhe [m]
// ══════════════════════════════════════════════════════════════════════════════
public class GsiDatenParser : ITachymeterDatenParser
{
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    // ── ITachymeterDatenParser ────────────────────────────────────────────────
    public string FormatName        => "GSI";
    public string FormatBeschreibung => "Leica GSI-8 / GSI-16 (GEO Serial Interface)";

    public bool KannVerarbeiten(string zeile)
    {
        if (string.IsNullOrWhiteSpace(zeile)) return false;
        // GSI-Zeile: erstes Token ist mindestens 15 Zeichen lang,
        // beginnt mit zweistelligem WI (z. B. "11" oder "21").
        // GSI-16 Zeilen können mit "*" beginnen (Zeilenpräfix).
        var trimmed = zeile.TrimStart();
        if (trimmed.Length > 0 && trimmed[0] == '*') trimmed = trimmed[1..];
        var tokens = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return false;
        string t = tokens[0];
        return t.Length is 15 or 23 && char.IsDigit(t[0]) && char.IsDigit(t[1]);
    }

    public TachymeterMessung? ParseZeile(string zeile) =>
        ParseGsiZeile(zeile.Trim());

    public IEnumerable<TachymeterMessung> ParseMehrere(IEnumerable<string> zeilen)
    {
        foreach (var z in zeilen)
        {
            var m = ParseGsiZeile(z.Trim());
            if (m != null) yield return m;
        }
    }

    // ── Datei-Import (Kompatibilität mit bestehendem GsiParser-Workflow) ──────

    /// <summary>Importiert eine GSI-Datei und gibt alle Messungen zurück.</summary>
    public static List<TachymeterMessung> ImportDatei(string pfad)
    {
        var lines = File.ReadAllLines(pfad, Encoding.Latin1);
        var parser = new GsiDatenParser();
        return parser.ParseMehrere(lines).ToList();
    }

    /// <summary>Importiert eine GSI-Datei als KonvertierungPunkte (Rückwärtskompatibilität).</summary>
    public static List<KonvertierungPunkt> ImportAlsKonvertierungPunkte(string pfad) =>
        ImportDatei(pfad).Select(m => m.ZuKonvertierungPunkt()).ToList();

    // ── Kernlogik ─────────────────────────────────────────────────────────────

    private static TachymeterMessung? ParseGsiZeile(string line)
    {
        if (string.IsNullOrEmpty(line)) return null;
        // Optionalen * Zeilenpräfix (GSI-16) entfernen
        if (line.StartsWith("*")) line = line[1..];
        var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return null;

        bool isGsi16 = tokens.Any(t => t.Length >= 23);
        string quelle = isGsi16 ? "GSI-16" : "GSI-8";

        var m = new TachymeterMessung
        {
            Quelle   = quelle,
            Rohdaten = line,
            Typ      = MessungsTyp.Unbekannt
        };
        var extras = new List<string>();

        foreach (var token in tokens)
        {
            if (token.Length < 15) continue;

            string wiStr  = token.Substring(0, 2);
            string info   = token.Substring(2, 4);
            char   sign   = token[6];
            string data   = token.Substring(7);   // 8 oder 16 Zeichen

            if (!int.TryParse(wiStr, out int wi)) continue;
            char unitCode = info[3];

            // ── WI 11: Punktnummer ─────────────────────────────────────────
            if (wi == 11)
            {
                string raw = data.TrimEnd();
                m.PunktNr = long.TryParse(raw, out long pnNum)
                    ? pnNum.ToString()
                    : raw.TrimStart('0');
                continue;
            }

            // ── Numerischer Wert ───────────────────────────────────────────
            if (!long.TryParse(data.Trim(), out long rawVal)) continue;
            if (sign == '-') rawVal = -rawVal;
            double wert = Dekodiere(rawVal, unitCode);

            switch (wi)
            {
                case 21: m.Hz_gon              = wert; break;
                case 22: m.V_gon               = wert; break;
                case 31: m.Schraegstrecke_m    = wert; break;
                case 32: m.Horizontalstrecke_m ??= wert; break;
                case 33: m.Hoehenunterschied_m  = wert; break;
                case 81: m.E_m                 = wert; break;
                case 82: m.N_m                 = wert; break;
                case 83: m.H_m                 = wert; break;
                case 84: m.StandpunktE_m       = wert; break;
                case 85: m.StandpunktN_m       = wert; break;
                case 86: m.StandpunktH_m       = wert; break;
                case 87: m.Zielhoehe_m         = wert; break;
                case 88: m.Instrumenthoehe_m   = wert; break;
                case int c when c is >= 41 and <= 49:
                    string code = data.TrimEnd().TrimStart('0');
                    if (wi == 41 && string.IsNullOrEmpty(m.Punktcode))
                        m.Punktcode = code;
                    else if (!string.IsNullOrEmpty(code))
                        extras.Add($"WI{wi}={code}");
                    break;
                case int c when c is >= 71 and <= 79:
                    string attr = data.TrimEnd().TrimStart('0');
                    if (!string.IsNullOrEmpty(attr))
                        extras.Add($"Attr{wi}={attr}");
                    break;
            }
        }

        if (extras.Count > 0)
            m.Bemerkung = string.Join(", ", extras);

        // Typ ableiten
        bool hatKoord   = m.E_m.HasValue || m.N_m.HasValue;
        bool hatWinkel  = m.Hz_gon.HasValue || m.V_gon.HasValue;
        bool hatDist    = m.Schraegstrecke_m.HasValue || m.Horizontalstrecke_m.HasValue;

        m.Typ = (hatWinkel && hatDist)  ? MessungsTyp.Vollmessung
              : hatWinkel               ? MessungsTyp.Winkel
              : hatKoord                ? MessungsTyp.Koordinate
              : MessungsTyp.Unbekannt;

        if (m.Typ == MessungsTyp.Unbekannt && string.IsNullOrEmpty(m.PunktNr))
            return null;

        return m;
    }

    // ── GSI-Dekodierung ───────────────────────────────────────────────────────

    private static double Dekodiere(long raw, char unit) => unit switch
    {
        '0' => raw / 1000.0,          // m, 1 mm
        '1' => raw / 1000.0,          // ft, 1/1000 ft
        '2' => raw / 100000.0,        // gon
        '3' => raw / 100000.0,        // Grad dezimal
        '4' => DekodiereGradDMS(raw), // Grad DMS
        '5' => raw / 100000.0,        // Mil
        '6' => raw / 10000.0,         // m, 0,1 mm
        '7' => raw / 10000.0,         // ft, 0,1/1000 ft
        '8' => raw / 100000.0,        // m, 0,01 mm
        _   => raw / 1000.0
    };

    private static double DekodiereGradDMS(long raw)
    {
        double v   = raw / 100000.0;
        double sec = v % 100;
        long   rem = (long)(v / 100);
        double min = rem % 100;
        double deg = rem / 100;
        return deg + min / 60.0 + sec / 3600.0;
    }
}
