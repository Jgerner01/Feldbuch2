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
        lblS0AutoMatch    = new Label();
        nudS0AutoMatch    = new NumericUpDown();
        lblS0AutoMatchUnit = new Label();
        grpMassstab       = new GroupBox();
        chkFreierMassstab = new CheckBox();
        lblMassstabInfo   = new Label();
        grpBerechnungsModus  = new GroupBox();
        chkBerechnung3D      = new CheckBox();
        grpSigma             = new GroupBox();
        cmbSigma             = new ComboBox();
        grpPolarMassstab     = new GroupBox();
        rbMassstabEins       = new RadioButton();
        rbMassstabBerechnet  = new RadioButton();
        rbMassstabVorgegeben = new RadioButton();
        txtMassstabWert      = new TextBox();
        btnMassstabUtm       = new Button();
        lblMassstabHinweis   = new Label();
        grpGewichtung        = new GroupBox();
        chkGewichtung        = new CheckBox();
        lblZentrZiel         = new Label();
        nudZentrZiel         = new NumericUpDown();
        lblZentrZielUnit     = new Label();
        lblZentrStand        = new Label();
        nudZentrStand        = new NumericUpDown();
        lblZentrStandUnit    = new Label();
        lblGewichtungHinweis = new Label();
        btnOK            = new Button();
        btnAbbrechen      = new Button();

        ((System.ComponentModel.ISupportInitialize)nudWinkel).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudStrecke).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudHoehe).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudS0AutoMatch).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudZentrZiel).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudZentrStand).BeginInit();
        SuspendLayout();

        // ── Fenster ───────────────────────────────────────────────────────────
        ClientSize    = new Size(836, 528);
        Text          = "Rechenparameter – Stationierung und Polarpunkte";
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
        grpFehlergrenzen.Size     = new Size(392, 210);

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

        // Auto-Match s0-Schwelle
        lblS0AutoMatch.Text     = "Auto-Match (s₀ ≤):";
        lblS0AutoMatch.Font     = lblFont;
        lblS0AutoMatch.Location = new Point(16, 154);
        lblS0AutoMatch.Size     = new Size(140, 24);
        lblS0AutoMatch.AutoSize = false;

        nudS0AutoMatch.Location     = new Point(162, 151);
        nudS0AutoMatch.Size         = new Size(90, 26);
        nudS0AutoMatch.Font         = lblFont;
        nudS0AutoMatch.Minimum      = 1m;
        nudS0AutoMatch.Maximum      = 200m;
        nudS0AutoMatch.DecimalPlaces= 1;
        nudS0AutoMatch.Increment    = 1m;

        lblS0AutoMatchUnit.Text     = "mm";
        lblS0AutoMatchUnit.Font     = lblFont;
        lblS0AutoMatchUnit.Location = new Point(258, 154);
        lblS0AutoMatchUnit.AutoSize = true;

        grpFehlergrenzen.Controls.AddRange(new Control[]
        {
            lblWinkel, nudWinkel, lblWinkelUnit,
            lblStrecke, nudStrecke, lblStreckeUnit,
            lblHoehe, nudHoehe, lblHoeheUnit,
            lblS0AutoMatch, nudS0AutoMatch, lblS0AutoMatchUnit
        });

        // ── Gruppe: Maßstab ───────────────────────────────────────────────────
        grpMassstab.Text     = "Maßstab (Stationierung)";
        grpMassstab.Font     = grpFont;
        grpMassstab.Location = new Point(14, 232);
        grpMassstab.Size     = new Size(392, 76);

        chkFreierMassstab.Text     = "Freier Maßstab (Helmert-Maßstab als freie Variable)";
        chkFreierMassstab.Font     = lblFont;
        chkFreierMassstab.Location = new Point(14, 28);
        chkFreierMassstab.AutoSize = true;

        grpMassstab.Controls.Add(chkFreierMassstab);

        // ── Gruppe: Berechnungsmodus ──────────────────────────────────────────
        grpBerechnungsModus.Text     = "Berechnungsmodus";
        grpBerechnungsModus.Font     = grpFont;
        grpBerechnungsModus.Location = new Point(14, 318);
        grpBerechnungsModus.Size     = new Size(392, 76);

        chkBerechnung3D.Text     = "3D-Berechnung (mit Höhen)";
        chkBerechnung3D.Font     = lblFont;
        chkBerechnung3D.Location = new Point(14, 28);
        chkBerechnung3D.AutoSize = true;

        grpBerechnungsModus.Controls.Add(chkBerechnung3D);

        // ── Gruppe: Standardabweichung (Ausgabe) ──────────────────────────────
        grpSigma.Text     = "Ausgabe der Standardabweichung";
        grpSigma.Font     = grpFont;
        grpSigma.Location = new Point(14, 404);
        grpSigma.Size     = new Size(392, 70);

        cmbSigma.Location      = new Point(14, 28);
        cmbSigma.Size          = new Size(250, 28);
        cmbSigma.Font          = lblFont;
        cmbSigma.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbSigma.Items.AddRange(new object[]
        {
            "einfach (1·s₀)", "doppelt (2·s₀)", "dreifach (3·s₀)"
        });

        grpSigma.Controls.Add(cmbSigma);

        // ── Rechte Spalte: Polarpunkte ────────────────────────────────────────
        // Gruppe: Maßstab (Polarpunkte)
        grpPolarMassstab.Text     = "Maßstab (Polarpunkte)";
        grpPolarMassstab.Font     = grpFont;
        grpPolarMassstab.Location = new Point(424, 12);
        grpPolarMassstab.Size     = new Size(400, 214);

        rbMassstabEins.Text     = "1  (ohne Maßstab) – Standard";
        rbMassstabEins.Font     = lblFont;
        rbMassstabEins.Location = new Point(14, 30);
        rbMassstabEins.AutoSize = true;

        rbMassstabBerechnet.Text     = "berechneter Maßstab aus der Stationierung";
        rbMassstabBerechnet.Font     = lblFont;
        rbMassstabBerechnet.Location = new Point(14, 60);
        rbMassstabBerechnet.AutoSize = true;

        rbMassstabVorgegeben.Text     = "vorgegebener Maßstab:";
        rbMassstabVorgegeben.Font     = lblFont;
        rbMassstabVorgegeben.Location = new Point(14, 90);
        rbMassstabVorgegeben.AutoSize = true;

        txtMassstabWert.Location = new Point(210, 88);
        txtMassstabWert.Size     = new Size(100, 26);
        txtMassstabWert.Font     = new Font("Consolas", 10F);
        txtMassstabWert.TextAlign = HorizontalAlignment.Right;

        btnMassstabUtm.Text     = "Aus UTM berechnen…";
        btnMassstabUtm.Font     = new Font("Segoe UI", 9F);
        btnMassstabUtm.Location = new Point(210, 122);
        btnMassstabUtm.Size     = new Size(168, 28);
        btnMassstabUtm.Enabled  = false;
        new ToolTip().SetToolTip(btnMassstabUtm, "Folgt in einer späteren Ausbaustufe (UTM-Abbildung). Der Wert kann bis dahin von Hand eingegeben werden.");

        lblMassstabHinweis.Text     = "Der Maßstab wirkt nur auf die gemessene Horizontalstrecke (nicht auf Exzentrizitäten und Höhe). Bei m ≠ 1 erscheint eine Information.";
        lblMassstabHinweis.Font     = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        lblMassstabHinweis.Location = new Point(14, 158);
        lblMassstabHinweis.Size     = new Size(372, 46);
        lblMassstabHinweis.ForeColor = Color.FromArgb(90, 90, 90);

        grpPolarMassstab.Controls.AddRange(new Control[]
        {
            rbMassstabEins, rbMassstabBerechnet, rbMassstabVorgegeben,
            txtMassstabWert, btnMassstabUtm, lblMassstabHinweis
        });

        // Gruppe: Gewichtung bei nahen Zielen
        grpGewichtung.Text     = "Gewichtung";
        grpGewichtung.Font     = grpFont;
        grpGewichtung.Location = new Point(424, 236);
        grpGewichtung.Size     = new Size(400, 224);

        chkGewichtung.Text     = "Gewichtung bei nahen Zielen";
        chkGewichtung.Font     = lblFont;
        chkGewichtung.Location = new Point(14, 30);
        chkGewichtung.AutoSize = true;
        chkGewichtung.CheckedChanged += (s, e) => AktualisiereGewichtungAktiv();

        lblZentrZiel.Text     = "Zielzentrierung:";
        lblZentrZiel.Font     = lblFont;
        lblZentrZiel.Location = new Point(34, 68);
        lblZentrZiel.Size     = new Size(150, 24);
        lblZentrZiel.AutoSize = false;

        nudZentrZiel.Location      = new Point(190, 65);
        nudZentrZiel.Size          = new Size(80, 26);
        nudZentrZiel.Font          = lblFont;
        nudZentrZiel.Minimum       = 0m;
        nudZentrZiel.Maximum       = 50m;
        nudZentrZiel.DecimalPlaces = 1;
        nudZentrZiel.Increment     = 0.5m;

        lblZentrZielUnit.Text     = "mm";
        lblZentrZielUnit.Font     = lblFont;
        lblZentrZielUnit.Location = new Point(276, 68);
        lblZentrZielUnit.AutoSize = true;

        lblZentrStand.Text     = "Standpunktzentrierung:";
        lblZentrStand.Font     = lblFont;
        lblZentrStand.Location = new Point(34, 104);
        lblZentrStand.Size     = new Size(150, 24);
        lblZentrStand.AutoSize = false;

        nudZentrStand.Location      = new Point(190, 101);
        nudZentrStand.Size          = new Size(80, 26);
        nudZentrStand.Font          = lblFont;
        nudZentrStand.Minimum       = 0m;
        nudZentrStand.Maximum       = 50m;
        nudZentrStand.DecimalPlaces = 1;
        nudZentrStand.Increment     = 0.5m;

        lblZentrStandUnit.Text     = "mm";
        lblZentrStandUnit.Font     = lblFont;
        lblZentrStandUnit.Location = new Point(276, 104);
        lblZentrStandUnit.AutoSize = true;

        lblGewichtungHinweis.Text     = "Nahe Ziele haben wegen kleiner Lotfehler eine schlechte Richtung und erhalten ein geringeres Gewicht " +
                                        "bei der Orientierung. Wirkt nur im Polarmodul (nicht in der Freien Stationierung). " +
                                        "Die Standpunktzentrierung ist nur bei bekanntem Standpunkt wirksam.";
        lblGewichtungHinweis.Font     = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        lblGewichtungHinweis.Location = new Point(14, 142);
        lblGewichtungHinweis.Size     = new Size(372, 72);
        lblGewichtungHinweis.ForeColor = Color.FromArgb(90, 90, 90);

        grpGewichtung.Controls.AddRange(new Control[]
        {
            chkGewichtung, lblZentrZiel, nudZentrZiel, lblZentrZielUnit,
            lblZentrStand, nudZentrStand, lblZentrStandUnit, lblGewichtungHinweis
        });

        // ── Buttons ───────────────────────────────────────────────────────────
        btnOK.Text      = "OK";
        btnOK.Location  = new Point(616, 486);
        btnOK.Size      = new Size(96, 32);
        btnOK.Font      = lblFont;
        btnOK.BackColor = Color.FromArgb(60, 100, 160);
        btnOK.ForeColor = Color.White;
        btnOK.FlatStyle = FlatStyle.Flat;
        btnOK.Click    += btnOK_Click;

        btnAbbrechen.Text      = "Abbrechen";
        btnAbbrechen.Location  = new Point(724, 486);
        btnAbbrechen.Size      = new Size(100, 32);
        btnAbbrechen.Font      = lblFont;
        btnAbbrechen.Click    += btnAbbrechen_Click;

        Controls.AddRange(new Control[]
        {
            grpFehlergrenzen, grpMassstab, grpBerechnungsModus, grpSigma,
            grpPolarMassstab, grpGewichtung, btnOK, btnAbbrechen
        });

        ((System.ComponentModel.ISupportInitialize)nudWinkel).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudStrecke).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudHoehe).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudS0AutoMatch).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudZentrZiel).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudZentrStand).EndInit();
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
    private Label         lblS0AutoMatch    = null!;
    private NumericUpDown nudS0AutoMatch    = null!;
    private Label         lblS0AutoMatchUnit = null!;
    private GroupBox      grpMassstab          = null!;
    private CheckBox      chkFreierMassstab    = null!;
    private Label         lblMassstabInfo      = null!;
    private GroupBox      grpBerechnungsModus  = null!;
    private CheckBox      chkBerechnung3D      = null!;
    private GroupBox      grpSigma             = null!;
    private ComboBox      cmbSigma             = null!;
    private GroupBox      grpPolarMassstab     = null!;
    private RadioButton   rbMassstabEins       = null!;
    private RadioButton   rbMassstabBerechnet  = null!;
    private RadioButton   rbMassstabVorgegeben = null!;
    private TextBox       txtMassstabWert      = null!;
    private Button        btnMassstabUtm       = null!;
    private Label         lblMassstabHinweis   = null!;
    private GroupBox      grpGewichtung        = null!;
    private CheckBox      chkGewichtung        = null!;
    private Label         lblZentrZiel         = null!;
    private NumericUpDown nudZentrZiel         = null!;
    private Label         lblZentrZielUnit     = null!;
    private Label         lblZentrStand        = null!;
    private NumericUpDown nudZentrStand        = null!;
    private Label         lblZentrStandUnit    = null!;
    private Label         lblGewichtungHinweis = null!;
    private Button        btnOK             = null!;
    private Button        btnAbbrechen      = null!;
}
