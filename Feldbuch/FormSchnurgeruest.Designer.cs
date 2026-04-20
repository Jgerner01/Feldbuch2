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
        lblStation            = new Label();
        lblAbstand            = new Label();
        txtAbstand            = new TextBox();
        btnFixieren           = new Button();
        lblPolyTitle          = new Label();
        dgvPolygon            = new DataGridView();
        mapLageplan           = new AbsteckungMapPanel();
        lblAchsInfo           = new Label();
        btnLaden              = new Button();
        btnMessungBestaetigen = new Button();
        btnSchliessen         = new Button();

        SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvPolygon).BeginInit();

        ClientSize    = new Size(1260, 820);
        Text          = "Schnurgerüst";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;

        var lf   = new Font("Segoe UI", 9F);
        var lf10 = new Font("Segoe UI", 10F);

        // ── Standpunkt ────────────────────────────────────────────────────────
        lblStation.Location  = new Point(10, 6);
        lblStation.Size      = new Size(1240, 20);
        lblStation.Font      = lf;
        lblStation.ForeColor = Color.DarkBlue;
        lblStation.Text      = "";

        // ── Parameter-Zeile ───────────────────────────────────────────────────
        lblAbstand.Text     = "SG-Abstand [m]:";
        lblAbstand.Location = new Point(10, 34);
        lblAbstand.Size     = new Size(105, 22);
        lblAbstand.Font     = lf;

        txtAbstand.Location = new Point(118, 31);
        txtAbstand.Size     = new Size(70, 24);
        txtAbstand.Font     = lf;
        txtAbstand.Text     = "0";

        btnFixieren.Text      = "Fixieren";
        btnFixieren.Location  = new Point(200, 29);
        btnFixieren.Size      = new Size(130, 30);
        btnFixieren.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnFixieren.BackColor = Color.FromArgb(60, 120, 60);
        btnFixieren.ForeColor = Color.White;
        btnFixieren.FlatStyle = FlatStyle.Flat;
        btnFixieren.Click    += btnFixieren_Click;

        // ── Polygon-Tabelle ───────────────────────────────────────────────────
        lblPolyTitle.Text     = "Gebäude-Polygon:";
        lblPolyTitle.Location = new Point(10, 64);
        lblPolyTitle.Size     = new Size(360, 20);
        lblPolyTitle.Font     = new Font("Segoe UI", 9F, FontStyle.Bold);

        dgvPolygon.Location                    = new Point(10, 86);
        dgvPolygon.Size                        = new Size(360, 666);
        dgvPolygon.AllowUserToAddRows          = false;
        dgvPolygon.AllowUserToDeleteRows       = false;
        dgvPolygon.SelectionMode               = DataGridViewSelectionMode.FullRowSelect;
        dgvPolygon.Font                        = lf;
        dgvPolygon.RowHeadersWidth             = 28;
        dgvPolygon.AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill;
        dgvPolygon.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvPolygon.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;

        // ── Karte ─────────────────────────────────────────────────────────────
        mapLageplan.Location = new Point(380, 29);
        mapLageplan.Size     = new Size(870, 590);

        // ── Achsinfo-Zeile ────────────────────────────────────────────────────
        lblAchsInfo.Location    = new Point(380, 625);
        lblAchsInfo.Size        = new Size(870, 58);
        lblAchsInfo.Font        = new Font("Segoe UI", 9F);
        lblAchsInfo.BackColor   = Color.FromArgb(255, 255, 220);
        lblAchsInfo.BorderStyle = BorderStyle.FixedSingle;
        lblAchsInfo.Text        = "Achse: – (fixieren, dann Gebäudeseite anklicken)";
        lblAchsInfo.TextAlign   = ContentAlignment.MiddleLeft;
        lblAchsInfo.Padding     = new Padding(6, 2, 0, 0);

        // ── Buttons ───────────────────────────────────────────────────────────
        btnLaden.Text     = "AP aus DXF laden";
        btnLaden.Location = new Point(10, 770);
        btnLaden.Size     = new Size(175, 36);
        btnLaden.Font     = lf10;
        btnLaden.Click   += btnLaden_Click;

        btnMessungBestaetigen.Text      = "Messung bestätigen";
        btnMessungBestaetigen.Location  = new Point(520, 770);
        btnMessungBestaetigen.Size      = new Size(200, 36);
        btnMessungBestaetigen.Font      = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnMessungBestaetigen.BackColor = Color.SteelBlue;
        btnMessungBestaetigen.ForeColor = Color.White;
        btnMessungBestaetigen.FlatStyle = FlatStyle.Flat;
        btnMessungBestaetigen.Click    += btnMessungBestaetigen_Click;

        btnSchliessen.Text     = "Schließen";
        btnSchliessen.Location = new Point(1100, 770);
        btnSchliessen.Size     = new Size(150, 36);
        btnSchliessen.Font     = lf10;
        btnSchliessen.Click   += btnSchliessen_Click;

        Controls.AddRange(new Control[]
        {
            lblStation, lblAbstand, txtAbstand, btnFixieren,
            lblPolyTitle, dgvPolygon,
            mapLageplan, lblAchsInfo,
            btnLaden, btnMessungBestaetigen, btnSchliessen
        });

        ((System.ComponentModel.ISupportInitialize)dgvPolygon).EndInit();
        ResumeLayout(false);
    }

    private Label              lblStation            = null!;
    private Label              lblAbstand            = null!;
    private TextBox            txtAbstand            = null!;
    private Button             btnFixieren           = null!;
    private Label              lblPolyTitle          = null!;
    private DataGridView       dgvPolygon            = null!;
    private AbsteckungMapPanel mapLageplan           = null!;
    private Label              lblAchsInfo           = null!;
    private Button             btnLaden              = null!;
    private Button             btnMessungBestaetigen = null!;
    private Button             btnSchliessen         = null!;
}
