namespace Feldbuch;

partial class FormRechenparameter
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        grpFehlergrenzen  = new GroupBox();
        lblWinkel         = new Label();
        nudWinkel         = new NumericUpDown();
        lblWinkelUnit     = new Label();
        lblStrecke        = new Label();
        nudStrecke        = new NumericUpDown();
        lblStreckeUnit    = new Label();
        lblHoehe          = new Label();
        nudHoehe          = new NumericUpDown();
        lblHoeheUnit      = new Label();
        grpMassstab       = new GroupBox();
        chkFreierMassstab = new CheckBox();
        lblMassstabInfo   = new Label();
        grpBerechnungsModus  = new GroupBox();
        chkBerechnung3D      = new CheckBox();
        btnOK             = new Button();
        btnAbbrechen      = new Button();

        ((System.ComponentModel.ISupportInitialize)nudWinkel).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudStrecke).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudHoehe).BeginInit();
        SuspendLayout();

        // ── Fenster ───────────────────────────────────────────────────────────
        ClientSize    = new Size(420, 392);
        Text          = "Rechenparameter – Freie Stationierung";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox   = false; MinimizeBox = false;
        BackColor     = Color.FromArgb(245, 245, 245);

        var lblFont  = new Font("Segoe UI", 10F);
        var grpFont  = new Font("Segoe UI", 10F, FontStyle.Bold);

        // ── Gruppe: Fehlergrenzen ─────────────────────────────────────────────
        grpFehlergrenzen.Text     = "Fehlergrenzen";
        grpFehlergrenzen.Font     = grpFont;
        grpFehlergrenzen.Location = new Point(14, 12);
        grpFehlergrenzen.Size     = new Size(392, 170);

        // Winkel
        lblWinkel.Text     = "Winkelresiduen:";
        lblWinkel.Font     = lblFont;
        lblWinkel.Location = new Point(16, 34);
        lblWinkel.Size     = new Size(140, 24);
        lblWinkel.AutoSize = false;

        nudWinkel.Location     = new Point(162, 31);
        nudWinkel.Size         = new Size(90, 26);
        nudWinkel.Font         = lblFont;
        nudWinkel.Minimum      = 1m;
        nudWinkel.Maximum      = 500m;
        nudWinkel.DecimalPlaces= 1;
        nudWinkel.Increment    = 1m;

        lblWinkelUnit.Text     = "cc";
        lblWinkelUnit.Font     = lblFont;
        lblWinkelUnit.Location = new Point(258, 34);
        lblWinkelUnit.AutoSize = true;

        // Strecke
        lblStrecke.Text     = "Streckenresiduen:";
        lblStrecke.Font     = lblFont;
        lblStrecke.Location = new Point(16, 74);
        lblStrecke.Size     = new Size(140, 24);
        lblStrecke.AutoSize = false;

        nudStrecke.Location     = new Point(162, 71);
        nudStrecke.Size         = new Size(90, 26);
        nudStrecke.Font         = lblFont;
        nudStrecke.Minimum      = 1m;
        nudStrecke.Maximum      = 200m;
        nudStrecke.DecimalPlaces= 1;
        nudStrecke.Increment    = 1m;

        lblStreckeUnit.Text     = "mm";
        lblStreckeUnit.Font     = lblFont;
        lblStreckeUnit.Location = new Point(258, 74);
        lblStreckeUnit.AutoSize = true;

        // Höhe
        lblHoehe.Text     = "Höhenresiduen:";
        lblHoehe.Font     = lblFont;
        lblHoehe.Location = new Point(16, 114);
        lblHoehe.Size     = new Size(140, 24);
        lblHoehe.AutoSize = false;

        nudHoehe.Location     = new Point(162, 111);
        nudHoehe.Size         = new Size(90, 26);
        nudHoehe.Font         = lblFont;
        nudHoehe.Minimum      = 1m;
        nudHoehe.Maximum      = 200m;
        nudHoehe.DecimalPlaces= 1;
        nudHoehe.Increment    = 1m;

        lblHoeheUnit.Text     = "mm";
        lblHoeheUnit.Font     = lblFont;
        lblHoeheUnit.Location = new Point(258, 114);
        lblHoeheUnit.AutoSize = true;

        grpFehlergrenzen.Controls.AddRange(new Control[]
        {
            lblWinkel, nudWinkel, lblWinkelUnit,
            lblStrecke, nudStrecke, lblStreckeUnit,
            lblHoehe, nudHoehe, lblHoeheUnit
        });

        // ── Gruppe: Maßstab ───────────────────────────────────────────────────
        grpMassstab.Text     = "Maßstab";
        grpMassstab.Font     = grpFont;
        grpMassstab.Location = new Point(14, 192);
        grpMassstab.Size     = new Size(392, 76);

        chkFreierMassstab.Text     = "Freier Maßstab (Helmert-Maßstab als freie Variable)";
        chkFreierMassstab.Font     = lblFont;
        chkFreierMassstab.Location = new Point(14, 28);
        chkFreierMassstab.AutoSize = true;

        grpMassstab.Controls.Add(chkFreierMassstab);

        // ── Gruppe: Berechnungsmodus ──────────────────────────────────────────
        grpBerechnungsModus.Text     = "Berechnungsmodus";
        grpBerechnungsModus.Font     = grpFont;
        grpBerechnungsModus.Location = new Point(14, 278);
        grpBerechnungsModus.Size     = new Size(392, 76);

        chkBerechnung3D.Text     = "3D-Berechnung (mit Höhen)";
        chkBerechnung3D.Font     = lblFont;
        chkBerechnung3D.Location = new Point(14, 28);
        chkBerechnung3D.AutoSize = true;

        grpBerechnungsModus.Controls.Add(chkBerechnung3D);

        // ── Buttons ───────────────────────────────────────────────────────────
        btnOK.Text      = "OK";
        btnOK.Location  = new Point(200, 350);
        btnOK.Size      = new Size(96, 32);
        btnOK.Font      = lblFont;
        btnOK.BackColor = Color.FromArgb(60, 100, 160);
        btnOK.ForeColor = Color.White;
        btnOK.FlatStyle = FlatStyle.Flat;
        btnOK.Click    += btnOK_Click;

        btnAbbrechen.Text      = "Abbrechen";
        btnAbbrechen.Location  = new Point(308, 350);
        btnAbbrechen.Size      = new Size(100, 32);
        btnAbbrechen.Font      = lblFont;
        btnAbbrechen.Click    += btnAbbrechen_Click;

        Controls.AddRange(new Control[]
        {
            grpFehlergrenzen, grpMassstab, grpBerechnungsModus, btnOK, btnAbbrechen
        });

        ((System.ComponentModel.ISupportInitialize)nudWinkel).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudStrecke).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudHoehe).EndInit();
        ResumeLayout(false);
    }

    private GroupBox      grpFehlergrenzen  = null!;
    private Label         lblWinkel         = null!;
    private NumericUpDown nudWinkel         = null!;
    private Label         lblWinkelUnit     = null!;
    private Label         lblStrecke        = null!;
    private NumericUpDown nudStrecke        = null!;
    private Label         lblStreckeUnit    = null!;
    private Label         lblHoehe          = null!;
    private NumericUpDown nudHoehe          = null!;
    private Label         lblHoeheUnit      = null!;
    private GroupBox      grpMassstab          = null!;
    private CheckBox      chkFreierMassstab    = null!;
    private Label         lblMassstabInfo      = null!;
    private GroupBox      grpBerechnungsModus  = null!;
    private CheckBox      chkBerechnung3D      = null!;
    private Button        btnOK             = null!;
    private Button        btnAbbrechen      = null!;
}
