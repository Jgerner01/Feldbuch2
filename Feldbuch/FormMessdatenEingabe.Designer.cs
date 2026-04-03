namespace Feldbuch;

partial class FormMessdatenEingabe
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        grpKoordinaten  = new GroupBox();
        lblRVal         = new Label();
        lblHVal         = new Label();
        grpPunktdaten   = new GroupBox();
        lblPunktNr      = new Label();
        txtPunktNr      = new TextBox();
        lblHoeheKoord   = new Label();
        nudHoehe        = new NumericUpDown();
        lblHoeheUnit    = new Label();
        grpMessdaten    = new GroupBox();
        lblHz           = new Label();
        nudHz           = new NumericUpDown();
        lblHzUnit       = new Label();
        lblV            = new Label();
        nudV            = new NumericUpDown();
        lblVUnit        = new Label();
        lblStrecke      = new Label();
        nudStrecke      = new NumericUpDown();
        lblStreckeUnit  = new Label();
        lblZielhoehe    = new Label();
        nudZielhoehe    = new NumericUpDown();
        lblZielhoeheUnit= new Label();
        lblSimInfo      = new Label();
        btnSimulieren   = new Button();
        btnUebernehmen  = new Button();
        btnAbbrechen    = new Button();

        ((System.ComponentModel.ISupportInitialize)nudHoehe).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudHz).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudV).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudStrecke).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudZielhoehe).BeginInit();
        SuspendLayout();

        // ── Fenster ───────────────────────────────────────────────────────────
        ClientSize      = new Size(440, 460);
        Text            = "Anschlusspunkt – Messdaten";
        StartPosition   = FormStartPosition.CenterParent;
        AutoScaleMode   = AutoScaleMode.Font;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false; MinimizeBox = false;
        BackColor       = Color.FromArgb(245, 245, 245);

        var lblFont = new Font("Segoe UI", 10F);
        var grpFont = new Font("Segoe UI", 10F, FontStyle.Bold);
        var monoFont= new Font("Courier New", 10F);

        // ── Gruppe: Koordinaten (aus DXF, read-only) ──────────────────────────
        grpKoordinaten.Text     = "Koordinaten (aus DXF)";
        grpKoordinaten.Font     = grpFont;
        grpKoordinaten.Location = new Point(14, 10);
        grpKoordinaten.Size     = new Size(412, 80);

        lblRVal.Font     = monoFont;
        lblRVal.Location = new Point(14, 28);
        lblRVal.Size     = new Size(380, 22);
        lblRVal.Text     = "R (Rechtswert):   – m";

        lblHVal.Font     = monoFont;
        lblHVal.Location = new Point(14, 52);
        lblHVal.Size     = new Size(380, 22);
        lblHVal.Text     = "H (Hochwert):     – m";

        grpKoordinaten.Controls.AddRange(new Control[] { lblRVal, lblHVal });

        // ── Gruppe: Punktdaten ────────────────────────────────────────────────
        grpPunktdaten.Text     = "Punktdaten";
        grpPunktdaten.Font     = grpFont;
        grpPunktdaten.Location = new Point(14, 100);
        grpPunktdaten.Size     = new Size(412, 110);

        lblPunktNr.Text     = "Punktnummer:";
        lblPunktNr.Font     = lblFont;
        lblPunktNr.Location = new Point(14, 30);
        lblPunktNr.Size     = new Size(120, 24);
        lblPunktNr.AutoSize = false;

        txtPunktNr.Font     = lblFont;
        txtPunktNr.Location = new Point(140, 28);
        txtPunktNr.Size     = new Size(150, 26);

        lblHoeheKoord.Text     = "Höhe [m]:";
        lblHoeheKoord.Font     = lblFont;
        lblHoeheKoord.Location = new Point(14, 68);
        lblHoeheKoord.Size     = new Size(120, 24);
        lblHoeheKoord.AutoSize = false;

        nudHoehe.Location      = new Point(140, 66);
        nudHoehe.Size          = new Size(100, 26);
        nudHoehe.Font          = lblFont;
        nudHoehe.Minimum       = -999m;
        nudHoehe.Maximum       = 9999.999m;
        nudHoehe.DecimalPlaces = 3;
        nudHoehe.Increment     = 0.001m;
        nudHoehe.Value         = 0m;

        lblHoeheUnit.Text     = "m";
        lblHoeheUnit.Font     = lblFont;
        lblHoeheUnit.Location = new Point(248, 68);
        lblHoeheUnit.AutoSize = true;

        grpPunktdaten.Controls.AddRange(new Control[]
        {
            lblPunktNr, txtPunktNr,
            lblHoeheKoord, nudHoehe, lblHoeheUnit
        });

        // ── Gruppe: Messdaten ─────────────────────────────────────────────────
        grpMessdaten.Text     = "Messdaten  (Tachymeter / simuliert)";
        grpMessdaten.Font     = grpFont;
        grpMessdaten.Location = new Point(14, 220);
        grpMessdaten.Size     = new Size(412, 178);

        int y = 30, step = 36;

        // HZ
        lblHz.Text     = "HZ [gon]:";
        lblHz.Font     = lblFont;
        lblHz.Location = new Point(14, y);
        lblHz.Size     = new Size(120, 24);
        lblHz.AutoSize = false;

        nudHz.Location      = new Point(140, y - 2);
        nudHz.Size          = new Size(110, 26);
        nudHz.Font          = lblFont;
        nudHz.Minimum       = 0m;
        nudHz.Maximum       = 399.9999m;
        nudHz.DecimalPlaces = 4;
        nudHz.Increment     = 0.0001m;

        lblHzUnit.Text     = "gon";
        lblHzUnit.Font     = lblFont;
        lblHzUnit.Location = new Point(258, y);
        lblHzUnit.AutoSize = true;
        y += step;

        // V
        lblV.Text     = "V [gon]:";
        lblV.Font     = lblFont;
        lblV.Location = new Point(14, y);
        lblV.Size     = new Size(120, 24);
        lblV.AutoSize = false;

        nudV.Location      = new Point(140, y - 2);
        nudV.Size          = new Size(110, 26);
        nudV.Font          = lblFont;
        nudV.Minimum       = 0m;
        nudV.Maximum       = 199.9999m;
        nudV.DecimalPlaces = 4;
        nudV.Increment     = 0.0001m;
        nudV.Value         = 100.0000m;

        lblVUnit.Text     = "gon";
        lblVUnit.Font     = lblFont;
        lblVUnit.Location = new Point(258, y);
        lblVUnit.AutoSize = true;
        y += step;

        // Strecke
        lblStrecke.Text     = "Strecke [m]:";
        lblStrecke.Font     = lblFont;
        lblStrecke.Location = new Point(14, y);
        lblStrecke.Size     = new Size(120, 24);
        lblStrecke.AutoSize = false;

        nudStrecke.Location      = new Point(140, y - 2);
        nudStrecke.Size          = new Size(110, 26);
        nudStrecke.Font          = lblFont;
        nudStrecke.Minimum       = 0m;
        nudStrecke.Maximum       = 9999.999m;
        nudStrecke.DecimalPlaces = 3;
        nudStrecke.Increment     = 0.001m;

        lblStreckeUnit.Text     = "m";
        lblStreckeUnit.Font     = lblFont;
        lblStreckeUnit.Location = new Point(258, y);
        lblStreckeUnit.AutoSize = true;
        y += step;

        // Zielhöhe
        lblZielhoehe.Text     = "Zielhöhe [m]:";
        lblZielhoehe.Font     = lblFont;
        lblZielhoehe.Location = new Point(14, y);
        lblZielhoehe.Size     = new Size(120, 24);
        lblZielhoehe.AutoSize = false;

        nudZielhoehe.Location      = new Point(140, y - 2);
        nudZielhoehe.Size          = new Size(110, 26);
        nudZielhoehe.Font          = lblFont;
        nudZielhoehe.Minimum       = 0m;
        nudZielhoehe.Maximum       = 9.999m;
        nudZielhoehe.DecimalPlaces = 3;
        nudZielhoehe.Increment     = 0.001m;
        nudZielhoehe.Value         = 1.700m;

        lblZielhoeheUnit.Text     = "m";
        lblZielhoeheUnit.Font     = lblFont;
        lblZielhoeheUnit.Location = new Point(258, y);
        lblZielhoeheUnit.AutoSize = true;

        grpMessdaten.Controls.AddRange(new Control[]
        {
            lblHz, nudHz, lblHzUnit,
            lblV, nudV, lblVUnit,
            lblStrecke, nudStrecke, lblStreckeUnit,
            lblZielhoehe, nudZielhoehe, lblZielhoeheUnit
        });

        // ── Simulations-Hinweis ───────────────────────────────────────────────
        lblSimInfo.Text      = "⚠ Platzhalter – später: Leica TCR307";
        lblSimInfo.Font      = new Font("Segoe UI", 8.5F, FontStyle.Italic);
        lblSimInfo.ForeColor = Color.FromArgb(120, 100, 30);
        lblSimInfo.Location  = new Point(14, 406);
        lblSimInfo.AutoSize  = true;

        // ── Buttons ───────────────────────────────────────────────────────────
        btnSimulieren.Text      = "Simulieren";
        btnSimulieren.Location  = new Point(14, 418);
        btnSimulieren.Size      = new Size(110, 32);
        btnSimulieren.Font      = lblFont;
        btnSimulieren.BackColor = Color.FromArgb(140, 100, 20);
        btnSimulieren.ForeColor = Color.White;
        btnSimulieren.FlatStyle = FlatStyle.Flat;
        btnSimulieren.Click    += btnSimulieren_Click;

        btnUebernehmen.Text      = "Übernehmen";
        btnUebernehmen.Location  = new Point(210, 418);
        btnUebernehmen.Size      = new Size(110, 32);
        btnUebernehmen.Font      = lblFont;
        btnUebernehmen.BackColor = Color.FromArgb(60, 130, 60);
        btnUebernehmen.ForeColor = Color.White;
        btnUebernehmen.FlatStyle = FlatStyle.Flat;
        btnUebernehmen.Click    += btnUebernehmen_Click;

        btnAbbrechen.Text     = "Abbrechen";
        btnAbbrechen.Location = new Point(328, 418);
        btnAbbrechen.Size     = new Size(100, 32);
        btnAbbrechen.Font     = lblFont;
        btnAbbrechen.Click   += btnAbbrechen_Click;

        Controls.AddRange(new Control[]
        {
            grpKoordinaten, grpPunktdaten, grpMessdaten,
            lblSimInfo, btnSimulieren, btnUebernehmen, btnAbbrechen
        });

        ((System.ComponentModel.ISupportInitialize)nudHoehe).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudHz).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudV).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudStrecke).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudZielhoehe).EndInit();
        ResumeLayout(false);
    }

    private GroupBox      grpKoordinaten   = null!;
    private Label         lblRVal          = null!;
    private Label         lblHVal          = null!;
    private GroupBox      grpPunktdaten    = null!;
    private Label         lblPunktNr       = null!;
    private TextBox       txtPunktNr       = null!;
    private Label         lblHoeheKoord    = null!;
    private NumericUpDown nudHoehe         = null!;
    private Label         lblHoeheUnit     = null!;
    private GroupBox      grpMessdaten     = null!;
    private Label         lblHz            = null!;
    private NumericUpDown nudHz            = null!;
    private Label         lblHzUnit        = null!;
    private Label         lblV             = null!;
    private NumericUpDown nudV             = null!;
    private Label         lblVUnit         = null!;
    private Label         lblStrecke       = null!;
    private NumericUpDown nudStrecke       = null!;
    private Label         lblStreckeUnit   = null!;
    private Label         lblZielhoehe     = null!;
    private NumericUpDown nudZielhoehe     = null!;
    private Label         lblZielhoeheUnit = null!;
    private Label         lblSimInfo       = null!;
    private Button        btnSimulieren    = null!;
    private Button        btnUebernehmen   = null!;
    private Button        btnAbbrechen     = null!;
}
