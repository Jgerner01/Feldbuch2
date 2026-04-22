namespace Feldbuch;

using System.Text;

/// <summary>
/// Messfenster – unterstützt Leica GeoCOM, Sokkia SDR und Topcon GTS/GPT.
/// Das aktive Protokoll richtet sich nach TachymeterVerbindung.Modell.
/// </summary>
public partial class FormTestmessungen : Form
{
    // ── Farben für Terminal-Ausgabe ────────────────────────────────────────────
    private static readonly Color FarbeRoh     = Color.FromArgb(180, 220, 160); // hellgrün  – empfangene Rohdaten
    private static readonly Color FarbeSenden  = Color.FromArgb(220, 200, 130); // hellgelb  – gesendete Befehle
    private static readonly Color FarbeGeparst = Color.FromArgb(100, 210, 255); // hellblau  – geparste Messwerte
    private static readonly Color FarbeInfo    = Color.FromArgb(140, 150, 170); // grau      – Statusmeldungen
    private static readonly Color FarbeFehler  = Color.FromArgb(255, 110, 110); // hellrot   – Fehlermeldungen

    // ── Protokoll ─────────────────────────────────────────────────────────────
    private ITachymeterBefehlsgeber _befehlsgeber;
    private ITachymeterDatenParser  _datenParser;
    private GeoCOMParser            _geocomParser;  // Instanz für Kontext-Tracking (GeoCOM)

    // ── EDM-Modus (GeoCOM) ────────────────────────────────────────────────────
    private enum MessModus { Prisma, ReflektorlosKurz, ReflektorlosLang }
    private MessModus _messModus = MessModus.Prisma;

    private const int BAP_REFL_USE  = 0;
    private const int BAP_REFL_LESS = 1;
    private const int EDM_SINGLE_PRISM = 3;
    private const int EDM_SINGLE_RL_K  = 5;
    private const int EDM_SINGLE_RL_L  = 11;

    // ── Laserpointer ─────────────────────────────────────────────────────────
    private bool _laserAn = false;

    // ── Zeilenpuffer ─────────────────────────────────────────────────────────
    private readonly StringBuilder _zeilenPuffer = new();

    // ── Timer für Winkel-Dauerübertragung ─────────────────────────────────────
    private readonly System.Windows.Forms.Timer _winkelTimer = new() { Interval = 500 };
    private bool _winkelAktiv = false;

    // ── Timer für Dosenlibelle Live ───────────────────────────────────────────
    private readonly System.Windows.Forms.Timer _libelleTimer = new() { Interval = 800 };
    private bool _libelleAktiv = false;

    private enum LibelleZustand { Bereit, LesePPM, SetzePPM, Aktiv }
    private LibelleZustand _libelleZustand = LibelleZustand.Bereit;

    // ── Messungs-Zustandsmaschine ─────────────────────────────────────────────
    private enum MessungZustand { Bereit, WarteMessen, WarteErgebnis }
    private MessungZustand _messungZustand    = MessungZustand.Bereit;
    private bool           _messungLibelleWar = false;
    private bool           _messungWinkelWar  = false;

    // Zuletzt gesendeter GeoCOM-RPC (für Antwort-Kontext)
    private int _letzterRpc = 0;

    public FormTestmessungen()
    {
        InitializeComponent();

        _geocomParser  = new GeoCOMParser();
        _befehlsgeber  = TachymeterBefehlsgeberFactory.Erstellen(TachymeterVerbindung.Modell);
        _datenParser   = TachymeterBefehlsgeberFactory.ErzeugeParser(TachymeterVerbindung.Modell);

        TachymeterVerbindung.DatenEmpfangen += OnDatenEmpfangen;
        _winkelTimer.Tick  += WinkelTimer_Tick;
        _libelleTimer.Tick += LibelleTimer_Tick;

        AktualisiereStatus();
        AktualisiereProtokollUI();
        AktualisiereModusButton();
    }

    // ── Protokoll-UI ─────────────────────────────────────────────────────────

    private void AktualisiereProtokollUI()
    {
        // Buttons je nach Protokoll aktivieren/ausgrauen
        btnModus.Enabled   = _befehlsgeber.UnterstueztEdmModus;
        btnWinkel.Enabled  = _befehlsgeber.UnterstueztWinkelLive;
        btnLaser.Enabled   = _befehlsgeber.UnterstueztLaserpointer;
        btnLibelle.Enabled = _befehlsgeber.UnterstueztLibelleLive;

        // Protokoll-Hinweis in Statusleiste
        lblProtokollHinweis.Text = $"Protokoll: {_befehlsgeber.Name}";

        // Fenster-Titel aktualisieren
        Text = $"Messwerte / Testmessungen  –  {_befehlsgeber.Name}";

        // Sokkia: Hinweis im Terminal ausgeben
        if (_befehlsgeber.IstPassivEmpfang)
        {
            AppendInfo($"[INFO] {_befehlsgeber.Name}: Passiver Empfangsmodus. " +
                       "Drücken Sie MEAS am Instrument – Messdaten erscheinen automatisch.");
            AppendInfo("[INFO] Taste 'Messung auslösen' sendet optional einen CRLF-Trigger.");
        }
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
            lblStatusText.Text      = "Kein Tachymeter verbunden  –  bitte zuerst im Kommunikationsfenster verbinden";
            lblStatusText.ForeColor = Color.FromArgb(160, 40, 40);
        }
    }

    // ── Befehl senden ────────────────────────────────────────────────────────

    private void SendeBefehl(string befehl, int rpcKontext = 0)
    {
        if (rpcKontext != 0)
        {
            _letzterRpc = rpcKontext;
            _geocomParser.SetzeLetzenRpc(rpcKontext);
        }
        AppendFarbe($">> {(string.IsNullOrEmpty(befehl) ? "<CRLF-Trigger>" : befehl)}", FarbeSenden);
        TachymeterVerbindung.GeoCOM_Senden(befehl);
    }

    // ── Rohdaten-Empfang ──────────────────────────────────────────────────────

    private void OnDatenEmpfangen(object? sender, string daten)
    {
        if (InvokeRequired) { BeginInvoke(() => OnDatenEmpfangen(sender, daten)); return; }
        VerarbeiteDaten(daten);
    }

    private void VerarbeiteDaten(string daten)
    {
        _zeilenPuffer.Append(daten);
        string puffer = _zeilenPuffer.ToString();

        // CR und LF beide als Zeilentrenner akzeptieren (CR allein z. B. bei GSI Online TPS300)
        int pos;
        while ((pos = puffer.IndexOfAny(new[] { '\r', '\n' })) >= 0)
        {
            string zeile = puffer[..pos];
            puffer = puffer[(pos + 1)..];
            if (!string.IsNullOrWhiteSpace(zeile))
                VerarbeiteZeile(zeile);
        }

        _zeilenPuffer.Clear();
        _zeilenPuffer.Append(puffer);
    }

    private void VerarbeiteZeile(string zeile)
    {
        // 1. Rohdaten immer anzeigen
        AppendFarbe($"<< {zeile}", FarbeRoh);

        // 2. Protokoll-spezifisch verarbeiten
        if (_befehlsgeber is GeoCOMBefehlsgeber)
            VerarbeiteZeileGeoCOM(zeile);
        else
            VerarbeiteZeileGeneric(zeile);
    }

    // ── GeoCOM-Verarbeitung (bestehende Logik) ────────────────────────────────

    private void VerarbeiteZeileGeoCOM(string zeile)
    {
        int rpcKontext = _letzterRpc;
        _geocomParser.SetzeLetzenRpc(rpcKontext);
        var messung = GeoCOMParser.ParseAntwort(zeile, rpcKontext);
        if (messung == null) return;

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
            AktualisiereMesswertAnzeige(messung);

            if (messung.KreuzNeigung_rad.HasValue || messung.LaengsNeigung_rad.HasValue)
            {
                AktualisiereLibelle(
                    messung.KreuzNeigung_rad  ?? 0.0,
                    messung.LaengsNeigung_rad ?? 0.0,
                    messung.ReturnCode        ?? 0);
            }
            else if (_libelleAktiv && rpcKontext == GeoCOMParser.RPC_TMC_GetAngle1)
            {
                pnlLibelle.MarciereUngueltig();
                lblNeigung.ForeColor = Color.FromArgb(220, 100, 50);
                lblNeigung.Text      = "Quer:    außerh. Messbereich\r\nLängs:   außerh. Messbereich";
            }
        }

        // Libelle-Startsequenz weiterschalten
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

        // Messungs-Zustandsmaschine
        if (_messungZustand == MessungZustand.WarteMessen
            && rpcKontext == _befehlsgeber.MessSchritt1Rpc)
        {
            if (messung.Typ == MessungsTyp.Fehler)
            {
                AppendFehler($"[FEHLER] Messung konnte nicht gestartet werden: {messung.Bemerkung}");
                MessungAbschliessen();
            }
            else
            {
                _messungZustand = MessungZustand.WarteErgebnis;
                var step2 = _befehlsgeber.MessErgebnisBefehl()!;
                SendeBefehl(step2, _befehlsgeber.MessSchritt2Rpc);
            }
        }
        else if (_messungZustand == MessungZustand.WarteErgebnis
            && rpcKontext == GeoCOMParser.RPC_TMC_GetSimpleMea)
        {
            MessungAbschliessen();
        }
    }

    // ── Generische Verarbeitung (Sokkia SDR, Topcon, GSI Online) ─────────────

    private void VerarbeiteZeileGeneric(string zeile)
    {
        var messung = _datenParser.ParseZeile(zeile);
        if (messung == null) return;

        if (messung.Typ == MessungsTyp.Fehler)
        {
            AppendFarbe($"   !! FEHLER: {messung.Bemerkung}", FarbeFehler);
            if (_messungZustand == MessungZustand.WarteMessen)
                MessungAbschliessen();
        }
        else if (messung.Typ == MessungsTyp.Status)
        {
            AppendFarbe($"   → {messung.Bemerkung}", FarbeInfo);
        }
        else if (messung.Typ == MessungsTyp.Koordinate)
        {
            AppendFarbe(FormatiereKoordinate(messung), FarbeGeparst);
            AktualisiereMesswertAnzeige(messung);
        }
        else if (messung.HatWinkel || messung.IstVollmessung)
        {
            AppendFarbe(FormatiereMessung(messung), FarbeGeparst);
            AktualisiereMesswertAnzeige(messung);

            if (_messungZustand == MessungZustand.WarteMessen)
                MessungAbschliessen();
        }
    }

    // ── Messwert-Anzeige (rechte Spalte) ─────────────────────────────────────

    private void AktualisiereMesswertAnzeige(TachymeterMessung m)
    {
        if (m.Hz_gon.HasValue)             lblHz.Text = $"{m.Hz_gon.Value,10:F4} gon";
        if (m.V_gon.HasValue)              lblV.Text  = $"{m.V_gon.Value,10:F4} gon";
        if (m.Schraegstrecke_m.HasValue)   lblSd.Text = $"{m.Schraegstrecke_m.Value,9:F4} m";
        if (m.Horizontalstrecke_m.HasValue) lblHd.Text = $"{m.Horizontalstrecke_m.Value,9:F4} m";
        if (m.Hoehenunterschied_m.HasValue) lblDh.Text = $"{m.Hoehenunterschied_m.Value,9:F4} m";
        if (m.E_m.HasValue)                lblHz.Text = $"E: {m.E_m.Value:F3}";
        if (m.N_m.HasValue)                lblV.Text  = $"N: {m.N_m.Value:F3}";
        if (m.H_m.HasValue)                lblSd.Text = $"H: {m.H_m.Value:F3}";
    }

    // ── Messung auslösen ──────────────────────────────────────────────────────

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

            if (_befehlsgeber.IstPassivEmpfang)
            {
                // Passiver Modus (Sokkia): CRLF senden als optionaler Trigger
                AppendInfo("[INFO] Passiver Empfang: Sende CRLF-Trigger...");
                _messungZustand = MessungZustand.WarteMessen;
                TachymeterVerbindung.GeoCOM_Senden("");
            }
            else if (_befehlsgeber is GeoCOMBefehlsgeber)
            {
                // GeoCOM: zweistufige Messung
                _messungLibelleWar = _libelleAktiv;
                if (_libelleAktiv) _libelleTimer.Stop();
                _messungWinkelWar = _winkelAktiv;
                if (_winkelAktiv) _winkelTimer.Stop();

                _messungZustand = MessungZustand.WarteMessen;
                SendeBefehl(_befehlsgeber.MessTriggerBefehl()!, _befehlsgeber.MessSchritt1Rpc);
            }
            else
            {
                // Topcon u.a.: CRLF-Trigger
                _messungZustand = MessungZustand.WarteMessen;
                SendeBefehl(_befehlsgeber.MessTriggerBefehl()!);
            }
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

        if (_messungLibelleWar && _libelleAktiv)
            _libelleTimer.Start();
        _messungLibelleWar = false;

        if (_messungWinkelWar)
            _winkelTimer.Start();
        _messungWinkelWar = false;
    }

    // ── EDM-Modus umschalten (GeoCOM) ─────────────────────────────────────────

    private void btnModus_Click(object? sender, EventArgs e)
    {
        _messModus = _messModus switch
        {
            MessModus.Prisma           => MessModus.ReflektorlosKurz,
            MessModus.ReflektorlosKurz => MessModus.ReflektorlosLang,
            _                          => MessModus.Prisma
        };

        if (TachymeterVerbindung.IstVerbunden && _befehlsgeber.UnterstueztEdmModus)
        {
            try
            {
                int zieltyp  = _messModus == MessModus.Prisma ? BAP_REFL_USE : BAP_REFL_LESS;
                int edmModus = _messModus switch
                {
                    MessModus.ReflektorlosKurz => EDM_SINGLE_RL_K,
                    MessModus.ReflektorlosLang => EDM_SINGLE_RL_L,
                    _                          => EDM_SINGLE_PRISM
                };

                var befehle = _befehlsgeber.EdmModusBefehle(zieltyp, edmModus);
                if (befehle != null)
                {
                    SendeBefehl(befehle[0], GeoCOMParser.RPC_BAP_SetTargetType);
                    if (befehle.Length > 1)
                        SendeBefehl(befehle[1], GeoCOMParser.RPC_TMC_SetEdmMode);
                }
            }
            catch (Exception ex) { AppendFehler($"[FEHLER] {ex.Message}"); }
        }
        else
        {
            AppendInfo("[INFO] Modus lokal geändert (kein Gerät verbunden oder Funktion nicht unterstützt).");
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
        if (!_befehlsgeber.UnterstueztEdmModus)
        {
            btnModus.Text      = "EDM-Modus: am Gerät einstellen";
            btnModus.BackColor = Color.FromArgb(70, 75, 85);
            btnModus.FlatAppearance.BorderColor = Color.FromArgb(50, 55, 65);
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
        if (!TachymeterVerbindung.IstVerbunden || !_befehlsgeber.UnterstueztWinkelLive)
        {
            _winkelTimer.Stop();
            _winkelAktiv = false;
            BeginInvoke(() => { AktualisiereStatus(); AktualisiereWinkelButton(); });
            return;
        }
        try
        {
            var cmd = _befehlsgeber.WinkelBefehl();
            if (cmd != null)
            {
                _letzterRpc = _befehlsgeber.WinkelRpc;
                _geocomParser.SetzeLetzenRpc(_letzterRpc);
                TachymeterVerbindung.GeoCOM_Senden(cmd);
            }
        }
        catch (Exception ex)
        {
            _winkelTimer.Stop();
            _winkelAktiv = false;
            BeginInvoke(() => { AppendFehler($"[FEHLER] {ex.Message}"); AktualisiereWinkelButton(); });
        }
    }

    private void AktualisiereWinkelButton()
    {
        if (_winkelAktiv)
        {
            btnWinkel.Text      = "Winkel-Live  stoppen";
            btnWinkel.BackColor = Color.FromArgb(160, 40, 40);
            btnWinkel.FlatAppearance.BorderColor = Color.FromArgb(130, 20, 20);
        }
        else
        {
            btnWinkel.Text      = "Winkel-Live  starten";
            btnWinkel.BackColor = Color.FromArgb(80, 50, 130);
            btnWinkel.FlatAppearance.BorderColor = Color.FromArgb(60, 30, 110);
        }
    }

    // ── Laserpointer ─────────────────────────────────────────────────────────

    private void btnLaser_Click(object? sender, EventArgs e)
    {
        if (!TachymeterVerbindung.IstVerbunden)
        {
            AppendFehler("[FEHLER] Kein Tachymeter verbunden.");
            return;
        }
        try
        {
            _laserAn = !_laserAn;
            var cmd = _befehlsgeber.LaserBefehl(_laserAn);
            if (cmd != null)
                SendeBefehl(cmd, GeoCOMParser.RPC_EDM_Laserpointer);
        }
        catch (Exception ex)
        {
            _laserAn = false;
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

    private void btnLibelle_Click(object? sender, EventArgs e)
    {
        if (_libelleAktiv || _libelleZustand != LibelleZustand.Bereit)
        {
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
                return;
            }
            AppendInfo("[INFO] Lese atmosphärische Korrekturdaten (RPC 2029)...");
            _libelleZustand = LibelleZustand.LesePPM;
            _letzterRpc     = GeoCOMParser.RPC_TMC_GetAtmCorr;
            _geocomParser.SetzeLetzenRpc(_letzterRpc);
            TachymeterVerbindung.GeoCOM_Senden("%R1Q,2029,0:");
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
            _letzterRpc = _befehlsgeber.LibelleRpc;
            _geocomParser.SetzeLetzenRpc(_letzterRpc);
            var cmd = _befehlsgeber.LibelleBefehl();
            if (cmd != null)
                TachymeterVerbindung.GeoCOM_Senden(cmd);
        }
        catch (Exception ex)
        {
            _libelleTimer.Stop();
            _libelleAktiv = false;
            BeginInvoke(() => { AppendFehler($"[FEHLER] {ex.Message}"); AktualisiereLibelleButton(); });
        }
    }

    private void VerarbeiteAtmKorrektur(TachymeterMessung m)
    {
        if (_libelleZustand != LibelleZustand.LesePPM) return;

        double p      = m.Atm_Druck_mbar  ?? 0.0;
        double tTrock = m.Atm_TempTrock_C ?? 0.0;
        double lambda = m.Atm_Lambda_m    ?? 6.58e-7;

        bool plausibel = p      is > 800.0 and < 1100.0
                      && tTrock is > -30.0  and < 55.0;

        if (plausibel && m.Atm_PPM.HasValue)
        {
            double ppm = m.Atm_PPM.Value;
            lblPPM.Text      = $"PPM: {ppm:+0.00;-0.00;+0.00} ppm  (P={p:F1} mbar, T={tTrock:F1} °C)";
            lblPPM.ForeColor = Color.FromArgb(100, 200, 100);
            AppendInfo($"[INFO] Atmosphäre OK: PPM={ppm:+0.0}  P={p:F1} mbar  T={tTrock:F1} °C");

            _libelleZustand = LibelleZustand.Aktiv;
            _libelleAktiv   = true;
            _libelleTimer.Start();
            AktualisiereLibelleButton();
        }
        else
        {
            AppendInfo($"[WARN] Atmosphäre unplausibel → setze ICAO-Standard (P=1013.25 mbar, T=15 °C).");
            lblPPM.Text      = "PPM: wird gesetzt (ICAO-Standard)";
            lblPPM.ForeColor = Color.FromArgb(220, 150, 40);

            _libelleZustand = LibelleZustand.SetzePPM;
            _letzterRpc     = GeoCOMParser.RPC_TMC_SetAtmCorr;
            _geocomParser.SetzeLetzenRpc(_letzterRpc);
            string cmd = string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"%R1Q,2028,0:{lambda:G6},1013.25,15.0,15.0");
            SendeBefehl(cmd);
        }
    }

    private void AktualisiereLibelle(double kreuz_rad, double laengs_rad, int rc = 0)
    {
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

    // ── Darstellung ───────────────────────────────────────────────────────────

    private static string FormatiereMessung(TachymeterMessung m)
    {
        var sb = new StringBuilder("   → ");
        if (m.Hz_gon.HasValue)              sb.Append($"Hz: {m.Hz_gon.Value,10:F4} gon   ");
        if (m.V_gon.HasValue)               sb.Append($"V: {m.V_gon.Value,10:F4} gon   ");
        if (m.Schraegstrecke_m.HasValue)    sb.Append($"D: {m.Schraegstrecke_m.Value,9:F4} m   ");
        if (m.Horizontalstrecke_m.HasValue) sb.Append($"Dh: {m.Horizontalstrecke_m.Value,8:F4} m   ");
        if (m.Hoehenunterschied_m.HasValue) sb.Append($"Δh: {m.Hoehenunterschied_m.Value,8:F4} m");
        if (!string.IsNullOrEmpty(m.Bemerkung)) sb.Append($"  ({m.Bemerkung})");
        return sb.ToString();
    }

    private static string FormatiereKoordinate(TachymeterMessung m)
    {
        var sb = new StringBuilder("   → KOORD ");
        if (!string.IsNullOrEmpty(m.PunktNr)) sb.Append($"Pkt:{m.PunktNr}  ");
        if (m.E_m.HasValue) sb.Append($"E:{m.E_m.Value:F3}  ");
        if (m.N_m.HasValue) sb.Append($"N:{m.N_m.Value:F3}  ");
        if (m.H_m.HasValue) sb.Append($"H:{m.H_m.Value:F3}");
        return sb.ToString();
    }

    // ── Farbige Ausgabe ───────────────────────────────────────────────────────

    private void AppendFarbe(string text, Color farbe)
    {
        txtDaten.SelectionStart  = txtDaten.TextLength;
        txtDaten.SelectionLength = 0;
        txtDaten.SelectionColor  = farbe;
        txtDaten.AppendText(text + "\r\n");
        txtDaten.SelectionColor  = FarbeRoh;
        txtDaten.SelectionStart  = txtDaten.TextLength;
        txtDaten.ScrollToCaret();
    }

    private void AppendInfo(string text)   => AppendFarbe(text, FarbeInfo);
    private void AppendFehler(string text) => AppendFarbe(text, FarbeFehler);

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

        if (_laserAn && TachymeterVerbindung.IstVerbunden)
        {
            try { TachymeterVerbindung.GeoCOM_Senden("%R1Q,1004,0:0"); } catch { }
        }

        TachymeterVerbindung.DatenEmpfangen -= OnDatenEmpfangen;
        base.OnFormClosing(e);
    }
}
