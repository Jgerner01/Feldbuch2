namespace Feldbuch;

partial class FormRueckwaertsschnitt
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        lblStandpunkt  = new Label();
        txtStandpunkt  = new TextBox();
        dgvPunkte      = new DataGridView();
        pnlErgebnis    = new Panel();
        lblR           = new Label();
        lblH           = new Label();
        lblZ           = new Label();
        lblS0          = new Label();
        lblKreis       = new Label();
        lblResTitle    = new Label();
        dgvResiduen    = new DataGridView();
        btnLaden       = new Button();
        btnBerechnen   = new Button();
        btnSchliessen  = new Button();

        SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvPunkte).BeginInit();
        ((System.ComponentModel.ISupportInitialize)dgvResiduen).BeginInit();

        // ── Fenster ───────────────────────────────────────────────────────────
        ClientSize    = new Size(900, 720);
        Text          = "Rückwärtschnitt";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;

        var lf  = new Font("Segoe UI", 10F);
        var lf9 = new Font("Segoe UI", 9F);

        // ── Kopfzeile ─────────────────────────────────────────────────────────
        lblStandpunkt.Text     = "Standpunktnummer:";
        lblStandpunkt.Location = new Point(20, 22);
        lblStandpunkt.Size     = new Size(145, 24);
        lblStandpunkt.Font     = lf;

        txtStandpunkt.Location = new Point(170, 19);
        txtStandpunkt.Size     = new Size(120, 28);
        txtStandpunkt.Font     = lf;
        txtStandpunkt.Text     = "9000";

        // ── Eingabe-Tabelle ───────────────────────────────────────────────────
        dgvPunkte.Location                    = new Point(20, 58);
        dgvPunkte.Size                        = new Size(860, 340);
        dgvPunkte.AllowUserToAddRows          = false;
        dgvPunkte.AllowUserToDeleteRows       = false;
        dgvPunkte.SelectionMode               = DataGridViewSelectionMode.CellSelect;
        dgvPunkte.Font                        = lf9;
        dgvPunkte.RowHeadersWidth             = 32;
        dgvPunkte.AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill;
        dgvPunkte.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvPunkte.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;

        // ── Ergebnis-Panel ────────────────────────────────────────────────────
        pnlErgebnis.Location  = new Point(20, 410);
        pnlErgebnis.Size      = new Size(860, 252);
        pnlErgebnis.Visible   = false;
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

        lblZ.Text     = "Orientierung z: –";
        lblZ.Location = new Point(10, 36);
        lblZ.Size     = new Size(360, 22);
        lblZ.Font     = lf;

        lblS0.Text     = "–";
        lblS0.Location = new Point(10, 58);
        lblS0.Size     = new Size(840, 22);
        lblS0.Font     = lf9;

        lblKreis.Text      = "";
        lblKreis.Location  = new Point(10, 80);
        lblKreis.Size      = new Size(840, 36);
        lblKreis.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblKreis.ForeColor = Color.DarkRed;
        lblKreis.Visible   = false;
        lblKreis.AutoSize  = false;

        lblResTitle.Text     = "Residuen:";
        lblResTitle.Location = new Point(10, 120);
        lblResTitle.Size     = new Size(100, 20);
        lblResTitle.Font     = lf9;

        dgvResiduen.Location                    = new Point(10, 140);
        dgvResiduen.Size                        = new Size(840, 118);
        dgvResiduen.AllowUserToAddRows          = false;
        dgvResiduen.AllowUserToDeleteRows       = false;
        dgvResiduen.ReadOnly                    = true;
        dgvResiduen.Font                        = lf9;
        dgvResiduen.RowHeadersWidth             = 28;
        dgvResiduen.AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill;
        dgvResiduen.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvResiduen.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PunktNr",  Name = "PunktNr",  FillWeight = 25 });
        dgvResiduen.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "s [m]",    Name = "Strecke",  FillWeight = 20 });
        dgvResiduen.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "v [cc]",   Name = "vWinkel",  FillWeight = 20 });

        pnlErgebnis.Controls.AddRange(new Control[]
            { lblR, lblH, lblZ, lblS0, lblKreis, lblResTitle, dgvResiduen });

        // ── Buttons ───────────────────────────────────────────────────────────
        btnLaden.Text      = "AP aus DXF laden";
        btnLaden.Location  = new Point(20, 660);
        btnLaden.Size      = new Size(200, 36);
        btnLaden.Font      = lf;
        btnLaden.Click    += btnAnschlusspunkteLaden_Click;

        btnBerechnen.Text      = "Berechnen";
        btnBerechnen.Location  = new Point(560, 660);
        btnBerechnen.Size      = new Size(150, 36);
        btnBerechnen.Font      = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnBerechnen.BackColor = Color.SteelBlue;
        btnBerechnen.ForeColor = Color.White;
        btnBerechnen.FlatStyle = FlatStyle.Flat;
        btnBerechnen.Click    += btnBerechnen_Click;

        btnSchliessen.Text      = "Schließen";
        btnSchliessen.Location  = new Point(720, 660);
        btnSchliessen.Size      = new Size(160, 36);
        btnSchliessen.Font      = lf;
        btnSchliessen.Click    += btnSchliessen_Click;

        Controls.AddRange(new Control[]
        {
            lblStandpunkt, txtStandpunkt,
            dgvPunkte,
            pnlErgebnis,
            btnLaden, btnBerechnen, btnSchliessen
        });

        ((System.ComponentModel.ISupportInitialize)dgvPunkte).EndInit();
        ((System.ComponentModel.ISupportInitialize)dgvResiduen).EndInit();
        ResumeLayout(false);
    }

    private Label          lblStandpunkt  = null!;
    private TextBox        txtStandpunkt  = null!;
    private DataGridView   dgvPunkte      = null!;
    private Panel          pnlErgebnis    = null!;
    private Label          lblR           = null!;
    private Label          lblH           = null!;
    private Label          lblZ           = null!;
    private Label          lblS0          = null!;
    private Label          lblKreis       = null!;
    private Label          lblResTitle    = null!;
    private DataGridView   dgvResiduen    = null!;
    private Button         btnLaden       = null!;
    private Button         btnBerechnen   = null!;
    private Button         btnSchliessen  = null!;
}
