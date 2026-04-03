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
        canvas        = new DxfCanvas();
        pnlSide       = new Panel();
        btnOpen       = new Button();
        btnZoomIn     = new Button();
        btnZoomOut    = new Button();
        btnFit        = new Button();
        btnSnap       = new Button();
        btnPunkte     = new Button();
        btnDxfToggle  = new Button();
        btnExportDxf  = new Button();
        lblStatus     = new Label();

        SuspendLayout();

        // ── Fenster ───────────────────────────────────────────────────────────
        ClientSize    = new Size(1100, 750);
        Text          = "DXF-Viewer";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        BackColor     = Color.FromArgb(230, 230, 230);

        // ── Canvas (füllt den Hauptbereich) ───────────────────────────────────
        canvas.Location = new Point(0, 0);
        canvas.Size     = new Size(1040, 720);
        canvas.Anchor   = AnchorStyles.Top | AnchorStyles.Bottom |
                          AnchorStyles.Left | AnchorStyles.Right;

        // ── Seitenleiste rechts ───────────────────────────────────────────────
        pnlSide.Location  = new Point(1042, 0);
        pnlSide.Size      = new Size(58, 720);
        pnlSide.Anchor    = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
        pnlSide.BackColor = Color.FromArgb(210, 210, 210);

        var iconFont   = new Font("Segoe UI", 16F, FontStyle.Bold);
        var iconFontSm = new Font("Segoe UI", 9F, FontStyle.Bold);

        // ── Öffnen-Button ─────────────────────────────────────────────────────
        btnOpen.Text      = "📂";
        btnOpen.Size      = new Size(54, 54);
        btnOpen.Location  = new Point(2, 10);
        btnOpen.Font      = new Font("Segoe UI", 18F);
        btnOpen.FlatStyle = FlatStyle.Flat;
        btnOpen.BackColor = Color.FromArgb(60, 100, 160);
        btnOpen.ForeColor = Color.White;
        btnOpen.FlatAppearance.BorderColor = Color.FromArgb(80, 120, 180);
        btnOpen.Click    += btnOpen_Click;
        ToolTip ttOpen = new(); ttOpen.SetToolTip(btnOpen, "DXF-Datei öffnen");

        // ── Zoom In ───────────────────────────────────────────────────────────
        btnZoomIn.Text      = "+";
        btnZoomIn.Size      = new Size(54, 54);
        btnZoomIn.Location  = new Point(2, 80);
        btnZoomIn.Font      = iconFont;
        btnZoomIn.FlatStyle = FlatStyle.Flat;
        btnZoomIn.BackColor = Color.FromArgb(190, 190, 190);
        btnZoomIn.ForeColor = Color.FromArgb(30, 30, 30);
        btnZoomIn.FlatAppearance.BorderColor = Color.FromArgb(160, 160, 160);
        btnZoomIn.Click    += btnZoomIn_Click;
        ToolTip ttIn = new(); ttIn.SetToolTip(btnZoomIn, "Zoom vergrößern");

        // ── Zoom Out ──────────────────────────────────────────────────────────
        btnZoomOut.Text      = "−";
        btnZoomOut.Size      = new Size(54, 54);
        btnZoomOut.Location  = new Point(2, 140);
        btnZoomOut.Font      = iconFont;
        btnZoomOut.FlatStyle = FlatStyle.Flat;
        btnZoomOut.BackColor = Color.FromArgb(190, 190, 190);
        btnZoomOut.ForeColor = Color.FromArgb(30, 30, 30);
        btnZoomOut.FlatAppearance.BorderColor = Color.FromArgb(160, 160, 160);
        btnZoomOut.Click    += btnZoomOut_Click;
        ToolTip ttOut = new(); ttOut.SetToolTip(btnZoomOut, "Zoom verkleinern");

        // ── Einpassen ─────────────────────────────────────────────────────────
        btnFit.Text      = "⊡";
        btnFit.Size      = new Size(54, 54);
        btnFit.Location  = new Point(2, 200);
        btnFit.Font      = iconFont;
        btnFit.FlatStyle = FlatStyle.Flat;
        btnFit.BackColor = Color.FromArgb(190, 190, 190);
        btnFit.ForeColor = Color.FromArgb(30, 30, 30);
        btnFit.FlatAppearance.BorderColor = Color.FromArgb(160, 160, 160);
        btnFit.Click    += btnFit_Click;
        ToolTip ttFit = new(); ttFit.SetToolTip(btnFit, "Gesamte Zeichnung einpassen");

        // ── Snap-Button (standardmäßig aktiv, hervorgehoben) ──────────────────
        btnSnap.Text      = "⊕";
        btnSnap.Size      = new Size(54, 54);
        btnSnap.Location  = new Point(2, 280);
        btnSnap.Font      = iconFont;
        btnSnap.FlatStyle = FlatStyle.Flat;
        btnSnap.BackColor = Color.FromArgb(200, 120, 30);   // orange = aktiv
        btnSnap.ForeColor = Color.White;
        btnSnap.FlatAppearance.BorderColor = Color.FromArgb(220, 140, 50);
        btnSnap.Click    += btnSnap_Click;
        ToolTip ttSnap = new(); ttSnap.SetToolTip(btnSnap, "Punktfang (Snap) ein/aus");

        // ── Katasterpunkte ein/aus ────────────────────────────────────────────
        btnPunkte.Text      = "⊙";
        btnPunkte.Size      = new Size(54, 54);
        btnPunkte.Location  = new Point(2, 340);
        btnPunkte.Font      = iconFont;
        btnPunkte.FlatStyle = FlatStyle.Flat;
        btnPunkte.BackColor = Color.FromArgb(200, 120, 30);   // orange = aktiv
        btnPunkte.ForeColor = Color.White;
        btnPunkte.FlatAppearance.BorderColor = Color.FromArgb(220, 140, 50);
        btnPunkte.Click    += btnPunkte_Click;
        ToolTip ttPunkte = new(); ttPunkte.SetToolTip(btnPunkte, "Katasterpunkte ein/aus");

        // ── DXF-Darstellung ein/aus ───────────────────────────────────────────
        btnDxfToggle.Text      = "DXF";
        btnDxfToggle.Size      = new Size(54, 54);
        btnDxfToggle.Location  = new Point(2, 410);
        btnDxfToggle.Font      = iconFontSm;
        btnDxfToggle.FlatStyle = FlatStyle.Flat;
        btnDxfToggle.BackColor = Color.FromArgb(60, 100, 160);   // blau = aktiv
        btnDxfToggle.ForeColor = Color.White;
        btnDxfToggle.FlatAppearance.BorderColor = Color.FromArgb(40, 80, 140);
        btnDxfToggle.Click    += btnDxfToggle_Click;
        ToolTip ttDxf = new(); ttDxf.SetToolTip(btnDxfToggle, "DXF-Darstellung ein/aus");

        // ── DXF-Export ────────────────────────────────────────────────────────
        btnExportDxf.Text      = "↗DXF";
        btnExportDxf.Size      = new Size(54, 54);
        btnExportDxf.Location  = new Point(2, 470);
        btnExportDxf.Font      = iconFontSm;
        btnExportDxf.FlatStyle = FlatStyle.Flat;
        btnExportDxf.BackColor = Color.FromArgb(190, 190, 190);
        btnExportDxf.ForeColor = Color.FromArgb(30, 30, 30);
        btnExportDxf.FlatAppearance.BorderColor = Color.FromArgb(160, 160, 160);
        btnExportDxf.Click    += btnExportDxf_Click;
        ToolTip ttExp = new(); ttExp.SetToolTip(btnExportDxf, "Feldbuchpunkte als DXF exportieren");

        pnlSide.Controls.AddRange(new Control[]
            { btnOpen, btnZoomIn, btnZoomOut, btnFit, btnSnap, btnPunkte,
              btnDxfToggle, btnExportDxf });

        // ── Statusleiste unten ────────────────────────────────────────────────
        lblStatus.Text      = "DXF-Datei öffnen …";
        lblStatus.Location  = new Point(0, 722);
        lblStatus.Size      = new Size(1100, 28);
        lblStatus.Anchor    = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lblStatus.Font      = new Font("Courier New", 10F);
        lblStatus.ForeColor = Color.FromArgb(40, 40, 40);
        lblStatus.BackColor = Color.FromArgb(210, 210, 210);
        lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        lblStatus.Padding   = new Padding(6, 0, 0, 0);

        Controls.AddRange(new Control[] { canvas, pnlSide, lblStatus });

        ResumeLayout(false);
    }

    private DxfCanvas canvas    = null!;
    private Panel     pnlSide   = null!;
    private Button    btnOpen   = null!;
    private Button    btnZoomIn = null!;
    private Button    btnZoomOut= null!;
    private Button    btnFit    = null!;
    private Button    btnSnap      = null!;
    private Button    btnPunkte    = null!;
    private Button    btnDxfToggle = null!;
    private Button    btnExportDxf = null!;
    private Label     lblStatus    = null!;
}
