namespace Feldbuch;

partial class FormInfo
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        picIcon      = new PictureBox();
        lblName      = new Label();
        lblVersion   = new Label();
        lblAutor     = new Label();
        lblCopyright = new Label();
        lblTrenn1    = new Label();
        lblHilfe     = new Label();
        lblHilfeText = new Label();
        lblTrenn2    = new Label();
        btnOK        = new Button();

        ((System.ComponentModel.ISupportInitialize)picIcon).BeginInit();
        SuspendLayout();

        // ── Fenster ───────────────────────────────────────────────────────────
        ClientSize      = new Size(460, 490);
        Text            = "Info";
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        AutoScaleMode   = AutoScaleMode.None;
        Font            = new Font("Segoe UI", 9F);

        const int lx = 24;          // linker Rand
        const int cw = 412;         // Inhaltsbreite (460 - 2×24)
        int y = 24;

        // ── Programm-Icon ─────────────────────────────────────────────────────
        picIcon.Location = new Point(lx, y);
        picIcon.Size     = new Size(52, 52);
        picIcon.SizeMode = PictureBoxSizeMode.Zoom;
        picIcon.Image    = SystemIcons.Application.ToBitmap();

        // ── Programmname ──────────────────────────────────────────────────────
        lblName.Text      = "Feldbuch";
        lblName.Location  = new Point(90, y);
        lblName.Size      = new Size(346, 32);
        lblName.Font      = new Font("Segoe UI", 18F, FontStyle.Bold);
        lblName.ForeColor = Color.FromArgb(30, 60, 110);

        // ── Version ───────────────────────────────────────────────────────────
        lblVersion.Text      = "Version 1.1.0";
        lblVersion.Location  = new Point(92, y + 34);
        lblVersion.Size      = new Size(344, 22);
        lblVersion.Font      = new Font("Segoe UI", 10F);
        lblVersion.ForeColor = Color.FromArgb(80, 80, 80);

        y += 62;   // unter Icon-Block (max von Icon-Höhe 52 und Text-Block 56)

        // ── Trennlinie 1 ──────────────────────────────────────────────────────
        y += 14;
        lblTrenn1.BorderStyle = BorderStyle.Fixed3D;
        lblTrenn1.Location    = new Point(lx, y);
        lblTrenn1.Size        = new Size(cw, 2);
        y += 2;

        // ── Beschreibung ──────────────────────────────────────────────────────
        y += 14;
        var lblBeschreibung = new Label
        {
            Text      = "Geodätisches Feldbuch mit Freier Stationierung,\n" +
                        "Tachymeteranbindung und DXF-Viewer.",
            Location  = new Point(lx, y),
            Size      = new Size(cw, 48),
            Font      = new Font("Segoe UI", 9.5F),
            ForeColor = Color.FromArgb(50, 50, 50)
        };
        y += 48;

        // ── Autor / Copyright ─────────────────────────────────────────────────
        y += 14;
        lblAutor.Text      = "Autor:   Johann Gerner";
        lblAutor.Location  = new Point(lx, y);
        lblAutor.Size      = new Size(cw, 22);
        lblAutor.Font      = new Font("Segoe UI", 9.5F);
        y += 22;

        y += 4;
        lblCopyright.Text      = "© 2026 Johann Gerner";
        lblCopyright.Location  = new Point(lx, y);
        lblCopyright.Size      = new Size(cw, 22);
        lblCopyright.Font      = new Font("Segoe UI", 9F);
        lblCopyright.ForeColor = Color.FromArgb(100, 100, 100);
        y += 22;

        // ── Trennlinie 2 ──────────────────────────────────────────────────────
        y += 18;
        lblTrenn2.BorderStyle = BorderStyle.Fixed3D;
        lblTrenn2.Location    = new Point(lx, y);
        lblTrenn2.Size        = new Size(cw, 2);
        y += 2;

        // ── Hilfe-Abschnitt ───────────────────────────────────────────────────
        y += 14;
        lblHilfe.Text      = "Hilfe";
        lblHilfe.Location  = new Point(lx, y);
        lblHilfe.Size      = new Size(cw, 22);
        lblHilfe.Font      = new Font("Segoe UI", 10F, FontStyle.Bold);
        y += 22;

        y += 8;
        lblHilfeText.Text =
            "• Bluetooth-Gerät zuerst über Windows-Einstellungen koppeln,\n" +
            "  dann unter Tachymeter-Kommunikation den COM-Port wählen.\n" +
            "• DXF-Datei über den DXF-Viewer öffnen.\n" +
            "• Freie Stationierung benötigt mind. 2 bekannte Punkte im DXF.";
        lblHilfeText.Location  = new Point(lx, y);
        lblHilfeText.Size      = new Size(cw, 88);
        lblHilfeText.Font      = new Font("Segoe UI", 9F);
        lblHilfeText.ForeColor = Color.FromArgb(50, 50, 50);
        y += 88;

        // ── OK-Button ─────────────────────────────────────────────────────────
        y += 22;
        btnOK.Text     = "OK";
        btnOK.Location = new Point(460 - 24 - 88, y);
        btnOK.Size     = new Size(88, 32);
        btnOK.Font     = new Font("Segoe UI", 10F);
        btnOK.Click   += btnOK_Click;

        Controls.AddRange(
        [
            picIcon, lblName, lblVersion,
            lblTrenn1, lblBeschreibung,
            lblAutor, lblCopyright,
            lblTrenn2, lblHilfe, lblHilfeText,
            btnOK
        ]);

        AcceptButton = btnOK;
        ((System.ComponentModel.ISupportInitialize)picIcon).EndInit();
        ResumeLayout(false);
    }

    private PictureBox picIcon;
    private Label      lblName;
    private Label      lblVersion;
    private Label      lblAutor;
    private Label      lblCopyright;
    private Label      lblTrenn1;
    private Label      lblHilfe;
    private Label      lblHilfeText;
    private Label      lblTrenn2;
    private Button     btnOK;
}
