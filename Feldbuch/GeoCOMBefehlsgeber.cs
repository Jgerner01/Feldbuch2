namespace Feldbuch;

// ══════════════════════════════════════════════════════════════════════════════
// GeoCOMBefehlsgeber  –  Leica GeoCOM-Protokoll Befehle (TPS1200 / TPS300)
// ══════════════════════════════════════════════════════════════════════════════
public class GeoCOMBefehlsgeber : ITachymeterBefehlsgeber
{
    public string Name => "Leica GeoCOM";

    public bool UnterstueztEdmModus     => true;
    public bool UnterstueztWinkelLive   => true;
    public bool UnterstueztLibelleLive  => true;
    public bool UnterstueztLaserpointer => true;
    public bool IstPassivEmpfang        => false;

    // ── Messung (zweistufig) ──────────────────────────────────────────────────
    public string? MessTriggerBefehl()  => "%R1Q,2008,0:1,1";    // TMC_DoMeasure
    public string? MessErgebnisBefehl() => "%R1Q,2108,0:5000,1"; // TMC_GetSimpleMea
    public int MessSchritt1Rpc => GeoCOMParser.RPC_TMC_DoMeasure;
    public int MessSchritt2Rpc => GeoCOMParser.RPC_TMC_GetSimpleMea;

    // ── Winkel + Libelle ──────────────────────────────────────────────────────
    public string? WinkelBefehl()      => "%R1Q,2107,0:0"; // TMC_GetAngle
    public int     WinkelRpc           => GeoCOMParser.RPC_TMC_GetAngle;

    public string? LibelleBefehl()     => "%R1Q,2003,0:1"; // TMC_GetAngle1 (inkl. Kompensator)
    public int     LibelleRpc          => GeoCOMParser.RPC_TMC_GetAngle1;

    // ── Laser + EDM ───────────────────────────────────────────────────────────
    public string? LaserBefehl(bool an) => $"%R1Q,1004,0:{(an ? 1 : 0)}"; // EDM_Laserpointer

    public string[]? EdmModusBefehle(int zielTyp, int edmProg) =>
    [
        $"%R1Q,17021,0:{zielTyp}",  // BAP_SetTargetType
        $"%R1Q,2020,0:{edmProg}"    // TMC_SetEdmMode
    ];
}
