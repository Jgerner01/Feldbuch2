namespace Feldbuch;

using System.Text;

/// <summary>
/// Testfenster für die GeoCOM-Schnittstelle (Leica TPS1200).
/// Zeigt Rohdaten, gesendete Befehle und geparste Messwerte farblich getrennt.
/// </summary>
public partial class FormTestmessungen : Form
{
    // ── Farben für Terminal-Ausgabe ────────────────────────────────────────────
    private static readonly Color FarbeRoh       = Color.FromArgb(180, 220, 160); // hellgrün  – empfangene Rohdaten
    private static readonly Color FarbeSenden    = Color.FromArgb(220, 200, 130); // hellgelb  – gesendete Befehle
    private static readonly Color FarbeGeparst   = Color.FromArgb(100, 210, 255); // hellblau  – geparste Messwerte
    private static readonly Color FarbeInfo      = Color.FromArgb(140, 150, 170); // grau      – Statusmeldungen
    private static readonly Color FarbeFehler    = Color.FromArgb(255, 110, 110); // hellrot   – Fehlermeldungen

    // ── EDM-Modus ─────────────────────────────────────────────────────────────
    // Drei Messmodi, zyklisch per Button wählbar:
    //   Prisma          – BAP_REFL_USE(0)  + TMC_SetEdmMode(3)  EDM_SINGLE_PRISM
    //   Reflektorlos-K  – BAP_REFL_LESS(1) + TMC_SetEdmMode(2)  EDM_SINGLE_LRRL (kurze Dist.)
    //   Reflektorlos-L  – BAP_REFL_LESS(1) + TMC_SetEdmMode(10) EDM_SINGLE_LRRL (lange Dist.)
    private enum MessModus { Prisma, ReflektorlosKurz, ReflektorlosLang }
    private MessModus _messModus = MessModus.Prisma;

    private const int BAP_REFL_USE      = 0;
    private const int BAP_REFL_LESS     = 1;
    private const int EDM_SINGLE_PRISM  = 3;   // IR Einzel-Prisma
    private const int EDM_SINGLE_RL_K   = 5;   // RL Einzelmessung Kurzstrecke (EDM_SINGLE_SRANGE)
    private const int EDM_SINGLE_RL_L   = 11;  // RL Mittelwert Langstrecke   (EDM_AVERAGE_SR)

    // ── Laserpointer ─────────────────────────────────────────────────────────
    private bool _laserAn = false;

    // ── GeoCOM-Parser mit Kontext-Tracking ────────────────────────────────────
    private readonly GeoCOMParser _parser = new();
    private readonly StringBuilder _zeilenPuffer = new();

    // ── Timer für Winkel-Dauerübertragung ─────────────────────────────────────
    private readonly System.Windows.Forms.Timer _winkelTimer = new() { Interval = 500 };
    private bool _winkelAktiv = false;

    // ── Timer für Dosenlibelle Live ───────────────────────────────────────────
    private readonly System.Windows.Forms.Timer _libelleTimer = new() { Interval = 800 };
    private bool _libelleAktiv = false;

    // Start-Sequenz: PPM lesen → setzen → Timer starten
    private enum LibelleZustand { Bereit, LesePPM, SetzePPM, Aktiv }
    private LibelleZustand _libelleZustand = LibelleZustand.Bereit;

    // ── Messungs-Zustandsmaschine ─────────────────────────────────────────────
    // WarteMessen:   TMC_DoMeasure gesendet, warte auf Bestätigung
    // WarteErgebnis: TMC_GetSimpleMea gesendet, warte auf Messdaten
    private enum MessungZustand { Bereit, WarteMessen, WarteErgebnis }
    private MessungZustand _messungZustand    = MessungZustand.Bereit;
    private bool           _messungLibelleWar = false;  // Libelle vor Messung aktiv?

    public FormTestmessungen()
    {
        InitializeComponent();
        TachymeterVerbindung.DatenEmpfangen += OnDatenEmpfangen;
        _winkelTimer.Tick  += WinkelTimer_Tick;
        _libelleTimer.Tick += LibelleTimer_Tick;
        AktualisiereStatus();
        AktualisiereModusButton();
    }

    // ── Verbindungsstatus ─────────────────────────────────────────────────────

    private void AktualisiereStatus()
    {
        if (TachymeterVerbindung.IstVerbunden)
        {
            lblStatusDot.BackColor  = Color.LimeGreen;
            lblStatusText.Text      = $"Verbunden  ({TachymeterVerbindung.Port})  –  {TachymeterVerbindung.BaudRate} Baud";
            lblStatusText.ForeColor = Color.DarkGreen;
        }
        else
        {
            lblStatusDot.BackColor  = Color.FromArgb(200, 50, 50);
            lblStatusText.Text      = "Kein Tachymeter verbunden  –  bitte zuerst im Tachymeter-Fenster verbinden";
            lblStatusText.ForeColor = Color.FromArgb(160, 40, 40);
        }
    }

    // ── Befehl senden mit Kontext-Tracking ───────────────────────────────────

    private void SendeBefehl(string befehl)
    {
        // RPC extrahieren und Parser-Kontext setzen → korrekte Typisierung der Antwort
        int rpc = GeoCOMParser.ExtrRpcAusAnfrage(befehl);
        if (rpc != 0) _parser.SetzeLetzenRpc(rpc);

        AppendFarbe($">> {befehl}", FarbeSenden);
        TachymeterVerbindung.GeoCOM_Senden(befehl);
    }

    // ── Rohdaten-Empfang – Puffer und Zeilenerkennung ─────────────────────────

    private void OnDatenEmpfangen(object? sender, string daten)
    {
        if (InvokeRequired) { BeginInvoke(() => OnDatenEmpfangen(sender, daten)); return; }
        VerarbeiteDaten(daten);
    }

    private void VerarbeiteDaten(string daten)
    {
        _zeilenPuffer.Append(daten);
        string puffer = _zeilenPuffer.ToString();

        // Vollständige Zeilen (CR+LF) herausschneiden und verarbeiten
        int pos;
        while ((pos = puffer.IndexOf('\n')) >= 0)
        {
            // Zeile extrahieren (CR optional entfernen)
            string zeile = puffer.Substring(0, pos).TrimEnd('\r');
            puffer = puffer.Substring(pos + 1);

            if (!string.IsNullOrWhiteSpace(zeile))
                VerarbeiteZeile(zeile);
        }

        _zeilenPuffer.Clear();
        _zeilenPuffer.Append(puffer);  // unvollständigen Rest aufbewahren
    }

    private void VerarbeiteZeile(string zeile)
    {
        // 1. Rohdaten anzeigen
        AppendFarbe($"<< {zeile}", FarbeRoh);

        // 2. RPC-Kontext sichern, bevor mögliche SendeBefehl-Aufrufe ihn überschreiben
        int rpcKontext = _parser_letzterRpc;

        // 3. Parsen
        var messung = GeoCOMParser.ParseAntwort(zeile, rpcKontext);
        if (messung == null) return;

        // 3. Geparste Darstellung
        if (messung.IstFehler)
        {
            AppendFarbe($"   !! FEHLER: {messung.Bemerkung}", FarbeFehler);
        }
        else if (messung.Typ == MessungsTyp.Status)
        {
            AppendFarbe($"   → {messung.Bemerkung}", FarbeInfo);
        }
        else if (messung.Typ == MessungsTyp.EdmModusInfo)
        {
            AppendFarbe($"   → {messung.Bemerkung}", FarbeInfo);
        }
        else if (messung.Typ == MessungsTyp.AtmKorrektur)
        {
            AppendFarbe($"   → {messung.Bemerkung}", FarbeGeparst);
            VerarbeiteAtmKorrektur(messung);
        }
        else if (messung.HatWinkel || messung.IstVollmessung)
        {
            AppendFarbe(FormatiereMessung(messung), FarbeGeparst);

            // Kompensatordaten an Libelle weitergeben (falls vorhanden)
            if (messung.KreuzNeigung_rad.HasValue || messung.LaengsNeigung_rad.HasValue)
            {
                AktualisiereLibelle(
                    messung.KreuzNeigung_rad  ?? 0.0,
                    messung.LaengsNeigung_rad ?? 0.0,
                    messung.ReturnCode        ?? 0);
            }
            else if (_libelleAktiv && rpcKontext == GeoCOMParser.RPC_TMC_GetAngle1)
            {
                // Angle1-Antwort ohne Kompensatordaten → Kompensator außerhalb Bereich
                pnlLibelle.MarciereUngueltig();
                lblNeigung.ForeColor = Color.FromArgb(220, 100, 50);
                lblNeigung.Text      = "Quer:    außerh. Messbereich\r\nLängs:   außerh. Messbereich";
            }
        }

        // ── Libelle-Startsequenz: Zustand weiterschalten ──────────────────────
        if (_libelleZustand == LibelleZustand.SetzePPM
            && messung.Typ == MessungsTyp.Status
            && rpcKontext == GeoCOMParser.RPC_TMC_SetAtmCorr)
        {
            AppendInfo("[INFO] Atmosphäre gesetzt. Starte Dosenlibelle Live...");
            _libelleAktiv   = true;
            _libelleZustand = LibelleZustand.Aktiv;
            _libelleTimer.Start();
            AktualisiereLibelleButton();
        }

        // ── Messungs-Zustandsmaschine ──────────────────────────────────────────
        if (_messungZustand == MessungZustand.WarteMessen
            && rpcKontext == GeoCOMParser.RPC_TMC_DoMeasure)
        {
            if (messung.IstFehler)
            {
                AppendFehler($"[FEHLER] Messung konnte nicht gestartet werden: {messung.Bemerkung}");
                MessungAbschliessen();
            }
            else
            {
                // DoMeasure bestätigt → jetzt Messwert abrufen
                _messungZustand    = MessungZustand.WarteErgebnis;
                _parser_letzterRpc = GeoCOMParser.RPC_TMC_GetSimpleMea;
                SendeBefehl("%R1Q,2108,0:5000,1");
            }
        }
        else if (_messungZustand == MessungZustand.WarteErgebnis
            && rpcKontext == GeoCOMParser.RPC_TMC_GetSimpleMea)
        {
            // Messergebnis (oder Fehler) empfangen → Messung abschließen
            MessungAbschliessen();
        }
    }

    // Workaround: letzten RPC aus Parser-Zustand abrufen
    // (GeoCOMParser ist eine Instanz; _letzterRpc nur intern – wir lesen ihn über die Anfrage-Methode)
    private int _parser_letzterRpc = 0;

    // ── Darstellung geparster Messwerte ───────────────────────────────────────

    private static string FormatiereMessung(TachymeterMessung m)
    {
        var sb = new StringBuilder("   → ");
        if (m.Hz_gon.HasValue)              sb.Append($"Hz: {m.Hz_gon.Value,10:F4} gon   ");
        if (m.V_gon.HasValue)               sb.Append($"V: {m.V_gon.Value,10:F4} gon   ");
        if (m.Schraegstrecke_m.HasValue)    sb.Append($"D: {m.Schraegstrecke_m.Value,9:F4} m   ");
        if (m.Horizontalstrecke_m.HasValue) sb.Append($"Dh: {m.Horizontalstrecke_m.Value,8:F4} m   ");
        if (m.Hoehenunterschied_m.HasValue) sb.Append($"Δh: {m.Hoehenunterschied_m.Value,8:F4} m");
        if (!string.IsNullOrEmpty(m.Bemerkung)) sb.Append($"   ({m.Bemerkung})");
        return sb.ToString();
    }

    // ── Farbige Ausgabe ───────────────────────────────────────────────────────

    private void AppendFarbe(string text, Color farbe)
    {
        txtDaten.SelectionStart  = txtDaten.TextLength;
        txtDaten.SelectionLength = 0;
        txtDaten.SelectionColor  = farbe;
        txtDaten.AppendText(text + "\r\n");
        txtDaten.SelectionColor  = FarbeRoh;   // Standardfarbe wiederherstellen
        txtDaten.SelectionStart  = txtDaten.TextLength;
        txtDaten.ScrollToCaret();
    }

    private void AppendInfo(string text)    => AppendFarbe(text, FarbeInfo);
    private void AppendFehler(string text)  => AppendFarbe(text, FarbeFehler);

    // ── Messung auslösen ──────────────────────────────────────────────────────
    //
    // Zwei-Schritt-Sequenz (Zustandsmaschine):
    //   Schritt 1: TMC_DoMeasure (RPC 2008, Cmd=1 DEF_DIST, Mode=1 AUTO_INC)
    //              Gerät startet die EDM-Messung, antwortet sofort mit GRC.
    //   Schritt 2: TMC_GetSimpleMea (RPC 2108, WaitTime=5000, Mode=1)
    //              Wird erst nach OK-Bestätigung von DoMeasure gesendet.
    //              Liefert Hz, V, Schrägdistanz.
    //
    // Kein BAP_SetTargetType vor der Messung – der EDM-Modus wird über
    // "Modus"-Button (TMC_SetEdmMode) separat verwaltet.

    private void btnMessung_Click(object? sender, EventArgs e)
    {
        if (_messungZustand != MessungZustand.Bereit) return;

        if (!TachymeterVerbindung.IstVerbunden)
        {
            AppendFehler("[FEHLER] Kein Tachymeter verbunden.");
            AktualisiereStatus();
            return;
        }

        try
        {
            btnMessung.Enabled = false;

            // Libelle während der Messung pausieren (verhindert Kollision auf serieller Schnittstelle)
            _messungLibelleWar = _libelleAktiv;
            if (_libelleAktiv) _libelleTimer.Stop();

            // Schritt 1: Messung starten
            _messungZustand = MessungZustand.WarteMessen;
            _parser_letzterRpc = GeoCOMParser.RPC_TMC_DoMeasure;
            SendeBefehl("%R1Q,2008,0:1,1");
        }
        catch (Exception ex)
        {
            AppendFehler($"[FEHLER] {ex.Message}");
            MessungAbschliessen();
        }
    }

    private void MessungAbschliessen()
    {
        _messungZustand = MessungZustand.Bereit;
        btnMessung.Enabled = true;

        // Libelle wieder starten falls sie vorher aktiv war
        if (_messungLibelleWar && _libelleAktiv)
            _libelleTimer.Start();
        _messungLibelleWar = false;
    }

    // ── EDM-Modus umschalten ──────────────────────────────────────────────────

    private void btnModus_Click(object? sender, EventArgs e)
    {
        // Zyklisch weiterschalten: Prisma → RL-Kurz → RL-Lang → Prisma
        _messModus = _messModus switch
        {
            MessModus.Prisma           => MessModus.ReflektorlosKurz,
            MessModus.ReflektorlosKurz => MessModus.ReflektorlosLang,
            _                          => MessModus.Prisma
        };

        if (TachymeterVerbindung.IstVerbunden)
        {
            try
            {
                // Schritt 1: IR/RL-Zieltyp setzen (BAP_SetTargetType, RPC 17021)
                int zieltyp = _messModus == MessModus.Prisma ? BAP_REFL_USE : BAP_REFL_LESS;
                _parser_letzterRpc = GeoCOMParser.RPC_BAP_SetTargetType;
                SendeBefehl($"%R1Q,17021,0:{zieltyp}");

                // Schritt 2: EDM-Messprogramm setzen (TMC_SetEdmMode, RPC 2020)
                int edmModus = _messModus switch
                {
                    MessModus.ReflektorlosKurz => EDM_SINGLE_RL_K,
                    MessModus.ReflektorlosLang => EDM_SINGLE_RL_L,
                    _                          => EDM_SINGLE_PRISM
                };
                _parser_letzterRpc = GeoCOMParser.RPC_TMC_SetEdmMode;
                SendeBefehl($"%R1Q,2020,0:{edmModus}");
            }
            catch (Exception ex) { AppendFehler($"[FEHLER] {ex.Message}"); }
        }
        else
        {
            AppendInfo("[INFO] Modus lokal geändert (kein Tachymeter verbunden).");
        }

        AktualisiereModusButton();
    }

    private void AktualisiereModusButton()
    {
        switch (_messModus)
        {
            case MessModus.Prisma:
                btnModus.Text      = "Modus: Prisma / Reflektor  →  weiter: RL Kurz";
                btnModus.BackColor = Color.FromArgb(38, 110, 72);
                btnModus.FlatAppearance.BorderColor = Color.FromArgb(26, 86, 54);
                break;
            case MessModus.ReflektorlosKurz:
                btnModus.Text      = "Modus: Reflektorlos Kurz  →  weiter: RL Lang";
                btnModus.BackColor = Color.FromArgb(140, 80, 20);
                btnModus.FlatAppearance.BorderColor = Color.FromArgb(110, 60, 10);
                break;
            case MessModus.ReflektorlosLang:
                btnModus.Text      = "Modus: Reflektorlos Lang  →  weiter: Prisma";
                btnModus.BackColor = Color.FromArgb(100, 50, 130);
                btnModus.FlatAppearance.BorderColor = Color.FromArgb(75, 30, 105);
                break;
        }
    }

    // ── Winkel-Dauerübertragung ───────────────────────────────────────────────

    private void btnWinkel_Click(object? sender, EventArgs e)
    {
        if (_winkelAktiv)
        {
            _winkelTimer.Stop();
            _winkelAktiv = false;
            AppendInfo("[INFO] Winkel-Dauerübertragung gestoppt.");
        }
        else
        {
            if (!TachymeterVerbindung.IstVerbunden)
            {
                AppendFehler("[FEHLER] Kein Tachymeter verbunden.");
                AktualisiereStatus();
                return;
            }
            _winkelAktiv = true;
            _winkelTimer.Start();
            AppendInfo("[INFO] Winkel-Dauerübertragung gestartet (alle 500 ms).");
        }
        AktualisiereWinkelButton();
    }

    private void WinkelTimer_Tick(object? sender, EventArgs e)
    {
        if (!TachymeterVerbindung.IstVerbunden)
        {
            _winkelTimer.Stop();
            _winkelAktiv = false;
            BeginInvoke(() =>
            {
                AppendFehler("[INFO] Verbindung verloren – Dauerübertragung gestoppt.");
                AktualisiereStatus();
                AktualisiereWinkelButton();
            });
            return;
        }
        try
        {
            _parser_letzterRpc = GeoCOMParser.RPC_TMC_GetAngle;
            TachymeterVerbindung.GeoCOM_Senden("%R1Q,2107,0:0");
            // Kein >> im Terminal bei Dauerübertragung (zu viele Zeilen)
        }
        catch (Exception ex)
        {
            _winkelTimer.Stop();
            _winkelAktiv = false;
            BeginInvoke(() =>
            {
                AppendFehler($"[FEHLER] {ex.Message} – Dauerübertragung gestoppt.");
                AktualisiereWinkelButton();
            });
        }
    }

    private void AktualisiereWinkelButton()
    {
        if (_winkelAktiv)
        {
            btnWinkel.Text      = "Winkel-Dauerübertragung stoppen";
            btnWinkel.BackColor = Color.FromArgb(160, 40, 40);
            btnWinkel.FlatAppearance.BorderColor = Color.FromArgb(130, 20, 20);
        }
        else
        {
            btnWinkel.Text      = "Winkel-Dauerübertragung starten";
            btnWinkel.BackColor = Color.FromArgb(80, 50, 130);
            btnWinkel.FlatAppearance.BorderColor = Color.FromArgb(60, 30, 110);
        }
    }

    // ── Laserpointer ─────────────────────────────────────────────────────────
    //   RPC 1004 = EDM_Laserpointer(bSwitch)  –  0=AUS, 1=EIN
    //   Achtung: Laserpointer beim Schließen des Fensters immer ausschalten!

    private void btnLaser_Click(object? sender, EventArgs e)
    {
        if (!TachymeterVerbindung.IstVerbunden)
        {
            AppendFehler("[FEHLER] Kein Tachymeter verbunden.");
            AktualisiereStatus();
            return;
        }
        try
        {
            _laserAn = !_laserAn;
            int schalt = _laserAn ? 1 : 0;
            _parser_letzterRpc = GeoCOMParser.RPC_EDM_Laserpointer;
            SendeBefehl($"%R1Q,1004,0:{schalt}");
        }
        catch (Exception ex)
        {
            _laserAn = false;   // Zustand zurücksetzen bei Fehler
            AppendFehler($"[FEHLER] {ex.Message}");
        }
        AktualisiereLaserButton();
    }

    private void AktualisiereLaserButton()
    {
        if (_laserAn)
        {
            btnLaser.Text      = "Laserpointer  AUS";
            btnLaser.BackColor = Color.FromArgb(200, 50, 40);
            btnLaser.FlatAppearance.BorderColor = Color.FromArgb(160, 30, 20);
        }
        else
        {
            btnLaser.Text      = "Laserpointer  EIN";
            btnLaser.BackColor = Color.FromArgb(75, 60, 60);
            btnLaser.FlatAppearance.BorderColor = Color.FromArgb(60, 40, 40);
        }
    }

    // ── Dosenlibelle Live ─────────────────────────────────────────────────────
    //   RPC 2003 = TMC_GetAngle1 – liefert Hz, V, Genauigkeit, AngleTime,
    //              CrossIncline [rad], LengthIncline [rad], AccuracyIncline,
    //              InclineTime, FaceDef

    private void btnLibelle_Click(object? sender, EventArgs e)
    {
        if (_libelleAktiv || _libelleZustand != LibelleZustand.Bereit)
        {
            // Stoppen
            _libelleTimer.Stop();
            _libelleAktiv   = false;
            _libelleZustand = LibelleZustand.Bereit;
            pnlLibelle.Zuruecksetzen();
            lblNeigung.ForeColor = Color.FromArgb(130, 160, 200);
            lblNeigung.Text      = "Quer:    –\r\nLängs:   –";
            lblPPM.Text          = "PPM: –";
            lblPPM.ForeColor     = Color.FromArgb(200, 160, 80);
            AppendInfo("[INFO] Dosenlibelle Live gestoppt.");
        }
        else
        {
            if (!TachymeterVerbindung.IstVerbunden)
            {
                AppendFehler("[FEHLER] Kein Tachymeter verbunden.");
                AktualisiereStatus();
                return;
            }

            // Start-Sequenz: 1. PPM lesen, 2. PPM=0 setzen, 3. Timer starten
            AppendInfo("[INFO] Lese atmosphärische Korrekturdaten (RPC 2029)...");
            _libelleZustand    = LibelleZustand.LesePPM;
            _parser_letzterRpc = GeoCOMParser.RPC_TMC_GetAtmCorr;
            SendeBefehl("%R1Q,2029,0:");
        }
        AktualisiereLibelleButton();
    }

    private void LibelleTimer_Tick(object? sender, EventArgs e)
    {
        if (!TachymeterVerbindung.IstVerbunden)
        {
            _libelleTimer.Stop();
            _libelleAktiv = false;
            BeginInvoke(() =>
            {
                pnlLibelle.Zuruecksetzen();
                lblNeigung.Text = "Quer:    –\r\nLängs:   –";
                AktualisiereStatus();
                AktualisiereLibelleButton();
            });
            return;
        }
        try
        {
            // TMC_GetAngle1 (RPC 2003): Winkelmessung + Kompensatordaten
            _parser_letzterRpc = GeoCOMParser.RPC_TMC_GetAngle1;
            TachymeterVerbindung.GeoCOM_Senden("%R1Q,2003,0:1");  // Mode=1 (TMC_AUTO_INC)
        }
        catch (Exception ex)
        {
            _libelleTimer.Stop();
            _libelleAktiv = false;
            BeginInvoke(() =>
            {
                AppendFehler($"[FEHLER] {ex.Message} – Libelle-Live gestoppt.");
                AktualisiereLibelleButton();
            });
        }
    }

    /// <summary>
    /// Verarbeitet AtmKorrektur-Antwort im Rahmen der Libelle-Startsequenz.
    ///
    /// Logik:
    ///  • Sind die Geräte-Werte für Luftdruck und Temperatur plausibel
    ///    (800–1100 mbar, −30…+55 °C) → Werte UNVERÄNDERT belassen, PPM anzeigen.
    ///  • Sind die Geräte-Werte unplausibel (nicht eingestellt, Null) →
    ///    ICAO-Standardatmosphäre setzen (P=1013.25 mbar, T=15 °C).
    /// </summary>
    private void VerarbeiteAtmKorrektur(TachymeterMessung m)
    {
        if (_libelleZustand != LibelleZustand.LesePPM) return;

        double p      = m.Atm_Druck_mbar  ?? 0.0;
        double tTrock = m.Atm_TempTrock_C ?? 0.0;
        double lambda = m.Atm_Lambda_m    ?? 6.58e-7;

        // Plausibilitätsprüfung der Geräteeinstellungen
        bool plausibel = p      is > 800.0 and < 1100.0
                      && tTrock is > -30.0  and < 55.0;

        if (plausibel && m.Atm_PPM.HasValue)
        {
            // ── Gerätewerte gültig → unverändert übernehmen ───────────────────
            double ppm = m.Atm_PPM.Value;
            lblPPM.Text      = $"PPM: {ppm:+0.00;-0.00;+0.00} ppm  " +
                               $"(P={p:F1} mbar, T={tTrock:F1} °C  –  Gerätewerte)";
            lblPPM.ForeColor = Color.FromArgb(100, 200, 100);

            AppendInfo($"[INFO] Atmosphäre OK: PPM={ppm:+0.0} ppm  " +
                       $"P={p:F1} mbar  T={tTrock:F1} °C  –  Gerätewerte übernommen.");

            // Keine SetAtmCorr nötig → direkt zu Aktiv
            _libelleZustand = LibelleZustand.Aktiv;
            _libelleAktiv   = true;
            _libelleTimer.Start();
            AktualisiereLibelleButton();
        }
        else
        {
            // ── Unplausible/fehlende Werte → ICAO-Standardatmosphäre setzen ───
            string grund = p <= 0
                ? "Luftdruck nicht eingestellt (P=0)"
                : $"unplausibler Wert (P={p:F0} mbar, T={tTrock:F0} °C)";

            AppendInfo($"[WARN] Atmosphäre: {grund} → setze ICAO-Standard (P=1013.25 mbar, T=15 °C).");

            lblPPM.Text      = $"PPM: {(m.Atm_PPM.HasValue ? m.Atm_PPM.Value.ToString("+0.00;-0.00;+0.00") : "?")} ppm " +
                               $"→ ICAO-Standard wird gesetzt";
            lblPPM.ForeColor = Color.FromArgb(220, 150, 40);

            // ICAO-Standard: T=15 °C, P=1013.25 mbar → PPM ≈ −0.5 (nahe 0)
            const double pICAO = 1013.25;
            const double tICAO = 15.0;
            _libelleZustand    = LibelleZustand.SetzePPM;
            _parser_letzterRpc = GeoCOMParser.RPC_TMC_SetAtmCorr;
            string cmd = string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"%R1Q,2028,0:{lambda:G6},{pICAO:F2},{tICAO:F1},{tICAO:F1}");
            AppendInfo($"[INFO] {cmd}");
            SendeBefehl(cmd);
        }
    }

    /// <summary>Aktualisiert Libelle-Zeichenfläche und Neigungsanzeige.</summary>
    private void AktualisiereLibelle(double kreuz_rad, double laengs_rad, int rc = 0)
    {
        // GRC=1283 + beide Werte exakt 0 → Kompensator konnte nicht messen
        bool ausserBereich = rc == 1283
                          && Math.Abs(kreuz_rad) < 1e-12
                          && Math.Abs(laengs_rad) < 1e-12;

        if (ausserBereich)
        {
            pnlLibelle.MarciereUngueltig();
            lblNeigung.ForeColor = Color.FromArgb(220, 100, 50);
            lblNeigung.Text      = "Quer:    außerh. Messbereich\r\nLängs:   außerh. Messbereich";
            return;
        }

        pnlLibelle.SetzeNeigung(kreuz_rad, laengs_rad, gueltig: true);
        lblNeigung.ForeColor = Color.FromArgb(130, 160, 200);

        // Anzeige in mgon und mm/m (1 rad = 200/π gon; mm/m = rad * 1000)
        double kreuz_mgon  = kreuz_rad  * (200.0 / Math.PI) * 1000.0;
        double laengs_mgon = laengs_rad * (200.0 / Math.PI) * 1000.0;
        double kreuz_mmm   = kreuz_rad  * 1000.0;
        double laengs_mmm  = laengs_rad * 1000.0;

        lblNeigung.Text =
            $"Quer:  {kreuz_mgon:+0.00;-0.00;+0.00} mgon  ({kreuz_mmm:+0.000;-0.000;+0.000} mm/m)\r\n" +
            $"Längs: {laengs_mgon:+0.00;-0.00;+0.00} mgon  ({laengs_mmm:+0.000;-0.000;+0.000} mm/m)";
    }

    private void AktualisiereLibelleButton()
    {
        if (_libelleAktiv)
        {
            btnLibelle.Text      = "Libelle Live  stoppen";
            btnLibelle.BackColor = Color.FromArgb(160, 40, 40);
            btnLibelle.FlatAppearance.BorderColor = Color.FromArgb(130, 20, 20);
        }
        else
        {
            btnLibelle.Text      = "Libelle Live  starten";
            btnLibelle.BackColor = Color.FromArgb(50, 90, 70);
            btnLibelle.FlatAppearance.BorderColor = Color.FromArgb(35, 70, 52);
        }
    }

    // ── Löschen ───────────────────────────────────────────────────────────────

    private void btnClear_Click(object? sender, EventArgs e)
    {
        txtDaten.Clear();
        _zeilenPuffer.Clear();
    }

    // ── Aufräumen ─────────────────────────────────────────────────────────────

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _winkelTimer.Stop();
        _winkelTimer.Dispose();
        _libelleTimer.Stop();
        _libelleTimer.Dispose();

        // Laserpointer sicher ausschalten
        if (_laserAn && TachymeterVerbindung.IstVerbunden)
        {
            try { TachymeterVerbindung.GeoCOM_Senden("%R1Q,1004,0:0"); } catch { }
        }

        TachymeterVerbindung.DatenEmpfangen -= OnDatenEmpfangen;
        base.OnFormClosing(e);
    }
}
