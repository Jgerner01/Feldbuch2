namespace Feldbuch;

partial class FormHochpunktherablegung
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        lblPunktNr    = new Label();
        txtPunktNr    = new TextBox();
        lblHint       = new Label();
        dgvPunkte     = new DataGridView();
        pnlErgebnis   = new Panel();
        lblR          = new Label();
        lblH          = new Label();
        lblHoehe      = new Label();
        lblS0         = new Label();
        lblResTitle   = new Label();
        dgvResiduen   = new DataGridView();
        btnLaden      = new Button();
        btnzLaden     = new Button();
        btnBerechnen  = new Button();
        btnSchliessen = new Button();

        SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvPunkte).BeginInit();
        ((System.ComponentModel.ISupportInitialize)dgvResiduen).BeginInit();

        // ── Fenster ───────────────────────────────────────────────────────────
        ClientSize    = new Size(1080, 740);
        Text          = "Hochpunktherablegung";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;

        var lf  = new Font("Segoe UI", 10F);
        var lf9 = new Font("Segoe UI", 9F);
        var lfB = new Font("Segoe UI", 10F, FontStyle.Bold);

        // ── Kopfzeile ─────────────────────────────────────────────────────────
        lblPunktNr.Text     = "Hochpunkt-Nr.:";
        lblPunktNr.Location = new Point(20, 22);
        lblPunktNr.Size     = new Size(115, 24);
        lblPunktNr.Font     = lf;

        txtPunktNr.Location = new Point(140, 19);
        txtPunktNr.Size     = new Size(120, 28);
        txtPunktNr.Font     = lf;
        txtPunktNr.Text     = "9001";

        lblHint.Text      = "t = Hz + z  |  ΔH = s · cot(V)  |  H_P = H_Station + iH + ΔH − zh";
        lblHint.Location  = new Point(280, 24);
        lblHint.Size      = new Size(780, 20);
        lblHint.Font      = new Font("Segoe UI", 8.5F, FontStyle.Italic);
        lblHint.ForeColor = Color.DimGray;

        // ── Eingabe-Tabelle ───────────────────────────────────────────────────
        dgvPunkte.Location                    = new Point(20, 56);
        dgvPunkte.Size                        = new Size(1040, 340);
        dgvPunkte.AllowUserToAddRows          = false;
        dgvPunkte.AllowUserToDeleteRows       = false;
        dgvPunkte.SelectionMode               = DataGridViewSelectionMode.CellSelect;
        dgvPunkte.Font                        = lf9;
        dgvPunkte.RowHeadersWidth             = 32;
        dgvPunkte.AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill;
        dgvPunkte.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvPunkte.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;

        // ── Ergebnis-Panel ────────────────────────────────────────────────────
        pnlErgebnis.Location    = new Point(20, 410);
        pnlErgebnis.Size        = new Size(1040, 260);
        pnlErgebnis.Visible     = false;
        pnlErgebnis.BorderStyle = BorderStyle.FixedSingle;

        lblR.Text     = "R: –";
        lblR.Location = new Point(10, 10);
        lblR.Size     = new Size(320, 22);
        lblR.Font     = lfB;

        lblH.Text     = "H: –";
        lblH.Location = new Point(340, 10);
        lblH.Size     = new Size(320, 22);
        lblH.Font     = lfB;

        lblHoehe.Text     = "Höhe: –";
        lblHoehe.Location = new Point(670, 10);
        lblHoehe.Size     = new Size(360, 22);
        lblHoehe.Font     = lfB;

        lblS0.Text     = "–";
        lblS0.Location = new Point(10, 36);
        lblS0.Size     = new Size(1020, 22);
        lblS0.Font     = lf9;

        lblResTitle.Text     = "Residuen:";
        lblResTitle.Location = new Point(10, 64);
        lblResTitle.Size     = new Size(100, 20);
        lblResTitle.Font     = lf9;

        dgvResiduen.Location                    = new Point(10, 84);
        dgvResiduen.Size                        = new Size(1020, 162);
        dgvResiduen.AllowUserToAddRows          = false;
        dgvResiduen.AllowUserToDeleteRows       = false;
        dgvResiduen.ReadOnly                    = true;
        dgvResiduen.Font                        = lf9;
        dgvResiduen.RowHeadersWidth             = 28;
        dgvResiduen.AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill;
        dgvResiduen.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvResiduen.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PunktNr",      Name = "PunktNr",  FillWeight = 20 });
        dgvResiduen.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "s [m]",        Name = "Strecke",  FillWeight = 15 });
        dgvResiduen.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "v_Hz [cc]",    Name = "vDir",     FillWeight = 15 });
        dgvResiduen.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "H_P_i [m]",    Name = "HoehePi",  FillWeight = 20 });
        dgvResiduen.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "v_H [mm]",     Name = "vH",       FillWeight = 15 });

        pnlErgebnis.Controls.AddRange(new Control[]
            { lblR, lblH, lblHoehe, lblS0, lblResTitle, dgvResiduen });

        // ── Buttons ───────────────────────────────────────────────────────────
        btnLaden.Text     = "AP aus DXF laden";
        btnLaden.Location = new Point(20, 685);
        btnLaden.Size     = new Size(185, 36);
        btnLaden.Font     = lf;
        btnLaden.Click   += btnLaden_Click;

        btnzLaden.Text     = "z aus Freier Stat.";
        btnzLaden.Location = new Point(215, 685);
        btnzLaden.Size     = new Size(185, 36);
        btnzLaden.Font     = lf;
        btnzLaden.Click   += btnzLaden_Click;

        btnBerechnen.Text      = "Berechnen";
        btnBerechnen.Location  = new Point(700, 685);
        btnBerechnen.Size      = new Size(160, 36);
        btnBerechnen.Font      = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnBerechnen.BackColor = Color.SteelBlue;
        btnBerechnen.ForeColor = Color.White;
        btnBerechnen.FlatStyle = FlatStyle.Flat;
        btnBerechnen.Click    += btnBerechnen_Click;

        btnSchliessen.Text     = "Schließen";
        btnSchliessen.Location = new Point(870, 685);
        btnSchliessen.Size     = new Size(190, 36);
        btnSchliessen.Font     = lf;
        btnSchliessen.Click   += btnSchliessen_Click;

        Controls.AddRange(new Control[]
        {
            lblPunktNr, txtPunktNr, lblHint,
            dgvPunkte,
            pnlErgebnis,
            btnLaden, btnzLaden, btnBerechnen, btnSchliessen
        });

        ((System.ComponentModel.ISupportInitialize)dgvPunkte).EndInit();
        ((System.ComponentModel.ISupportInitialize)dgvResiduen).EndInit();
        ResumeLayout(false);
    }

    private Label          lblPunktNr    = null!;
    private TextBox        txtPunktNr    = null!;
    private Label          lblHint       = null!;
    private DataGridView   dgvPunkte     = null!;
    private Panel          pnlErgebnis   = null!;
    private Label          lblR          = null!;
    private Label          lblH          = null!;
    private Label          lblHoehe      = null!;
    private Label          lblS0         = null!;
    private Label          lblResTitle   = null!;
    private DataGridView   dgvResiduen   = null!;
    private Button         btnLaden      = null!;
    private Button         btnzLaden     = null!;
    private Button         btnBerechnen  = null!;
    private Button         btnSchliessen = null!;
}
