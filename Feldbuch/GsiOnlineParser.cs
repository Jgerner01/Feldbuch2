namespace Feldbuch;

// ══════════════════════════════════════════════════════════════════════════════
// GsiOnlineParser  –  Parser für Leica TPS300 GSI Online-Antworten
//
// Erkennt und verarbeitet:
//   – Normale GSI-Datenzeilen (GSI-8 und GSI-16, via GsiDatenParser)
//   – Fehler-/Warnzeilen:  @W<Code>  oder  @E<Code>  oder  @?<Code>
//
// Antwortformat (Gerät → PC), CR-terminiert:
//   Normal:  21.102+17920860 22.102+09843660 31.116+00152340
//   Warnung: @W127
//   Fehler:  @E2
//   Unbekannt: @?
// ══════════════════════════════════════════════════════════════════════════════
public class GsiOnlineParser : ITachymeterDatenParser
{
    private readonly GsiDatenParser _inner = new();

    // ── ITachymeterDatenParser ────────────────────────────────────────────────
    public string FormatName        => "GSI-Online";
    public string FormatBeschreibung => "Leica GSI Online (TPS300) – GET/M GET/I Antworten";

    public bool KannVerarbeiten(string zeile)
    {
        if (string.IsNullOrWhiteSpace(zeile)) return false;
        var t = zeile.TrimStart();
        // GSI-Fehlermeldungen
        if (t.StartsWith("@W") || t.StartsWith("@E") || t.StartsWith("@?"))
            return true;
        // Normale GSI-Zeilen (delegiert an GsiDatenParser)
        return _inner.KannVerarbeiten(t);
    }

    public TachymeterMessung? ParseZeile(string zeile)
    {
        var trimmed = zeile.Trim();

        // ── GSI Fehler- / Warnzeilen ──────────────────────────────────────────
        if (trimmed.StartsWith("@E") || trimmed.StartsWith("@W") || trimmed.StartsWith("@?"))
        {
            string beschreibung = BeschreibeFehler(trimmed);
            return new TachymeterMessung
            {
                Typ      = MessungsTyp.Fehler,
                Quelle   = FormatName,
                Rohdaten = zeile,
                Bemerkung = beschreibung
            };
        }

        // ── Normale GSI-Datenzeile ────────────────────────────────────────────
        var messung = _inner.ParseZeile(zeile);
        if (messung != null)
            messung.Quelle = FormatName;
        return messung;
    }

    public IEnumerable<TachymeterMessung> ParseMehrere(IEnumerable<string> zeilen)
    {
        foreach (var z in zeilen)
        {
            var m = ParseZeile(z);
            if (m != null) yield return m;
        }
    }

    // ── Fehlerbeschreibungen ──────────────────────────────────────────────────
    private static string BeschreibeFehler(string code) => code switch
    {
        "@W0"  => "Kein Fehler",
        "@W1"  => "Unbekannter Befehl",
        "@W2"  => "Falscher Befehlscode",
        "@W3"  => "Falscher Parameter",
        "@W4"  => "Parameter außerhalb Bereich",
        "@W9"  => "Gerät nicht bereit (Messung läuft)",
        "@W12" => "Instrument nicht horizontal",
        "@W14" => "Kein EDM Prisma gefunden",
        "@W17" => "EDM: Prisma fehlt",
        "@W18" => "EDM: Signal zu schwach",
        "@W19" => "EDM: Signal zu stark",
        "@W23" => "Messung nicht möglich",
        "@W127"=> "Kein Messergebnis verfügbar",
        "@?"   => "Unbekannter Befehl vom Gerät nicht erkannt",
        _      => code   // Rohen Code anzeigen wenn unbekannt
    };
}
