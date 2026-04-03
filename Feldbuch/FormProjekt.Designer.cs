namespace Feldbuch;

partial class FormProjekt
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        grpAktuell    = new GroupBox();
        lblAktName    = new Label();
        lblAktVerz    = new Label();
        grpAuswahl    = new GroupBox();
        lblName       = new Label();
        txtName       = new TextBox();
        lblVerz       = new Label();
        txtVerzeichnis= new TextBox();
        btnBrowse     = new Button();
        btnNeuAnlegen = new Button();
        btnOK         = new Button();
        btnAbbrechen  = new Button();

        SuspendLayout();

        // ── Fenster ───────────────────────────────────────────────────────────
        ClientSize      = new Size(520, 310);
        Text            = "Projekt";
        StartPosition   = FormStartPosition.CenterParent;
        AutoScaleMode   = AutoScaleMode.Font;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false; MinimizeBox = false;
        BackColor       = Color.FromArgb(245, 245, 245);

        var lblFont  = new Font("Segoe UI", 10F);
        var grpFont  = new Font("Segoe UI", 10F, FontStyle.Bold);
        var monoFont = new Font("Courier New", 9.5F);

        // ── Gruppe: Aktuelles Projekt ─────────────────────────────────────────
        grpAktuell.Text     = "Aktuelles Projekt";
        grpAktuell.Font     = grpFont;
        grpAktuell.Location = new Point(14, 10);
        grpAktuell.Size     = new Size(492, 78);

        lblAktName.Font     = monoFont;
        lblAktName.Location = new Point(12, 26);
        lblAktName.Size     = new Size(468, 20);
        lblAktName.Text     = "Name:         (kein Projekt gewählt)";

        lblAktVerz.Font     = monoFont;
        lblAktVerz.Location = new Point(12, 50);
        lblAktVerz.Size     = new Size(468, 20);
        lblAktVerz.Text     = "Verzeichnis:  –";

        grpAktuell.Controls.AddRange(new Control[] { lblAktName, lblAktVerz });

        // ── Gruppe: Projekt auswählen / anlegen ───────────────────────────────
        grpAuswahl.Text     = "Projekt auswählen oder neu anlegen";
        grpAuswahl.Font     = grpFont;
        grpAuswahl.Location = new Point(14, 98);
        grpAuswahl.Size     = new Size(492, 156);

        // Projektname
        lblName.Text     = "Projektname:";
        lblName.Font     = lblFont;
        lblName.Location = new Point(12, 32);
        lblName.Size     = new Size(110, 24);
        lblName.AutoSize = false;

        txtName.Font     = lblFont;
        txtName.Location = new Point(128, 30);
        txtName.Size     = new Size(352, 26);

        // Verzeichnis
        lblVerz.Text     = "Verzeichnis:";
        lblVerz.Font     = lblFont;
        lblVerz.Location = new Point(12, 72);
        lblVerz.Size     = new Size(110, 24);
        lblVerz.AutoSize = false;

        txtVerzeichnis.Font     = lblFont;
        txtVerzeichnis.Location = new Point(128, 70);
        txtVerzeichnis.Size     = new Size(316, 26);

        btnBrowse.Text      = "…";
        btnBrowse.Font      = new Font("Segoe UI", 10F);
        btnBrowse.Location  = new Point(450, 70);
        btnBrowse.Size      = new Size(30, 26);
        btnBrowse.FlatStyle = FlatStyle.Flat;
        btnBrowse.Click    += btnBrowse_Click;

        // "Neues Projekt anlegen"
        btnNeuAnlegen.Text      = "Neues Projekt anlegen";
        btnNeuAnlegen.Font      = lblFont;
        btnNeuAnlegen.Location  = new Point(12, 112);
        btnNeuAnlegen.Size      = new Size(200, 32);
        btnNeuAnlegen.FlatStyle = FlatStyle.Flat;
        btnNeuAnlegen.BackColor = Color.FromArgb(60, 100, 160);
        btnNeuAnlegen.ForeColor = Color.White;
        btnNeuAnlegen.Click    += btnNeuAnlegen_Click;

        grpAuswahl.Controls.AddRange(new Control[]
        {
            lblName, txtName,
            lblVerz, txtVerzeichnis, btnBrowse,
            btnNeuAnlegen
        });

        // ── Buttons ───────────────────────────────────────────────────────────
        btnOK.Text      = "OK";
        btnOK.Location  = new Point(290, 265);
        btnOK.Size      = new Size(100, 32);
        btnOK.Font      = lblFont;
        btnOK.BackColor = Color.FromArgb(60, 130, 60);
        btnOK.ForeColor = Color.White;
        btnOK.FlatStyle = FlatStyle.Flat;
        btnOK.Click    += btnOK_Click;

        btnAbbrechen.Text     = "Abbrechen";
        btnAbbrechen.Location = new Point(404, 265);
        btnAbbrechen.Size     = new Size(100, 32);
        btnAbbrechen.Font     = lblFont;
        btnAbbrechen.Click   += btnAbbrechen_Click;

        Controls.AddRange(new Control[]
        {
            grpAktuell, grpAuswahl, btnOK, btnAbbrechen
        });

        ResumeLayout(false);
    }

    private GroupBox grpAktuell    = null!;
    private Label    lblAktName    = null!;
    private Label    lblAktVerz    = null!;
    private GroupBox grpAuswahl    = null!;
    private Label    lblName       = null!;
    private TextBox  txtName       = null!;
    private Label    lblVerz       = null!;
    private TextBox  txtVerzeichnis= null!;
    private Button   btnBrowse     = null!;
    private Button   btnNeuAnlegen = null!;
    private Button   btnOK         = null!;
    private Button   btnAbbrechen  = null!;
}
