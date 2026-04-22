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
    public const int RPC_TMC_GetAngle1    = 2003;  // volle Winkelmessung + Kompensator (CrossIncline, LengthIncline)
    public const int RPC_TMC_SetAtmCorr   = 2028;  // atmosphärische Korrektur setzen (Lambda, Druck, TempTrock, TempFeucht)
    public const int RPC_TMC_GetAtmCorr   = 2029;  // atmosphärische Korrektur lesen
    public const int RPC_BAP_MeasDist      = 17017; // TPS300: BAP_MeasDist – Messung auslösen (vereinfacht)
    public const int RPC_BAP_SetTargetType = 17021; // Zieltyp setzen: 0=Reflektor (IR), 1=Reflektorlos (RL)
    public const int RPC_BAP_GetTargetType = 17022; // Zieltyp abfragen
    public const int RPC_TMC_GetPrismCorr = 2023;  // Prismenkonstante lesen [m]
    public const int RPC_TMC_SetPrismCorr = 2024;  // Prismenkonstante setzen [m]
    public const int RPC_TMC_GetAngle     = 2107;
    public const int RPC_TMC_GetSimpleMea = 2108;
    public const int RPC_TMC_GetFullMeas  = 2167;

    // Kontext: zuletzt gesendeter RPC (0 = unbekannt / kein Kontext)
    private int _letzterRpc = 0;

    // Informative RCs: Befehl wurde ausgeführt, Gerät meldet Zusatzinfo – kein Fehler.
    // GRC_TMC_NO_FULL_CORRECTION=1283, GRC_TMC_ACCURACY_GUARANTEE=1284,
    // GRC_TMC_ANGLE_OK=1285, GRC_TMC_ANGLE_NO_FULL_CORRECTION=1288
    // GRC_BAP_CHANGE_ALL_TO_DIST=3079 – EDM-Parameter auf Reflektor/IR umgestellt (OK)
    // GRC_BAP_CHANGE_ALL_TO_RL=3081   – EDM-Parameter auf Reflektorlos/RL umgestellt (OK)
    private static readonly HashSet<int> _informativeRcs = [1283, 1284, 1285, 1288, 3079, 3081];

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

        // ── Rückgabecode auswerten ────────────────────────────────────────────
        // Informative RCs (1283–1288): Daten sind vorhanden, aber nicht vollständig
        // korrigiert → weiter parsen, Hinweis in Bemerkung.
        // Echte Fehler: Messung sofort als Fehler markieren.
        if (rc != 0)
        {
            if (_informativeRcs.Contains(rc))
            {
                // Warnung, aber Daten weiter parsen
                m.Bemerkung = $"[Hinweis GRC={rc}: {GeoCOMReturnCodeText(rc)}]";
            }
            else
            {
                m.Typ       = MessungsTyp.Fehler;
                m.Bemerkung = $"GRC={rc}  {GeoCOMReturnCodeText(rc)}";
                return m;
            }
        }

        // ── Erfolgreiche oder informative Antwort: Daten interpretieren ───────
        return rpcKontext switch
        {
            RPC_TMC_GetAngle1    => ParseWinkelVoll(m, daten),
            RPC_TMC_GetAtmCorr   => ParseAtmKorrektur(m, daten),
            RPC_TMC_GetAngle     => ParseWinkel(m, daten),
            RPC_TMC_GetSimpleMea => ParseVollmessung(m, daten),
            RPC_TMC_GetFullMeas  => ParseVollmessungErweitert(m, daten),
            RPC_TMC_GetEdmMode   => ParseEdmModus(m, daten),
            RPC_TMC_GetPrismCorr => ParsePrismkorrektur(m, daten),
            RPC_BAP_GetTargetType => ParseZieltyp(m, daten),
            RPC_COM_GetSWBaud    => ParseBaudrate(m, daten),

            // Bestätigungen ohne Nutzdaten / Einzel-Wert-Antworten
            RPC_COM_NullProc       or
            RPC_BAP_MeasDist       or
            RPC_TMC_DoMeasure      or
            RPC_TMC_SetEdmMode     or
            RPC_TMC_SetAtmCorr     or
            RPC_TMC_SetPrismCorr   or
            RPC_BAP_SetTargetType  or
            RPC_COM_SetSWBaud      or
            RPC_EDM_Laserpointer   => ParseStatus(m, daten, rpcKontext),

            // Kein Kontext: aus Datenanzahl schließen
            _ => InferiereTyp(m, daten)
        };
    }

    // ── Atmosphärische Korrektur (TMC_GetAtmCorr, RPC 2029) ──────────────────
    // Antwort: RC, Lambda[double], Pressure[double], DryTemperature[double], WetTemperature[double]
    // PPM-Formel (Barrel & Sears, Leica-Variante):
    //   PPM = 281.8 − (79.661 × P) / (273.15 + T_trocken)
    //   Für PPM=0 bei T=15°C: P_ref = 281.8 × 288.15 / 79.661 ≈ 1019.4 mbar
    private static TachymeterMessung ParseAtmKorrektur(TachymeterMessung m, string[] d)
    {
        m.Typ = MessungsTyp.AtmKorrektur;
        if (d.Length >= 1 && TryParseDouble(d[0], out double lambda))
            m.Atm_Lambda_m     = lambda;
        if (d.Length >= 2 && TryParseDouble(d[1], out double druck))
            m.Atm_Druck_mbar   = druck;
        if (d.Length >= 3 && TryParseDouble(d[2], out double tTrock))
            m.Atm_TempTrock_C  = tTrock;
        if (d.Length >= 4 && TryParseDouble(d[3], out double tFeucht))
            m.Atm_TempFeucht_C = tFeucht;

        // PPM berechnen wenn Druck und Temperatur vorhanden
        if (m.Atm_Druck_mbar.HasValue && m.Atm_TempTrock_C.HasValue)
        {
            double ppm = 281.8 - 79.661 * m.Atm_Druck_mbar.Value
                         / (273.15 + m.Atm_TempTrock_C.Value);
            m.Atm_PPM = Math.Round(ppm, 2);
        }

        m.Bemerkung = m.Atm_PPM.HasValue
            ? $"PPM={m.Atm_PPM.Value:+0.00;-0.00;+0.00}  " +
              $"P={m.Atm_Druck_mbar:F1} mbar  " +
              $"T={m.Atm_TempTrock_C:F1}°C"
            : "AtmCorr gelesen (unvollständige Daten)";
        return m;
    }

    // ── Winkel vollständig mit Kompensator (TMC_GetAngle1, RPC 2003) ──────────
    // Antwort: RC, Hz, V, AngleAccuracy, AngleTime, CrossIncline, LengthIncline,
    //          AccuracyIncline, InclineTime, FaceDef
    private static TachymeterMessung ParseWinkelVoll(TachymeterMessung m, string[] d)
    {
        m.Typ = MessungsTyp.Winkel;
        if (d.Length >= 1 && TryParseDouble(d[0], out double hz))
            m.Hz_gon = RadNachGon(hz);
        if (d.Length >= 2 && TryParseDouble(d[1], out double v))
            m.V_gon  = RadNachGon(v);
        if (d.Length >= 3 && TryParseDouble(d[2], out double accH))
            m.WinkelGenauigkeit_cc = accH * (200.0 / Math.PI) * 10000;
        // d[3] = AngleTime [ms seit Einschalten], überspringen
        if (d.Length >= 5 && TryParseDouble(d[4], out double cross))
            m.KreuzNeigung_rad  = cross;
        if (d.Length >= 6 && TryParseDouble(d[5], out double length))
            m.LaengsNeigung_rad = length;
        return m;
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

    // ── Prismenkonstante (TMC_GetPrismCorr, RPC 2023) ────────────────────────
    // Antwort: RC, PrismCorr_m [double, Meter – negativ bei Standard-Leica-Prisma]
    // Gerät speichert Prismenkonstante in Metern (z.B. -0.034 m = -34 mm).
    private static TachymeterMessung ParsePrismkorrektur(TachymeterMessung m, string[] d)
    {
        m.Typ = MessungsTyp.Status;
        if (d.Length >= 1 && TryParseDouble(d[0], out double pk_m))
        {
            m.Prismenkonstante_mm = pk_m * 1000.0;   // m → mm
            m.Bemerkung = $"Prismenkonstante: {m.Prismenkonstante_mm:+0.0;-0.0;+0.0} mm";
        }
        return m;
    }

    // ── Zieltyp (BAP_GetTargetType, RPC 17022) ───────────────────────────────
    // Antwort: RC, TargetType  (0=BAP_REFL_USE Prisma/IR, 1=BAP_REFL_LESS Reflektorlos/RL)
    private static TachymeterMessung ParseZieltyp(TachymeterMessung m, string[] d)
    {
        m.Typ = MessungsTyp.EdmModusInfo;
        if (d.Length >= 1 && int.TryParse(d[0].Trim(), out int typ))
        {
            m.EdmModusRoh = typ;
            m.EdmModus    = typ == 0 ? MessungsEdmModus.Prisma : MessungsEdmModus.Reflektorlos;
            m.Bemerkung   = typ == 0
                ? "Zieltyp: Reflektor / Prisma (IR-EDM, BAP_REFL_USE)"
                : "Zieltyp: Reflektorlos (RL-EDM, BAP_REFL_LESS)";
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
            RPC_BAP_MeasDist      => "Messung gestartet (BAP_MeasDist)",
            RPC_TMC_DoMeasure     => "Messung gestartet",
            RPC_TMC_SetEdmMode    => "EDM-Modus gesetzt",
            RPC_TMC_SetAtmCorr    => "Atmosphärische Korrektur (PPM) gesetzt",
            RPC_BAP_SetTargetType => "Zieltyp (IR/RL) gesetzt",
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
        2  => MessungsEdmModus.Reflektorlos,   // EDM_SINGLE_LRRL (selten genutzt)
        3  => MessungsEdmModus.Prisma,
        4  => MessungsEdmModus.Prisma,          // Dauermessung Prisma
        5  => MessungsEdmModus.Reflektorlos,   // EDM_SINGLE_SRANGE – RL Kurzstrecke
        7  => MessungsEdmModus.Prisma,          // Tracking Prisma
        8  => MessungsEdmModus.Reflektorlos,   // EDM_CONT_REFLESS – RL Dauermessung
        10 => MessungsEdmModus.Reflektorlos,   // EDM_SINGLE_LRRL – RL Langdistanz
        11 => MessungsEdmModus.Reflektorlos,   // EDM_AVERAGE_SR – RL Mittelwert Langstrecke
        _  => MessungsEdmModus.Unbekannt
    };

    private static string EdmModusText(int code) => code switch
    {
        0  => "Nicht aktiv",
        1  => "EDM_SINGLE_TAPE – Folie/Kleintarget",
        2  => "EDM_SINGLE_LRRL – RL Langdistanz (alt)",
        3  => "EDM_SINGLE_PRISM – Prisma Einzelmessung",
        4  => "EDM_CONT_STANDARD – Prisma Dauermessung",
        5  => "EDM_SINGLE_SRANGE – RL Kurzstrecke Einzelmessung",
        6  => "EDM_CONT_DYNAMIC – Prisma Dauermessung dynamisch",
        7  => "EDM_SINGLE_TRACKING – Prisma Tracking",
        8  => "EDM_CONT_REFLESS – RL Dauermessung",
        9  => "EDM_SINGLE_FAST – Schnellmessung",
        10 => "EDM_SINGLE_LRRL – RL Langdistanz",
        11 => "EDM_AVERAGE_SR – RL Mittelwert Langstrecke",
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
        // TMC-Fehler/Hinweise (1280–1299) – korrekt laut GeoCOM Reference Manual V1.10
        1280 => "GRC_TMC_NO_MEASUREMENT – Keine Messung gestartet",
        1281 => "GRC_TMC_NOT_STABLE – Instrument nicht stabil",
        1282 => "GRC_TMC_DIST_WAIT – Messung läuft noch, Ergebnis noch nicht bereit",
        1283 => "GRC_TMC_NO_FULL_CORRECTION – Ergebnis nicht vollständig korrigiert (Kompensator/PPM prüfen!)",
        1284 => "GRC_TMC_ACCURACY_GUARANTEE – Genauigkeit nicht garantiert, Daten vorhanden",
        1285 => "GRC_TMC_ANGLE_OK – Nur Winkel gültig, keine Distanz",
        1286 => "GRC_TMC_SIGNAL_ERROR – Kein Reflexionssignal empfangen",
        1288 => "GRC_TMC_ANGLE_NO_FULL_CORRECTION – Winkel vorhanden, nicht vollständig korrigiert",
        1291 => "GRC_TMC_DIST_PPM – Falsche EDM/PPM-Einstellungen, keine Distanz",
        1292 => "GRC_TMC_DIST_ERROR – Distanzfehler (kein Reflex / falscher Modus?)",
        1293 => "GRC_TMC_BUSY – Messmodul beschäftigt (TMC_DoMeasure läuft noch)",
        508  => "GRC_TMC_NO_FULL_CORRECTION – Keine vollständige Korrektion",
        // EDM-Fehler (ab 1792)
        1792 => "GRC_EDM_BUSYMODE – EDM in parallelem Messmodus",
        1793 => "GRC_EDM_NO_VALID_MEAS – Keine gültige Messung (Reflektormodus/Ziel prüfen)",
        1800 => "GRC_EDM_DEV_NOT_INSTALLED – Reflektorlos-Option nicht installiert",
        // BAP-Hinweise (3079, 3081) – Befehl OK, Gerät meldet Parameteranpassung
        3079 => "GRC_BAP_CHANGE_ALL_TO_DIST – Alle EDM-Parameter auf Reflektor/IR umgestellt",
        3081 => "GRC_BAP_CHANGE_ALL_TO_RL – Alle EDM-Parameter auf Reflektorlos/RL umgestellt",
        _    => ""
    };

    private static bool TryParseDouble(string s, out double value) =>
        double.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
}
