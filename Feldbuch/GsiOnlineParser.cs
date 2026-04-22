namespace Feldbuch;

// ══════════════════════════════════════════════════════════════════════════════
// GsiOnlineParser  –  Parser für Leica TPS300/700 GSI Online-Antworten
//
// Referenz: Geosystems "GSI ONLINE for Leica TPS and DNA", Juni 2002
//
// Erkennt und verarbeitet:
//   – Normale GSI-Datenzeilen (GSI-8 und GSI-16, via GsiDatenParser)
//   – Bestätigung:  ?             (Antwort auf SET/PUT, kein Messwert)
//   – Fehler-/Warnzeilen:  @W<Code>  oder  @E<Code>
//
// Fehlercodes TPS300/700 (PDF S. 17–29):
//   @W100  Gerät beschäftigt (Messung läuft)
//   @W127  Ungültiger Befehl / nicht erkannt
//   @E139  EDM-Fehler (kein Prisma, Signal zu schwach/stark)
//   @E158  Kompensator-Fehler (Instrument zu stark geneigt)
// ══════════════════════════════════════════════════════════════════════════════
public class GsiOnlineParser : ITachymeterDatenParser
{
    private readonly GsiDatenParser _inner = new();

    // ── ITachymeterDatenParser ────────────────────────────────────────────────
    public string FormatName        => "GSI-Online";
    public string FormatBeschreibung => "Leica GSI Online (TPS300/700) – GET/M GET/I Antworten";

    public bool KannVerarbeiten(string zeile)
    {
        if (string.IsNullOrWhiteSpace(zeile)) return false;
        var t = zeile.TrimStart();
        if (t == "?") return true;
        if (t.StartsWith("@W") || t.StartsWith("@E") || t.StartsWith("@?"))
            return true;
        return _inner.KannVerarbeiten(t);
    }

    public TachymeterMessung? ParseZeile(string zeile)
    {
        var trimmed = zeile.Trim();

        // ── Erfolgsbestätigung für SET/PUT (kein Messwert) ────────────────────
        if (trimmed == "?")
            return new TachymeterMessung
            {
                Typ      = MessungsTyp.Status,
                Quelle   = FormatName,
                Rohdaten = zeile,
                Bemerkung = "OK"
            };

        // ── GSI Fehler- / Warnzeilen ──────────────────────────────────────────
        if (trimmed.StartsWith("@E") || trimmed.StartsWith("@W") || trimmed.StartsWith("@?"))
            return new TachymeterMessung
            {
                Typ      = MessungsTyp.Fehler,
                Quelle   = FormatName,
                Rohdaten = zeile,
                Bemerkung = BeschreibeFehler(trimmed)
            };

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

    // ── Fehlerbeschreibungen (TPS300/700, PDF S. 17–29) ───────────────────────
    private static string BeschreibeFehler(string code) => code switch
    {
        "@W100" => "Gerät beschäftigt – Messung läuft noch",
        "@W127" => "Ungültiger Befehl oder Parameter nicht erkannt",
        "@E139" => "EDM-Fehler: Prisma fehlt oder Signal zu schwach/stark",
        "@E158" => "Kompensator-Fehler: Instrument zu stark geneigt",
        "@?"    => "Befehl vom Gerät nicht erkannt",
        _       => code
    };
}
