namespace Feldbuch;

partial class FormDxfViewer
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        canvas              = new DxfCanvas();
        pnlTop              = new Panel();
        btnPrismenkonstante = new Button();
        lblNr               = new Label();
        txtPunktNr          = new TextBox();
        lblCode             = new Label();
        txtCode             = new TextBox();
        pnlSide             = new Panel();
        btnOpen             = new Button();
        btnZoomIn           = new Button();
        btnZoomOut          = new Button();
        btnFit              = new Button();
        btnSnap             = new Button();
        btnPunkte           = new Button();
        btnDxfToggle        = new Button();
        btnNeu              = new Button();
        btnExportDxf        = new Button();
        btnImportKorCsv     = new Button();
        btnImportJson       = new Button();
        pnlStatus           = new Panel();
        lblStatus           = new Label();
        flpLayers           = new FlowLayoutPanel();

        SuspendLayout();
        pnlTop.SuspendLayout();
        pnlStatus.SuspendLayout();

        // ── Fenster ───────────────────────────────────────────────────────────
        ClientSize    = new Size(1100, 750);
        Text          = "DXF-Viewer";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        BackColor     = Color.FromArgb(228, 230, 235);

        // ══ Toolbar oben (42px) ═══════════════════════════════════════════════
        pnlTop.Dock      = DockStyle.Top;
        pnlTop.Height    = 42;
        pnlTop.BackColor = Color.FromArgb(42, 72, 130);

        // ── Prismenkonstante: owner-drawn, kein Text, zentriertes GDI+-Icon ──
        btnPrismenkonstante.Text      = "";
        btnPrismenkonstante.Size      = new Size(36, 36);
        btnPrismenkonstante.Location  = new Point(4, 3);
        btnPrismenkonstante.FlatStyle = FlatStyle.Flat;
        btnPrismenkonstante.BackColor = Color.FromArgb(60, 95, 160);
        btnPrismenkonstante.ForeColor = Color.White;
        btnPrismenkonstante.FlatAppearance.BorderColor = Color.FromArgb(80, 115, 185);
        btnPrismenkonstante.FlatAppearance.BorderSize  = 1;
        btnPrismenkonstante.Cursor    = Cursors.Hand;
        btnPrismenkonstante.Click    += btnPrismenkonstante_Click;
        IconLoader.Apply(btnPrismenkonstante, "toolbar_prisma.png");
        new ToolTip().SetToolTip(btnPrismenkonstante, "Prismenkonstante");

        // ── Trennstrich links ─────────────────────────────────────────────────
        var sep1 = new Label
        {
            Location  = new Point(46, 6),
            Size      = new Size(1, 28),
            BackColor = Color.FromArgb(80, 110, 170)
        };

        // ── Punktnummer + Code – rechts ausgerichtet, Anchor Right ───────────
        // Abstände vom rechten Rand (bei 1100px Fensterbreite):
        //   txtCode   rechts=8,   Breite=90  → links=1002
        //   lblCode   rechts=108, Breite=52  → links=940  (Abstand 10px zum Feld)
        //   txtNr     rechts=170, Breite=100 → links=830
        //   lblNr     rechts=280, Breite=70  → links=750  (Abstand 10px zum Feld)

        // Feste Schrift & Farbe
        var lblFont  = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        var lblColor = Color.FromArgb(185, 205, 240);

        // Code-Label
        lblCode.Text      = "Code";
        lblCode.Size      = new Size(52, 24);
        lblCode.Font      = lblFont;
        lblCode.ForeColor = lblColor;
        lblCode.TextAlign = ContentAlignment.MiddleRight;

        // Code-Eingabe
        txtCode.Size        = new Size(90, 24);
        txtCode.Font        = new Font("Consolas", 10F);
        txtCode.BackColor   = Color.FromArgb(55, 88, 150);
        txtCode.ForeColor   = Color.White;
        txtCode.BorderStyle = BorderStyle.FixedSingle;
        new ToolTip().SetToolTip(txtCode, "Punktcode");

        // PunktNr-Label
        lblNr.Text      = "Punkt-Nr";
        lblNr.Size      = new Size(70, 24);
        lblNr.Font      = lblFont;
        lblNr.ForeColor = lblColor;
        lblNr.TextAlign = ContentAlignment.MiddleRight;

        // PunktNr-Eingabe
        txtPunktNr.Size        = new Size(100, 24);
        txtPunktNr.Font        = new Font("Consolas", 10F);
        txtPunktNr.BackColor   = Color.FromArgb(55, 88, 150);
        txtPunktNr.ForeColor   = Color.White;
        txtPunktNr.BorderStyle = BorderStyle.FixedSingle;
        new ToolTip().SetToolTip(txtPunktNr, "Punktnummer");

        pnlTop.Controls.AddRange(new Control[]
            { btnPrismenkonstante, sep1, lblNr, txtPunktNr, lblCode, txtCode });

        // Rechtsbündige Positionierung – beim Laden und bei jedem Resize
        pnlTop.Resize += (s, e) => PositioniereToolbarFelder();
        Load          += (s, e) => PositioniereToolbarFelder();

        // ══ Statusleiste unten (28px) ═════════════════════════════════════════
        pnlStatus.Dock      = DockStyle.Bottom;
        pnlStatus.Height    = 28;
        pnlStatus.BackColor = Color.FromArgb(210, 213, 222);

        lblStatus.Text      = "DXF-Datei öffnen …";
        lblStatus.Dock      = DockStyle.Left;
        lblStatus.Width     = 430;
        lblStatus.Font      = new Font("Consolas", 9F);
        lblStatus.ForeColor = Color.FromArgb(40, 45, 70);
        lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        lblStatus.Padding   = new Padding(6, 0, 0, 0);

        flpLayers.Dock          = DockStyle.Fill;
        flpLayers.AutoScroll    = false;
        flpLayers.WrapContents  = false;
        flpLayers.FlowDirection = FlowDirection.LeftToRight;
        flpLayers.Padding       = new Padding(4, 5, 0, 0);
        flpLayers.BackColor     = Color.FromArgb(210, 213, 222);

        pnlStatus.Controls.Add(flpLayers);
        pnlStatus.Controls.Add(lblStatus);

        // ══ Seitenleiste rechts (42px, Buttons 36×36) ════════════════════════
        pnlSide.Dock      = DockStyle.Right;
        pnlSide.Width     = 42;
        pnlSide.BackColor = Color.FromArgb(52, 56, 70);

        // Icon-Schriften
        var fntIco  = new Font("Segoe UI", 12F, FontStyle.Bold);
        var fntSm   = new Font("Segoe UI", 7.5F, FontStyle.Bold);

        // Farben
        var colBase    = Color.FromArgb(68, 74, 92);
        var colActive  = Color.FromArgb(52, 110, 190);
        var colGreen   = Color.FromArgb(36, 108, 68);
        var colRed     = Color.FromArgb(150, 38, 38);
        var colBorder  = Color.FromArgb(85, 92, 115);

        int sy = 6, ss = 36, sp = 6;   // start-y, size, spacing

        // ── Öffnen ────────────────────────────────────────────────────────────
        SideBtn(btnOpen, "▤", fntIco, sy, ss, colActive, Color.White,
                Color.FromArgb(40, 90, 165));
        IconLoader.Apply(btnOpen, "sidebar_open.png");
        btnOpen.Click += btnOpen_Click;
        new ToolTip().SetToolTip(btnOpen, "DXF-Datei öffnen");
        sy += ss + sp;

        // ── Zoom + ────────────────────────────────────────────────────────────
        SideBtn(btnZoomIn, "⊕", fntIco, sy, ss, colBase, Color.FromArgb(210, 215, 230), colBorder);
        IconLoader.Apply(btnZoomIn, "sidebar_zoom_in.png");
        btnZoomIn.Click += btnZoomIn_Click;
        new ToolTip().SetToolTip(btnZoomIn, "Zoom +");
        sy += ss + 2;

        // ── Zoom − ────────────────────────────────────────────────────────────
        SideBtn(btnZoomOut, "⊖", fntIco, sy, ss, colBase, Color.FromArgb(210, 215, 230), colBorder);
        IconLoader.Apply(btnZoomOut, "sidebar_zoom_out.png");
        btnZoomOut.Click += btnZoomOut_Click;
        new ToolTip().SetToolTip(btnZoomOut, "Zoom −");
        sy += ss + 2;

        // ── Einpassen ─────────────────────────────────────────────────────────
        SideBtn(btnFit, "⊡", fntIco, sy, ss, colBase, Color.FromArgb(210, 215, 230), colBorder);
        IconLoader.Apply(btnFit, "sidebar_fit.png");
        btnFit.Click += btnFit_Click;
        new ToolTip().SetToolTip(btnFit, "Einpassen");
        sy += ss + sp;

        // ── Snap ──────────────────────────────────────────────────────────────
        SideBtn(btnSnap, "◎", fntIco, sy, ss, colActive, Color.White,
                Color.FromArgb(40, 90, 165));
        IconLoader.Apply(btnSnap, "sidebar_snap.png");
        btnSnap.Click += btnSnap_Click;
        new ToolTip().SetToolTip(btnSnap, "Punktfang (Snap)");
        sy += ss + 2;

        // ── Katasterpunkte ────────────────────────────────────────────────────
        SideBtn(btnPunkte, "◉", fntIco, sy, ss, colActive, Color.White,
                Color.FromArgb(40, 90, 165));
        IconLoader.Apply(btnPunkte, "sidebar_points.png");
        btnPunkte.Click += btnPunkte_Click;
        new ToolTip().SetToolTip(btnPunkte, "Katasterpunkte ein/aus");
        sy += ss + sp;

        // ── DXF ein/aus ───────────────────────────────────────────────────────
        SideBtn(btnDxfToggle, "DXF", fntSm, sy, ss, colActive, Color.White,
                Color.FromArgb(40, 90, 165));
        IconLoader.Apply(btnDxfToggle, "sidebar_dxf_toggle.png");
        btnDxfToggle.Click += btnDxfToggle_Click;
        new ToolTip().SetToolTip(btnDxfToggle, "DXF-Darstellung ein/aus");
        sy += ss + sp;

        // ── DXF-Export ────────────────────────────────────────────────────────
        SideBtn(btnExportDxf, "↑DXF", fntSm, sy, ss, colBase, Color.FromArgb(200, 205, 225), colBorder);
        IconLoader.Apply(btnExportDxf, "sidebar_dxf_export.png");
        btnExportDxf.Click += btnExportDxf_Click;
        new ToolTip().SetToolTip(btnExportDxf, "Als DXF exportieren");
        sy += ss + sp;

        // ── NEU ───────────────────────────────────────────────────────────────
        SideBtn(btnNeu, "⊠", fntIco, sy, ss, colBase, Color.FromArgb(200, 205, 225), colBorder);
        IconLoader.Apply(btnNeu, "sidebar_new.png");
        btnNeu.Click += btnNeu_Click;
        new ToolTip().SetToolTip(btnNeu, "DXF leeren");
        sy += ss + sp;

        // ── KOR/CSV Import ────────────────────────────────────────────────────
        SideBtn(btnImportKorCsv, "↓KOR", fntSm, sy, ss, colRed, Color.White,
                Color.FromArgb(120, 28, 28));
        IconLoader.Apply(btnImportKorCsv, "sidebar_import_kor.png");
        btnImportKorCsv.Click += btnImportKorCsv_Click;
        new ToolTip().SetToolTip(btnImportKorCsv, "KOR / CSV importieren");
        sy += ss + 2;

        // ── JSON Import ───────────────────────────────────────────────────────
        SideBtn(btnImportJson, "↓JSON", fntSm, sy, ss, colRed, Color.White,
                Color.FromArgb(120, 28, 28));
        IconLoader.Apply(btnImportJson, "sidebar_import_json.png");
        btnImportJson.Click += btnImportJson_Click;
        new ToolTip().SetToolTip(btnImportJson, "JSON importieren");

        pnlSide.Controls.AddRange(new Control[]
        {
            btnOpen, btnZoomIn, btnZoomOut, btnFit,
            btnSnap, btnPunkte, btnDxfToggle, btnExportDxf,
            btnNeu, btnImportKorCsv, btnImportJson
        });

        // ══ Canvas ════════════════════════════════════════════════════════════
        canvas.Dock = DockStyle.Fill;

        // ── Zusammenbau (Reihenfolge wichtig für Dock-Layout) ─────────────────
        Controls.Add(canvas);       // Fill
        Controls.Add(pnlSide);      // Right
        Controls.Add(pnlStatus);    // Bottom
        Controls.Add(pnlTop);       // Top

        pnlTop.ResumeLayout(false);
        pnlStatus.ResumeLayout(false);
        ResumeLayout(false);
    }

    // ── Hilfsmethode: Seitenleisten-Button ───────────────────────────────────
    private static void SideBtn(Button b, string text, Font font,
        int y, int size, Color back, Color fore, Color border)
    {
        b.Text      = text;
        b.Size      = new Size(size, size);
        b.Location  = new Point(3, y);
        b.Font      = font;
        b.FlatStyle = FlatStyle.Flat;
        b.BackColor = back;
        b.ForeColor = fore;
        b.FlatAppearance.BorderColor = border;
        b.Cursor    = Cursors.Hand;
        b.Padding   = new Padding(0);
    }

    // ── Felder ────────────────────────────────────────────────────────────────
    private DxfCanvas          canvas              = null!;
    private Panel              pnlTop              = null!;
    private Button             btnPrismenkonstante = null!;
    private Label              lblNr               = null!;
    private TextBox            txtPunktNr          = null!;
    private Label              lblCode             = null!;
    private TextBox            txtCode             = null!;
    private Panel              pnlSide             = null!;
    private Button             btnOpen             = null!;
    private Button             btnZoomIn           = null!;
    private Button             btnZoomOut          = null!;
    private Button             btnFit              = null!;
    private Button             btnSnap             = null!;
    private Button             btnPunkte           = null!;
    private Button             btnDxfToggle        = null!;
    private Button             btnNeu              = null!;
    private Button             btnExportDxf        = null!;
    private Button             btnImportKorCsv     = null!;
    private Button             btnImportJson       = null!;
    private Panel              pnlStatus           = null!;
    private Label              lblStatus           = null!;
    private FlowLayoutPanel    flpLayers           = null!;
}
