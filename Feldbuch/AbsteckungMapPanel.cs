namespace Feldbuch;

using System.Globalization;
using System.Text;

// ──────────────────────────────────────────────────────────────────────────────
// AbsteckungMapPanel – DxfCanvas + Sidebar + Statuszeile für alle Absteckformen
// Identische Funktionen wie DXF-Viewer, außer Absteckungsbutton (kein rekursiver Aufruf)
// ──────────────────────────────────────────────────────────────────────────────
public class AbsteckungMapPanel : UserControl
{
    // ── Öffentlicher Zugang zum Canvas ────────────────────────────────────────
    public DxfCanvas Canvas => _canvas;

    // ── Messung eingetroffen ──────────────────────────────────────────────────
    public event Action<TachymeterMessung>? MessungEingetroffen;

    // ── Punkt auf Karte angeklickt (R, H, PunktNr?) ──────────────────────────
    public event Action<double, double, string?>? PunktGefangen;

    // ── Private Felder ────────────────────────────────────────────────────────
    private readonly DxfCanvas         _canvas       = new();
    private readonly Panel             _pnlSide      = new();
    private readonly Panel             _pnlStatus    = new();
    private readonly Label             _lblKoord     = new();
    private readonly Label             _lblMaßstab   = new();
    private readonly FlowLayoutPanel   _flpLayers    = new();

    // Header-Leiste (oben) – Tachymeter-Steuerung
    private readonly Panel  _pnlHeader  = new();
    private readonly Label  _lblVerbindung    = new();
    private readonly Button _btnPrismkonstante = new();
    private readonly Button _btnEdmModus      = new();
    private readonly Button _btnLaserpointer  = new();
    private readonly Button _btnMessen        = new();

    // Tachymeter-Zustand
    private ITachymeterBefehlsgeber _befehlsgeber;
    private readonly GeoCOMParser   _geocomParser = new();
    private enum MessModus { Prisma, ReflektorlosKurz, ReflektorlosLang }
    private MessModus _messModus = MessModus.Prisma;
    private enum MessungZustand { Bereit, WarteMessen, WarteErgebnis }
    private MessungZustand _messungZustand = MessungZustand.Bereit;
    private bool    _laserAn = false;
    private decimal _prismenkonstante_mm = 0m;
    private int     _letzterRpc = 0;
    private readonly StringBuilder _zeilenPuffer = new();

    private const int BAP_REFL_USE   = 0;
    private const int BAP_REFL_LESS  = 1;
    private const int EDM_SINGLE_PRISM = 3;
    private const int EDM_SINGLE_RL_K  = 5;
    private const int EDM_SINGLE_RL_L  = 11;

    // Sidebar-Buttons
    private readonly Button _btnZoomIn         = new();
    private readonly Button _btnZoomOut        = new();
    private readonly Button _btnFit            = new();
    private readonly Button _btnSnap           = new();
    private readonly Button _btnPunkte         = new();
    private readonly Button _btnDxfToggle      = new();
    private readonly Button _btnExportDxf      = new();
    private readonly Button _btnDigitalisierung = new();

    // Layer-Checkboxen (fest)
    private CheckBox? _chkNeupunkte;
    private CheckBox? _chkResidual;
    private CheckBox? _chkPunktMarker;

    // Koordinatenübernahme: letztes aktives Eingabefeld / Grid
    private TextBox?       _letztesFeld = null;
    private DataGridView?  _letztesGrid = null;

    // Digitalisier-Zustand
    private bool   _digModus    = false;
    private int    _digZaehler  = 1;
    private string _digKorPfad  = "";
    private ImportLayer? _digLayer = null;

    // Letzter geladener DXF-Pfad
    private string _dxfPfad = "";

    // ── Konstruktor ───────────────────────────────────────────────────────────
    public AbsteckungMapPanel()
    {
        _befehlsgeber = TachymeterBefehlsgeberFactory.Erstellen(TachymeterVerbindung.Modell);

        InitLayout();
        BaueNeupunkteCheckboxen();

        _canvas.PointPicked   += OnPointPicked;
        _canvas.ScaleChanged  += OnScaleChanged;
        Load                  += OnLoad;

        TachymeterVerbindung.DatenEmpfangen   += OnTachDatenEmpfangen;
        TachymeterVerbindung.VerbindungGetrennt += OnVerbindungGetrennt;
    }

    // ── Layout ────────────────────────────────────────────────────────────────
    private new void InitLayout()
    {
        var fntIco  = new Font("Segoe UI", 12F, FontStyle.Bold);
        var fntSm   = new Font("Segoe UI", 7.5F, FontStyle.Bold);
        var colBase   = Color.FromArgb(68, 74, 92);
        var colActive = Color.FromArgb(52, 110, 190);
        var colRed    = Color.FromArgb(150, 38, 38);
        var colBorder = Color.FromArgb(85, 92, 115);

        // Header-Leiste (oben) – Tachymeter-Steuerung
        _pnlHeader.Dock      = DockStyle.Top;
        _pnlHeader.Height    = 36;
        _pnlHeader.BackColor = Color.FromArgb(30, 34, 48);

        // Verbindungs-LED
        _lblVerbindung.Size      = new Size(14, 14);
        _lblVerbindung.BackColor = Color.FromArgb(200, 50, 50);
        _lblVerbindung.BorderStyle = BorderStyle.FixedSingle;
        _lblVerbindung.Location  = new Point(8, 11);
        new ToolTip().SetToolTip(_lblVerbindung, "Tachymeter-Verbindungsstatus");

        void HdrBtn(Button b, string text, string tip, int width, Color back, Color border, EventHandler handler)
        {
            b.Text      = text;
            b.Size      = new Size(width, 26);
            b.FlatStyle = FlatStyle.Flat;
            b.BackColor = back;
            b.ForeColor = Color.White;
            b.FlatAppearance.BorderColor = border;
            b.Font      = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            b.Cursor    = Cursors.Hand;
            b.Padding   = new Padding(0);
            b.Click    += handler;
            new ToolTip().SetToolTip(b, tip);
            _pnlHeader.Controls.Add(b);
        }

        HdrBtn(_btnPrismkonstante, "PK: 0 mm",
            "Prismenkonstante – Klick zum Ändern",
            100, Color.FromArgb(50, 55, 75), Color.FromArgb(70, 80, 110),
            (s, e) => PrismkonstanteAendern());

        HdrBtn(_btnEdmModus, "Prisma",
            "EDM-Modus umschalten: Prisma → RL Kurz → RL Lang",
            90, Color.FromArgb(38, 110, 72), Color.FromArgb(26, 86, 54),
            (s, e) => EdmModusUmschalten());

        HdrBtn(_btnLaserpointer, "Laser EIN",
            "Laserpointer ein-/ausschalten",
            80, Color.FromArgb(75, 60, 60), Color.FromArgb(60, 40, 40),
            (s, e) => LaserpointerUmschalten());

        HdrBtn(_btnMessen, "▶ MESSEN",
            "Messung auslösen",
            90, Color.FromArgb(160, 90, 20), Color.FromArgb(130, 70, 10),
            (s, e) => MessungAuslösen());

        // Controls in Header positionieren (FlowLayout-ähnlich)
        _pnlHeader.Controls.Add(_lblVerbindung);

        // Buttons werden via Dock+Anchor nicht möglich → manuell im Resize positionieren
        _pnlHeader.Resize += (s, e) => PositioniereHeaderButtons();
        PositioniereHeaderButtons();

        // Canvas
        _canvas.Dock = DockStyle.Fill;

        // Sidebar (rechts)
        _pnlSide.Dock      = DockStyle.Right;
        _pnlSide.Width     = 42;
        _pnlSide.BackColor = Color.FromArgb(52, 56, 70);

        int sy = 6, ss = 36, sp = 6;
        void Btn(Button b, string text, Font font, Color back, Color fore, Color border,
                 string icon, string tip, EventHandler handler)
        {
            b.Text      = text;
            b.Size      = new Size(ss, ss);
            b.Location  = new Point(3, sy);
            b.Font      = font;
            b.FlatStyle = FlatStyle.Flat;
            b.BackColor = back;
            b.ForeColor = fore;
            b.FlatAppearance.BorderColor = border;
            b.Cursor    = Cursors.Hand;
            b.Padding   = new Padding(0);
            b.Click    += handler;
            IconLoader.Apply(b, icon);
            new ToolTip().SetToolTip(b, tip);
            _pnlSide.Controls.Add(b);
            sy += ss + sp;
        }

        Btn(_btnZoomIn,  "⊕", fntIco, colBase, Color.FromArgb(210,215,230), colBorder,
            "sidebar_zoom_in.png",  "Zoom +",             (s,e) => _canvas.ZoomIn());
        sy -= sp - 2; // Zoom-Buttons enger
        Btn(_btnZoomOut, "⊖", fntIco, colBase, Color.FromArgb(210,215,230), colBorder,
            "sidebar_zoom_out.png", "Zoom −",             (s,e) => _canvas.ZoomOut());
        sy -= sp - 2;
        Btn(_btnFit,     "⊡", fntIco, colBase, Color.FromArgb(210,215,230), colBorder,
            "sidebar_fit.png",      "Einpassen",          (s,e) => _canvas.FitToView());

        Btn(_btnSnap,  "◎", fntIco, colActive, Color.White, Color.FromArgb(40,90,165),
            "sidebar_snap.png",   "Punktfang (Snap)",    (s,e) => ToggleSnap());
        sy -= sp - 2;
        Btn(_btnPunkte, "◉", fntIco, colActive, Color.White, Color.FromArgb(40,90,165),
            "sidebar_points.png", "Katasterpunkte ein/aus", (s,e) => TogglePunkte());

        Btn(_btnDxfToggle, "DXF", fntSm, colActive, Color.White, Color.FromArgb(40,90,165),
            "sidebar_dxf_toggle.png", "DXF-Darstellung ein/aus", (s,e) => ToggleDxf());

        Btn(_btnExportDxf, "↑DXF", fntSm, colBase, Color.FromArgb(200,205,225), colBorder,
            "sidebar_dxf_export.png", "Als DXF exportieren", (s,e) => ExportDxf());

        Btn(_btnDigitalisierung, "✎", fntIco, colBase, Color.FromArgb(200,230,210), colBorder,
            "", "Digitalisierung: Klick → Punkt schreiben", (s,e) => ToggleDigitalisierung());

        // Statusleiste (unten)
        _pnlStatus.Dock      = DockStyle.Bottom;
        _pnlStatus.Height    = 28;
        _pnlStatus.BackColor = Color.FromArgb(210, 213, 222);

        _lblKoord.Dock      = DockStyle.Left;
        _lblKoord.Width     = 340;
        _lblKoord.Font      = new Font("Consolas", 9F);
        _lblKoord.ForeColor = Color.FromArgb(40, 45, 70);
        _lblKoord.TextAlign = ContentAlignment.MiddleLeft;
        _lblKoord.Padding   = new Padding(6, 0, 0, 0);
        _lblKoord.Text      = "DXF wird geladen …";

        _lblMaßstab.Dock      = DockStyle.Right;
        _lblMaßstab.Width     = 90;
        _lblMaßstab.Font      = new Font("Consolas", 9F);
        _lblMaßstab.ForeColor = Color.FromArgb(40, 45, 70);
        _lblMaßstab.TextAlign = ContentAlignment.MiddleRight;
        _lblMaßstab.Padding   = new Padding(0, 0, 6, 0);

        _flpLayers.Dock          = DockStyle.Fill;
        _flpLayers.AutoScroll    = false;
        _flpLayers.WrapContents  = false;
        _flpLayers.FlowDirection = FlowDirection.LeftToRight;
        _flpLayers.Padding       = new Padding(4, 5, 0, 0);
        _flpLayers.BackColor     = Color.FromArgb(210, 213, 222);

        _pnlStatus.Controls.Add(_flpLayers);
        _pnlStatus.Controls.Add(_lblMaßstab);
        _pnlStatus.Controls.Add(_lblKoord);

        Controls.Add(_canvas);
        Controls.Add(_pnlHeader);
        Controls.Add(_pnlSide);
        Controls.Add(_pnlStatus);

        // 2:1-Zoomgrenze: beim ersten Zeichnen gesetzt (DPI-abhängig)
        // Vorläufiger Wert mit 96 DPI:
        _canvas.MaxScale = 2.0 * 96 / 0.0254;
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        // Exakte DPI nach Fenster-Handle verfügbar
        try
        {
            using var gr = _canvas.CreateGraphics();
            _canvas.MaxScale = 2.0 * gr.DpiX / 0.0254;
        }
        catch { }

        // Eingabefelder der Elternform für Koordinatenübernahme registrieren
        var form = FindForm();
        if (form != null) RegistriereEingabeFelder(form);

        LetztesDxfLaden();
    }

    // Alle TextBoxen und DataGridViews der Form rekursiv aufspüren und Enter-Events einhängen
    private void RegistriereEingabeFelder(Control container)
    {
        foreach (Control c in container.Controls)
        {
            if (c == this) continue; // Panel selbst überspringen
            if (c is TextBox tb)
            {
                tb.Enter += (s, e) => { _letztesFeld = tb; _letztesGrid = null; };
            }
            else if (c is DataGridView dgv)
            {
                dgv.CellEnter  += (s, e) => { _letztesGrid = dgv; _letztesFeld = null; };
                dgv.GotFocus   += (s, e) => { _letztesGrid = dgv; _letztesFeld = null; };
            }
            if (c.HasChildren) RegistriereEingabeFelder(c);
        }
    }

    // ── Neupunkte/Residual-Checkboxen ─────────────────────────────────────────
    private void BaueNeupunkteCheckboxen()
    {
        _chkNeupunkte = Chk("Neupunkte", _canvas.NeupunkteVisible,
            c => { _canvas.NeupunkteVisible = c; _canvas.Invalidate(); });
        _chkResidual  = Chk("Residuen",  _canvas.ResidualVisible,
            c => { _canvas.ResidualVisible  = c; _canvas.Invalidate(); });
        _flpLayers.Controls.Add(_chkNeupunkte);
        _flpLayers.Controls.Add(_chkResidual);
    }

    private static CheckBox Chk(string text, bool initial, Action<bool> onChange)
    {
        var chk = new CheckBox
        {
            Text      = text,
            Checked   = initial,
            AutoSize  = true,
            Font      = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(30, 30, 30),
            Margin    = new Padding(6, 3, 0, 0),
            Cursor    = Cursors.Hand
        };
        chk.CheckedChanged += (s, e) => onChange(chk.Checked);
        return chk;
    }

    // ── DXF laden ─────────────────────────────────────────────────────────────
    public void LetztesDxfLaden()
    {
        if (!ProjektManager.HatDxfDatei) { _lblKoord.Text = "Kein DXF geladen."; return; }
        try
        {
            LadeDxf(ProjektManager.DxfPfad);
        }
        catch (Exception ex)
        {
            _lblKoord.Text = $"DXF-Fehler: {ex.Message}";
        }
    }

    public void LadeDxf(string pfad)
    {
        if (!File.Exists(pfad)) { _lblKoord.Text = $"DXF nicht gefunden: {Path.GetFileName(pfad)}"; return; }
        try
        {
            Cursor = Cursors.WaitCursor;
            var entities = DxfReader.Read(pfad);
            _canvas.Entities   = entities;
            _canvas.PunktIndex = DxfPunktIndex.Aufbauen(entities);
            _canvas.PunktMarker.Clear();
            foreach (var e in entities)
            {
                switch (e)
                {
                    case DxfPoint pt:
                        var nr1 = _canvas.PunktIndex.GetPunktNr(pt.X, pt.Y);
                        if (nr1 != null) _canvas.PunktMarker.Add(new DxfPunktMarker(nr1, pt.X, pt.Y));
                        break;
                    case DxfInsert ins:
                        var nr2 = _canvas.PunktIndex.GetPunktNr(ins.X, ins.Y);
                        if (nr2 != null) _canvas.PunktMarker.Add(new DxfPunktMarker(nr2, ins.X, ins.Y));
                        break;
                    case DxfCircle circ:
                        var nr3 = _canvas.PunktIndex.GetPunktNr(circ.CX, circ.CY);
                        if (nr3 != null) _canvas.PunktMarker.Add(new DxfPunktMarker(nr3, circ.CX, circ.CY));
                        break;
                    case DxfLine ln:
                        var nr4 = _canvas.PunktIndex.GetPunktNr(ln.X1, ln.Y1);
                        var nr5 = _canvas.PunktIndex.GetPunktNr(ln.X2, ln.Y2);
                        if (nr4 != null) _canvas.PunktMarker.Add(new DxfPunktMarker(nr4, ln.X1, ln.Y1));
                        if (nr5 != null) _canvas.PunktMarker.Add(new DxfPunktMarker(nr5, ln.X2, ln.Y2));
                        break;
                    case DxfArc arc:
                        double sr = arc.StartAngle * Math.PI / 180.0;
                        double er = arc.EndAngle   * Math.PI / 180.0;
                        double sx = arc.CX + arc.Radius * Math.Cos(sr);
                        double sy = arc.CY + arc.Radius * Math.Sin(sr);
                        double ex = arc.CX + arc.Radius * Math.Cos(er);
                        double ey = arc.CY + arc.Radius * Math.Sin(er);
                        var nr6 = _canvas.PunktIndex.GetPunktNr(sx, sy);
                        var nr7 = _canvas.PunktIndex.GetPunktNr(ex, ey);
                        if (nr6 != null) _canvas.PunktMarker.Add(new DxfPunktMarker(nr6, sx, sy));
                        if (nr7 != null) _canvas.PunktMarker.Add(new DxfPunktMarker(nr7, ex, ey));
                        break;
                }
            }

            // Import-Overlay (gespeicherte Importpunkte)
            BaueImportOverlay();

            // Zoom wiederherstellen
            var zoom = ProjektManager.LadeZoom(pfad);
            if (zoom.HasValue)
            {
                _canvas.Scale = zoom.Value.Scale;
                _canvas.PanX  = zoom.Value.PanX;
                _canvas.PanY  = zoom.Value.PanY;
                _canvas.Invalidate();
                OnScaleChanged();   // Maßstab-Label aktualisieren
            }
            else
                _canvas.FitToView();

            // PunktMarker-Checkbox
            BauePunktMarkerCheckbox();

            _dxfPfad       = pfad;
            _lblKoord.Text = $"DXF: {Path.GetFileName(pfad)}  |  {_canvas.Entities.Count} Obj.";
        }
        catch (Exception ex)
        {
            _lblKoord.Text = $"Fehler: {ex.Message}";
        }
        finally { Cursor = Cursors.Default; }
    }

    // ── Header-Button-Positionierung ──────────────────────────────────────────
    private void PositioniereHeaderButtons()
    {
        int x = 28;
        int y = 5;
        foreach (Button b in new[] { _btnPrismkonstante, _btnEdmModus, _btnLaserpointer })
        {
            b.Location = new Point(x, y);
            x += b.Width + 4;
        }
        // MESSEN rechts andocken
        _btnMessen.Location = new Point(_pnlHeader.Width - _btnMessen.Width - 6, y);
    }

    // ── Verbindungsstatus ─────────────────────────────────────────────────────
    private void AktualisiereVerbindungsLED()
    {
        _lblVerbindung.BackColor = TachymeterVerbindung.IstVerbunden
            ? Color.LimeGreen : Color.FromArgb(200, 50, 50);
        string tip = TachymeterVerbindung.IstVerbunden
            ? $"Verbunden ({TachymeterVerbindung.Port})"
            : "Kein Tachymeter verbunden";
        new ToolTip().SetToolTip(_lblVerbindung, tip);
    }

    private void OnVerbindungGetrennt(object? sender, EventArgs e)
    {
        if (InvokeRequired) { BeginInvoke(AktualisiereVerbindungsLED); return; }
        _laserAn = false;
        AktualisiereVerbindungsLED();
        AktualisiereLaserButton();
        MessungAbschliessen();
    }

    // ── Prismenkonstante ──────────────────────────────────────────────────────
    private void PrismkonstanteAendern()
    {
        using var form = new FormPrismenkonstante(_prismenkonstante_mm);
        if (form.ShowDialog(FindForm()) != DialogResult.OK) return;
        _prismenkonstante_mm = form.GewähltePrismenkonstante;
        _btnPrismkonstante.Text = $"PK: {_prismenkonstante_mm:0.#} mm";

        if (TachymeterVerbindung.IstVerbunden && _befehlsgeber.UnterstueztEdmModus)
        {
            try
            {
                double pk_m = (double)_prismenkonstante_mm / 1000.0;
                string cmd  = $"%R1Q,{GeoCOMParser.RPC_TMC_SetPrismCorr},0:{pk_m.ToString("F4", CultureInfo.InvariantCulture)}";
                SendeTachBefehl(cmd, GeoCOMParser.RPC_TMC_SetPrismCorr);
            }
            catch { }
        }
    }

    // ── EDM-Modus ─────────────────────────────────────────────────────────────
    private void EdmModusUmschalten()
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
                    SendeTachBefehl(befehle[0], GeoCOMParser.RPC_BAP_SetTargetType);
                    if (befehle.Length > 1)
                        SendeTachBefehl(befehle[1], GeoCOMParser.RPC_TMC_SetEdmMode);
                }
            }
            catch { }
        }
        AktualisiereEdmModusButton();
    }

    private void AktualisiereEdmModusButton()
    {
        (_btnEdmModus.Text, _btnEdmModus.BackColor, _btnEdmModus.FlatAppearance.BorderColor) =
            _messModus switch
            {
                MessModus.Prisma           => ("Prisma",   Color.FromArgb(38,110,72),  Color.FromArgb(26,86,54)),
                MessModus.ReflektorlosKurz => ("RL Kurz",  Color.FromArgb(140,80,20),  Color.FromArgb(110,60,10)),
                _                          => ("RL Lang",  Color.FromArgb(100,50,130), Color.FromArgb(75,30,105))
            };
    }

    // ── Laserpointer ──────────────────────────────────────────────────────────
    private void LaserpointerUmschalten()
    {
        if (!TachymeterVerbindung.IstVerbunden) return;
        try
        {
            _laserAn = !_laserAn;
            var cmd = _befehlsgeber.LaserBefehl(_laserAn);
            if (cmd != null)
                SendeTachBefehl(cmd, GeoCOMParser.RPC_EDM_Laserpointer);
        }
        catch { _laserAn = false; }
        AktualisiereLaserButton();
    }

    private void AktualisiereLaserButton()
    {
        _btnLaserpointer.Text      = _laserAn ? "Laser AUS" : "Laser EIN";
        _btnLaserpointer.BackColor = _laserAn ? Color.FromArgb(200,50,40) : Color.FromArgb(75,60,60);
        _btnLaserpointer.FlatAppearance.BorderColor = _laserAn ? Color.FromArgb(160,30,20) : Color.FromArgb(60,40,40);
    }

    // ── Messung auslösen ──────────────────────────────────────────────────────
    private void MessungAuslösen()
    {
        if (_messungZustand != MessungZustand.Bereit) return;
        if (!TachymeterVerbindung.IstVerbunden) return;
        try
        {
            _btnMessen.Enabled = false;
            if (_befehlsgeber is GeoCOMBefehlsgeber)
            {
                _messungZustand = MessungZustand.WarteMessen;
                SendeTachBefehl(_befehlsgeber.MessTriggerBefehl()!, _befehlsgeber.MessSchritt1Rpc);
            }
            else
            {
                _messungZustand = MessungZustand.WarteMessen;
                TachymeterVerbindung.GeoCOM_Senden(_befehlsgeber.MessTriggerBefehl() ?? "");
            }
        }
        catch { MessungAbschliessen(); }
    }

    private void MessungAbschliessen()
    {
        _messungZustand    = MessungZustand.Bereit;
        _btnMessen.Enabled = true;
    }

    // ── Tachymeter-Datenempfang ───────────────────────────────────────────────
    private void OnTachDatenEmpfangen(object? sender, string daten)
    {
        if (InvokeRequired) { BeginInvoke(() => OnTachDatenEmpfangen(sender, daten)); return; }

        _zeilenPuffer.Append(daten);
        string puffer = _zeilenPuffer.ToString();
        int pos;
        while ((pos = puffer.IndexOf('\n')) >= 0)
        {
            string zeile = puffer[..pos].TrimEnd('\r');
            puffer = puffer[(pos + 1)..];
            if (!string.IsNullOrWhiteSpace(zeile))
                VerarbeiteZeile(zeile);
        }
        _zeilenPuffer.Clear();
        _zeilenPuffer.Append(puffer);
    }

    private void VerarbeiteZeile(string zeile)
    {
        if (!(_befehlsgeber is GeoCOMBefehlsgeber)) return;

        _geocomParser.SetzeLetzenRpc(_letzterRpc);
        var messung = GeoCOMParser.ParseAntwort(zeile, _letzterRpc);
        if (messung == null) return;

        if (messung.IstVollmessung || messung.HatWinkel)
            MessungEingetroffen?.Invoke(messung);

        if (_messungZustand == MessungZustand.WarteMessen
            && _letzterRpc == _befehlsgeber.MessSchritt1Rpc)
        {
            if (messung.IstFehler) { MessungAbschliessen(); return; }
            _messungZustand = MessungZustand.WarteErgebnis;
            SendeTachBefehl(_befehlsgeber.MessErgebnisBefehl()!, _befehlsgeber.MessSchritt2Rpc);
        }
        else if (_messungZustand == MessungZustand.WarteErgebnis
            && _letzterRpc == _befehlsgeber.MessSchritt2Rpc)
        {
            MessungAbschliessen();
        }
    }

    private void SendeTachBefehl(string befehl, int rpcKontext = 0)
    {
        if (rpcKontext != 0)
        {
            _letzterRpc = rpcKontext;
            _geocomParser.SetzeLetzenRpc(rpcKontext);
        }
        TachymeterVerbindung.GeoCOM_Senden(befehl);
    }

    // ── Header-Text (rückwärtskompatibel, nicht mehr sichtbar) ───────────────
    public void SetHeaderText(string text) { /* Station-Info ist nun implizit im Standpunkt-Label der Form */ }

    // ── Zoom speichern ────────────────────────────────────────────────────────
    public void SpeichereZoom()
    {
        if (!string.IsNullOrEmpty(_dxfPfad))
            ProjektManager.SpeichereZoom(_dxfPfad, _canvas.Scale, _canvas.PanX, _canvas.PanY);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SpeichereZoom();
            TachymeterVerbindung.DatenEmpfangen   -= OnTachDatenEmpfangen;
            TachymeterVerbindung.VerbindungGetrennt -= OnVerbindungGetrennt;
            if (_laserAn)
            {
                try
                {
                    var cmd = _befehlsgeber.LaserBefehl(false);
                    if (cmd != null) TachymeterVerbindung.GeoCOM_Senden(cmd);
                }
                catch { }
            }
        }
        base.Dispose(disposing);
    }

    // ── Button-Handler ────────────────────────────────────────────────────────
    private void ToggleSnap()
    {
        _canvas.SnapActive = !_canvas.SnapActive;
        UpdateSnapButton();
    }
    private void UpdateSnapButton()
    {
        bool a = _canvas.SnapActive;
        _btnSnap.BackColor = a ? Color.FromArgb(52,110,190) : Color.FromArgb(68,74,92);
        _btnSnap.ForeColor = a ? Color.White : Color.FromArgb(200,215,230);
        _btnSnap.FlatAppearance.BorderColor = a ? Color.FromArgb(40,90,165) : Color.FromArgb(85,92,115);
    }

    private void TogglePunkte()
    {
        _canvas.PunkteVisible = !_canvas.PunkteVisible;
        _canvas.Invalidate();
        bool a = _canvas.PunkteVisible;
        _btnPunkte.BackColor = a ? Color.FromArgb(200,120,30) : Color.FromArgb(68,74,92);
        _btnPunkte.ForeColor = a ? Color.White : Color.FromArgb(200,210,230);
        _btnPunkte.FlatAppearance.BorderColor = a ? Color.FromArgb(220,140,50) : Color.FromArgb(85,92,115);
    }

    private void ToggleDxf()
    {
        _canvas.DxfVisible = !_canvas.DxfVisible;
        _canvas.Invalidate();
        bool a = _canvas.DxfVisible;
        _btnDxfToggle.BackColor = a ? Color.FromArgb(60,100,160) : Color.FromArgb(68,74,92);
        _btnDxfToggle.ForeColor = a ? Color.White : Color.FromArgb(200,210,230);
        _btnDxfToggle.FlatAppearance.BorderColor = a ? Color.FromArgb(40,80,140) : Color.FromArgb(85,92,115);
    }

    private void ExportDxf()
    {
        using var form = new FormDxfExport();
        form.ShowDialog(FindForm());
    }

    public void BaueImportOverlay()
    {
        var punkte = ImportPunkteManager.Punkte.ToList();
        if (punkte.Count == 0) return;
        const string name = "ImportPunkte.json";
        var layer = _canvas.ImportLayers.FirstOrDefault(
            l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? new ImportLayer(name);
        layer.Entities.Clear();
        foreach (var p in punkte)
            layer.Entities.Add(new OverlayImportPunkt(p.PunktNr, p.R, p.H, p.Hoehe));
        if (!_canvas.ImportLayers.Contains(layer)) _canvas.ImportLayers.Add(layer);
        BaueCheckboxen();
        _canvas.Invalidate();
    }

    private void BaueCheckboxen()
    {
        var toRemove = _flpLayers.Controls.OfType<CheckBox>()
            .Where(c => c != _chkNeupunkte && c != _chkResidual && c != _chkPunktMarker)
            .ToList();
        foreach (var c in toRemove) _flpLayers.Controls.Remove(c);
        foreach (var layer in _canvas.ImportLayers)
        {
            var l   = layer;
            var chk = Chk(l.Name, l.Visible, v => { l.Visible = v; _canvas.Invalidate(); });
            new ToolTip().SetToolTip(chk, l.Name);
            _flpLayers.Controls.Add(chk);
        }
    }

    private void BauePunktMarkerCheckbox()
    {
        if (_chkPunktMarker != null) _flpLayers.Controls.Remove(_chkPunktMarker);
        _chkPunktMarker = Chk("DXF-Punkte", _canvas.PunktMarkerVisible,
            v => { _canvas.PunktMarkerVisible = v; _canvas.Invalidate(); });
        new ToolTip().SetToolTip(_chkPunktMarker, "DXF-Punktnummern ein-/ausblenden");
        _flpLayers.Controls.Add(_chkPunktMarker);
        int idx = _flpLayers.Controls.IndexOf(_chkNeupunkte);
        if (idx >= 0) _flpLayers.Controls.SetChildIndex(_chkPunktMarker, idx);
    }

    // ── Digitalisierung ───────────────────────────────────────────────────────
    private void ToggleDigitalisierung()
    {
        if (_digModus)
        {
            _digModus = false;
            UpdateDigButton();
            _lblKoord.Text = $"Digitalisierung beendet – {Path.GetFileName(_digKorPfad)}";
            return;
        }

        if (string.IsNullOrEmpty(_digKorPfad))
        {
            string dir  = ProjektManager.ProjektVerzeichnis;
            string fn   = ProjektManager.ProjektName + "-dig.kor";
            string pfad = Path.Combine(dir, fn);
            if (File.Exists(pfad))
            {
                _digZaehler  = NaechstePunktNr(pfad);
                _digKorPfad  = pfad;
            }
            else
            {
                using var dlg = new SaveFileDialog
                {
                    Title = "Digitalisierte Punkte speichern als …",
                    Filter = "KOR-Datei|*.kor|Alle Dateien|*.*",
                    DefaultExt = "kor",
                    InitialDirectory = Directory.Exists(dir) ? dir : "",
                    FileName = fn
                };
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _digKorPfad = dlg.FileName;
                File.WriteAllText(_digKorPfad,
                    $"% Digitalisierte Punkte  {DateTime.Now:yyyy-MM-dd HH:mm}\n",
                    Encoding.UTF8);
                var csv = new StringBuilder();
                csv.AppendLine("# METADATA");
                csv.AppendLine($"Projekt: {ProjektManager.ProjektName}");
                csv.AppendLine("Sensor: Digitalisierung");
                csv.AppendLine($"Datum: {DateTime.Now:yyyy-MM-dd}");
                csv.AppendLine("---"); csv.AppendLine("# DATENTYP"); csv.AppendLine("Koordinaten");
                csv.AppendLine("# DATEN"); csv.AppendLine("PunktNr; X; Y; Z; Code");
                File.WriteAllText(GetKorCsvPfad(_digKorPfad), csv.ToString(), Encoding.UTF8);
                _digZaehler = 1;
                _digLayer   = null;
            }
        }

        _digModus = true;
        UpdateDigButton();
        _lblKoord.Text = $"Digitalisierung aktiv  → {Path.GetFileName(_digKorPfad)}  [Klick = Punkt]";
    }

    private void UpdateDigButton()
    {
        bool a = _digModus;
        _btnDigitalisierung.BackColor = a ? Color.FromArgb(36,120,60) : Color.FromArgb(68,74,92);
        _btnDigitalisierung.ForeColor = a ? Color.White : Color.FromArgb(200,230,210);
        _btnDigitalisierung.FlatAppearance.BorderColor =
            a ? Color.FromArgb(20,90,40) : Color.FromArgb(85,92,115);
    }

    // ── PointPicked ───────────────────────────────────────────────────────────
    private void OnPointPicked(double x, double y, DxfEntity? entity, bool isClick)
    {
        if (_digModus && isClick)
        {
            string nr = _digZaehler.ToString();
            SchreibeDigPunkt(nr, x, y, 0.0, "");
            if (_digLayer == null)
            {
                _digLayer = new ImportLayer("Digitalisierung");
                _canvas.ImportLayers.Add(_digLayer);
                BaueCheckboxen();
            }
            _digLayer.Entities.Add(new OverlayImportPunkt(nr, x, y, 0.0));
            _canvas.Invalidate();
            _lblKoord.Text = $"Dig. {nr}: R={x:F3}  H={y:F3}";
            _digZaehler++;
            return;
        }

        string snap = _canvas.SnapActive ? " [SNAP]" : "";
        _lblKoord.Text = entity != null
            ? $"R: {x:F3}   H: {y:F3}{snap}   |   {entity.GetInfo()}"
            : $"R: {x:F3}   H: {y:F3}{snap}";

        if (!isClick) return;

        // Punktnummer aus Index (wenn Snap aktiv)
        string? punktNr = _canvas.PunktIndex?.GetPunktNr(x, y);

        // Event für alle Subscriber (z.B. Grids)
        PunktGefangen?.Invoke(x, y, punktNr);

        // TextBox auto-füllen
        if (_letztesFeld != null)
        {
            char typ = FeldTyp(_letztesFeld);
            string wert = typ switch
            {
                'R' => x.ToString("F3", CultureInfo.InvariantCulture),
                'H' => y.ToString("F3", CultureInfo.InvariantCulture),
                'N' => punktNr ?? "",
                _   => ""
            };
            if (wert != "")
            {
                _letztesFeld.Text = wert;
                // Fokus auf nächstes Eingabefeld setzen
                _letztesFeld.FindForm()?.SelectNextControl(_letztesFeld, true, true, true, false);
            }
        }
        else if (_letztesGrid != null)
        {
            FuelleGridZelle(_letztesGrid, x, y, punktNr);
        }
    }

    // Bestimmt den Koordinatentyp eines Textfeldes ('R', 'H', 'N', oder '\0')
    private static char FeldTyp(TextBox tb)
    {
        // Explizit per Tag gesetzt?
        if (tb.Tag is char c && (c == 'R' || c == 'H' || c == 'N')) return c;
        if (tb.Tag is string s && s.Length == 1)
        {
            char cs = char.ToUpperInvariant(s[0]);
            if (cs == 'R' || cs == 'H' || cs == 'N') return cs;
        }

        // Namensmuster: txtR_A, txtH_A, txtR0, txtH0, … (zweites Zeichen nach "txt" = R/H, drittes = '_' oder Ziffer)
        string name = tb.Name;
        int pfx = name.StartsWith("txt", StringComparison.OrdinalIgnoreCase) ? 3 : 0;
        if (name.Length >= pfx + 2)
        {
            char first  = char.ToUpperInvariant(name[pfx]);
            char second = name[pfx + 1];
            if ((first == 'R' || first == 'H') && (second == '_' || char.IsDigit(second)))
                return first;
        }
        return '\0';
    }

    // Füllt die aktuelle Zelle einer DataGridView mit R oder H (je nach Spalte)
    private static void FuelleGridZelle(DataGridView dgv, double r, double h, string? nr)
    {
        var cell = dgv.CurrentCell;
        if (cell == null) return;
        string colName = dgv.Columns[cell.ColumnIndex].Name.ToUpperInvariant();
        string wert = colName switch
        {
            "R" => r.ToString("F3", CultureInfo.InvariantCulture),
            "H" => h.ToString("F3", CultureInfo.InvariantCulture),
            _   => ""
        };
        if (wert == "") return;
        cell.Value = wert;
        // Cursor zur nächsten Zeile gleiche Spalte oder nächste Spalte
        int nextCol = cell.ColumnIndex + 1 < dgv.Columns.Count ? cell.ColumnIndex + 1 : cell.ColumnIndex;
        int nextRow = nextCol == cell.ColumnIndex ? Math.Min(cell.RowIndex + 1, dgv.Rows.Count - 1) : cell.RowIndex;
        dgv.CurrentCell = dgv[nextCol, nextRow];
    }

    // ── ScaleChanged ──────────────────────────────────────────────────────────
    private void OnScaleChanged()
    {
        _lblMaßstab.Text = "M " + _canvas.GetMaßstabText();
    }

    // ── Hilfsmethoden ─────────────────────────────────────────────────────────
    private void SchreibeDigPunkt(string nr, double r, double h, double hoehe, string code)
    {
        if (string.IsNullOrEmpty(_digKorPfad)) return;
        var ic = CultureInfo.InvariantCulture;
        string x = r    .ToString("F3", ic);
        string y = h    .ToString("F3", ic);
        string z = hoehe.ToString("F3", ic);
        File.AppendAllText(_digKorPfad, $"{nr,-8}{x,16}{y,16}{z,10}    {code}\n", Encoding.UTF8);
        File.AppendAllText(GetKorCsvPfad(_digKorPfad), $"{nr}; {x}; {y}; {z}; {code}\n", Encoding.UTF8);
    }

    private static string GetKorCsvPfad(string korPfad)
    {
        string dir = Path.GetDirectoryName(korPfad) ?? "";
        string bas = Path.GetFileNameWithoutExtension(korPfad);
        return Path.Combine(dir, bas + "-kor.csv");
    }

    private static int NaechstePunktNr(string korPfad)
    {
        int max = 0;
        try
        {
            foreach (var line in File.ReadLines(korPfad, Encoding.UTF8))
            {
                if (line.StartsWith("%") || string.IsNullOrWhiteSpace(line)) continue;
                string s = line[..Math.Min(8, line.Length)].Trim();
                if (int.TryParse(s, out int n) && n > max) max = n;
            }
        }
        catch { }
        return max > 0 ? max + 1 : 1;
    }
}
