namespace Feldbuch;

// ══════════════════════════════════════════════════════════════════════════════
// ITachymeterDatenParser  –  Schnittstelle für alle Tachymeter-Datenparser
//
// Neue Formate werden durch eine eigene Klasse implementiert und über die
// TachymeterParserFactory registriert – bestehender Code bleibt unverändert.
// ══════════════════════════════════════════════════════════════════════════════
public interface ITachymeterDatenParser
{
    // ── Metadaten ─────────────────────────────────────────────────────────────

    /// <summary>Kurzbezeichnung des Formats, z. B. "GeoCOM", "GSI-8", "GSI-16".</summary>
    string FormatName { get; }

    /// <summary>Ausführliche Beschreibung für UI-Anzeige.</summary>
    string FormatBeschreibung { get; }

    // ── Prüfung ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Gibt zurück ob diese Zeile / dieser Datenblock vom Parser verarbeitet
    /// werden kann (Schnellprüfung ohne vollständiges Parsen).
    /// </summary>
    bool KannVerarbeiten(string zeile);

    // ── Parsen einer einzelnen Zeile / Nachricht ──────────────────────────────

    /// <summary>
    /// Parst eine einzelne Zeile und gibt die Messung zurück.
    /// Gibt <c>null</c> zurück wenn die Zeile leer, unvollständig oder
    /// kein erkennbares Datenformat ist.
    /// </summary>
    TachymeterMessung? ParseZeile(string zeile);

    // ── Batch-Parsen (Datei / Puffer) ─────────────────────────────────────────

    /// <summary>
    /// Parst mehrere Zeilen und liefert alle erkannten Messungen.
    /// Leerzeilen und nicht erkannte Zeilen werden übersprungen.
    /// </summary>
    IEnumerable<TachymeterMessung> ParseMehrere(IEnumerable<string> zeilen);
}
