namespace Feldbuch;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        lblProjektInfo        = new Label();
        btnProjekt            = new Button();
        lblTrennlinie         = new Label();
        btnFreieStationierung = new Button();
        btnMessen             = new Button();
        btnPrismenkonstante   = new Button();
        btnDxfViewer          = new Button();
        btnProjektdaten       = new Button();
        SuspendLayout();

        // ── Fenster ───────────────────────────────────────────────────────────
        ClientSize    = new Size(600, 760);
        Text          = "Feldbuch";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox   = false;
        AutoScaleMode = AutoScaleMode.Font;

        // ── Projektinfo-Label ─────────────────────────────────────────────────
        lblProjektInfo.Text      = "Kein Projekt gewählt";
        lblProjektInfo.Location  = new Point(20, 18);
        lblProjektInfo.Size      = new Size(560, 22);
        lblProjektInfo.Font      = new Font("Segoe UI", 9.5F, FontStyle.Italic);
        lblProjektInfo.ForeColor = Color.FromArgb(80, 80, 80);

        // ── Button: Projekt ───────────────────────────────────────────────────
        btnProjekt.Text      = "Projekt";
        btnProjekt.Size      = new Size(300, 50);
        btnProjekt.Location  = new Point(150, 48);
        btnProjekt.Font      = new Font("Segoe UI", 12F, FontStyle.Bold);
        btnProjekt.BackColor = Color.FromArgb(60, 100, 160);
        btnProjekt.ForeColor = Color.White;
        btnProjekt.FlatStyle = FlatStyle.Flat;
        btnProjekt.FlatAppearance.BorderColor = Color.FromArgb(40, 80, 140);
        btnProjekt.Click    += btnProjekt_Click;

        // ── Trennlinie ────────────────────────────────────────────────────────
        lblTrennlinie.BorderStyle = BorderStyle.Fixed3D;
        lblTrennlinie.Location    = new Point(20, 112);
        lblTrennlinie.Size        = new Size(560, 2);

        // ── Button: Freie Stationierung ───────────────────────────────────────
        btnFreieStationierung.Text     = "Freie Stationierung";
        btnFreieStationierung.Size     = new Size(300, 60);
        btnFreieStationierung.Location = new Point(150, 130);
        btnFreieStationierung.Font     = new Font("Segoe UI", 12F);
        btnFreieStationierung.Click   += btnFreieStationierung_Click;

        // ── Button: Messen ────────────────────────────────────────────────────
        btnMessen.Text     = "Messen";
        btnMessen.Size     = new Size(300, 60);
        btnMessen.Location = new Point(150, 230);
        btnMessen.Font     = new Font("Segoe UI", 12F);
        btnMessen.Click   += btnMessen_Click;

        // ── Button: Prismenkonstante ──────────────────────────────────────────
        btnPrismenkonstante.Text     = "Prismenkonstante";
        btnPrismenkonstante.Size     = new Size(300, 60);
        btnPrismenkonstante.Location = new Point(150, 330);
        btnPrismenkonstante.Font     = new Font("Segoe UI", 12F);
        btnPrismenkonstante.Click   += btnPrismenkonstante_Click;

        // ── Button: DXF Viewer ────────────────────────────────────────────────
        btnDxfViewer.Text     = "DXF Viewer";
        btnDxfViewer.Size     = new Size(300, 60);
        btnDxfViewer.Location = new Point(150, 430);
        btnDxfViewer.Font     = new Font("Segoe UI", 12F);
        btnDxfViewer.Click   += btnDxfViewer_Click;

        // ── Button: Projektdaten ──────────────────────────────────────────────
        btnProjektdaten.Text     = "Projektdaten";
        btnProjektdaten.Size     = new Size(300, 60);
        btnProjektdaten.Location = new Point(150, 530);
        btnProjektdaten.Font     = new Font("Segoe UI", 12F);
        btnProjektdaten.Click   += btnProjektdaten_Click;

        Controls.Add(lblProjektInfo);
        Controls.Add(btnProjekt);
        Controls.Add(lblTrennlinie);
        Controls.Add(btnFreieStationierung);
        Controls.Add(btnMessen);
        Controls.Add(btnPrismenkonstante);
        Controls.Add(btnDxfViewer);
        Controls.Add(btnProjektdaten);
        ResumeLayout(false);
    }

    private Label  lblProjektInfo        = null!;
    private Button btnProjekt            = null!;
    private Label  lblTrennlinie         = null!;
    private Button btnFreieStationierung = null!;
    private Button btnMessen             = null!;
    private Button btnPrismenkonstante   = null!;
    private Button btnDxfViewer          = null!;
    private Button btnProjektdaten       = null!;
}
