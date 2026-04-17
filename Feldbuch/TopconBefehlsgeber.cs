namespace Feldbuch;

// ══════════════════════════════════════════════════════════════════════════════
// TopconBefehlsgeber  –  Topcon GTS/GPT-Reihe
//
// Topcon GTS/GPT akzeptiert CRLF als Messtrigger.
// Das Instrument muss im RS-232-Fernsteuerungsmodus konfiguriert sein.
// Antwort enthält Hz, V und optional SD im Topcon-Format.
//
// Prismenkonstante und EDM-Modus werden direkt am Gerät eingestellt.
// ══════════════════════════════════════════════════════════════════════════════
public class TopconBefehlsgeber : ITachymeterBefehlsgeber
{
    public string Name => "Topcon GTS/GPT";

    public bool UnterstueztEdmModus     => false;
    public bool UnterstueztWinkelLive   => false;
    public bool UnterstueztLibelleLive  => false;
    public bool UnterstueztLaserpointer => false;
    public bool IstPassivEmpfang        => false;  // aktiver CRLF-Trigger

    // ── Messung (einschrittig, CRLF-Trigger) ─────────────────────────────────
    public string? MessTriggerBefehl()  => "";   // CRLF-Trigger
    public string? MessErgebnisBefehl() => null;
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
