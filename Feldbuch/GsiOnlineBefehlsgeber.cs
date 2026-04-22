namespace Feldbuch;

// ══════════════════════════════════════════════════════════════════════════════
// GsiOnlineBefehlsgeber  –  Leica TPS300/700-Reihe  (TC/TCR 302/303/305/307)
//
// Referenz: Geosystems "GSI ONLINE for Leica TPS and DNA", Juni 2002, S. 17–29
//
// Befehlsformat (PC → Gerät):   <CMD>/<SPEC>/<WI-Liste>\r\n
//   GET/M/WI<n>/WI<n>  – EDM-Messung auslösen, Ergebnis zurückgeben
//   GET/I/WI<n>/WI<n>  – Momentanwert lesen (ohne EDM-Messung)
//   SET/<n>/<v>        – Geräteparameter setzen  (Antwort: ? bei Erfolg)
//   CONF/<n>           – Geräteparameter abfragen
//   PUT/<WI><Data>_    – Wert ins Gerät schreiben
//
// Antwortformat (Gerät → PC):
//   Normal:  21.102+17920860 22.102+09843660 31.116+00152340\r
//   SET/PUT: ?\r
//   Fehler:  @W<Code>\r  oder  @E<Code>\r
//
// Wichtige WI-Codes:
//   21 = Hz [gon],  22 = V [gon],  31 = Schrägstrecke [m]
//   32 = Horizontalstrecke,  33 = Höhenunterschied
//
// SET/161 – EDM-Modus (TPS300/700):
//   0 = IR Standard   1 = IR Fast   5 = IR Tracking
//   6 = RL Long       7 = RL Short  10 = IR Tape
//
// SET/36 – Laserpointer (nur TCR-Modelle):
//   0 = aus,  1 = ein
// ══════════════════════════════════════════════════════════════════════════════
public class GsiOnlineBefehlsgeber : ITachymeterBefehlsgeber
{
    public string Name => "Leica GSI Online (TPS300/700)";

    // ── Feature-Unterstützung ─────────────────────────────────────────────────
    public bool UnterstueztEdmModus     => true;   // SET/161
    public bool UnterstueztWinkelLive   => true;   // GET/I kontinuierlich
    public bool UnterstueztLibelleLive  => false;  // kein Kompensatorzugriff über GSI
    public bool UnterstueztLaserpointer => true;   // SET/36 (TCR-Modelle)
    public bool IstPassivEmpfang        => false;

    // ── Messung (einstufig) ───────────────────────────────────────────────────
    // GET/M: löst EDM-Messung aus, gibt Hz + V + Schrägstrecke in einem Schritt zurück
    public string? MessTriggerBefehl()  => "GET/M/WI21/WI22/WI31";
    public string? MessErgebnisBefehl() => null;   // Ergebnis kommt direkt
    public int     MessSchritt1Rpc      => 0;
    public int     MessSchritt2Rpc      => 0;

    // ── Winkel-Momentanwert (kein EDM, für Live-Anzeige) ─────────────────────
    public string? WinkelBefehl() => "GET/I/WI21/WI22";
    public int     WinkelRpc     => 0;

    // ── Kompensator (nicht verfügbar über GSI Online) ──────────────────────────
    public string? LibelleBefehl() => null;
    public int     LibelleRpc     => 0;

    // ── Laserpointer (SET/36 – nur TCR-Modelle) ───────────────────────────────
    public string? LaserBefehl(bool an) => $"SET/36/{(an ? 1 : 0)}";

    // ── EDM-Modus (SET/161) ───────────────────────────────────────────────────
    // zielTyp: 0 = Prisma, 1 = Reflektorlos
    // edmProg:  GeoCOM-Wert (wird auf TPS300/700-Codes abgebildet)
    //   TPS300/700: 0=IR Std, 1=IR Fast, 5=Tracking, 6=RL Long, 7=RL Short
    public string[]? EdmModusBefehle(int zielTyp, int edmProg)
    {
        int mode = zielTyp == 1
            ? (edmProg == 6 ? 6 : 7)   // Reflektorlos: RL Long (6) oder RL Short (7)
            : 0;                        // Prisma: IR Standard
        return [$"SET/161/{mode}"];
    }
}
