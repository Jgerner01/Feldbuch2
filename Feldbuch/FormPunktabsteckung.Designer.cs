namespace Feldbuch;

partial class FormPunktabsteckung
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
        dgvPunkte     = new DataGridView();
        mapLageplan   = new AbsteckungMapPanel();
        einweiser     = new AbsteckungEinweiserPanel();
        btnLaden      = new Button();
        btnBerechnen  = new Button();
        btnSchliessen = new Button();

        SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvPunkte).BeginInit();

        ClientSize    = new Size(1220, 820);
        Text          = "Punktabsteckung";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;

        var lf   = new Font("Segoe UI", 9F);
        var lf10 = new Font("Segoe UI", 10F);

        // ── Statuszeile ───────────────────────────────────────────────────────
        lblStation.Text      = "Kein Standpunkt geladen.";
        lblStation.Location  = new Point(10, 8);
        lblStation.Size      = new Size(1200, 22);
        lblStation.Font      = lf;
        lblStation.ForeColor = Color.DarkBlue;

        // ── Punktliste ────────────────────────────────────────────────────────
        dgvPunkte.Location                    = new Point(10, 36);
        dgvPunkte.Size                        = new Size(400, 726);
        dgvPunkte.AllowUserToAddRows          = false;
        dgvPunkte.AllowUserToDeleteRows       = false;
        dgvPunkte.SelectionMode               = DataGridViewSelectionMode.FullRowSelect;
        dgvPunkte.Font                        = lf;
        dgvPunkte.RowHeadersWidth             = 28;
        dgvPunkte.AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill;
        dgvPunkte.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvPunkte.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;

        // ── Lageplan ──────────────────────────────────────────────────────────
        mapLageplan.Location = new Point(420, 36);
        mapLageplan.Size     = new Size(790, 420);

        // ── Einweiser ─────────────────────────────────────────────────────────
        einweiser.Location = new Point(420, 464);
        einweiser.Size     = new Size(790, 298);

        // ── Buttons ───────────────────────────────────────────────────────────
        btnLaden.Text      = "AP aus DXF laden";
        btnLaden.Location  = new Point(10, 770);
        btnLaden.Size      = new Size(175, 36);
        btnLaden.Font      = lf10;
        btnLaden.Click    += btnLaden_Click;

        btnBerechnen.Text      = "Absteckdaten";
        btnBerechnen.Location  = new Point(195, 770);
        btnBerechnen.Size      = new Size(150, 36);
        btnBerechnen.Font      = lf10;
        btnBerechnen.Click    += btnBerechnen_Click;

        btnSchliessen.Text     = "Schließen";
        btnSchliessen.Location = new Point(1060, 770);
        btnSchliessen.Size     = new Size(150, 36);
        btnSchliessen.Font     = lf10;
        btnSchliessen.Click   += btnSchliessen_Click;

        Controls.AddRange(new Control[]
        {
            lblStation,
            dgvPunkte, mapLageplan, einweiser,
            btnLaden, btnBerechnen, btnSchliessen
        });

        ((System.ComponentModel.ISupportInitialize)dgvPunkte).EndInit();
        ResumeLayout(false);
    }

    private Label                    lblStation    = null!;
    private DataGridView             dgvPunkte     = null!;
    private AbsteckungMapPanel       mapLageplan   = null!;
    private AbsteckungEinweiserPanel einweiser     = null!;
    private Button                   btnLaden      = null!;
    private Button                   btnBerechnen  = null!;
    private Button                   btnSchliessen = null!;
}
