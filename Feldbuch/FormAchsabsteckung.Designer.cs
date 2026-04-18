namespace Feldbuch;

partial class FormAchsabsteckung
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
        dgvAchse      = new DataGridView();
        lblIntervall  = new Label(); txtIntervall = new TextBox();
        lblOffsets    = new Label(); txtOffsets   = new TextBox();
        dgvPunkte     = new DataGridView();
        mapLageplan   = new AbsteckungMapPanel();
        einweiser     = new AbsteckungEinweiserPanel();
        btnBerechnen  = new Button();
        btnSchliessen = new Button();

        SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvAchse).BeginInit();
        ((System.ComponentModel.ISupportInitialize)dgvPunkte).BeginInit();

        ClientSize    = new Size(1100, 740);
        Text          = "Achsabsteckung";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;

        var lf   = new Font("Segoe UI", 9F);
        var lf10 = new Font("Segoe UI", 10F);

        // ── Statuszeile ───────────────────────────────────────────────────────
        lblStation.Location  = new Point(10, 8);
        lblStation.Size      = new Size(1080, 20);
        lblStation.Font      = lf;
        lblStation.ForeColor = Color.DarkBlue;
        lblStation.Text      = "";

        // ── Achsdefinitions-Grid (2 Zeilen mit genug Höhe) ────────────────────
        dgvAchse.Location                    = new Point(10, 34);
        dgvAchse.Size                        = new Size(620, 96);
        dgvAchse.AllowUserToAddRows          = false;
        dgvAchse.AllowUserToDeleteRows       = false;
        dgvAchse.SelectionMode               = DataGridViewSelectionMode.FullRowSelect;
        dgvAchse.Font                        = lf;
        dgvAchse.RowHeadersWidth             = 28;
        dgvAchse.RowTemplate.Height          = 32;
        dgvAchse.AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill;
        dgvAchse.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvAchse.BackgroundColor             = Color.FromArgb(240, 244, 255);
        dgvAchse.GridColor                   = Color.FromArgb(180, 190, 220);
        dgvAchse.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Punkt",   Name = "Typ",     FillWeight = 14, ReadOnly = true });
        dgvAchse.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PunktNr", Name = "PunktNr", FillWeight = 18 });
        dgvAchse.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "R [m]",   Name = "R",       FillWeight = 24 });
        dgvAchse.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "H [m]",   Name = "H",       FillWeight = 24 });
        dgvAchse.Rows.Add("Anfang", "", "", "");
        dgvAchse.Rows.Add("Ende",   "", "", "");
        dgvAchse.Rows[0].Cells["Typ"].Style.BackColor = Color.FromArgb(220, 235, 255);
        dgvAchse.Rows[1].Cells["Typ"].Style.BackColor = Color.FromArgb(220, 235, 255);
        dgvAchse.Rows[0].Cells["Typ"].Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dgvAchse.Rows[1].Cells["Typ"].Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

        // ── Parameter-Zeile ───────────────────────────────────────────────────
        void Lbl(Label l, string t, int x, int y, int w = 80)
            { l.Text = t; l.Location = new Point(x, y); l.Size = new Size(w, 22); l.Font = lf; }
        void Txt(TextBox t, int x, int y, int w = 100)
            { t.Location = new Point(x, y); t.Size = new Size(w, 24); t.Font = lf; }

        Lbl(lblIntervall, "Intervall [m]:", 10, 140, 95);
        Txt(txtIntervall, 108, 137, 70); txtIntervall.Text = "10.0";
        Lbl(lblOffsets, "Offsets [m]:", 190, 140, 85);
        Txt(txtOffsets, 278, 137, 200); txtOffsets.Text = "0";
        new ToolTip().SetToolTip(txtOffsets, "Kommagetrennt: z.B. -3.75, 0, 3.75");

        btnBerechnen.Text      = "Berechnen";
        btnBerechnen.Location  = new Point(492, 133);
        btnBerechnen.Size      = new Size(138, 32);
        btnBerechnen.Font      = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnBerechnen.BackColor = Color.SteelBlue;
        btnBerechnen.ForeColor = Color.White;
        btnBerechnen.FlatStyle = FlatStyle.Flat;
        btnBerechnen.Click    += btnBerechnen_Click;

        // ── Ergebnistabelle ───────────────────────────────────────────────────
        dgvPunkte.Location                    = new Point(10, 174);
        dgvPunkte.Size                        = new Size(620, 498);
        dgvPunkte.AllowUserToAddRows          = false;
        dgvPunkte.AllowUserToDeleteRows       = false;
        dgvPunkte.ReadOnly                    = true;
        dgvPunkte.Font                        = lf;
        dgvPunkte.RowHeadersWidth             = 28;
        dgvPunkte.AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill;
        dgvPunkte.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvPunkte.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PunktNr",  Name = "PunktNr",  FillWeight = 12 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Station",  Name = "Station",  FillWeight = 14 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "R [m]",    Name = "R",        FillWeight = 14 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "H [m]",    Name = "H",        FillWeight = 14 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Abs. [m]", Name = "Abszisse", FillWeight = 12 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ord. [m]", Name = "Ordinate", FillWeight = 12 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Hz [gon]", Name = "Hz",       FillWeight = 14 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "s [m]",    Name = "s",        FillWeight = 12 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status",   Name = "Status",   FillWeight = 12 });

        // ── Lageplan (rechte obere Hälfte) ────────────────────────────────────
        mapLageplan.Location = new Point(640, 34);
        mapLageplan.Size     = new Size(450, 424);

        // ── Einweiser (rechte untere Hälfte) ──────────────────────────────────
        einweiser.Location = new Point(640, 462);
        einweiser.Size     = new Size(450, 242);

        // ── Schließen ─────────────────────────────────────────────────────────
        btnSchliessen.Text     = "Schließen";
        btnSchliessen.Location = new Point(940, 694);
        btnSchliessen.Size     = new Size(150, 36);
        btnSchliessen.Font     = lf10;
        btnSchliessen.Click   += btnSchliessen_Click;

        Controls.AddRange(new Control[]
        {
            lblStation,
            dgvAchse,
            lblIntervall, txtIntervall, lblOffsets, txtOffsets,
            btnBerechnen,
            dgvPunkte, mapLageplan, einweiser,
            btnSchliessen
        });

        ((System.ComponentModel.ISupportInitialize)dgvAchse).EndInit();
        ((System.ComponentModel.ISupportInitialize)dgvPunkte).EndInit();
        ResumeLayout(false);
    }

    private Label                    lblStation    = null!;
    private DataGridView             dgvAchse      = null!;
    private Label                    lblIntervall  = null!; private TextBox txtIntervall = null!;
    private Label                    lblOffsets    = null!; private TextBox txtOffsets   = null!;
    private DataGridView             dgvPunkte     = null!;
    private AbsteckungMapPanel       mapLageplan   = null!;
    private AbsteckungEinweiserPanel einweiser     = null!;
    private Button                   btnBerechnen  = null!;
    private Button                   btnSchliessen = null!;
}
