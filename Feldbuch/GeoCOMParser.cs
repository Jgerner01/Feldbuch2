namespace Feldbuch;

using System.Globalization;

// ══════════════════════════════════════════════════════════════════════════════
// GeoCOMParser  –  Parser für Leica GeoCOM RS-232-Protokoll (TPS1200)
//
// Protokollformat:
//   Anfrage PC → Gerät:  %R1Q,{RPC},0:{param1},{param2},...\r\n
//   Antwort Gerät → PC:  %R1P,0,0:{RC},{daten1},{daten2},...\r\n
//
//   RC = 0 → GRC_OK (Erfolg)
//   Winkel in Bogenmass [rad], Strecken in [m]
//
// Kontext-Tracking:
//   Weil GeoCOM-Antworten keinen RPC-Code enthalten, kann der zuletzt gesendete
//   RPC via SetzeLetzenRpc() als Kontext gesetzt werden → präzisere Typisierung.
//   Ohne Kontext erfolgt eine Inferenz anhand der Anzahl der Datenwerte.
// ══════════════════════════════════════════════════════════════════════════════
public class GeoCOMParser : ITachymeterDatenParser
{
    // ── Bekannte RPC-Codes ────────────────────────────────────────────────────
    public const int RPC_COM_NullProc      = 0;
    public const int RPC_COM_GetSWBaud    = 1000;
    public const int RPC_COM_SetSWBaud    = 1001;
    public const int RPC_EDM_Laserpointer = 1004;  // EDM_Laserpointer(0=off, 1=on)
    public const int RPC_TMC_DoMeasure    = 2008;
    public const int RPC_TMC_SetEdmMode   = 2020;
    public const int RPC_TMC_GetEdmMode   = 2021;
    public const int RPC_TMC_GetAngle     = 2107;
    public const int RPC_TMC_GetSimpleMea = 2108;
    public const int RPC_TMC_GetFullMeas  = 2167;

    // Kontext: zuletzt gesendeter RPC (0 = unbekannt / kein Kontext)
    private int _letzterRpc = 0;

    // ── ITachymeterDatenParser ────────────────────────────────────────────────
    public string FormatName        => "GeoCOM";
    public string FormatBeschreibung => "Leica GeoCOM RS-232 (TPS1200) – %R1P-Antworten";

    public bool KannVerarbeiten(string zeile) =>
        !string.IsNullOrEmpty(zeile) && zeile.TrimStart().StartsWith("%R1", StringComparison.Ordinal);

    public TachymeterMessung? ParseZeile(string zeile) =>
        ParseAntwort(zeile.Trim(), _letzterRpc);

    public IEnumerable<TachymeterMessung> ParseMehrere(IEnumerable<string> zeilen)
    {
        foreach (var z in zeilen)
        {
            var m = ParseZeile(z);
            if (m != null) yield return m;
        }
    }

    // ── Kontext setzen ────────────────────────────────────────────────────────

    /// <summary>
    /// Setzt den zuletzt gesendeten RPC als Kontext für die nächste Antwort.
    /// Damit können mehrdeutige Antworten (gleiche Anzahl Datenwerte) korrekt
    /// typisiert werden.
    /// </summary>
    public void SetzeLetzenRpc(int rpc) => _letzterRpc = rpc;

    /// <summary>
    /// Extrahiert den RPC-Code aus einer gesendeten Anfrage (%R1Q,...).
    /// Gibt 0 zurück wenn kein Code erkannt wurde.
    /// </summary>
    public static int ExtrRpcAusAnfrage(string anfrage)
    {
        // Format: %R1Q,{RPC},0:...
        if (!anfrage.StartsWith("%R1Q,", StringComparison.Ordinal)) return 0;
        var teile = anfrage.Substring(5).Split(',');
        return int.TryParse(teile[0], out var rpc) ? rpc : 0;
    }

    // ── Kernlogik: Antwort parsen ─────────────────────────────────────────────

    /// <summary>
    /// Parst eine GeoCOM-Antwortzeile.
    /// <paramref name="rpcKontext"/> ist der RPC der letzten Anfrage (0 = unbekannt).
    /// </summary>
    public static TachymeterMessung? ParseAntwort(string zeile, int rpcKontext = 0)
    {
        if (string.IsNullOrWhiteSpace(zeile)) return null;

        // ── Anfrage-Zeile mitloggen aber nicht parsen ─────────────────────────
        if (zeile.StartsWith("%R1Q", StringComparison.Ordinal))
            return null;   // Anfragen werden nicht als Messung zurückgegeben

        // ── Antwort-Zeile: %R1P,0,0:{RC},{daten...} ──────────────────────────
        if (!zeile.StartsWith("%R1P,", StringComparison.Ordinal))
            return null;

        // Nutzdaten-Teil nach dem ersten ':' trennen
        int doppelPunkt = zeile.IndexOf(':');
        if (doppelPunkt < 0) return null;

        string nutz = zeile.Substring(doppelPunkt + 1).Trim();
        var werte = nutz.Split(',');
        if (werte.Length == 0) return null;

        // Returncode (erstes Feld)
        if (!int.TryParse(werte[0].Trim(), out int rc)) return null;
        var daten = werte.Skip(1).ToArray();

        var m = new TachymeterMessung
        {
            Quelle     = "GeoCOM",
            Rohdaten   = zeile,
            ReturnCode = rc
        };

        // ── Fehler-Antwort ────────────────────────────────────────────────────
        if (rc != 0)
        {
            m.Typ      = MessungsTyp.Fehler;
            m.Bemerkung = $"GRC={rc}  {GeoCOMReturnCodeText(rc)}";
            return m;
        }

        // ── Erfolgreiche Antwort: Daten interpretieren ────────────────────────
        return rpcKontext switch
        {
            RPC_TMC_GetAngle     => ParseWinkel(m, daten),
            RPC_TMC_GetSimpleMea => ParseVollmessung(m, daten),
            RPC_TMC_GetFullMeas  => ParseVollmessungErweitert(m, daten),
            RPC_TMC_GetEdmMode   => ParseEdmModus(m, daten),
            RPC_COM_GetSWBaud    => ParseBaudrate(m, daten),

            // Bestätigungen ohne Nutzdaten / Einzel-Wert-Antworten
            RPC_COM_NullProc      or
            RPC_TMC_DoMeasure     or
            RPC_TMC_SetEdmMode    or
            RPC_COM_SetSWBaud     or
            RPC_EDM_Laserpointer  => ParseStatus(m, daten, rpcKontext),

            // Kein Kontext: aus Datenanzahl schließen
            _ => InferiereTyp(m, daten)
        };
    }

    // ── Winkel (TMC_GetAngle, RPC 2107) ──────────────────────────────────────
    // Antwort: RC, Hz_rad, V_rad  [, AngAccH, AngAccV, …]
    private static TachymeterMessung ParseWinkel(TachymeterMessung m, string[] d)
    {
        m.Typ = MessungsTyp.Winkel;
        if (d.Length >= 1 && TryParseDouble(d[0], out double hz))
            m.Hz_gon = RadNachGon(hz);
        if (d.Length >= 2 && TryParseDouble(d[1], out double v))
            m.V_gon  = RadNachGon(v);
        if (d.Length >= 3 && TryParseDouble(d[2], out double accH))
            m.WinkelGenauigkeit_cc = accH * (200.0 / Math.PI) * 10000; // rad → cc
        return m;
    }

    // ── Vollmessung (TMC_GetSimpleMea, RPC 2108) ──────────────────────────────
    // Antwort: RC, Hz_rad, V_rad, SlopeDist_m
    private static TachymeterMessung ParseVollmessung(TachymeterMessung m, string[] d)
    {
        m.Typ = MessungsTyp.Vollmessung;
        if (d.Length >= 1 && TryParseDouble(d[0], out double hz))
            m.Hz_gon = RadNachGon(hz);
        if (d.Length >= 2 && TryParseDouble(d[1], out double v))
            m.V_gon  = RadNachGon(v);
        if (d.Length >= 3 && TryParseDouble(d[2], out double dist))
            m.Schraegstrecke_m = dist;
        BerechneAbgeleiteteMasse(m);
        return m;
    }

    // ── Vollmessung erweitert (TMC_GetFullMeas, RPC 2167) ─────────────────────
    // Antwort: RC, Hz, V, AngAccH, AngAccV, SlopeDist, DistTime, CrossAcc, …
    private static TachymeterMessung ParseVollmessungErweitert(TachymeterMessung m, string[] d)
    {
        m.Typ = MessungsTyp.Vollmessung;
        if (d.Length >= 1 && TryParseDouble(d[0], out double hz))  m.Hz_gon = RadNachGon(hz);
        if (d.Length >= 2 && TryParseDouble(d[1], out double v))   m.V_gon  = RadNachGon(v);
        if (d.Length >= 5 && TryParseDouble(d[4], out double dist)) m.Schraegstrecke_m = dist;
        if (d.Length >= 3 && TryParseDouble(d[2], out double accH))
            m.WinkelGenauigkeit_cc = accH * (200.0 / Math.PI) * 10000;
        BerechneAbgeleiteteMasse(m);
        return m;
    }

    // ── EDM-Modus (TMC_GetEdmMode, RPC 2021) ─────────────────────────────────
    // Antwort: RC, mode_int
    private static TachymeterMessung ParseEdmModus(TachymeterMessung m, string[] d)
    {
        m.Typ = MessungsTyp.EdmModusInfo;
        if (d.Length >= 1 && int.TryParse(d[0].Trim(), out int modus))
        {
            m.EdmModusRoh = modus;
            m.EdmModus    = EdmModusAusCode(modus);
            m.Bemerkung   = $"EDM-Modus {modus}: {EdmModusText(modus)}";
        }
        return m;
    }

    // ── Baudrate (COM_GetSWBaudrate, RPC 1000) ────────────────────────────────
    private static TachymeterMessung ParseBaudrate(TachymeterMessung m, string[] d)
    {
        m.Typ = MessungsTyp.Status;
        if (d.Length >= 1 && int.TryParse(d[0].Trim(), out int code))
            m.Bemerkung = $"Baudrate-Code {code} ({BaudrateAusCode(code)} Baud)";
        return m;
    }

    // ── Bestätigung / Status ──────────────────────────────────────────────────
    private static TachymeterMessung ParseStatus(TachymeterMessung m, string[] d, int rpc)
    {
        m.Typ = MessungsTyp.Status;
        m.Bemerkung = rpc switch
        {
            RPC_COM_NullProc      => "Ping OK",
            RPC_TMC_DoMeasure     => "Messung gestartet",
            RPC_TMC_SetEdmMode    => "EDM-Modus gesetzt",
            RPC_COM_SetSWBaud     => "Baudrate geändert",
            RPC_EDM_Laserpointer  => d.Length > 0 && d[0].Trim() == "1"
                                     ? "Laserpointer EIN" : "Laserpointer AUS",
            _                     => "OK"
        };
        return m;
    }

    // ── Inferenz ohne Kontext ─────────────────────────────────────────────────
    private static TachymeterMessung InferiereTyp(TachymeterMessung m, string[] d)
    {
        switch (d.Length)
        {
            case 0:
                m.Typ = MessungsTyp.Status;
                m.Bemerkung = "OK";
                break;
            case 1:
                // Könnte EDM-Modus, Baudrate, etc. sein
                m.Typ = MessungsTyp.Status;
                if (int.TryParse(d[0].Trim(), out int val))
                    m.Bemerkung = $"Wert={val}";
                break;
            case 2:
                // Wahrscheinlich TMC_GetAngle: Hz, V
                return ParseWinkel(m, d);
            case >= 3:
                // Wahrscheinlich TMC_GetSimpleMea: Hz, V, Dist
                return ParseVollmessung(m, d);
        }
        return m;
    }

    // ── Hilfsmethoden: Umrechnungen ───────────────────────────────────────────

    private static double RadNachGon(double rad) => rad * (200.0 / Math.PI);

    private static void BerechneAbgeleiteteMasse(TachymeterMessung m)
    {
        if (!m.V_gon.HasValue || !m.Schraegstrecke_m.HasValue) return;
        double vRad = m.V_gon.Value * (Math.PI / 200.0);
        m.Horizontalstrecke_m  = Math.Round(m.Schraegstrecke_m.Value * Math.Sin(vRad), 4);
        m.Hoehenunterschied_m  = Math.Round(m.Schraegstrecke_m.Value * Math.Cos(vRad), 4);
    }

    private static MessungsEdmModus EdmModusAusCode(int code) => code switch
    {
        1  => MessungsEdmModus.Folie,
        2  => MessungsEdmModus.Reflektorlos,
        3  => MessungsEdmModus.Prisma,
        5  => MessungsEdmModus.Prisma,
        8  => MessungsEdmModus.Reflektorlos,
        10 => MessungsEdmModus.Reflektorlos,
        11 => MessungsEdmModus.Reflektorlos,
        _  => MessungsEdmModus.Unbekannt
    };

    private static string EdmModusText(int code) => code switch
    {
        0  => "Nicht aktiv",
        1  => "EDM_SINGLE_TAPE – Folie",
        2  => "EDM_SINGLE_LRRL – Reflektorlos kurz",
        3  => "EDM_SINGLE_PRISM – Prisma",
        4  => "EDM_CONT_STANDARD – Dauermessung",
        5  => "EDM_SINGLE_STANDARD – Einzelmessung Standard",
        6  => "EDM_CONT_DYNAMIC – Dauermessung dynamisch",
        7  => "EDM_SINGLE_TRACKING – Tracking",
        8  => "EDM_CONT_REFLESS – Dauermessung RL",
        9  => "EDM_SINGLE_FAST – Schnellmessung",
        10 => "EDM_SINGLE_LRRL – Reflektorlos Langdistanz",
        11 => "EDM_CONT_LRRL – Dauermessung RL Langdistanz",
        12 => "EDM_SCAN_REF",
        _  => $"Unbekannt ({code})"
    };

    private static int BaudrateAusCode(int code) => code switch
    {
        0 => 1200, 1 => 2400, 2 => 4800, 3 => 9600,
        4 => 19200, 5 => 38400, 6 => 57600, 7 => 115200,
        _ => 0
    };

    private static string GeoCOMReturnCodeText(int rc) => rc switch
    {
        2    => "GRC_UNDEFINED",
        3    => "GRC_IVPARAM – Ungültiger Parameter",
        5    => "GRC_NOTOK – Gerät nicht bereit",
        13   => "GRC_IVRESULT – Ungültiges Ergebnis",
        25   => "GRC_ABORT – Messung abgebrochen",
        30   => "GRC_NOMEAS – Keine Messung vorhanden",
        35   => "GRC_NOTACCEPTED – Befehl abgelehnt (Funktion nicht verfügbar?)",
        // TMC-Fehler (1280–1299)
        1280 => "GRC_TMC_NO_MEASUREMENT – Keine Messung gestartet",
        1281 => "GRC_TMC_NOT_STABLE – Instrument nicht stabil",
        1282 => "GRC_TMC_DIST_WAIT – Messung läuft noch, Ergebnis noch nicht bereit",
        1283 => "GRC_TMC_DIST_PPM – Ungültiger ppm-Wert",
        1284 => "GRC_TMC_DIST_ERROR – Distanzfehler (kein Reflex / falscher Modus?)",
        1285 => "GRC_TMC_BUSY – Messmodul beschäftigt (TMC_DoMeasure läuft noch)",
        1286 => "GRC_TMC_SIGNAL_ERROR – Kein Reflexionssignal empfangen",
        1288 => "GRC_TMC_NO_FULL_CORRECTION – Keine vollständige Korrektion",
        508  => "GRC_TMC_NO_FULL_CORRECTION – Keine vollständige Korrektion",
        // EDM-Fehler (ab 1792)
        1792 => "GRC_EDM_BUSYMODE – EDM in parallelem Messmodus",
        1793 => "GRC_EDM_NO_VALID_MEAS – Keine gültige Messung (Reflektormodus/Ziel prüfen)",
        1800 => "GRC_EDM_DEV_NOT_INSTALLED – Reflektorlos-Option nicht installiert",
        _    => ""
    };

    private static bool TryParseDouble(string s, out double value) =>
        double.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
}
