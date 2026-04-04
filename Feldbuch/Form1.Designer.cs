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
        btnProjektdaten          = new Button();
        btnTachymeterKommunikation = new Button();
        btnInfo                    = new Button();
        grpOptionen                = new GroupBox();
        chkProtokoll               = new CheckBox();
        chkAutoBackup              = new CheckBox();
        chkKoordTooltip            = new CheckBox();
        chkTon                     = new CheckBox();
        chkErwProto                = new CheckBox();
        SuspendLayout();

        // ── Fenster ───────────────────────────────────────────────────────────
        ClientSize    = new Size(600, 830);
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

        // ── Button: Tachymeter Kommunikation ──────────────────────────────────
        btnTachymeterKommunikation.Text      = "Tachymeter Kommunikation";
        btnTachymeterKommunikation.Size      = new Size(300, 60);
        btnTachymeterKommunikation.Location  = new Point(150, 630);
        btnTachymeterKommunikation.Font      = new Font("Segoe UI", 12F);
        btnTachymeterKommunikation.BackColor = Color.FromArgb(40, 100, 70);
        btnTachymeterKommunikation.ForeColor = Color.White;
        btnTachymeterKommunikation.FlatStyle = FlatStyle.Flat;
        btnTachymeterKommunikation.FlatAppearance.BorderColor = Color.FromArgb(20, 80, 50);
        btnTachymeterKommunikation.Click    += btnTachymeterKommunikation_Click;

        // ── GroupBox: Optionen ────────────────────────────────────────────────────
        grpOptionen.Text     = "Optionen";
        grpOptionen.Location = new Point(20, 694);
        grpOptionen.Size     = new Size(560, 80);
        grpOptionen.Font     = new Font("Segoe UI", 9F);

        // Linke Spalte
        chkProtokoll.Text     = "Protokoll";
        chkProtokoll.Location = new Point(10, 17);
        chkProtokoll.AutoSize = true;
        chkProtokoll.CheckedChanged += chkProtokoll_CheckedChanged;

        chkAutoBackup.Text     = "Autom. Backup";
        chkAutoBackup.Location = new Point(10, 37);
        chkAutoBackup.AutoSize = true;
        chkAutoBackup.CheckedChanged += chkOption_CheckedChanged;

        chkKoordTooltip.Text     = "Koordinaten-Tooltip";
        chkKoordTooltip.Location = new Point(10, 57);
        chkKoordTooltip.AutoSize = true;
        chkKoordTooltip.CheckedChanged += chkOption_CheckedChanged;

        // Rechte Spalte
        chkTon.Text     = "Ton bei Berechnung";
        chkTon.Location = new Point(290, 17);
        chkTon.AutoSize = true;
        chkTon.CheckedChanged += chkOption_CheckedChanged;

        chkErwProto.Text     = "Erw. Protokollierung";
        chkErwProto.Location = new Point(290, 37);
        chkErwProto.AutoSize = true;
        chkErwProto.CheckedChanged += chkOption_CheckedChanged;

        grpOptionen.Controls.Add(chkProtokoll);
        grpOptionen.Controls.Add(chkAutoBackup);
        grpOptionen.Controls.Add(chkKoordTooltip);
        grpOptionen.Controls.Add(chkTon);
        grpOptionen.Controls.Add(chkErwProto);

        Controls.Add(lblProjektInfo);
        Controls.Add(btnProjekt);
        Controls.Add(lblTrennlinie);
        Controls.Add(btnFreieStationierung);
        Controls.Add(btnMessen);
        Controls.Add(btnPrismenkonstante);
        Controls.Add(btnDxfViewer);
        Controls.Add(btnProjektdaten);
        Controls.Add(btnTachymeterKommunikation);
        Controls.Add(grpOptionen);

        // ── Button: Info / Hilfe (links unten) ────────────────────────────────
        btnInfo.Text      = "?";
        btnInfo.Size      = new Size(36, 36);
        btnInfo.Location  = new Point(12, 782);
        btnInfo.Font      = new Font("Segoe UI", 12F, FontStyle.Bold);
        btnInfo.FlatStyle = FlatStyle.Flat;
        btnInfo.ForeColor = Color.FromArgb(80, 80, 80);
        btnInfo.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
        btnInfo.Click    += btnInfo_Click;
        Controls.Add(btnInfo);

        ResumeLayout(false);
    }

    private Label  lblProjektInfo        = null!;
    private Button btnProjekt            = null!;
    private Label  lblTrennlinie         = null!;
    private Button btnFreieStationierung = null!;
    private Button btnMessen             = null!;
    private Button btnPrismenkonstante   = null!;
    private Button btnDxfViewer               = null!;
    private Button btnProjektdaten            = null!;
    private Button btnTachymeterKommunikation = null!;
    private Button    btnInfo                    = null!;
    private GroupBox  grpOptionen               = null!;
    private CheckBox  chkProtokoll              = null!;
    private CheckBox  chkAutoBackup             = null!;
    private CheckBox  chkKoordTooltip           = null!;
    private CheckBox  chkTon                    = null!;
    private CheckBox  chkErwProto               = null!;
}
