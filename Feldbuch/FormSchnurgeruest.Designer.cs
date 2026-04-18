namespace Feldbuch;

partial class FormSchnurgeruest
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
        lblAbstand    = new Label();
        txtAbstand    = new TextBox();
        lblPolyTitle  = new Label();
        dgvPolygon    = new DataGridView();
        lblSGTitle    = new Label();
        dgvSG         = new DataGridView();
        mapLageplan   = new AbsteckungMapPanel();
        btnLaden      = new Button();
        btnBerechnen  = new Button();
        btnSchliessen = new Button();

        SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvPolygon).BeginInit();
        ((System.ComponentModel.ISupportInitialize)dgvSG).BeginInit();

        ClientSize    = new Size(1080, 740);
        Text          = "Schnurgerüst";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;

        var lf  = new Font("Segoe UI", 9F);
        var lf10 = new Font("Segoe UI", 10F);

        lblStation.Location  = new Point(10, 8);
        lblStation.Size      = new Size(1060, 20);
        lblStation.Font      = lf;
        lblStation.ForeColor = Color.DarkBlue;
        lblStation.Text      = "";

        lblAbstand.Text     = "Schnurgerüst-Abstand [m]:";
        lblAbstand.Location = new Point(10, 38);
        lblAbstand.Size     = new Size(190, 22);
        lblAbstand.Font     = lf10;

        txtAbstand.Location = new Point(205, 35);
        txtAbstand.Size     = new Size(80, 24);
        txtAbstand.Font     = lf10;
        txtAbstand.Text     = "0.50";

        lblPolyTitle.Text     = "Gebäude-Polygon:";
        lblPolyTitle.Location = new Point(10, 68);
        lblPolyTitle.Size     = new Size(200, 20);
        lblPolyTitle.Font     = new Font("Segoe UI", 9F, FontStyle.Bold);

        dgvPolygon.Location                    = new Point(10, 90);
        dgvPolygon.Size                        = new Size(220, 560);
        dgvPolygon.AllowUserToAddRows          = false;
        dgvPolygon.AllowUserToDeleteRows       = false;
        dgvPolygon.Font                        = lf;
        dgvPolygon.RowHeadersWidth             = 28;
        dgvPolygon.AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill;
        dgvPolygon.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvPolygon.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;

        lblSGTitle.Text     = "Schnurgerüst-Punkte:";
        lblSGTitle.Location = new Point(240, 68);
        lblSGTitle.Size     = new Size(200, 20);
        lblSGTitle.Font     = new Font("Segoe UI", 9F, FontStyle.Bold);

        dgvSG.Location                    = new Point(240, 90);
        dgvSG.Size                        = new Size(380, 560);
        dgvSG.AllowUserToAddRows          = false;
        dgvSG.AllowUserToDeleteRows       = false;
        dgvSG.ReadOnly                    = true;
        dgvSG.Font                        = lf;
        dgvSG.RowHeadersWidth             = 28;
        dgvSG.AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill;
        dgvSG.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvSG.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;
        dgvSG.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PunktNr",  Name = "PunktNr", FillWeight = 20 });
        dgvSG.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "R [m]",    Name = "R",       FillWeight = 25 });
        dgvSG.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "H [m]",    Name = "H",       FillWeight = 25 });
        dgvSG.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Hz [gon]", Name = "Hz",      FillWeight = 20 });
        dgvSG.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "s [m]",    Name = "s",       FillWeight = 16 });

        mapLageplan.Location = new Point(630, 68);
        mapLageplan.Size     = new Size(440, 582);

        btnLaden.Text     = "AP aus DXF laden";
        btnLaden.Location = new Point(10, 668);
        btnLaden.Size     = new Size(170, 36);
        btnLaden.Font     = lf10;
        btnLaden.Click   += btnLaden_Click;

        btnBerechnen.Text      = "Berechnen";
        btnBerechnen.Location  = new Point(300, 668);
        btnBerechnen.Size      = new Size(130, 36);
        btnBerechnen.Font      = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnBerechnen.BackColor = Color.SteelBlue;
        btnBerechnen.ForeColor = Color.White;
        btnBerechnen.FlatStyle = FlatStyle.Flat;
        btnBerechnen.Click    += btnBerechnen_Click;

        btnSchliessen.Text     = "Schließen";
        btnSchliessen.Location = new Point(920, 668);
        btnSchliessen.Size     = new Size(150, 36);
        btnSchliessen.Font     = lf10;
        btnSchliessen.Click   += btnSchliessen_Click;

        Controls.AddRange(new Control[]
        {
            lblStation, lblAbstand, txtAbstand,
            lblPolyTitle, dgvPolygon, lblSGTitle, dgvSG,
            mapLageplan,
            btnLaden, btnBerechnen, btnSchliessen
        });

        ((System.ComponentModel.ISupportInitialize)dgvPolygon).EndInit();
        ((System.ComponentModel.ISupportInitialize)dgvSG).EndInit();
        ResumeLayout(false);
    }

    private Label        lblStation    = null!;
    private Label        lblAbstand    = null!;
    private TextBox      txtAbstand    = null!;
    private Label        lblPolyTitle  = null!;
    private DataGridView dgvPolygon    = null!;
    private Label        lblSGTitle    = null!;
    private DataGridView dgvSG         = null!;
    private AbsteckungMapPanel mapLageplan = null!;
    private Button       btnLaden      = null!;
    private Button       btnBerechnen  = null!;
    private Button       btnSchliessen = null!;
}
