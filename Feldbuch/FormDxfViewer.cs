namespace Feldbuch;

using System.Globalization;
using System.Text;

public partial class FormDxfViewer : Form
{
    // ── Öffentliche Eigenschaften ─────────────────────────────────────────────
    public (double X, double Y)? GepickterPunkt { get; private set; }
    public string AktuellePunktNr => txtPunktNr.Text.Trim();
    public string AktuellerCode   => txtCode.Text.Trim();

    public static string AnschlusspunktePfad =>
        ProjektManager.GetPfad("Anschlusspunkte.csv");

    // ── Private Felder ────────────────────────────────────────────────────────
    private string _aktuellerDxfPfad = "";

    // Messmodus: Stationierung oder Neupunkt
    private bool _istNeupunktModus = false;

    // Warnung bei Messung ohne gültige Stationierung bereits gezeigt
    private bool _stationierungsWarnungGezeigt = false;

    // GeoCOM-Zustandsmaschine
    // Sequenz: DoMeasure → GetSimpleMea → GetAngle1 → GetAtmCorr → GetEdmMode → GetPrismCorr → Verarbeitung
    private enum MessungZustand
    {
        Bereit,
        WarteMessen,       // DoMeasure gesendet
        WarteErgebnis,     // GetSimpleMea gesendet  – Hz/V/Dist
        WarteKompensator,  // GetAngle1 gesendet      – Kompensator (CrossIncline, LengthIncline)
        WarteAtmKorr,      // GetAtmCorr gesendet     – PPM, Druck, Temperatur
        WarteEdmModus,     // GetEdmMode gesendet     – EDM-Modus (Prisma/Folie/RL)
        WartePrisma        // GetPrismCorr gesendet   – Prismenkonstante [mm]
    }
    private MessungZustand    _messungZustand    = MessungZustand.Bereit;
    private int               _parser_letzterRpc = 0;
    private TachymeterMessung? _sammlung          = null;   // akkumulierte Messung über alle Schritte
    private readonly GeoCOMParser _parser = new();
    private readonly StringBuilder _zeilenPuffer = new();

    // ── EDM / Laser-Zustand ───────────────────────────────────────────────────
    private bool    _edmIstPrisma        = true;   // false = Reflektorlos
    private bool    _laserAktiv          = false;
    private decimal _prismaKonstante_mm  = 0m;     // Letzte Auswahl aus FormPrismenkonstante
    private string  _prismaName          = "GPR1 Standard";

    // Punktnummer-Auto-Increment
    private int _neupunktZaehler;

    // ── Konstruktor ───────────────────────────────────────────────────────────
    public FormDxfViewer()
    {
        InitializeComponent();

        // Zähler aus ProjektManager laden
        _neupunktZaehler = ProjektManager.NeupunktZaehler;

        // Stationsdaten laden (aktuellen Standpunkt aus ProjektdatenManager)
        StationsdatenManager.LadeAktuellenStandpunkt();
        if (StationsdatenManager.AktiveStation.InstrumentenHoehe > 0)
            txtInstrHoehe.Text = StationsdatenManager.InstrumentenHoehe
                .ToString("F3", CultureInfo.InvariantCulture);

        // Standpunktnummer in Eingabefeld zeigen
        if (!string.IsNullOrEmpty(StationsdatenManager.StandpunktNr))
            txtStandpunktNr.Text = StationsdatenManager.StandpunktNr;
        else if (!string.IsNullOrEmpty(ProjektManager.LetzteStandpunktNr))
            txtStandpunktNr.Text = ProjektManager.LetzteStandpunktNr;

        // Snap-Zustand laden
        string? snapState = ProjektdatenManager.GetValue("Einstellungen", "Snap");
        canvas.SnapActive = snapState != "Inaktiv";
        UpdateSnapButton();

        // Katasterpunkte-Zustand laden
        string? punkteState = ProjektdatenManager.GetValue("Einstellungen", "Katasterpunkte");
        canvas.PunkteVisible = punkteState != "Inaktiv";
        UpdatePunkteButton();

        // Neupunkte + Residual Sichtbarkeit aus ProjektManager
        canvas.NeupunkteVisible = ProjektManager.NeupunkteVisible;
        canvas.ResidualVisible  = ProjektManager.ResidualVisible;

        // Events
        canvas.PointPicked       += OnPointPicked;
        canvas.RectangleSelected += OnRectangleSelected;
        FormClosed               += OnFormClosed;
        Activated                += OnFormActivated;

        // TachymeterVerbindung anschließen
        TachymeterVerbindung.DatenEmpfangen += OnTachymeterDaten;

        // Manager initialisieren
        ImportPunkteManager.Initialize(ProjektManager.GetPfad("ImportPunkte.json"));
        NeupunkteManager.Initialize(
            ProjektManager.GetPfad(ProjektManager.ProjektName + "-Neupunkte.json"));

        // Overlays aufbauen
        BaueOverlay();
        BaueNeupunkteOverlay();
        BaueImportOverlay();

        // DXF laden
        if (ProjektManager.HatDxfDatei)
            LadeDxfDatei(ProjektManager.DxfPfad);
        else if (canvas.OverlayEntities.Count > 0 || canvas.ImportLayers.Count > 0
              || canvas.NeupunkteEntities.Count > 0)
            canvas.FitToView();

        // Signallampe aktualisieren
        AktualisiereSignallampe();

        // EDM- und Laser-Button initialisieren
        AktualisiereEdmButton();
        AktualisiereLaserButton();

        // Messmodus initialisieren
        AktualisiereModus();

        // Neupunkte-Checkbox in Statusleiste einrichten
        BaueNeupunkteCheckbox();
    }

    // ── Neupunkte-Checkbox in der Statusleiste ────────────────────────────────
    private CheckBox? _chkNeupunkte;
    private CheckBox? _chkResidual;

    private void BaueNeupunkteCheckbox()
    {
        _chkNeupunkte = new CheckBox
        {
            Text     = "Neupunkte",
            Checked  = canvas.NeupunkteVisible,
            AutoSize = true,
            Font     = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(30, 30, 30),
            Margin   = new Padding(6, 3, 0, 0),
            Cursor   = Cursors.Hand
        };
        _chkNeupunkte.CheckedChanged += (s, e) =>
        {
            canvas.NeupunkteVisible   = _chkNeupunkte.Checked;
            ProjektManager.NeupunkteVisible = _chkNeupunkte.Checked;
            ProjektManager.SpeichereOptionen();
            canvas.Invalidate();
        };
        new ToolTip().SetToolTip(_chkNeupunkte, "Gemessene Neupunkte ein-/ausblenden");

        _chkResidual = new CheckBox
        {
            Text      = "Residuen",
            Checked   = canvas.ResidualVisible,
            AutoSize  = true,
            Font      = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(30, 30, 30),
            Margin    = new Padding(6, 3, 0, 0),
            Cursor    = Cursors.Hand
        };
        _chkResidual.CheckedChanged += (s, e) =>
        {
            canvas.ResidualVisible   = _chkResidual.Checked;
            ProjektManager.ResidualVisible  = _chkResidual.Checked;
            ProjektManager.SpeichereOptionen();
            canvas.Invalidate();
        };
        new ToolTip().SetToolTip(_chkResidual, "Residual-Marker (Stationierung) ein-/ausblenden");

        flpLayers.Controls.Add(_chkNeupunkte);
        flpLayers.Controls.Add(_chkResidual);
    }

    // ── DXF-Punkt-Marker Checkbox ─────────────────────────────────────────────
    private CheckBox? _chkPunktMarker;

    private void BauePunktMarkerCheckbox()
    {
        // Bestehende Checkbox entfernen falls vorhanden
        if (_chkPunktMarker != null) flpLayers.Controls.Remove(_chkPunktMarker);

        _chkPunktMarker = new CheckBox
        {
            Text      = "DXF-Punkte",
            Checked   = canvas.PunktMarkerVisible,
            AutoSize  = true,
            Font      = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(30, 30, 30),
            Margin    = new Padding(6, 3, 0, 0),
            Cursor    = Cursors.Hand
        };
        _chkPunktMarker.CheckedChanged += (s, e) =>
        {
            canvas.PunktMarkerVisible = _chkPunktMarker.Checked;
            canvas.Invalidate();
        };
        new ToolTip().SetToolTip(_chkPunktMarker, "DXF-Punktnummern ein-/ausblenden");

        // Vor Neupunkte/Residual einfügen
        int idx = flpLayers.Controls.IndexOf(_chkNeupunkte);
        if (idx >= 0) flpLayers.Controls.SetChildIndex(_chkPunktMarker, idx);
        else flpLayers.Controls.Add(_chkPunktMarker);
    }

    // ── Signallampe ───────────────────────────────────────────────────────────
    // Farbe und Tooltip werden aus dem aktuellen Stationierungsergebnis abgeleitet.
    private Color _signalFarbe = Color.Black;
    private string _signalTooltip = "Keine Stationierung";

    public void AktualisiereSignallampe()
    {
        var ergebnis = StationsdatenManager.AktuellesErgebnis;
        bool hatStation = StationsdatenManager.HatGueltigeStation;

        if (!hatStation || ergebnis == null)
        {
            _signalFarbe   = Color.Black;
            _signalTooltip = "Keine gültige Stationierung";
        }
        else
        {
            double s0 = ergebnis.s0_mm;
            if (s0 > 15 || ergebnis.Redundanz <= 0)
            {
                _signalFarbe   = Color.FromArgb(220, 30,  30);
                _signalTooltip = $"Schwache Stationierung: s₀={s0:F1} mm  r={ergebnis.Redundanz}";
            }
            else if (s0 > 5)
            {
                _signalFarbe   = Color.FromArgb(230, 160, 0);
                _signalTooltip = $"Bedingt brauchbar: s₀={s0:F1} mm  r={ergebnis.Redundanz}";
            }
            else
            {
                _signalFarbe   = Color.FromArgb(30,  190, 50);
                _signalTooltip = $"Gute Stationierung: s₀={s0:F2} mm  r={ergebnis.Redundanz}";
            }
        }

        pnlLampe.Invalidate();
        new ToolTip().SetToolTip(pnlLampe, _signalTooltip);
        lblLampeInfo.Text = _signalFarbe == Color.Black
            ? "–"
            : $"s₀={ergebnis!.s0_mm:F1} mm";
    }

    private void PnlLampe_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        float r = Math.Min(pnlLampe.Width, pnlLampe.Height) / 2f - 2f;
        float cx = pnlLampe.Width  / 2f;
        float cy = pnlLampe.Height / 2f;

        // Glow-Effekt (heller innerer Ring)
        if (_signalFarbe != Color.Black)
        {
            using var glowBrush = new System.Drawing.Drawing2D.PathGradientBrush(
                new PointF[] {
                    new(cx, cy - r), new(cx + r, cy),
                    new(cx, cy + r), new(cx - r, cy)
                })
            {
                CenterColor    = Color.White,
                SurroundColors = new[] { _signalFarbe }
            };
            g.FillEllipse(glowBrush, cx - r, cy - r, r * 2, r * 2);
        }

        using var fillBrush = new SolidBrush(_signalFarbe);
        using var pen       = new Pen(Color.FromArgb(80, 80, 80), 1.5f);
        if (_signalFarbe == Color.Black)
            g.FillEllipse(fillBrush, cx - r, cy - r, r * 2, r * 2);
        g.DrawEllipse(pen, cx - r, cy - r, r * 2, r * 2);

        // Kleiner Glanzpunkt oben-links
        if (_signalFarbe != Color.Black)
        {
            using var gBrush = new SolidBrush(Color.FromArgb(120, 255, 255, 255));
            float gr = r * 0.35f;
            g.FillEllipse(gBrush, cx - r * 0.5f - gr, cy - r * 0.55f - gr, gr * 2, gr * 2);
        }
    }

    // ── Formular aktiviert (Rückkehr vom Stationierungsfenster) ──────────────
    private void OnFormActivated(object? sender, EventArgs e)
    {
        // Stationierung kann sich geändert haben → nachladen und aktualisieren
        StationsdatenManager.LadeAktuellenStandpunkt();
        AktualisiereSignallampe();
        BaueOverlay();
        BaueNeupunkteOverlay();
        canvas.Invalidate();
    }

    // ── Messmodus: Stationierung ↔ Neupunkt ──────────────────────────────────
    private void btnModus_Click(object? sender, EventArgs e)
    {
        _istNeupunktModus = !_istNeupunktModus;
        AktualisiereModus();
    }

    private void AktualisiereModus()
    {
        if (_istNeupunktModus)
        {
            btnModus.Text      = "Neupunkt";
            btnModus.BackColor = Color.FromArgb(20, 80, 200);
            btnModus.FlatAppearance.BorderColor = Color.FromArgb(10, 60, 160);
            // Auto-Increment Punktnummer anzeigen
            if (string.IsNullOrWhiteSpace(txtPunktNr.Text) ||
                int.TryParse(txtPunktNr.Text, out _))
                txtPunktNr.Text = _neupunktZaehler.ToString();
        }
        else
        {
            btnModus.Text      = "Stationierung";
            btnModus.BackColor = Color.FromArgb(42, 130, 65);
            btnModus.FlatAppearance.BorderColor = Color.FromArgb(28, 100, 45);
        }
        new ToolTip().SetToolTip(btnModus,
            _istNeupunktModus
                ? "Messmodus: Neupunkt (Klick zum Wechseln)"
                : "Messmodus: Stationierung (Klick zum Wechseln)");
    }

    // ── EDM-Modus umschalten: Prisma ↔ Reflektorlos ─────────────────────────
    private void btnEdmToggle_Click(object? sender, EventArgs e)
    {
        _edmIstPrisma = !_edmIstPrisma;
        AktualisiereEdmButton();

        if (!TachymeterVerbindung.IstVerbunden) return;

        if (_edmIstPrisma)
        {
            // Prisma-Modus setzen + gespeicherte Prismenkonstante übertragen
            TachymeterVerbindung.GeoCOM_Senden("%R1Q,17021,0:0"); // BAP_SetTargetType = Reflektor
            double pk_m = (double)_prismaKonstante_mm / 1000.0;
            TachymeterVerbindung.GeoCOM_Senden(
                $"%R1Q,{GeoCOMParser.RPC_TMC_SetPrismCorr},0:{pk_m.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}");
        }
        else
        {
            // Reflektorlos: BAP_SetTargetType = 1, PrismCorr = +0,0344 m (Leica-Konvention)
            TachymeterVerbindung.GeoCOM_Senden("%R1Q,17021,0:1"); // BAP_SetTargetType = RL
            TachymeterVerbindung.GeoCOM_Senden("%R1Q,2024,0:0.0344");
        }
    }

    private void AktualisiereEdmButton()
    {
        if (_edmIstPrisma)
        {
            btnEdmToggle.BackColor = Color.FromArgb(60, 95, 160);
            btnEdmToggle.FlatAppearance.BorderColor = Color.FromArgb(80, 115, 185);
            IconLoader.Apply(btnEdmToggle, "toolbar_edm_prisma.png");
            new ToolTip().SetToolTip(btnEdmToggle,
                $"EDM: Prisma ({_prismaName}, {_prismaKonstante_mm:+0.0;-0.0;0.0} mm) – Klick → Reflektorlos");
        }
        else
        {
            btnEdmToggle.BackColor = Color.FromArgb(150, 70, 20);
            btnEdmToggle.FlatAppearance.BorderColor = Color.FromArgb(200, 100, 40);
            IconLoader.Apply(btnEdmToggle, "toolbar_edm_rl.png");
            new ToolTip().SetToolTip(btnEdmToggle,
                "EDM: Reflektorlos (+34,4 mm, Leica-Konvention) – Klick → Prisma");
        }
    }

    // ── Laserpointer ein/aus ──────────────────────────────────────────────────
    private void btnLaserpointer_Click(object? sender, EventArgs e)
    {
        _laserAktiv = !_laserAktiv;
        AktualisiereLaserButton();

        if (!TachymeterVerbindung.IstVerbunden) return;
        // EDM_Laserpointer: 0 = aus, 1 = ein
        TachymeterVerbindung.GeoCOM_Senden(
            $"%R1Q,{GeoCOMParser.RPC_EDM_Laserpointer},0:{(_laserAktiv ? 1 : 0)}");
    }

    private void AktualisiereLaserButton()
    {
        if (_laserAktiv)
        {
            btnLaserpointer.BackColor = Color.FromArgb(180, 55, 15);
            btnLaserpointer.FlatAppearance.BorderColor = Color.FromArgb(240, 90, 30);
            new ToolTip().SetToolTip(btnLaserpointer, "Laserpointer AN – Klick zum Ausschalten");
        }
        else
        {
            btnLaserpointer.BackColor = Color.FromArgb(60, 95, 160);
            btnLaserpointer.FlatAppearance.BorderColor = Color.FromArgb(80, 115, 185);
            new ToolTip().SetToolTip(btnLaserpointer, "Laserpointer AUS – Klick zum Einschalten");
        }
    }

    // ── Tachymeter-Messung auslösen ──────────────────────────────────────────
    private void btnMessung_Click(object? sender, EventArgs e)
    {
        if (_messungZustand != MessungZustand.Bereit) return;

        // Warnung wenn keine Stationierung und Neupunkt-Modus
        if (_istNeupunktModus && !StationsdatenManager.HatGueltigeStation
            && !_stationierungsWarnungGezeigt)
        {
            MessageBox.Show(
                "Es liegt keine gültige Stationierung vor.\n" +
                "Die Messung wird gespeichert, aber keine Koordinaten berechnet.\n\n" +
                "Bitte zuerst die Freie Stationierung durchführen oder\n" +
                "in den Modus \"Stationierung\" wechseln.",
                "Keine Stationierung",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _stationierungsWarnungGezeigt = true;
        }

        if (!TachymeterVerbindung.IstVerbunden)
        {
            lblStatus.Text = "Kein Tachymeter verbunden.";
            return;
        }

        try
        {
            btnMessung.Enabled = false;
            lblStatus.Text     = "Messung läuft …";

            _messungZustand    = MessungZustand.WarteMessen;
            _parser_letzterRpc = GeoCOMParser.RPC_TMC_DoMeasure;
            TachymeterVerbindung.GeoCOM_Senden("%R1Q,2008,0:1,1");
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Fehler: {ex.Message}";
            MessungAbschliessen();
        }
    }

    private void MessungAbschliessen()
    {
        _messungZustand    = MessungZustand.Bereit;
        _sammlung          = null;
        _parser_letzterRpc = 0;
        if (InvokeRequired) BeginInvoke(() => btnMessung.Enabled = true);
        else                 btnMessung.Enabled = true;
    }

    // ── GeoCOM-Datenempfang ───────────────────────────────────────────────────
    private void OnTachymeterDaten(object? sender, string daten)
    {
        if (InvokeRequired) { BeginInvoke(() => OnTachymeterDaten(sender, daten)); return; }

        _zeilenPuffer.Append(daten);
        string puffer = _zeilenPuffer.ToString();

        int pos;
        while ((pos = puffer.IndexOf('\n')) >= 0)
        {
            string zeile = puffer[..pos].TrimEnd('\r');
            puffer       = puffer[(pos + 1)..];
            if (!string.IsNullOrWhiteSpace(zeile))
                VerarbeiteZeile(zeile);
        }
        _zeilenPuffer.Clear();
        _zeilenPuffer.Append(puffer);
    }

    private void VerarbeiteZeile(string zeile)
    {
        int rpcKontext = _parser_letzterRpc;
        var messung    = GeoCOMParser.ParseAntwort(zeile, rpcKontext);
        if (messung == null) return;

        // ── Schritt 1: DoMeasure-Bestätigung ──────────────────────────────────
        if (_messungZustand == MessungZustand.WarteMessen
            && rpcKontext == GeoCOMParser.RPC_TMC_DoMeasure)
        {
            if (messung.IstFehler)
            {
                lblStatus.Text = $"Messfehler: {messung.Bemerkung}";
                MessungAbschliessen();
                return;
            }
            _messungZustand    = MessungZustand.WarteErgebnis;
            _parser_letzterRpc = GeoCOMParser.RPC_TMC_GetSimpleMea;
            TachymeterVerbindung.GeoCOM_Senden("%R1Q,2108,0:5000,1");
            return;
        }

        // ── Schritt 2: Hz/V/Dist ──────────────────────────────────────────────
        if (_messungZustand == MessungZustand.WarteErgebnis
            && rpcKontext == GeoCOMParser.RPC_TMC_GetSimpleMea)
        {
            if (messung.IstFehler)
            {
                lblStatus.Text = $"Messung fehlgeschlagen: {messung.Bemerkung}";
                MessungAbschliessen();
                return;
            }
            if (!messung.IstVollmessung)
            {
                lblStatus.Text = "Unvollständige Messung (Hz/V/D fehlen).";
                MessungAbschliessen();
                return;
            }
            // Zielhöhe aus Eingabefeld übernehmen
            if (double.TryParse(txtZielhoehe.Text.Replace(',', '.'),
                NumberStyles.Any, CultureInfo.InvariantCulture, out double zh))
                messung.Zielhoehe_m = zh;

            // Sammlung starten – Folgeschritte reichern an
            _sammlung          = messung;
            _messungZustand    = MessungZustand.WarteKompensator;
            _parser_letzterRpc = GeoCOMParser.RPC_TMC_GetAngle1;
            lblStatus.Text     = "Messung OK – Kompensator …";
            TachymeterVerbindung.GeoCOM_Senden("%R1Q,2003,0:1");
            return;
        }

        // ── Schritt 3: Kompensator (CrossIncline / LengthIncline) ─────────────
        if (_messungZustand == MessungZustand.WarteKompensator
            && rpcKontext == GeoCOMParser.RPC_TMC_GetAngle1)
        {
            if (!messung.IstFehler && _sammlung != null)
            {
                _sammlung.KreuzNeigung_rad  = messung.KreuzNeigung_rad;
                _sammlung.LaengsNeigung_rad = messung.LaengsNeigung_rad;
            }
            _messungZustand    = MessungZustand.WarteAtmKorr;
            _parser_letzterRpc = GeoCOMParser.RPC_TMC_GetAtmCorr;
            lblStatus.Text     = "Kompensator OK – Atmosph. Korrektur …";
            TachymeterVerbindung.GeoCOM_Senden("%R1Q,2029,0:");
            return;
        }

        // ── Schritt 4: Atmosphärische Korrektur (PPM/Druck/Temp) ─────────────
        if (_messungZustand == MessungZustand.WarteAtmKorr
            && rpcKontext == GeoCOMParser.RPC_TMC_GetAtmCorr)
        {
            if (!messung.IstFehler && _sammlung != null)
            {
                _sammlung.Atm_Lambda_m     = messung.Atm_Lambda_m;
                _sammlung.Atm_Druck_mbar   = messung.Atm_Druck_mbar;
                _sammlung.Atm_TempTrock_C  = messung.Atm_TempTrock_C;
                _sammlung.Atm_TempFeucht_C = messung.Atm_TempFeucht_C;
                _sammlung.Atm_PPM          = messung.Atm_PPM;
            }
            _messungZustand    = MessungZustand.WarteEdmModus;
            _parser_letzterRpc = GeoCOMParser.RPC_TMC_GetEdmMode;
            lblStatus.Text     = "AtmKorr OK – EDM-Modus …";
            TachymeterVerbindung.GeoCOM_Senden("%R1Q,2021,0:");
            return;
        }

        // ── Schritt 5: EDM-Modus ─────────────────────────────────────────────
        if (_messungZustand == MessungZustand.WarteEdmModus
            && rpcKontext == GeoCOMParser.RPC_TMC_GetEdmMode)
        {
            if (!messung.IstFehler && _sammlung != null)
            {
                _sammlung.EdmModus    = messung.EdmModus;
                _sammlung.EdmModusRoh = messung.EdmModusRoh;
            }
            _messungZustand    = MessungZustand.WartePrisma;
            _parser_letzterRpc = GeoCOMParser.RPC_TMC_GetPrismCorr;
            lblStatus.Text     = "EDM-Modus OK – Prismenkonstante …";
            TachymeterVerbindung.GeoCOM_Senden("%R1Q,2023,0:");
            return;
        }

        // ── Schritt 6: Prismenkonstante – Sequenz abschließen ─────────────────
        if (_messungZustand == MessungZustand.WartePrisma
            && rpcKontext == GeoCOMParser.RPC_TMC_GetPrismCorr)
        {
            if (!messung.IstFehler && _sammlung != null)
                _sammlung.Prismenkonstante_mm = messung.Prismenkonstante_mm;

            var vollstaendig = _sammlung;
            _sammlung = null;
            MessungAbschliessen();

            if (vollstaendig != null)
                VerarbeiteMiessung(vollstaendig);
            return;
        }
    }

    private void VerarbeiteMiessung(TachymeterMessung messung)
    {
        string punktNr = AktuellePunktNr;
        string code    = AktuellerCode;
        double instrH  = LadeInstrumentenHoehe();
        string spNr    = txtStandpunktNr.Text.Trim();
        if (string.IsNullOrEmpty(spNr)) spNr = "SP01";

        // ── Lückenloses Tagesprotokoll ────────────────────────────────────────
        // Jede Messung sofort sichern – unabhängig von Modus und Ergebnis.
        string protokollNr = _istNeupunktModus && string.IsNullOrEmpty(punktNr)
            ? _neupunktZaehler.ToString() : punktNr;
        MessdatenProtokoll.Schreibe(
            _istNeupunktModus ? "Neupunkt" : "Stationierung",
            spNr, instrH, protokollNr, code, messung);

        if (_istNeupunktModus)
        {
            // ─── Neupunkt-Modus ───────────────────────────────────────────────
            if (string.IsNullOrEmpty(punktNr))
                punktNr = _neupunktZaehler.ToString();

            var rohdaten = new NeupunktRohdaten
            {
                PunktNr      = punktNr,
                Code         = code,
                Hz_gon       = messung.Hz_gon!.Value,
                V_gon        = messung.V_gon!.Value,
                Strecke_m    = messung.Schraegstrecke_m!.Value,
                Zielhoehe_m  = messung.Zielhoehe_m ?? 0.0,
                Zeitstempel  = messung.Zeitstempel,
                StandpunktNr = spNr
            };

            // Messung in CSV schreiben
            MessdatenCSV.SchreibeNeupunktmessung(rohdaten, spNr, instrH);

            string statusInfo = $"NP {punktNr}: Hz={rohdaten.Hz_gon:F4}  V={rohdaten.V_gon:F4}  D={rohdaten.Strecke_m:F3} m";

            // Koordinaten berechnen wenn Stationierung gültig
            if (StationsdatenManager.HatGueltigeStation)
            {
                var ergebnis = NeupunktRechner.Berechnen(
                    rohdaten, StationsdatenManager.AktuellesErgebnis!);

                if (ergebnis != null)
                {
                    NeupunkteManager.HinzufuegenOderErsetzen(rohdaten, ergebnis);
                    BaueNeupunkteOverlay();
                    canvas.Invalidate();

                    statusInfo = $"NP {punktNr}:  R={ergebnis.R:F3}  H={ergebnis.H:F3}"
                        + (ergebnis.Ist3D ? $"  Höhe={ergebnis.Hoehe:F3}" : "");
                }
            }
            else
            {
                // Keine gültige Stationierung – Rohdaten wurden in CSV/Protokoll gespeichert.
                // Kein Overlay-Eintrag: R=0/H=0 würden bei echten Koordinatensystemen
                // (Gauss-Krüger) einen GDI+-Overflow beim Zeichnen verursachen.
                statusInfo = $"NP {punktNr} gespeichert (keine Stationierung – nur Rohdaten)";
            }

            lblStatus.Text = statusInfo;

            // Zähler erhöhen, wenn Punktnummer numerisch
            if (int.TryParse(punktNr, out int pn))
            {
                _neupunktZaehler = pn + 1;
                ProjektManager.NeupunktZaehler = _neupunktZaehler;
                ProjektManager.SpeichereOptionen();
                txtPunktNr.Text = _neupunktZaehler.ToString();
            }
        }
        else
        {
            // ─── Stationierungsmodus ──────────────────────────────────────────
            string anschlussNr = string.IsNullOrEmpty(punktNr)
                ? $"AP{StationsdatenManager.AktiveStation.Messungen.Count + 1}"
                : punktNr;

            // Anschlusspunkt-Koordinaten aus Importpunkten / Feldbuchpunkten suchen
            var (rAP, hAP, hoeheAP) = SucheAnschlusspunkt(anschlussNr);

            var stPunkt = new StationierungsPunkt
            {
                PunktNr   = anschlussNr,
                R         = rAP,
                H         = hAP,
                Hoehe     = hoeheAP,
                HZ        = messung.Hz_gon!.Value,
                V         = messung.V_gon!.Value,
                Strecke   = messung.Schraegstrecke_m!.Value,
                Zielhoehe = messung.Zielhoehe_m ?? 0.0
            };

            // Standpunkt sicherstellen
            if (string.IsNullOrEmpty(StationsdatenManager.StandpunktNr)
                || StationsdatenManager.StandpunktNr != spNr)
            {
                StationsdatenManager.NeuerStandpunkt(spNr, instrH);
                ProjektManager.LetzteStandpunktNr = spNr;
                ProjektManager.SpeichereOptionen();
            }
            StationsdatenManager.InstrumentenHoehe = instrH;

            // Messung in CSV schreiben
            MessdatenCSV.SchreibeStationierungsmessung(stPunkt, spNr, instrH, messung.Zeitstempel);

            // Neu berechnen
            var ergebnis = StationsdatenManager.HinzufuegenUndBerechnen(stPunkt);

            AktualisiereSignallampe();
            BaueOverlay();          // Standpunkt neu im Overlay

            if (ergebnis != null)
            {
                // Residual-Overlay aktualisieren
                BaueResidualOverlay(ergebnis);

                // Alle vorhandenen Neupunkte neu berechnen
                NeupunkteManager.BerechneNeu(ergebnis, spNr);
                BaueNeupunkteOverlay();

                // Standpunktinfo in CSV kommentieren
                MessdatenCSV.SchreibeStandpunktinfo(spNr, instrH, ergebnis);

                lblStatus.Text = $"Stationierung {spNr}: s₀={ergebnis.s0_mm:F1} mm  " +
                                 $"r={ergebnis.Redundanz}  n={StationsdatenManager.AktiveStation.Messungen.Count}";
            }
            else
            {
                lblStatus.Text = $"Anschlusspunkt {anschlussNr} gespeichert – " +
                                 $"noch {2 - StationsdatenManager.AktiveStation.Messungen.Count} Punkt(e) benötigt.";
            }

            canvas.Invalidate();
        }
    }

    // ── Overlay-Aufbau ────────────────────────────────────────────────────────

    public void BaueOverlay()
    {
        canvas.OverlayEntities.Clear();
        foreach (var p in FeldbuchpunkteManager.Punkte)
        {
            canvas.OverlayEntities.Add(p.Typ == "Standpunkt"
                ? (DxfEntity)new OverlayStandpunkt(p)
                : new OverlayNeupunkt(p));
        }
        canvas.Invalidate();
    }

    public void BaueNeupunkteOverlay()
    {
        canvas.NeupunkteEntities.Clear();
        foreach (var e in NeupunkteManager.Koordinaten.Where(e => e.R != 0 || e.H != 0))
            canvas.NeupunkteEntities.Add(new OverlayGemessenerNeupunkt(e));
        canvas.Invalidate();
    }

    public void BaueResidualOverlay(StationierungsErgebnis ergebnis)
    {
        canvas.ResidualEntities.Clear();
        foreach (var res in ergebnis.Residuen)
        {
            // Koordinaten des Anschlusspunktes suchen
            var (r, h, _) = SucheAnschlusspunkt(res.PunktNr);
            if (r != 0 || h != 0)
                canvas.ResidualEntities.Add(new OverlayResidualPunkt(res, r, h));
        }
        canvas.Invalidate();
    }

    // ── Hilfsmethoden ─────────────────────────────────────────────────────────

    private double LadeInstrumentenHoehe()
    {
        string txt = txtInstrHoehe.Text.Replace(',', '.');
        if (double.TryParse(txt, NumberStyles.Any,
                CultureInfo.InvariantCulture, out double ih) && ih >= 0)
        {
            ProjektManager.InstrumentenHoehe = ih;
            ProjektManager.SpeichereOptionen();
            return ih;
        }
        return ProjektManager.InstrumentenHoehe;
    }

    /// <summary>
    /// Sucht die Koordinaten eines Anschlusspunktes in ImportPunkten und
    /// Feldbuchpunkten.  Gibt (0,0,0) zurück wenn nicht gefunden.
    /// </summary>
    private static (double R, double H, double Hoehe) SucheAnschlusspunkt(string punktNr)
    {
        // 1. Importpunkte
        var imp = ImportPunkteManager.Punkte
            .FirstOrDefault(p => string.Equals(p.PunktNr, punktNr, StringComparison.OrdinalIgnoreCase));
        if (imp != null) return (imp.R, imp.H, imp.Hoehe);

        // 2. Feldbuchpunkte (Standpunkte + Neupunkte)
        var fb = FeldbuchpunkteManager.Punkte
            .FirstOrDefault(p => string.Equals(p.PunktNr, punktNr, StringComparison.OrdinalIgnoreCase));
        if (fb != null) return (fb.R, fb.H, fb.Hoehe);

        // 3. Anschlusspunkte.csv
        if (File.Exists(AnschlusspunktePfad))
        {
            foreach (var line in File.ReadAllLines(AnschlusspunktePfad).Skip(1))
            {
                var parts = line.Split(',');
                if (parts.Length >= 3 &&
                    string.Equals(parts[0].Trim(), punktNr, StringComparison.OrdinalIgnoreCase))
                {
                    var ic = CultureInfo.InvariantCulture;
                    double.TryParse(parts[1].Trim(), NumberStyles.Any, ic, out double r);
                    double.TryParse(parts[2].Trim(), NumberStyles.Any, ic, out double h);
                    double hoe = parts.Length >= 4 ? (double.TryParse(parts[3].Trim(), NumberStyles.Any, ic, out double ho) ? ho : 0) : 0;
                    return (r, h, hoe);
                }
            }
        }

        return (0, 0, 0);
    }

    // ── Instrument-Höhe geändert ──────────────────────────────────────────────
    private void txtInstrHoehe_Leave(object? sender, EventArgs e)
    {
        double ih = LadeInstrumentenHoehe();
        StationsdatenManager.InstrumentenHoehe = ih;
    }

    // ── Standpunkt-Nr geändert ────────────────────────────────────────────────
    private void txtStandpunktNr_Leave(object? sender, EventArgs e)
    {
        string spNr = txtStandpunktNr.Text.Trim();
        if (!string.IsNullOrEmpty(spNr))
        {
            ProjektManager.LetzteStandpunktNr = spNr;
            ProjektManager.SpeichereOptionen();
            StationsdatenManager.Laden(spNr);
            AktualisiereSignallampe();
        }
    }

    // ── Import-Layer aufbauen ─────────────────────────────────────────────────
    private void AddImportLayer(string dateiPfad, List<ImportPunkt> punkte)
    {
        string name = Path.GetFileName(dateiPfad);
        var layer = canvas.ImportLayers.FirstOrDefault(
            l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (layer == null) { layer = new ImportLayer(name); canvas.ImportLayers.Add(layer); }
        else layer.Entities.Clear();

        foreach (var p in punkte)
            layer.Entities.Add(new OverlayImportPunkt(p.PunktNr, p.R, p.H, p.Hoehe));

        BaueCheckboxen();
        canvas.Invalidate();
    }

    private void BaueCheckboxen()
    {
        // Import-Layer-Checkboxen neu aufbauen, Neupunkte+Residual-Checkboxen erhalten
        // Alle bisherigen Checkboxen entfernen außer _chkNeupunkte + _chkResidual
        var zuEntfernen = flpLayers.Controls
            .OfType<CheckBox>()
            .Where(c => c != _chkNeupunkte && c != _chkResidual)
            .ToList();
        foreach (var c in zuEntfernen) flpLayers.Controls.Remove(c);

        foreach (var layer in canvas.ImportLayers)
        {
            var chk = new CheckBox
            {
                Text     = layer.Name,
                Checked  = layer.Visible,
                AutoSize = true,
                Font     = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(30, 30, 30),
                Margin   = new Padding(6, 3, 0, 0),
                Cursor   = Cursors.Hand
            };
            var l = layer;
            chk.CheckedChanged += (s, e) => { l.Visible = chk.Checked; canvas.Invalidate(); };
            new ToolTip().SetToolTip(chk, layer.Name);
            flpLayers.Controls.Add(chk);
        }
    }

    public void BaueImportOverlay()
    {
        var punkte = ImportPunkteManager.Punkte.ToList();
        if (punkte.Count == 0) return;
        string name = "ImportPunkte.json";
        var layer = canvas.ImportLayers.FirstOrDefault(
            l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? new ImportLayer(name);
        layer.Entities.Clear();
        foreach (var p in punkte)
            layer.Entities.Add(new OverlayImportPunkt(p.PunktNr, p.R, p.H, p.Hoehe));
        if (!canvas.ImportLayers.Contains(layer)) canvas.ImportLayers.Add(layer);
        BaueCheckboxen();
        canvas.Invalidate();
    }

    // ── KOR / CSV einlesen ────────────────────────────────────────────────────
    private void btnImportKorCsv_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title  = "KOR- oder CSV-Datei importieren",
            Filter = "KOR / CSV|*.kor;*.csv|KOR-Datei|*.kor|CSV-Datei|*.csv|Alle Dateien|*.*"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        List<ImportPunkt> punkte;
        string ext = Path.GetExtension(dlg.FileName).ToLowerInvariant();
        try
        {
            punkte = ext == ".csv"
                ? ImportPunkteManager.LeseCsv(dlg.FileName)
                : ImportPunkteManager.LeseKor(dlg.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Lesen:\n{ex.Message}", "Importfehler",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        var (added, dupl) = ImportPunkteManager.AddRange(punkte);
        AddImportLayer(dlg.FileName, punkte);
        canvas.FitToView();
        string msg = $"{added} Punkte importiert aus {Path.GetFileName(dlg.FileName)}";
        if (dupl > 0) msg += $"  ({dupl} Duplikat(e) übersprungen)";
        lblStatus.Text = msg;
    }

    // ── JSON einlesen ─────────────────────────────────────────────────────────
    private void btnImportJson_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title  = "JSON-Datei importieren",
            Filter = "JSON-Dateien|*.json|Alle Dateien|*.*"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        List<ImportPunkt> punkte;
        try { punkte = ImportPunkteManager.LeseJson(dlg.FileName); }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Lesen:\n{ex.Message}", "Importfehler",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (punkte.Count == 0)
        {
            MessageBox.Show("Keine Punkte in der JSON-Datei gefunden.",
                "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var (added, dupl) = ImportPunkteManager.AddRange(punkte);
        AddImportLayer(dlg.FileName, punkte);
        canvas.FitToView();
        string msg = $"{added} Punkte importiert aus {Path.GetFileName(dlg.FileName)}";
        if (dupl > 0) msg += $"  ({dupl} Duplikat(e) übersprungen)";
        lblStatus.Text = msg;
    }

    // ── Toolbar: rechtsbündige Positionierung ─────────────────────────────────
    // Layout (von rechts): Code – Nr – Zielhoehe – Messen | sep2 | SP – IH – Modus – Lampe | sep1
    // Layout (von links, fest): Prismenkonstante – EdmToggle – Laserpointer
    private void PositioniereToolbarFelder()
    {
        int cy = (pnlTop.Height - 24) / 2;
        int r  = pnlTop.Width - 8;

        txtCode.Location    = new Point(r - txtCode.Width, cy);     r -= txtCode.Width + 4;
        lblCode.Location    = new Point(r - lblCode.Width, cy);     r -= lblCode.Width + 12;
        txtPunktNr.Location = new Point(r - txtPunktNr.Width, cy);  r -= txtPunktNr.Width + 4;
        lblNr.Location      = new Point(r - lblNr.Width, cy);       r -= lblNr.Width + 12;
        txtZielhoehe.Location = new Point(r - txtZielhoehe.Width, cy); r -= txtZielhoehe.Width + 4;
        lblZielhoehe.Location = new Point(r - lblZielhoehe.Width, cy); r -= lblZielhoehe.Width + 12;
        btnMessung.Location   = new Point(r - btnMessung.Width, 3);  r -= btnMessung.Width + 10;

        // Trennlinie vor Messung-Bereich
        sep2.Location = new Point(r, 6);
        r -= sep2.Width + 8;

        txtStandpunktNr.Location = new Point(r - txtStandpunktNr.Width, cy); r -= txtStandpunktNr.Width + 4;
        lblSP.Location           = new Point(r - lblSP.Width, cy);           r -= lblSP.Width + 4;
        txtInstrHoehe.Location   = new Point(r - txtInstrHoehe.Width, cy);   r -= txtInstrHoehe.Width + 4;
        lblInstrH.Location       = new Point(r - lblInstrH.Width, cy);       r -= lblInstrH.Width + 8;
        btnModus.Location        = new Point(r - btnModus.Width, 3);         r -= btnModus.Width + 8;

        // Signallampe rechts, direkt links neben sep1
        lblLampeInfo.Location = new Point(r - lblLampeInfo.Width, cy - 2); r -= lblLampeInfo.Width + 4;
        pnlLampe.Location     = new Point(r - pnlLampe.Width,  (pnlTop.Height - pnlLampe.Height) / 2);
        r -= pnlLampe.Width + 6;

        sep1.Location = new Point(r, 6);
        // (Linke Seite: btnPrismenkonstante, btnEdmToggle, btnLaserpointer – feste Positionen)
    }

    // ── Prismenkonstante ──────────────────────────────────────────────────────
    private void btnPrismenkonstante_Click(object? sender, EventArgs e)
    {
        using var form = new FormPrismenkonstante(_prismaKonstante_mm);
        if (form.ShowDialog(this) != DialogResult.OK) return;

        _prismaKonstante_mm = form.GewähltePrismenkonstante;
        _prismaName         = form.GewählterPrismaName;
        AktualisiereEdmButton(); // Tooltip mit neuem Prismanamen aktualisieren

        // Prismenkonstante ans Gerät senden (nur wenn Prisma-Modus aktiv)
        if (TachymeterVerbindung.IstVerbunden && _edmIstPrisma)
        {
            double pk_m = (double)_prismaKonstante_mm / 1000.0;
            TachymeterVerbindung.GeoCOM_Senden(
                $"%R1Q,{GeoCOMParser.RPC_TMC_SetPrismCorr},0:{pk_m.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}");
        }

        lblStatus.Text = $"Prisma: {form.GewählterPrismaName}  {form.GewähltePrismenkonstante:+0.0;-0.0;0.0} mm";
    }

    // ── NEU: DXF-Inhalt löschen ───────────────────────────────────────────────
    private void btnNeu_Click(object? sender, EventArgs e)
    {
        var res = MessageBox.Show(
            "Die aktuelle DXF-Datei aus dem Viewer entfernen?\n(Datei auf der Festplatte bleibt erhalten.)",
            "DXF leeren", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (res != DialogResult.Yes) return;
        canvas.Entities.Clear();
        _aktuellerDxfPfad = "";
        Text           = "DXF-Viewer";
        lblStatus.Text = "DXF-Inhalt geleert.";
        canvas.Invalidate();
    }

    // ── Datei öffnen ──────────────────────────────────────────────────────────
    private void btnOpen_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title            = "DXF-Datei öffnen",
            Filter           = "DXF-Dateien (*.dxf)|*.dxf|Alle Dateien (*.*)|*.*",
            InitialDirectory = ProjektManager.ProjektVerzeichnis
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        LadeDxfDatei(dlg.FileName);
    }

    private void LadeDxfDatei(string pfad)
    {
        try
        {
            Cursor = Cursors.WaitCursor;
            canvas.Entities = DxfReader.Read(pfad);

            // Bestehenden persistenten Punkt-Index laden (falls vorhanden)
            string indexPfad = PunktIndexPfad();
            var bestehend = DxfPunktIndexManager.Laden(indexPfad);
            bool istNeuLadung = bestehend != null;

            // Punkt-Index aufbauen mit Merge-Logik (bestehende Nummern übernehmen)
            canvas.PunktIndex = DxfPunktIndexManager.AufbauenMitMerge(
                canvas.Entities, bestehend);

            // Persistenter Index speichern
            DxfPunktIndexManager.Speichern(canvas.PunktIndex._eintraege, indexPfad);

            // Punkt-Marker erstellen (für jede eindeutige Koordinate eine Beschriftung)
            canvas.PunktMarker.Clear();
            foreach (var e in canvas.Entities)
            {
                switch (e)
                {
                    case DxfInsert ins:
                    {
                        var nr = canvas.PunktIndex.GetPunktNr(ins.X, ins.Y);
                        if (nr != null) canvas.PunktMarker.Add(new DxfPunktMarker(nr, ins.X, ins.Y));
                        break;
                    }
                    case DxfPoint pt:
                    {
                        var nr = canvas.PunktIndex.GetPunktNr(pt.X, pt.Y);
                        if (nr != null) canvas.PunktMarker.Add(new DxfPunktMarker(nr, pt.X, pt.Y));
                        break;
                    }
                    case DxfCircle circ:
                    {
                        var nr = canvas.PunktIndex.GetPunktNr(circ.CX, circ.CY);
                        if (nr != null) canvas.PunktMarker.Add(new DxfPunktMarker(nr, circ.CX, circ.CY));
                        break;
                    }
                    case DxfLine line:
                    {
                        var nr1 = canvas.PunktIndex.GetPunktNr(line.X1, line.Y1);
                        var nr2 = canvas.PunktIndex.GetPunktNr(line.X2, line.Y2);
                        if (nr1 != null) canvas.PunktMarker.Add(new DxfPunktMarker(nr1, line.X1, line.Y1));
                        if (nr2 != null) canvas.PunktMarker.Add(new DxfPunktMarker(nr2, line.X2, line.Y2));
                        break;
                    }
                    case DxfLwPolyline poly:
                    {
                        foreach (var v in poly.Vertices)
                        {
                            var nr = canvas.PunktIndex.GetPunktNr(v.x, v.y);
                            if (nr != null) canvas.PunktMarker.Add(new DxfPunktMarker(nr, v.x, v.y));
                        }
                        break;
                    }
                    case DxfArc arc:
                    {
                        double sr = arc.StartAngle * Math.PI / 180.0;
                        double er = arc.EndAngle * Math.PI / 180.0;
                        double sx = arc.CX + arc.Radius * Math.Cos(sr);
                        double sy = arc.CY + arc.Radius * Math.Sin(sr);
                        double ex = arc.CX + arc.Radius * Math.Cos(er);
                        double ey = arc.CY + arc.Radius * Math.Sin(er);
                        var nr1 = canvas.PunktIndex.GetPunktNr(sx, sy);
                        var nr2 = canvas.PunktIndex.GetPunktNr(ex, ey);
                        if (nr1 != null) canvas.PunktMarker.Add(new DxfPunktMarker(nr1, sx, sy));
                        if (nr2 != null) canvas.PunktMarker.Add(new DxfPunktMarker(nr2, ex, ey));
                        break;
                    }
                }
            }

            var zoom = ProjektManager.LadeZoom(pfad);
            if (zoom.HasValue)
            {
                canvas.Scale = zoom.Value.Scale;
                canvas.PanX  = zoom.Value.PanX;
                canvas.PanY  = zoom.Value.PanY;
                canvas.Invalidate();
            }
            else canvas.FitToView();

            _aktuellerDxfPfad = pfad;
            string name       = Path.GetFileName(pfad);
            int neuPunkte = canvas.PunktIndex.Anzahl - (bestehend?.Count ?? 0);
            lblStatus.Text    = $"Geladen: {name}  |  {canvas.Entities.Count} Objekte, " +
                                $"{canvas.PunktIndex.Anzahl} Punkte" +
                                (istNeuLadung && neuPunkte > 0
                                    ? $"  ({neuPunkte} neu, {canvas.PunktIndex.Anzahl - neuPunkte} übernommen)"
                                    : istNeuLadung ? "  (Nummern übernommen)"
                                    : "");
            Text              = $"DXF-Viewer – {name}";

            // Checkbox für Punkt-Marker Sichtbarkeit
            BauePunktMarkerCheckbox();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Fehler beim Lesen der DXF-Datei:\n" + ex.Message,
                "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { Cursor = Cursors.Default; }
    }

    // ── Zoom beim Schließen speichern ─────────────────────────────────────────
    private void OnFormClosed(object? sender, FormClosedEventArgs e)
    {
        // Laserpointer beim Schließen sicherheitshalber ausschalten
        if (_laserAktiv && TachymeterVerbindung.IstVerbunden)
        {
            try { TachymeterVerbindung.GeoCOM_Senden($"%R1Q,{GeoCOMParser.RPC_EDM_Laserpointer},0:0"); }
            catch { }
        }

        TachymeterVerbindung.DatenEmpfangen -= OnTachymeterDaten;

        if (!string.IsNullOrEmpty(_aktuellerDxfPfad))
            ProjektManager.SpeichereZoom(
                _aktuellerDxfPfad, canvas.Scale, canvas.PanX, canvas.PanY);
    }

    // ── Zoom-Buttons ──────────────────────────────────────────────────────────
    private void btnZoomIn_Click (object? s, EventArgs e) => canvas.ZoomIn();
    private void btnZoomOut_Click(object? s, EventArgs e) => canvas.ZoomOut();
    private void btnFit_Click    (object? s, EventArgs e) => canvas.FitToView();

    // ── Snap-Button ───────────────────────────────────────────────────────────
    private void btnSnap_Click(object? s, EventArgs e)
    {
        canvas.SnapActive = !canvas.SnapActive;
        UpdateSnapButton();
        ProjektdatenManager.SetValue("Einstellungen", "Snap",
            canvas.SnapActive ? "Aktiv" : "Inaktiv");
    }

    // ── DXF ein-/ausblenden ───────────────────────────────────────────────────
    private void btnDxfToggle_Click(object? s, EventArgs e)
    {
        canvas.DxfVisible = !canvas.DxfVisible;
        canvas.Invalidate();
        UpdateDxfToggleButton();
    }

    private void UpdateDxfToggleButton()
    {
        if (canvas.DxfVisible)
        {
            btnDxfToggle.BackColor = Color.FromArgb(60, 100, 160);
            btnDxfToggle.ForeColor = Color.White;
            btnDxfToggle.FlatAppearance.BorderColor = Color.FromArgb(40, 80, 140);
        }
        else
        {
            btnDxfToggle.BackColor = Color.FromArgb(190, 190, 190);
            btnDxfToggle.ForeColor = Color.FromArgb(30, 30, 30);
            btnDxfToggle.FlatAppearance.BorderColor = Color.FromArgb(160, 160, 160);
        }
        new ToolTip().SetToolTip(btnDxfToggle,
            $"DXF-Darstellung – aktuell {(canvas.DxfVisible ? "EIN" : "AUS")}");
    }

    // ── DXF-Export ────────────────────────────────────────────────────────────
    private void btnExportDxf_Click(object? s, EventArgs e)
    {
        using var dlg = new FormDxfExport();
        dlg.ShowDialog(this);
    }

    // ── Katasterpunkte-Button ─────────────────────────────────────────────────
    private void btnPunkte_Click(object? s, EventArgs e)
    {
        canvas.PunkteVisible = !canvas.PunkteVisible;
        canvas.Invalidate();
        UpdatePunkteButton();
        ProjektdatenManager.SetValue("Einstellungen", "Katasterpunkte",
            canvas.PunkteVisible ? "Aktiv" : "Inaktiv");
    }

    private void UpdatePunkteButton()
    {
        if (canvas.PunkteVisible)
        {
            btnPunkte.BackColor = Color.FromArgb(200, 120, 30);
            btnPunkte.ForeColor = Color.White;
            btnPunkte.FlatAppearance.BorderColor = Color.FromArgb(220, 140, 50);
        }
        else
        {
            btnPunkte.BackColor = Color.FromArgb(190, 190, 190);
            btnPunkte.ForeColor = Color.FromArgb(30, 30, 30);
            btnPunkte.FlatAppearance.BorderColor = Color.FromArgb(160, 160, 160);
        }
        new ToolTip().SetToolTip(btnPunkte,
            $"Katasterpunkte – aktuell {(canvas.PunkteVisible ? "EIN" : "AUS")}");
    }

    private void UpdateSnapButton()
    {
        if (canvas.SnapActive)
        {
            btnSnap.BackColor = Color.FromArgb(200, 120, 30);
            btnSnap.ForeColor = Color.White;
            btnSnap.FlatAppearance.BorderColor = Color.FromArgb(220, 140, 50);
        }
        else
        {
            btnSnap.BackColor = Color.FromArgb(190, 190, 190);
            btnSnap.ForeColor = Color.FromArgb(30, 30, 30);
            btnSnap.FlatAppearance.BorderColor = Color.FromArgb(160, 160, 160);
        }
        new ToolTip().SetToolTip(btnSnap,
            $"Punktfang (Snap) – aktuell {(canvas.SnapActive ? "EIN" : "AUS")}");
    }

    // ── Picking / Koordinatenanzeige ──────────────────────────────────────────
    private void OnPointPicked(double x, double y, DxfEntity? entity)
    {
        string snapHinweis = canvas.SnapActive ? " [SNAP]" : "";
        if (entity != null)
        {
            GepickterPunkt = (x, y);
            lblStatus.Text = $"R: {x:F3}   H: {y:F3}{snapHinweis}   |   {entity.GetInfo()}";

            // Punktnummer und Höhe aus der geklickten Entity extrahieren
            string punktNr = entity switch
            {
                OverlayImportPunkt         imp => imp.PunktNr,
                FeldbuchOverlayEntity      fb  => fb.Punkt.PunktNr,
                OverlayGemessenerNeupunkt  np  => np.Ergebnis.PunktNr,
                DxfPunktMarker             m   => m.PunktNr,
                DxfInsert                  ins => canvas.PunktIndex?.GetPunktNr(ins.X, ins.Y),
                DxfPoint                   pt  => canvas.PunktIndex?.GetPunktNr(pt.X, pt.Y),
                DxfCircle                  circ => canvas.PunktIndex?.GetPunktNr(circ.CX, circ.CY),
                DxfLine                    line => canvas.PunktIndex?.GetPunktNr(line.X1, line.Y1),
                _                              => ""
            } ?? "";
            double hoehe = entity switch
            {
                OverlayImportPunkt        imp => imp.Hoehe,
                FeldbuchOverlayEntity     fb  => fb.Punkt.Hoehe,
                _                             => 0.0
            };

            // Punktnummer ins Eingabefeld übernehmen
            if (!string.IsNullOrEmpty(punktNr))
                txtPunktNr.Text = punktNr;

            using var dlg = new FormMessdatenEingabe(x, y, punktNr, hoehe);
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Ergebnis != null)
            {
                SchreibeAnschlusspunkt(dlg.Ergebnis);
                lblStatus.Text = $"Punkt '{dlg.Ergebnis.PunktNr}' gespeichert  |  " +
                                 $"R: {x:F3}   H: {y:F3}";
            }
        }
        else
        {
            lblStatus.Text = canvas.Entities.Count > 0
                ? $"R: {x:F3}   H: {y:F3}{snapHinweis}   |   Kein Objekt ausgewählt"
                : $"R: {x:F3}   H: {y:F3}";
        }
    }

    // ── Anschlusspunkt in CSV schreiben ───────────────────────────────────────
    private static void SchreibeAnschlusspunkt(StationierungsPunkt p)
    {
        var ic     = CultureInfo.InvariantCulture;
        const string header = "PunktNr,R,H,Hoehe,HZ,V,Strecke,Zielhoehe";
        var zeilen = new List<string>();
        if (File.Exists(AnschlusspunktePfad))
        {
            foreach (var line in File.ReadAllLines(AnschlusspunktePfad).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split(',');
                if (parts.Length > 0 &&
                    string.Equals(parts[0].Trim(), p.PunktNr, StringComparison.Ordinal))
                    continue;
                zeilen.Add(line);
            }
        }
        zeilen.Add(string.Join(",", new[]
        {
            p.PunktNr,
            p.R        .ToString("F3", ic),
            p.H        .ToString("F3", ic),
            p.Hoehe    .ToString("F3", ic),
            p.HZ       .ToString("F4", ic),
            p.V        .ToString("F4", ic),
            p.Strecke  .ToString("F3", ic),
            p.Zielhoehe.ToString("F3", ic)
        }));
        File.WriteAllLines(AnschlusspunktePfad,
            new[] { header }.Concat(zeilen),
            System.Text.Encoding.UTF8);
    }

    // ── Lösch-Modus ───────────────────────────────────────────────────────────
    private void btnLoeschen_Click(object? sender, EventArgs e)
    {
        canvas.DeleteModeActive = !canvas.DeleteModeActive;
        UpdateLoeschenButton();
        lblStatus.Text = canvas.DeleteModeActive
            ? "Lösch-Modus: Fenster aufziehen  |  Erneut klicken zum Abbrechen"
            : "Lösch-Modus beendet.";
    }

    // ── Punkt-Index zurücksetzen ──────────────────────────────────────────────
    private void btnPunktIndexReset_Click(object? sender, EventArgs e)
    {
        using var dlg = new FormPunktIndexReset();
        if (dlg.ShowDialog(this) == DialogResult.OK)
            PunktIndexZuruecksetzen();
    }

    private void UpdateLoeschenButton()
    {
        if (canvas.DeleteModeActive)
        {
            btnLoeschen.BackColor = Color.FromArgb(160, 40, 40);
            btnLoeschen.ForeColor = Color.White;
            btnLoeschen.FlatAppearance.BorderColor = Color.FromArgb(130, 20, 20);
        }
        else
        {
            btnLoeschen.BackColor = Color.FromArgb(68, 74, 92);
            btnLoeschen.ForeColor = Color.FromArgb(220, 200, 200);
            btnLoeschen.FlatAppearance.BorderColor = Color.FromArgb(85, 92, 115);
        }
    }

    private void OnRectangleSelected(double minX, double minY, double maxX, double maxY)
    {
        bool InRect(double r, double h) => r >= minX && r <= maxX && h >= minY && h <= maxY;

        var feldbuchTreffer = FeldbuchpunkteManager.Punkte
            .Where(p => InRect(p.R, p.H)).ToList();
        var neupunktTreffer = NeupunkteManager.Rohdaten
            .Where(p => InRect(
                NeupunkteManager.Koordinaten
                    .FirstOrDefault(e => e.PunktNr == p.PunktNr)?.R ?? 0,
                NeupunkteManager.Koordinaten
                    .FirstOrDefault(e => e.PunktNr == p.PunktNr)?.H ?? 0))
            .ToList();
        var importTreffer = ImportPunkteManager.Punkte
            .Where(p => InRect(p.R, p.H)).ToList();

        int gesamt = feldbuchTreffer.Count + neupunktTreffer.Count + importTreffer.Count;
        if (gesamt == 0) { lblStatus.Text = "Keine Punkte im markierten Bereich."; return; }

        var zeilen = new StringBuilder();
        if (feldbuchTreffer.Count > 0)
            zeilen.AppendLine($"Feldbuchpunkte ({feldbuchTreffer.Count}):  " +
                string.Join(", ", feldbuchTreffer.Take(8).Select(p => p.PunktNr)) +
                (feldbuchTreffer.Count > 8 ? $" …" : ""));
        if (neupunktTreffer.Count > 0)
            zeilen.AppendLine($"Neupunkte ({neupunktTreffer.Count}):  " +
                string.Join(", ", neupunktTreffer.Take(8).Select(p => p.PunktNr)) +
                (neupunktTreffer.Count > 8 ? $" …" : ""));
        if (importTreffer.Count > 0)
            zeilen.AppendLine($"Importpunkte ({importTreffer.Count}):  " +
                string.Join(", ", importTreffer.Take(8).Select(p => p.PunktNr)) +
                (importTreffer.Count > 8 ? $" …" : ""));

        var res = MessageBox.Show(
            $"{gesamt} Punkt(e) löschen?\n\n{zeilen}",
            "Punkte löschen", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (res != DialogResult.Yes) return;

        int gelFeldbuch = 0, gelNeu = 0, gelImport = 0;
        if (feldbuchTreffer.Count > 0)
        {
            var nrs = feldbuchTreffer.Select(p => p.PunktNr).ToHashSet();
            gelFeldbuch = FeldbuchpunkteManager.RemoveWhere(p => nrs.Contains(p.PunktNr));
            BaueOverlay();
        }
        if (neupunktTreffer.Count > 0)
        {
            var nrs = neupunktTreffer.Select(p => p.PunktNr).ToHashSet();
            gelNeu = NeupunkteManager.RemoveWhere(p => nrs.Contains(p.PunktNr));
            BaueNeupunkteOverlay();
        }
        if (importTreffer.Count > 0)
        {
            var nrs = importTreffer.Select(p => p.PunktNr).ToHashSet();
            gelImport = ImportPunkteManager.RemoveWhere(p => nrs.Contains(p.PunktNr));
            BaueImportOverlay();
        }

        canvas.DeleteModeActive = false;
        UpdateLoeschenButton();
        lblStatus.Text = $"{gesamt} Punkt(e) gelöscht (FB:{gelFeldbuch} NP:{gelNeu} Imp:{gelImport}).";
        ProtokollManager.Log("LÖSCH", $"{gesamt} Punkte gelöscht");
    }

    // ── Persistenter Punkt-Index Pfad ─────────────────────────────────────────
    private string PunktIndexPfad()
    {
        string projektName = ProjektManager.ProjektName ?? "OhneProjekt";
        return ProjektManager.GetPfad($"{projektName}-PunktIndex.json");
    }

    // ── Punkt-Index zurücksetzen ──────────────────────────────────────────────
    public void PunktIndexZuruecksetzen()
    {
        string indexPfad = PunktIndexPfad();
        DxfPunktIndexManager.Loeschen(indexPfad);

        if (!string.IsNullOrEmpty(_aktuellerDxfPfad) && canvas.Entities.Count > 0)
        {
            // Neu aufbauen ohne bestehende Einträge
            canvas.PunktIndex = DxfPunktIndex.Aufbauen(canvas.Entities);
            DxfPunktIndexManager.Speichern(canvas.PunktIndex._eintraege, indexPfad);

            // Marker neu erstellen
            canvas.PunktMarker.Clear();
            foreach (var e in canvas.Entities)
            {
                switch (e)
                {
                    case DxfInsert ins:
                    {
                        var nr = canvas.PunktIndex.GetPunktNr(ins.X, ins.Y);
                        if (nr != null) canvas.PunktMarker.Add(new DxfPunktMarker(nr, ins.X, ins.Y));
                        break;
                    }
                    case DxfPoint pt:
                    {
                        var nr = canvas.PunktIndex.GetPunktNr(pt.X, pt.Y);
                        if (nr != null) canvas.PunktMarker.Add(new DxfPunktMarker(nr, pt.X, pt.Y));
                        break;
                    }
                    case DxfCircle circ:
                    {
                        var nr = canvas.PunktIndex.GetPunktNr(circ.CX, circ.CY);
                        if (nr != null) canvas.PunktMarker.Add(new DxfPunktMarker(nr, circ.CX, circ.CY));
                        break;
                    }
                    case DxfLine line:
                    {
                        var nr1 = canvas.PunktIndex.GetPunktNr(line.X1, line.Y1);
                        var nr2 = canvas.PunktIndex.GetPunktNr(line.X2, line.Y2);
                        if (nr1 != null) canvas.PunktMarker.Add(new DxfPunktMarker(nr1, line.X1, line.Y1));
                        if (nr2 != null) canvas.PunktMarker.Add(new DxfPunktMarker(nr2, line.X2, line.Y2));
                        break;
                    }
                    case DxfLwPolyline poly:
                    {
                        foreach (var v in poly.Vertices)
                        {
                            var nr = canvas.PunktIndex.GetPunktNr(v.x, v.y);
                            if (nr != null) canvas.PunktMarker.Add(new DxfPunktMarker(nr, v.x, v.y));
                        }
                        break;
                    }
                    case DxfArc arc:
                    {
                        double sr = arc.StartAngle * Math.PI / 180.0;
                        double er = arc.EndAngle * Math.PI / 180.0;
                        double sx = arc.CX + arc.Radius * Math.Cos(sr);
                        double sy = arc.CY + arc.Radius * Math.Sin(sr);
                        double ex = arc.CX + arc.Radius * Math.Cos(er);
                        double ey = arc.CY + arc.Radius * Math.Sin(er);
                        var nr1 = canvas.PunktIndex.GetPunktNr(sx, sy);
                        var nr2 = canvas.PunktIndex.GetPunktNr(ex, ey);
                        if (nr1 != null) canvas.PunktMarker.Add(new DxfPunktMarker(nr1, sx, sy));
                        if (nr2 != null) canvas.PunktMarker.Add(new DxfPunktMarker(nr2, ex, ey));
                        break;
                    }
                }
            }

            canvas.Invalidate();
            lblStatus.Text = $"Punkt-Index zurückgesetzt: {canvas.PunktIndex.Anzahl} Punkte neu nummeriert.";
        }
        else
        {
            lblStatus.Text = "Keine DXF-Datei geladen – Punkt-Index gelöscht.";
        }
    }
}
