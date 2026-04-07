namespace Feldbuch;

// ══════════════════════════════════════════════════════════════════════════════
// TachymeterParserFactory  –  zentrale Registry aller Tachymeter-Parser
//
// Neue Formate registrieren:
//   TachymeterParserFactory.Registrieren(new MeinFormatParser());
//
// Parser abrufen:
//   var parser = TachymeterParserFactory.FuerFormat("GeoCOM");
//   var parser = TachymeterParserFactory.ErkenneFormat(zeile);  // auto-detect
// ══════════════════════════════════════════════════════════════════════════════
public static class TachymeterParserFactory
{
    private static readonly List<ITachymeterDatenParser> _parser = [];

    // ── Standard-Parser beim ersten Zugriff automatisch registrieren ──────────
    static TachymeterParserFactory()
    {
        Registrieren(new GeoCOMParser());
        Registrieren(new GsiDatenParser());
    }

    // ── Registrierung ─────────────────────────────────────────────────────────

    /// <summary>Registriert einen neuen Parser. Spätere Registrierungen haben Vorrang.</summary>
    public static void Registrieren(ITachymeterDatenParser parser)
    {
        // Doppelte Registrierung desselben Formats ersetzen
        _parser.RemoveAll(p => p.FormatName.Equals(parser.FormatName,
                                StringComparison.OrdinalIgnoreCase));
        _parser.Add(parser);
    }

    // ── Abruf ─────────────────────────────────────────────────────────────────

    /// <summary>Gibt den Parser für ein bestimmtes Format zurück, oder null.</summary>
    public static ITachymeterDatenParser? FuerFormat(string formatName) =>
        _parser.LastOrDefault(p =>
            p.FormatName.Equals(formatName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Erkennt das Format anhand der Zeile automatisch.
    /// Gibt den ersten passenden Parser zurück, oder null wenn kein Format erkannt.
    /// </summary>
    public static ITachymeterDatenParser? ErkenneFormat(string zeile) =>
        _parser.LastOrDefault(p => p.KannVerarbeiten(zeile));

    /// <summary>
    /// Parst eine Zeile mit automatischer Format-Erkennung.
    /// Gibt null zurück wenn kein Parser die Zeile verarbeiten kann.
    /// </summary>
    public static TachymeterMessung? Parse(string zeile)
    {
        var parser = ErkenneFormat(zeile);
        return parser?.ParseZeile(zeile);
    }

    /// <summary>Liste aller registrierten Parser (neueste zuerst).</summary>
    public static IReadOnlyList<ITachymeterDatenParser> Alle =>
        _parser.AsReadOnly();
}
