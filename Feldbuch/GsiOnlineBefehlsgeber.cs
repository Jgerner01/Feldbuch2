namespace Feldbuch;

// ══════════════════════════════════════════════════════════════════════════════
// GsiOnlineBefehlsgeber  –  Leica TPS300 / GSI Online-Protokoll
//
// Referenz: Geosystems "GSI ONLINE for Leica TPS and DNA" v1.14
//
// Befehlsformat (PC → Gerät):   <CMD>/<SPEC>/<WI-Liste>\r\n
//   GET/M/<WI>   – Messung auslösen, Ergebnis zurückgeben
//   GET/I/<WI>   – Momentanwert lesen (ohne EDM-Messung)
//   SET/<n>/<v>  – Geräteparameter setzen
//   CONF/<n>     – Geräteparameter abfragen
//   PUT/<WI><Data> – Wert ins Gerät schreiben
//
// Antwortformat (Gerät → PC):   GSI-8 oder GSI-16 Zeile, CR-terminiert
//   Normal:  21.102+17920860 22.102+09843660 31.116+00152340\r
//   Fehler:  @W<Code>\r   oder   @E<Code>\r
//
// Wichtige WI-Codes:
//   21 = Hz [gon], 22 = V [gon], 31 = Schrägstrecke [m]
//   32 = Horizontalstrecke [m], 33 = Höhenunterschied [m]
//
// Einschränkungen TPS300 gegenüber GeoCOM:
//   – Kein Laserpointer-Befehl
//   – Kein Kompensator (Dosenlibelle) über GSI Online
//   – EDM-Modus nur am Gerät umschaltbar (SET-Befehl nicht zuverlässig für alle Modelle)
// ══════════════════════════════════════════════════════════════════════════════
public class GsiOnlineBefehlsgeber : ITachymeterBefehlsgeber
{
    public string Name => "Leica GSI Online (TPS300)";

    // ── Feature-Unterstützung ─────────────────────────────────────────────────
    public bool UnterstueztEdmModus     => false;  // nur am Gerät
    public bool UnterstueztWinkelLive   => true;   // GET/I kontinuierlich
    public bool UnterstueztLibelleLive  => false;  // kein Kompensatorzugriff über GSI
    public bool UnterstueztLaserpointer => false;  // nur über GeoCOM verfügbar
    public bool IstPassivEmpfang        => false;

    // ── Messung (einstufig) ───────────────────────────────────────────────────
    // GET/M: löst EDM-Messung aus, gibt Hz + V + Schrägstrecke zurück
    public string? MessTriggerBefehl()  => "GET/M/WI21 WI22 WI31";
    public string? MessErgebnisBefehl() => null;   // Ergebnis kommt direkt in einem Schritt
    public int     MessSchritt1Rpc      => 0;
    public int     MessSchritt2Rpc      => 0;

    // ── Winkel-Momentanwert (kein EDM, für Dauerübertragung) ─────────────────
    public string? WinkelBefehl() => "GET/I/WI21 WI22";
    public int     WinkelRpc     => 0;

    // ── Kompensator (nicht verfügbar) ─────────────────────────────────────────
    public string? LibelleBefehl() => null;
    public int     LibelleRpc     => 0;

    // ── Laser (nicht verfügbar über GSI Online) ───────────────────────────────
    public string? LaserBefehl(bool an) => null;

    // ── EDM-Modus (nicht über GSI Online steuerbar) ───────────────────────────
    public string[]? EdmModusBefehle(int zielTyp, int edmProg) => null;
}
