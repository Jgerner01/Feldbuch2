namespace Feldbuch;

// ══════════════════════════════════════════════════════════════════════════════
// ITachymeterBefehlsgeber  –  Schnittstelle für protokollspezifische Befehle
//
// Jede Protokoll-Klasse erzeugt die passenden Befehls-Strings für das
// verbundene Instrument. FormTestmessungen delegiert alle Befehlsaufrufe
// an die aktive Implementierung.
//
// Besonderheit zweistufige Messung (Leica GeoCOM):
//   MessErgebnisBefehl() != null → Form führt zweiten Schritt durch.
//   MessErgebnisBefehl() == null → Ergebnis kommt passiv oder einschrittig.
// ══════════════════════════════════════════════════════════════════════════════
public interface ITachymeterBefehlsgeber
{
    /// <summary>Name des Protokolls, z.B. "Leica GeoCOM", "Sokkia SDR".</summary>
    string Name { get; }

    // ── Feature-Unterstützung ─────────────────────────────────────────────────
    bool UnterstueztEdmModus     { get; }
    bool UnterstueztWinkelLive   { get; }
    bool UnterstueztLibelleLive  { get; }
    bool UnterstueztLaserpointer { get; }
    /// <summary>True = Gerät sendet Daten von selbst (Operator drückt MEAS).</summary>
    bool IstPassivEmpfang        { get; }

    // ── Messbefehle ───────────────────────────────────────────────────────────
    /// <summary>
    /// Schritt 1: Messung auslösen.
    /// GeoCOM: TMC_DoMeasure; Sokkia/Topcon: "" (sendet CRLF als Trigger); passiv: null.
    /// </summary>
    string? MessTriggerBefehl();

    /// <summary>
    /// Schritt 2 (nur GeoCOM): Messergebnis abrufen.
    /// Für alle anderen Protokolle: null → kein zweiter Schritt.
    /// </summary>
    string? MessErgebnisBefehl();

    /// <summary>GeoCOM-RPC für Schritt 1, oder 0 für andere Protokolle.</summary>
    int MessSchritt1Rpc { get; }

    /// <summary>GeoCOM-RPC für Schritt 2, oder 0 für andere Protokolle.</summary>
    int MessSchritt2Rpc { get; }

    // ── Weitere Befehle ───────────────────────────────────────────────────────
    /// <summary>Befehl für Winkelabfrage (Dauerübertragung). Null = nicht unterstützt.</summary>
    string? WinkelBefehl();

    /// <summary>Befehl für Libellen-/Kompensatorabfrage. Null = nicht unterstützt.</summary>
    string? LibelleBefehl();

    /// <summary>Befehl für Laserpointer ein/aus. Null = nicht unterstützt.</summary>
    string? LaserBefehl(bool an);

    /// <summary>
    /// Befehle für EDM-Modus-Umschaltung (Array möglich, z.B. TargetType + EdmMode).
    /// Null = nicht unterstützt.
    /// </summary>
    string[]? EdmModusBefehle(int zielTyp, int edmProg);

    /// <summary>GeoCOM-RPC für Winkelbefehl (Dauerübertragung), oder 0.</summary>
    int WinkelRpc { get; }

    /// <summary>GeoCOM-RPC für Libellenbefehl, oder 0.</summary>
    int LibelleRpc { get; }
}
