namespace Feldbuch;

partial class FormProfilabsteckung
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
        lblA          = new Label();
        lblR_A        = new Label(); txtR_A = new TextBox();
        lblH_A        = new Label(); txtH_A = new TextBox();
        lblE          = new Label();
        lblR_E        = new Label(); txtR_E = new TextBox();
        lblH_E        = new Label(); txtH_E = new TextBox();
        lblIntervall  = new Label(); txtIntervall  = new TextBox();
        lblHalbbreite = new Label(); txtHalbbreite = new TextBox();
        lblBoesch     = new Label(); txtBoesch     = new TextBox();
        lblHint       = new Label();
        dgvProfile    = new DataGridView();
        mapLageplan   = new AbsteckungMapPanel();
        btnGenerieren = new Button();
        btnSchliessen = new Button();

        SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvProfile).BeginInit();

        ClientSize    = new Size(1100, 720);
        Text          = "Profilabsteckung";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;

        var lf   = new Font("Segoe UI", 9F);
        var lf10 = new Font("Segoe UI", 10F);

        lblStation.Location  = new Point(10, 6);
        lblStation.Size      = new Size(1080, 20);
        lblStation.Font      = lf;
        lblStation.ForeColor = Color.DarkBlue;
        lblStation.Text      = "";

        // ── Parameterzeile ────────────────────────────────────────────────────
        int y = 34;
        void Lbl(Label l, string t, int x) { l.Text = t; l.Location = new Point(x, y + 2); l.Size = new Size(t.Length * 7 + 6, 20); l.Font = lf; }
        void Txt(TextBox t, int x, int w = 100) { t.Location = new Point(x, y); t.Size = new Size(w, 24); t.Font = lf; }

        Lbl(lblA, "A:", 10);
        Lbl(lblR_A, "R:", 24); Txt(txtR_A, 38);
        Lbl(lblH_A, "H:", 145); Txt(txtH_A, 159);
        Lbl(lblE, "E:", 270);
        Lbl(lblR_E, "R:", 284); Txt(txtR_E, 298);
        Lbl(lblH_E, "H:", 405); Txt(txtH_E, 419);
        Lbl(lblIntervall,  "Int.[m]:", 530); Txt(txtIntervall,  600, 70); txtIntervall.Text = "25.0";
        Lbl(lblHalbbreite, "Halbbreite:", 678); Txt(txtHalbbreite, 760, 70); txtHalbbreite.Text = "3.000";
        Lbl(lblBoesch,     "Bösch. 1:", 840); Txt(txtBoesch, 908, 60); txtBoesch.Text = "1.5";

        lblHint.Text      = "H_Plan und H_Gelände (nach Messung) in der Tabelle eintragen. Böschungsfuß = Halbbreite + |ΔH| · n";
        lblHint.Location  = new Point(10, 66);
        lblHint.Size      = new Size(1080, 18);
        lblHint.Font      = new Font("Segoe UI", 8F, FontStyle.Italic);
        lblHint.ForeColor = Color.DimGray;

        // ── Tabelle ───────────────────────────────────────────────────────────
        dgvProfile.Location                    = new Point(10, 88);
        dgvProfile.Size                        = new Size(530, 548);
        dgvProfile.AllowUserToAddRows          = false;
        dgvProfile.AllowUserToDeleteRows       = false;
        dgvProfile.SelectionMode               = DataGridViewSelectionMode.CellSelect;
        dgvProfile.Font                        = lf;
        dgvProfile.RowHeadersWidth             = 28;
        dgvProfile.AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill;
        dgvProfile.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvProfile.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;

        // ── Karte ────────────────────────────────────────────────────────────
        mapLageplan.Location = new Point(548, 88);
        mapLageplan.Size     = new Size(542, 548);

        // ── Buttons ───────────────────────────────────────────────────────────
        btnGenerieren.Text      = "Profile generieren";
        btnGenerieren.Location  = new Point(10, 650);
        btnGenerieren.Size      = new Size(170, 36);
        btnGenerieren.Font      = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnGenerieren.BackColor = Color.SteelBlue;
        btnGenerieren.ForeColor = Color.White;
        btnGenerieren.FlatStyle = FlatStyle.Flat;
        btnGenerieren.Click    += btnGenerieren_Click;

        btnSchliessen.Text     = "Schließen";
        btnSchliessen.Location = new Point(940, 650);
        btnSchliessen.Size     = new Size(150, 36);
        btnSchliessen.Font     = lf10;
        btnSchliessen.Click   += btnSchliessen_Click;

        Controls.AddRange(new Control[]
        {
            lblStation,
            lblA, lblR_A, txtR_A, lblH_A, txtH_A,
            lblE, lblR_E, txtR_E, lblH_E, txtH_E,
            lblIntervall, txtIntervall, lblHalbbreite, txtHalbbreite, lblBoesch, txtBoesch,
            lblHint, dgvProfile, mapLageplan,
            btnGenerieren, btnSchliessen
        });

        ((System.ComponentModel.ISupportInitialize)dgvProfile).EndInit();
        ResumeLayout(false);
    }

    private Label        lblStation    = null!;
    private Label        lblA          = null!;
    private Label        lblR_A        = null!; private TextBox txtR_A        = null!;
    private Label        lblH_A        = null!; private TextBox txtH_A        = null!;
    private Label        lblE          = null!;
    private Label        lblR_E        = null!; private TextBox txtR_E        = null!;
    private Label        lblH_E        = null!; private TextBox txtH_E        = null!;
    private Label        lblIntervall  = null!; private TextBox txtIntervall  = null!;
    private Label        lblHalbbreite = null!; private TextBox txtHalbbreite = null!;
    private Label        lblBoesch     = null!; private TextBox txtBoesch     = null!;
    private Label        lblHint       = null!;
    private DataGridView       dgvProfile    = null!;
    private AbsteckungMapPanel mapLageplan   = null!;
    private Button             btnGenerieren = null!;
    private Button       btnSchliessen = null!;
}
