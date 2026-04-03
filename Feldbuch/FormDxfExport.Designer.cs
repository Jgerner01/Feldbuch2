namespace Feldbuch;

partial class FormDxfExport
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        grpMassstab    = new GroupBox();
        lblMassstab    = new Label();
        lblMassstabPre = new Label();
        nudMassstab    = new NumericUpDown();
        lblSymbol      = new Label();
        nudSymbolMm    = new NumericUpDown();
        lblSymbolUnit  = new Label();
        lblText        = new Label();
        nudTextMm      = new NumericUpDown();
        lblTextUnit    = new Label();
        lblVorschau    = new Label();
        grpLayer       = new GroupBox();
        lblLayerInfo   = new Label();
        btnExportieren = new Button();
        btnAbbrechen   = new Button();

        ((System.ComponentModel.ISupportInitialize)nudMassstab).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudSymbolMm).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudTextMm).BeginInit();
        SuspendLayout();

        // ── Fenster ───────────────────────────────────────────────────────────
        ClientSize      = new Size(460, 400);
        Text            = "Feldbuchpunkte – DXF-Export";
        StartPosition   = FormStartPosition.CenterParent;
        AutoScaleMode   = AutoScaleMode.Font;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false; MinimizeBox = false;
        BackColor       = Color.FromArgb(245, 245, 245);

        var lblFont  = new Font("Segoe UI", 10F);
        var grpFont  = new Font("Segoe UI", 10F, FontStyle.Bold);
        var monoFont = new Font("Courier New", 8.5F);

        // ── Gruppe: Maßstab & Symbolgrößen ───────────────────────────────────
        grpMassstab.Text     = "Maßstab und Symbolgrößen";
        grpMassstab.Font     = grpFont;
        grpMassstab.Location = new Point(14, 10);
        grpMassstab.Size     = new Size(432, 172);

        // Maßstab
        lblMassstabPre.Text     = "Maßstab  1 :";
        lblMassstabPre.Font     = lblFont;
        lblMassstabPre.Location = new Point(12, 32);
        lblMassstabPre.Size     = new Size(110, 24);
        lblMassstabPre.AutoSize = false;

        nudMassstab.Location      = new Point(128, 30);
        nudMassstab.Size          = new Size(120, 26);
        nudMassstab.Font          = lblFont;
        nudMassstab.Minimum       = 100m;
        nudMassstab.Maximum       = 50000m;
        nudMassstab.DecimalPlaces = 0;
        nudMassstab.Increment     = 100m;
        nudMassstab.Value         = 1000m;
        nudMassstab.ValueChanged += (s, e) => AktualisiereVorschau();

        // Symbolgröße
        lblSymbol.Text     = "Symbolgröße:";
        lblSymbol.Font     = lblFont;
        lblSymbol.Location = new Point(12, 72);
        lblSymbol.Size     = new Size(110, 24);
        lblSymbol.AutoSize = false;

        nudSymbolMm.Location      = new Point(128, 70);
        nudSymbolMm.Size          = new Size(90, 26);
        nudSymbolMm.Font          = lblFont;
        nudSymbolMm.Minimum       = 0.5m;
        nudSymbolMm.Maximum       = 10.0m;
        nudSymbolMm.DecimalPlaces = 1;
        nudSymbolMm.Increment     = 0.1m;
        nudSymbolMm.Value         = 1.5m;
        nudSymbolMm.ValueChanged += (s, e) => AktualisiereVorschau();

        lblSymbolUnit.Text     = "mm  (Kreisdurchmesser)";
        lblSymbolUnit.Font     = lblFont;
        lblSymbolUnit.Location = new Point(226, 72);
        lblSymbolUnit.AutoSize = true;

        // Schriftgröße
        lblText.Text     = "Schriftgröße:";
        lblText.Font     = lblFont;
        lblText.Location = new Point(12, 112);
        lblText.Size     = new Size(110, 24);
        lblText.AutoSize = false;

        nudTextMm.Location      = new Point(128, 110);
        nudTextMm.Size          = new Size(90, 26);
        nudTextMm.Font          = lblFont;
        nudTextMm.Minimum       = 0.5m;
        nudTextMm.Maximum       = 10.0m;
        nudTextMm.DecimalPlaces = 1;
        nudTextMm.Increment     = 0.1m;
        nudTextMm.Value         = 2.0m;
        nudTextMm.ValueChanged += (s, e) => AktualisiereVorschau();

        lblTextUnit.Text     = "mm  (Punktnr. und Höhe)";
        lblTextUnit.Font     = lblFont;
        lblTextUnit.Location = new Point(226, 112);
        lblTextUnit.AutoSize = true;

        // Vorschau der Weltmaße
        lblMassstab.Font      = new Font("Courier New", 9F, FontStyle.Italic);
        lblMassstab.ForeColor = Color.FromArgb(80, 80, 80);
        lblMassstab.Location  = new Point(12, 146);
        lblMassstab.Size      = new Size(410, 18);
        lblMassstab.AutoSize  = false;

        lblVorschau = lblMassstab;   // Alias

        grpMassstab.Controls.AddRange(new Control[]
        {
            lblMassstabPre, nudMassstab,
            lblSymbol, nudSymbolMm, lblSymbolUnit,
            lblText,   nudTextMm,  lblTextUnit,
            lblVorschau
        });

        // ── Gruppe: Ebenen ────────────────────────────────────────────────────
        grpLayer.Text     = "Ebenen (Layer)";
        grpLayer.Font     = grpFont;
        grpLayer.Location = new Point(14, 192);
        grpLayer.Size     = new Size(432, 148);

        lblLayerInfo.Font      = monoFont;
        lblLayerInfo.ForeColor = Color.FromArgb(40, 40, 80);
        lblLayerInfo.Location  = new Point(10, 24);
        lblLayerInfo.Size      = new Size(412, 116);
        lblLayerInfo.Text      =
            "Feldbuch_Standpunkt_Symbol   Kreis + Kreuz     (Rot)\r\n" +
            "Feldbuch_Standpunkt_Nummer   Punktnummer-Text  (Rot)\r\n" +
            "Feldbuch_Standpunkt_Hoehe    Höhen-Text        (Rot)\r\n" +
            "Feldbuch_Neupunkt_Symbol     Kreis             (Grün)\r\n" +
            "Feldbuch_Neupunkt_Nummer     Punktnummer-Text  (Grün)\r\n" +
            "Feldbuch_Neupunkt_Hoehe      Höhen-Text        (Grün)";

        grpLayer.Controls.Add(lblLayerInfo);

        // ── Buttons ───────────────────────────────────────────────────────────
        btnExportieren.Text      = "Exportieren";
        btnExportieren.Location  = new Point(240, 354);
        btnExportieren.Size      = new Size(120, 34);
        btnExportieren.Font      = lblFont;
        btnExportieren.BackColor = Color.FromArgb(60, 130, 60);
        btnExportieren.ForeColor = Color.White;
        btnExportieren.FlatStyle = FlatStyle.Flat;
        btnExportieren.Click    += btnExportieren_Click;

        btnAbbrechen.Text     = "Abbrechen";
        btnAbbrechen.Location = new Point(372, 354);
        btnAbbrechen.Size     = new Size(100, 34);
        btnAbbrechen.Font     = lblFont;
        btnAbbrechen.Click   += btnAbbrechen_Click;

        Controls.AddRange(new Control[]
        {
            grpMassstab, grpLayer, btnExportieren, btnAbbrechen
        });

        ((System.ComponentModel.ISupportInitialize)nudMassstab).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudSymbolMm).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudTextMm).EndInit();
        ResumeLayout(false);

        AktualisiereVorschau();
    }

    void AktualisiereVorschau()
    {
        double m  = (double)nudMassstab.Value;
        double sr = (double)nudSymbolMm.Value * m / 2000.0;
        double th = (double)nudTextMm.Value   * m / 1000.0;
        lblVorschau.Text =
            $"→ Symbolradius: {sr:F3} m   Texthöhe: {th:F3} m   (im Koordinatensystem)";
    }

    private GroupBox      grpMassstab    = null!;
    private Label         lblMassstabPre = null!;
    private NumericUpDown nudMassstab    = null!;
    private Label         lblSymbol      = null!;
    private NumericUpDown nudSymbolMm    = null!;
    private Label         lblSymbolUnit  = null!;
    private Label         lblText        = null!;
    private NumericUpDown nudTextMm      = null!;
    private Label         lblTextUnit    = null!;
    private Label         lblMassstab    = null!;
    private Label         lblVorschau    = null!;
    private GroupBox      grpLayer       = null!;
    private Label         lblLayerInfo   = null!;
    private Button        btnExportieren = null!;
    private Button        btnAbbrechen   = null!;
}
