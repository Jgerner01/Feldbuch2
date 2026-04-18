namespace Feldbuch;

partial class FormRasterabsteckung
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        lblStation    = new Label();
        lblR0         = new Label(); txtR0      = new TextBox();
        lblH0         = new Label(); txtH0      = new TextBox();
        lblRichtung   = new Label(); txtRichtung = new TextBox();
        lbldS         = new Label(); txtdS      = new TextBox();
        lbldQ         = new Label(); txtdQ      = new TextBox();
        lblnRows      = new Label(); txtnRows   = new TextBox();
        lblnCols      = new Label(); txtnCols   = new TextBox();
        dgvPunkte     = new DataGridView();
        mapLageplan   = new AbsteckungMapPanel();
        btnBerechnen  = new Button();
        btnSchliessen = new Button();

        SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvPunkte).BeginInit();

        ClientSize    = new Size(1080, 700);
        Text          = "Rasterabsteckung";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;

        var lf   = new Font("Segoe UI", 9F);
        var lf10 = new Font("Segoe UI", 10F);

        lblStation.Location  = new Point(10, 8);
        lblStation.Size      = new Size(1060, 20);
        lblStation.Font      = lf;
        lblStation.ForeColor = Color.DarkBlue;
        lblStation.Text      = "";

        // ── Parameterzeile ────────────────────────────────────────────────────
        int y = 36;
        void Row(Label lbl, string text, TextBox txt, int x, string def = "")
        {
            lbl.Text     = text;
            lbl.Location = new Point(x, y + 2);
            lbl.Size     = new Size(text.Length * 7 + 4, 20);
            lbl.Font     = lf;
            txt.Location = new Point(x + lbl.Width, y);
            txt.Size     = new Size(85, 24);
            txt.Font     = lf;
            txt.Text     = def;
        }

        Row(lblR0,       "R0 [m]:",       txtR0,       10);
        Row(lblH0,       "H0 [m]:",       txtH0,       115);
        Row(lblRichtung, "Richtung [gon]:", txtRichtung, 220,  "0.0000");
        Row(lbldS,       "ΔS [m]:",       txtdS,       370,  "5.000");
        Row(lbldQ,       "ΔQ [m]:",       txtdQ,       460,  "5.000");
        Row(lblnRows,    "Zeilen:",        txtnRows,    550,  "4");
        Row(lblnCols,    "Spalten:",       txtnCols,    635,  "4");

        // ── Tabelle ───────────────────────────────────────────────────────────
        dgvPunkte.Location                    = new Point(10, 72);
        dgvPunkte.Size                        = new Size(560, 556);
        dgvPunkte.AllowUserToAddRows          = false;
        dgvPunkte.AllowUserToDeleteRows       = false;
        dgvPunkte.ReadOnly                    = true;
        dgvPunkte.Font                        = lf;
        dgvPunkte.RowHeadersWidth             = 28;
        dgvPunkte.AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill;
        dgvPunkte.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvPunkte.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Punkt",    Name = "Punkt",   FillWeight = 18 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "R [m]",    Name = "R",       FillWeight = 22 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "H [m]",    Name = "H",       FillWeight = 22 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Hz [gon]", Name = "Hz",      FillWeight = 20 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "s [m]",    Name = "s",       FillWeight = 16 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status",   Name = "Status",  FillWeight = 14 });

        // ── Lageplan ──────────────────────────────────────────────────────────
        mapLageplan.Location = new Point(580, 72);
        mapLageplan.Size     = new Size(490, 556);

        // ── Buttons ───────────────────────────────────────────────────────────
        btnBerechnen.Text      = "Berechnen";
        btnBerechnen.Location  = new Point(10, 644);
        btnBerechnen.Size      = new Size(140, 36);
        btnBerechnen.Font      = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnBerechnen.BackColor = Color.SteelBlue;
        btnBerechnen.ForeColor = Color.White;
        btnBerechnen.FlatStyle = FlatStyle.Flat;
        btnBerechnen.Click    += btnBerechnen_Click;

        btnSchliessen.Text     = "Schließen";
        btnSchliessen.Location = new Point(920, 644);
        btnSchliessen.Size     = new Size(150, 36);
        btnSchliessen.Font     = lf10;
        btnSchliessen.Click   += btnSchliessen_Click;

        Controls.AddRange(new Control[]
        {
            lblStation,
            lblR0, txtR0, lblH0, txtH0, lblRichtung, txtRichtung,
            lbldS, txtdS, lbldQ, txtdQ, lblnRows, txtnRows, lblnCols, txtnCols,
            dgvPunkte, mapLageplan,
            btnBerechnen, btnSchliessen
        });

        ((System.ComponentModel.ISupportInitialize)dgvPunkte).EndInit();
        ResumeLayout(false);
    }

    private Label        lblStation    = null!;
    private Label        lblR0         = null!; private TextBox txtR0       = null!;
    private Label        lblH0         = null!; private TextBox txtH0       = null!;
    private Label        lblRichtung   = null!; private TextBox txtRichtung  = null!;
    private Label        lbldS         = null!; private TextBox txtdS       = null!;
    private Label        lbldQ         = null!; private TextBox txtdQ       = null!;
    private Label        lblnRows      = null!; private TextBox txtnRows    = null!;
    private Label        lblnCols      = null!; private TextBox txtnCols    = null!;
    private DataGridView dgvPunkte     = null!;
    private AbsteckungMapPanel mapLageplan = null!;
    private Button       btnBerechnen  = null!;
    private Button       btnSchliessen = null!;
}
