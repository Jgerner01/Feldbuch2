namespace Feldbuch;

partial class FormBogenschnitt
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        lblNeupunkt   = new Label();
        txtNeupunkt   = new TextBox();
        dgvPunkte     = new DataGridView();
        pnlErgebnis   = new Panel();
        lblR          = new Label();
        lblH          = new Label();
        lblS0         = new Label();
        lblResTitle   = new Label();
        dgvResiduen   = new DataGridView();
        btnLaden      = new Button();
        btnBerechnen  = new Button();
        btnSchliessen = new Button();

        SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvPunkte).BeginInit();
        ((System.ComponentModel.ISupportInitialize)dgvResiduen).BeginInit();

        ClientSize    = new Size(820, 680);
        Text          = "Bogenschnitt";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;

        var lf  = new Font("Segoe UI", 10F);
        var lf9 = new Font("Segoe UI", 9F);

        // ── Kopfzeile ─────────────────────────────────────────────────────────
        lblNeupunkt.Text     = "Neupunkt-Nr.:";
        lblNeupunkt.Location = new Point(20, 22);
        lblNeupunkt.Size     = new Size(110, 24);
        lblNeupunkt.Font     = lf;

        txtNeupunkt.Location = new Point(135, 19);
        txtNeupunkt.Size     = new Size(120, 28);
        txtNeupunkt.Font     = lf;
        txtNeupunkt.Text     = "8000";

        // ── Eingabe-Tabelle ───────────────────────────────────────────────────
        dgvPunkte.Location                    = new Point(20, 58);
        dgvPunkte.Size                        = new Size(780, 290);
        dgvPunkte.AllowUserToAddRows          = false;
        dgvPunkte.AllowUserToDeleteRows       = false;
        dgvPunkte.SelectionMode               = DataGridViewSelectionMode.CellSelect;
        dgvPunkte.Font                        = lf9;
        dgvPunkte.RowHeadersWidth             = 32;
        dgvPunkte.AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill;
        dgvPunkte.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvPunkte.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;

        // ── Ergebnis-Panel ────────────────────────────────────────────────────
        pnlErgebnis.Location    = new Point(20, 360);
        pnlErgebnis.Size        = new Size(780, 220);
        pnlErgebnis.Visible     = false;
        pnlErgebnis.BorderStyle = BorderStyle.FixedSingle;

        var lfB = new Font("Segoe UI", 10F, FontStyle.Bold);

        lblR.Text     = "R: –";
        lblR.Location = new Point(10, 10);
        lblR.Size     = new Size(280, 22);
        lblR.Font     = lfB;

        lblH.Text     = "H: –";
        lblH.Location = new Point(300, 10);
        lblH.Size     = new Size(280, 22);
        lblH.Font     = lfB;

        lblS0.Text      = "–";
        lblS0.Location  = new Point(10, 36);
        lblS0.Size      = new Size(760, 40);
        lblS0.Font      = lf9;
        lblS0.AutoSize  = false;

        lblResTitle.Text     = "Residuen:";
        lblResTitle.Location = new Point(10, 80);
        lblResTitle.Size     = new Size(100, 20);
        lblResTitle.Font     = lf9;

        dgvResiduen.Location                    = new Point(10, 100);
        dgvResiduen.Size                        = new Size(760, 108);
        dgvResiduen.AllowUserToAddRows          = false;
        dgvResiduen.AllowUserToDeleteRows       = false;
        dgvResiduen.ReadOnly                    = true;
        dgvResiduen.Font                        = lf9;
        dgvResiduen.RowHeadersWidth             = 28;
        dgvResiduen.AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill;
        dgvResiduen.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvResiduen.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PunktNr",  Name = "PunktNr",  FillWeight = 35 });
        dgvResiduen.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "v [mm]",   Name = "vStrecke", FillWeight = 25 });

        pnlErgebnis.Controls.AddRange(new Control[]
            { lblR, lblH, lblS0, lblResTitle, dgvResiduen });

        // ── Buttons ───────────────────────────────────────────────────────────
        btnLaden.Text      = "AP aus DXF laden";
        btnLaden.Location  = new Point(20, 596);
        btnLaden.Size      = new Size(185, 36);
        btnLaden.Font      = lf;
        btnLaden.Click    += btnLaden_Click;

        btnBerechnen.Text      = "Berechnen";
        btnBerechnen.Location  = new Point(480, 596);
        btnBerechnen.Size      = new Size(150, 36);
        btnBerechnen.Font      = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnBerechnen.BackColor = Color.SteelBlue;
        btnBerechnen.ForeColor = Color.White;
        btnBerechnen.FlatStyle = FlatStyle.Flat;
        btnBerechnen.Click    += btnBerechnen_Click;

        btnSchliessen.Text      = "Schließen";
        btnSchliessen.Location  = new Point(640, 596);
        btnSchliessen.Size      = new Size(160, 36);
        btnSchliessen.Font      = lf;
        btnSchliessen.Click    += btnSchliessen_Click;

        Controls.AddRange(new Control[]
        {
            lblNeupunkt, txtNeupunkt,
            dgvPunkte,
            pnlErgebnis,
            btnLaden, btnBerechnen, btnSchliessen
        });

        ((System.ComponentModel.ISupportInitialize)dgvPunkte).EndInit();
        ((System.ComponentModel.ISupportInitialize)dgvResiduen).EndInit();
        ResumeLayout(false);
    }

    private Label          lblNeupunkt   = null!;
    private TextBox        txtNeupunkt   = null!;
    private DataGridView   dgvPunkte     = null!;
    private Panel          pnlErgebnis   = null!;
    private Label          lblR          = null!;
    private Label          lblH          = null!;
    private Label          lblS0         = null!;
    private Label          lblResTitle   = null!;
    private DataGridView   dgvResiduen   = null!;
    private Button         btnLaden      = null!;
    private Button         btnBerechnen  = null!;
    private Button         btnSchliessen = null!;
}
