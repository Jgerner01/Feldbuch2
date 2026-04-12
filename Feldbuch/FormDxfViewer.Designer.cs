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
        btnEdmToggle        = new Button();
        btnLaserpointer     = new Button();
        pnlLampe            = new Panel();
        lblLampeInfo        = new Label();
        sep1                = new Label();
        btnModus            = new Button();
        lblSP               = new Label();
        txtStandpunktNr     = new TextBox();
        lblInstrH           = new Label();
        txtInstrHoehe       = new TextBox();
        sep2                = new Label();
        btnMessung          = new Button();
        lblZielhoehe        = new Label();
        txtZielhoehe        = new TextBox();
        lblCode             = new Label();
        txtCode             = new TextBox();
        lblNr               = new Label();
        txtPunktNr          = new TextBox();
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
        btnLoeschen         = new Button();
        btnPunktIndexReset  = new Button();
        pnlStatus           = new Panel();
        lblStatus           = new Label();
        flpLayers           = new FlowLayoutPanel();

        SuspendLayout();
        pnlTop.SuspendLayout();
        pnlStatus.SuspendLayout();

        // ── Fenster ───────────────────────────────────────────────────────────
        ClientSize    = new Size(1200, 780);
        Text          = "DXF-Viewer";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        BackColor     = Color.FromArgb(228, 230, 235);

        // ══ Toolbar oben (46px) ═══════════════════════════════════════════════
        pnlTop.Dock      = DockStyle.Top;
        pnlTop.Height    = 46;
        pnlTop.BackColor = Color.FromArgb(42, 72, 130);

        // ── Prismenkonstante ──────────────────────────────────────────────────
        btnPrismenkonstante.Text      = "";
        btnPrismenkonstante.Size      = new Size(36, 36);
        btnPrismenkonstante.Location  = new Point(4, 5);
        btnPrismenkonstante.FlatStyle = FlatStyle.Flat;
        btnPrismenkonstante.BackColor = Color.FromArgb(60, 95, 160);
        btnPrismenkonstante.ForeColor = Color.White;
        btnPrismenkonstante.FlatAppearance.BorderColor = Color.FromArgb(80, 115, 185);
        btnPrismenkonstante.FlatAppearance.BorderSize  = 1;
        btnPrismenkonstante.Cursor    = Cursors.Hand;
        btnPrismenkonstante.Click    += btnPrismenkonstante_Click;
        IconLoader.Apply(btnPrismenkonstante, "toolbar_prisma.png");
        new ToolTip().SetToolTip(btnPrismenkonstante, "Prismenkonstante");

        // ── EDM-Umschalter (Prisma ↔ Reflektorlos) ───────────────────────────
        btnEdmToggle.Text      = "";
        btnEdmToggle.Size      = new Size(36, 36);
        btnEdmToggle.Location  = new Point(btnPrismenkonstante.Right + 4, 5);
        btnEdmToggle.FlatStyle = FlatStyle.Flat;
        btnEdmToggle.BackColor = Color.FromArgb(60, 95, 160);
        btnEdmToggle.ForeColor = Color.White;
        btnEdmToggle.FlatAppearance.BorderColor = Color.FromArgb(80, 115, 185);
        btnEdmToggle.FlatAppearance.BorderSize  = 1;
        btnEdmToggle.Cursor    = Cursors.Hand;
        btnEdmToggle.Click    += btnEdmToggle_Click;
        IconLoader.Apply(btnEdmToggle, "toolbar_edm_prisma.png");
        new ToolTip().SetToolTip(btnEdmToggle, "EDM-Modus: Prisma / Reflektorlos");

        // ── Laserpointer ─────────────────────────────────────────────────────
        btnLaserpointer.Text      = "";
        btnLaserpointer.Size      = new Size(36, 36);
        btnLaserpointer.Location  = new Point(btnEdmToggle.Right + 4, 5);
        btnLaserpointer.FlatStyle = FlatStyle.Flat;
        btnLaserpointer.BackColor = Color.FromArgb(60, 95, 160);
        btnLaserpointer.ForeColor = Color.White;
        btnLaserpointer.FlatAppearance.BorderColor = Color.FromArgb(80, 115, 185);
        btnLaserpointer.FlatAppearance.BorderSize  = 1;
        btnLaserpointer.Cursor    = Cursors.Hand;
        btnLaserpointer.Click    += btnLaserpointer_Click;
        IconLoader.Apply(btnLaserpointer, "toolbar_laser.png");
        new ToolTip().SetToolTip(btnLaserpointer, "Laserpointer ein/aus");

        // ── Signal-Lampe (wird rechts dynamisch positioniert) ────────────────
        pnlLampe.Size      = new Size(26, 26);
        pnlLampe.Location  = new Point(btnLaserpointer.Right + 80, 10); // Platzhalter
        pnlLampe.BackColor = Color.Transparent;
        pnlLampe.Cursor    = Cursors.Default;
        pnlLampe.Paint    += PnlLampe_Paint;
        new ToolTip().SetToolTip(pnlLampe, "Stationierungsqualität");

        // ── Lampe-Info-Label ──────────────────────────────────────────────────
        lblLampeInfo.AutoSize  = false;
        lblLampeInfo.Size      = new Size(60, 20);
        lblLampeInfo.Location  = new Point(pnlLampe.Right + 3, 13);
        lblLampeInfo.Font      = new Font("Consolas", 8f);
        lblLampeInfo.ForeColor = Color.FromArgb(200, 220, 255);
        lblLampeInfo.BackColor = Color.Transparent;
        lblLampeInfo.TextAlign = ContentAlignment.MiddleLeft;
        lblLampeInfo.Text      = "–";

        // ── Trennlinie 1 (nach Lampe) ─────────────────────────────────────────
        sep1 = new Label
        {
            Size      = new Size(1, 28),
            BackColor = Color.FromArgb(80, 110, 170)
        };

        // ── Modus-Button (Stationierung ↔ Neupunkt) ───────────────────────────
        btnModus.Size      = new Size(100, 36);
        btnModus.FlatStyle = FlatStyle.Flat;
        btnModus.BackColor = Color.FromArgb(42, 130, 65);
        btnModus.ForeColor = Color.White;
        btnModus.FlatAppearance.BorderColor = Color.FromArgb(28, 100, 45);
        btnModus.FlatAppearance.BorderSize  = 1;
        btnModus.Font      = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        btnModus.Cursor    = Cursors.Hand;
        btnModus.Text      = "Stationierung";
        btnModus.Click    += btnModus_Click;
        new ToolTip().SetToolTip(btnModus, "Messmodus wechseln: Stationierung / Neupunkt");

        // ── Standpunkt-Label + Feld ───────────────────────────────────────────
        var lblFont  = new Font("Segoe UI", 8.0F, FontStyle.Bold);
        var lblColor = Color.FromArgb(185, 205, 240);

        lblSP.Text      = "SP";
        lblSP.AutoSize  = false;
        lblSP.Size      = new Size(24, 24);
        lblSP.Font      = lblFont;
        lblSP.ForeColor = lblColor;
        lblSP.TextAlign = ContentAlignment.MiddleRight;

        txtStandpunktNr.Size        = new Size(64, 24);
        txtStandpunktNr.Font        = new Font("Consolas", 10F);
        txtStandpunktNr.BackColor   = Color.FromArgb(55, 88, 150);
        txtStandpunktNr.ForeColor   = Color.White;
        txtStandpunktNr.BorderStyle = BorderStyle.FixedSingle;
        txtStandpunktNr.Leave      += txtStandpunktNr_Leave;
        new ToolTip().SetToolTip(txtStandpunktNr, "Standpunktnummer");

        // ── Instrumentenhöhe-Label + Feld ─────────────────────────────────────
        lblInstrH.Text      = "IH";
        lblInstrH.AutoSize  = false;
        lblInstrH.Size      = new Size(22, 24);
        lblInstrH.Font      = lblFont;
        lblInstrH.ForeColor = lblColor;
        lblInstrH.TextAlign = ContentAlignment.MiddleRight;

        txtInstrHoehe.Size        = new Size(58, 24);
        txtInstrHoehe.Font        = new Font("Consolas", 10F);
        txtInstrHoehe.BackColor   = Color.FromArgb(55, 88, 150);
        txtInstrHoehe.ForeColor   = Color.White;
        txtInstrHoehe.BorderStyle = BorderStyle.FixedSingle;
        txtInstrHoehe.Text        = "0.000";
        txtInstrHoehe.Leave      += txtInstrHoehe_Leave;
        new ToolTip().SetToolTip(txtInstrHoehe, "Instrumentenhöhe [m]");

        // ── Trennlinie 2 (vor Messung) ────────────────────────────────────────
        sep2 = new Label
        {
            Size      = new Size(1, 28),
            BackColor = Color.FromArgb(80, 110, 170)
        };

        // ── Messungs-Button ───────────────────────────────────────────────────
        btnMessung.Size      = new Size(80, 36);
        btnMessung.FlatStyle = FlatStyle.Flat;
        btnMessung.BackColor = Color.FromArgb(160, 60, 20);
        btnMessung.ForeColor = Color.White;
        btnMessung.FlatAppearance.BorderColor = Color.FromArgb(130, 40, 10);
        btnMessung.FlatAppearance.BorderSize  = 1;
        btnMessung.Font      = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        btnMessung.Cursor    = Cursors.Hand;
        btnMessung.Text      = "▶ Messen";
        btnMessung.Click    += btnMessung_Click;
        new ToolTip().SetToolTip(btnMessung, "Messung mit Tachymeter auslösen");

        // ── Zielhöhe-Label + Feld ─────────────────────────────────────────────
        lblZielhoehe.Text      = "ZH";
        lblZielhoehe.AutoSize  = false;
        lblZielhoehe.Size      = new Size(24, 24);
        lblZielhoehe.Font      = lblFont;
        lblZielhoehe.ForeColor = lblColor;
        lblZielhoehe.TextAlign = ContentAlignment.MiddleRight;

        txtZielhoehe.Size        = new Size(58, 24);
        txtZielhoehe.Font        = new Font("Consolas", 10F);
        txtZielhoehe.BackColor   = Color.FromArgb(55, 88, 150);
        txtZielhoehe.ForeColor   = Color.White;
        txtZielhoehe.BorderStyle = BorderStyle.FixedSingle;
        txtZielhoehe.Text        = "0.000";
        new ToolTip().SetToolTip(txtZielhoehe, "Zielhöhe / Reflektorhöhe [m]");

        // ── Code-Label + Feld ─────────────────────────────────────────────────
        lblCode.Text      = "Code";
        lblCode.AutoSize  = false;
        lblCode.Size      = new Size(40, 24);
        lblCode.Font      = lblFont;
        lblCode.ForeColor = lblColor;
        lblCode.TextAlign = ContentAlignment.MiddleRight;

        txtCode.Size        = new Size(90, 24);
        txtCode.Font        = new Font("Consolas", 10F);
        txtCode.BackColor   = Color.FromArgb(55, 88, 150);
        txtCode.ForeColor   = Color.White;
        txtCode.BorderStyle = BorderStyle.FixedSingle;
        new ToolTip().SetToolTip(txtCode, "Punktcode");

        // ── PunktNr-Label + Feld ──────────────────────────────────────────────
        lblNr.Text      = "Nr";
        lblNr.AutoSize  = false;
        lblNr.Size      = new Size(22, 24);
        lblNr.Font      = lblFont;
        lblNr.ForeColor = lblColor;
        lblNr.TextAlign = ContentAlignment.MiddleRight;

        txtPunktNr.Size        = new Size(80, 24);
        txtPunktNr.Font        = new Font("Consolas", 10F);
        txtPunktNr.BackColor   = Color.FromArgb(55, 88, 150);
        txtPunktNr.ForeColor   = Color.White;
        txtPunktNr.BorderStyle = BorderStyle.FixedSingle;
        new ToolTip().SetToolTip(txtPunktNr, "Punktnummer");

        pnlTop.Controls.AddRange(new Control[]
        {
            btnPrismenkonstante, btnEdmToggle, btnLaserpointer,
            pnlLampe, lblLampeInfo,
            sep1, btnModus,
            lblSP, txtStandpunktNr, lblInstrH, txtInstrHoehe,
            sep2, btnMessung,
            lblZielhoehe, txtZielhoehe, lblNr, txtPunktNr, lblCode, txtCode
        });

        // Rechtsbündige Positionierung – beim Laden und bei jedem Resize
        pnlTop.Resize += (s, e) => PositioniereToolbarFelder();
        Load          += (s, e) => PositioniereToolbarFelder();

        // ══ Statusleiste unten (28px) ═════════════════════════════════════════
        pnlStatus.Dock      = DockStyle.Bottom;
        pnlStatus.Height    = 28;
        pnlStatus.BackColor = Color.FromArgb(210, 213, 222);

        lblStatus.Text      = "DXF-Datei öffnen …";
        lblStatus.Dock      = DockStyle.Left;
        lblStatus.Width     = 480;
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

        // ══ Seitenleiste rechts (42px) ════════════════════════════════════════
        pnlSide.Dock      = DockStyle.Right;
        pnlSide.Width     = 42;
        pnlSide.BackColor = Color.FromArgb(52, 56, 70);

        var fntIco  = new Font("Segoe UI", 12F, FontStyle.Bold);
        var fntSm   = new Font("Segoe UI", 7.5F, FontStyle.Bold);
        var colBase   = Color.FromArgb(68, 74, 92);
        var colActive = Color.FromArgb(52, 110, 190);
        var colGreen  = Color.FromArgb(36, 108, 68);
        var colRed    = Color.FromArgb(150, 38, 38);
        var colBorder = Color.FromArgb(85, 92, 115);

        int sy = 6, ss = 36, sp = 6;

        SideBtn(btnOpen, "▤", fntIco, sy, ss, colActive, Color.White, Color.FromArgb(40, 90, 165));
        IconLoader.Apply(btnOpen, "sidebar_open.png");
        btnOpen.Click += btnOpen_Click;
        new ToolTip().SetToolTip(btnOpen, "DXF-Datei öffnen");
        sy += ss + sp;

        SideBtn(btnZoomIn, "⊕", fntIco, sy, ss, colBase, Color.FromArgb(210, 215, 230), colBorder);
        IconLoader.Apply(btnZoomIn, "sidebar_zoom_in.png");
        btnZoomIn.Click += btnZoomIn_Click;
        new ToolTip().SetToolTip(btnZoomIn, "Zoom +");
        sy += ss + 2;

        SideBtn(btnZoomOut, "⊖", fntIco, sy, ss, colBase, Color.FromArgb(210, 215, 230), colBorder);
        IconLoader.Apply(btnZoomOut, "sidebar_zoom_out.png");
        btnZoomOut.Click += btnZoomOut_Click;
        new ToolTip().SetToolTip(btnZoomOut, "Zoom −");
        sy += ss + 2;

        SideBtn(btnFit, "⊡", fntIco, sy, ss, colBase, Color.FromArgb(210, 215, 230), colBorder);
        IconLoader.Apply(btnFit, "sidebar_fit.png");
        btnFit.Click += btnFit_Click;
        new ToolTip().SetToolTip(btnFit, "Einpassen");
        sy += ss + sp;

        SideBtn(btnSnap, "◎", fntIco, sy, ss, colActive, Color.White, Color.FromArgb(40, 90, 165));
        IconLoader.Apply(btnSnap, "sidebar_snap.png");
        btnSnap.Click += btnSnap_Click;
        new ToolTip().SetToolTip(btnSnap, "Punktfang (Snap)");
        sy += ss + 2;

        SideBtn(btnPunkte, "◉", fntIco, sy, ss, colActive, Color.White, Color.FromArgb(40, 90, 165));
        IconLoader.Apply(btnPunkte, "sidebar_points.png");
        btnPunkte.Click += btnPunkte_Click;
        new ToolTip().SetToolTip(btnPunkte, "Katasterpunkte ein/aus");
        sy += ss + sp;

        SideBtn(btnDxfToggle, "DXF", fntSm, sy, ss, colActive, Color.White, Color.FromArgb(40, 90, 165));
        IconLoader.Apply(btnDxfToggle, "sidebar_dxf_toggle.png");
        btnDxfToggle.Click += btnDxfToggle_Click;
        new ToolTip().SetToolTip(btnDxfToggle, "DXF-Darstellung ein/aus");
        sy += ss + sp;

        SideBtn(btnExportDxf, "↑DXF", fntSm, sy, ss, colBase, Color.FromArgb(200, 205, 225), colBorder);
        IconLoader.Apply(btnExportDxf, "sidebar_dxf_export.png");
        btnExportDxf.Click += btnExportDxf_Click;
        new ToolTip().SetToolTip(btnExportDxf, "Als DXF exportieren");
        sy += ss + sp;

        SideBtn(btnNeu, "⊠", fntIco, sy, ss, colBase, Color.FromArgb(200, 205, 225), colBorder);
        IconLoader.Apply(btnNeu, "sidebar_new.png");
        btnNeu.Click += btnNeu_Click;
        new ToolTip().SetToolTip(btnNeu, "DXF leeren");
        sy += ss + sp;

        SideBtn(btnImportKorCsv, "↓KOR", fntSm, sy, ss, colRed, Color.White, Color.FromArgb(120, 28, 28));
        IconLoader.Apply(btnImportKorCsv, "sidebar_import_kor.png");
        btnImportKorCsv.Click += btnImportKorCsv_Click;
        new ToolTip().SetToolTip(btnImportKorCsv, "KOR / CSV importieren");
        sy += ss + 2;

        SideBtn(btnImportJson, "↓JSON", fntSm, sy, ss, colRed, Color.White, Color.FromArgb(120, 28, 28));
        IconLoader.Apply(btnImportJson, "sidebar_import_json.png");
        btnImportJson.Click += btnImportJson_Click;
        new ToolTip().SetToolTip(btnImportJson, "JSON importieren");
        sy += ss + sp;

        SideBtn(btnLoeschen, "⌫", fntIco, sy, ss, colBase, Color.FromArgb(220, 200, 200), colBorder);
        btnLoeschen.Click += btnLoeschen_Click;
        new ToolTip().SetToolTip(btnLoeschen, "Punkte löschen (Fenster aufziehen)");
        sy += ss + sp;

        SideBtn(btnPunktIndexReset, "↻", fntIco, sy, ss, colBase, Color.FromArgb(200, 210, 230), colBorder);
        btnPunktIndexReset.Click += btnPunktIndexReset_Click;
        new ToolTip().SetToolTip(btnPunktIndexReset, "Punkt-Index zurücksetzen (neu nummerieren)");

        pnlSide.Controls.AddRange(new Control[]
        {
            btnOpen, btnZoomIn, btnZoomOut, btnFit,
            btnSnap, btnPunkte, btnDxfToggle, btnExportDxf,
            btnNeu, btnImportKorCsv, btnImportJson, btnLoeschen, btnPunktIndexReset
        });

        // ══ Canvas ════════════════════════════════════════════════════════════
        canvas.Dock = DockStyle.Fill;

        // ── Zusammenbau ───────────────────────────────────────────────────────
        Controls.Add(canvas);
        Controls.Add(pnlSide);
        Controls.Add(pnlStatus);
        Controls.Add(pnlTop);

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
    private DxfCanvas        canvas              = null!;
    private Panel            pnlTop              = null!;
    private Button           btnPrismenkonstante = null!;
    private Button           btnEdmToggle        = null!;
    private Button           btnLaserpointer     = null!;
    private Panel            pnlLampe            = null!;
    private Label            lblLampeInfo        = null!;
    private Label            sep1                = null!;
    private Button           btnModus            = null!;
    private Label            lblSP               = null!;
    private TextBox          txtStandpunktNr     = null!;
    private Label            lblInstrH           = null!;
    private TextBox          txtInstrHoehe       = null!;
    private Label            sep2                = null!;
    private Button           btnMessung          = null!;
    private Label            lblZielhoehe        = null!;
    private TextBox          txtZielhoehe        = null!;
    private Label            lblNr               = null!;
    private TextBox          txtPunktNr          = null!;
    private Label            lblCode             = null!;
    private TextBox          txtCode             = null!;
    private Panel            pnlSide             = null!;
    private Button           btnOpen             = null!;
    private Button           btnZoomIn           = null!;
    private Button           btnZoomOut          = null!;
    private Button           btnFit              = null!;
    private Button           btnSnap             = null!;
    private Button           btnPunkte           = null!;
    private Button           btnDxfToggle        = null!;
    private Button           btnNeu              = null!;
    private Button           btnExportDxf        = null!;
    private Button           btnImportKorCsv     = null!;
    private Button           btnImportJson       = null!;
    private Button           btnLoeschen         = null!;
    private Button           btnPunktIndexReset  = null!;
    private Panel            pnlStatus           = null!;
    private Label            lblStatus           = null!;
    private FlowLayoutPanel  flpLayers           = null!;
}
