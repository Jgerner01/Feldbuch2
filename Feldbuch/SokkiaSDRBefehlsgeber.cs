namespace Feldbuch;

// ══════════════════════════════════════════════════════════════════════════════
// SokkiaSDRBefehlsgeber  –  Sokkia SET-Reihe (SDR33-Format)
//
// Das SDR-Protokoll ist primär passiv: der Messer drückt MEAS am Instrument
// und das Gerät sendet einen SDR33-Record an den PC.
// Alternativ kann CRLF als Trigger gesendet werden wenn das Instrument im
// externen Ausgabemodus konfiguriert ist.
//
// Prismenkonstante, EDM-Modus und Kompensator werden direkt am Gerät
// eingestellt – keine Fernsteuerungsbefehle verfügbar.
// ══════════════════════════════════════════════════════════════════════════════
public class SokkiaSDRBefehlsgeber : ITachymeterBefehlsgeber
{
    public string Name => "Sokkia SDR";

    public bool UnterstueztEdmModus     => false;
    public bool UnterstueztWinkelLive   => false;
    public bool UnterstueztLibelleLive  => false;
    public bool UnterstueztLaserpointer => false;
    public bool IstPassivEmpfang        => true;

    // ── Messung (einschrittig, CRLF-Trigger) ─────────────────────────────────
    // "" + "\r\n" in GeoCOM_Senden ergibt ein reines CRLF
    public string? MessTriggerBefehl()  => "";   // CRLF-Trigger
    public string? MessErgebnisBefehl() => null; // Kein zweiter Schritt
    public int MessSchritt1Rpc => 0;
    public int MessSchritt2Rpc => 0;

    // ── Nicht unterstützte Funktionen ─────────────────────────────────────────
    public string? WinkelBefehl()       => null;
    public int     WinkelRpc            => 0;
    public string? LibelleBefehl()      => null;
    public int     LibelleRpc           => 0;
    public string? LaserBefehl(bool an) => null;
    public string[]? EdmModusBefehle(int z, int e) => null;
}
