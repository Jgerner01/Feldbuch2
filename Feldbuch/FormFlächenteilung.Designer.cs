namespace Feldbuch;

partial class FormFlächenteilung
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        lblVerfahren     = new Label();
        cmbVerfahren     = new ComboBox();
        lblHint          = new Label();

        // Methoden-Parameter
        lblEdge          = new Label();
        cmbEdge          = new ComboBox();
        lblVertex        = new Label();
        cmbVertex        = new ComboBox();
        lblFixR          = new Label();
        txtFixR          = new TextBox();
        lblFixH          = new Label();
        txtFixH          = new TextBox();
        lblRichtung      = new Label();
        txtRichtung      = new TextBox();
        lblBreite        = new Label();
        txtBreite        = new TextBox();
        lblRatio         = new Label();
        pnlRatio         = new Panel();
        txtRatioA        = new TextBox();
        lblRatioColon    = new Label();
        txtRatioB        = new TextBox();
        lblRatioBasis    = new Label();
        cmbRatioBasis    = new ComboBox();
        lblASoll         = new Label();
        txtASoll         = new TextBox();

        // Polygon-Eingabe
        lblPolygon       = new Label();
        dgvPolygon       = new DataGridView();
        lblAGesamt       = new Label();

        // Ergebnisse
        lblResA1soll     = new Label();
        lblResA1ist      = new Label();
        lblResA2         = new Label();
        lblResDiff       = new Label();

        // Vorschau
        mapPreview       = new AbsteckungMapPanel();

        // Grenzpunkte
        lblGrenzpunkte   = new Label();
        dgvGrenzpunkte   = new DataGridView();

        // Buttons
        btnLaden         = new Button();
        btnBerechnen     = new Button();
        btnAbsteckung    = new Button();
        btnProtokoll     = new Button();
        btnSchliessen    = new Button();

        SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvPolygon).BeginInit();
        ((System.ComponentModel.ISupportInitialize)dgvGrenzpunkte).BeginInit();

        // ── Formular ──────────────────────────────────────────────────────────
        ClientSize      = new Size(1220, 800);
        Text            = "Flächenteilung (Abspaltung)";
        StartPosition   = FormStartPosition.CenterParent;
        AutoScaleMode   = AutoScaleMode.Font;
        MinimumSize     = new Size(1220, 800);

        var fLbl = new Font("Segoe UI", 9F);
        var fTxt = new Font("Segoe UI", 9.5F);
        var fTit = new Font("Segoe UI", 10F, FontStyle.Bold);

        // ── Verfahren ─────────────────────────────────────────────────────────
        lblVerfahren.Text     = "Verfahren:";
        lblVerfahren.Location = new Point(10, 10);
        lblVerfahren.Size     = new Size(80, 22);
        lblVerfahren.Font     = fLbl;

        cmbVerfahren.Location    = new Point(10, 30);
        cmbVerfahren.Size        = new Size(360, 26);
        cmbVerfahren.Font        = fTxt;
        cmbVerfahren.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbVerfahren.Items.AddRange(new object[]
        {
            "1 – Parallele zur Grundseite",
            "2 – Durch festen Punkt",
            "3 – Gegebene Richtung",
            "4 – Dreiecksabtrennung von Eckpunkt",
            "5 – Verhältnisteilung",
            "6 – Streifenabtrennung",
        });
        cmbVerfahren.SelectedIndexChanged += cmbVerfahren_SelectedIndexChanged;

        lblHint.Location  = new Point(10, 62);
        lblHint.Size      = new Size(360, 36);
        lblHint.Font      = new Font("Segoe UI", 8.5F, FontStyle.Italic);
        lblHint.ForeColor = Color.FromArgb(80, 80, 120);

        // ── Methoden-Parameter (y=100..220) ──────────────────────────────────
        int py = 104;

        lblEdge.Text     = "Grundkante:";
        lblEdge.Location = new Point(10, py);
        lblEdge.Size     = new Size(100, 22);
        lblEdge.Font     = fLbl;
        cmbEdge.Location    = new Point(115, py - 2);
        cmbEdge.Size        = new Size(255, 26);
        cmbEdge.Font        = fTxt;
        cmbEdge.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbEdge.SelectedIndexChanged += cmbEdge_SelectedIndexChanged;

        lblVertex.Text     = "Eckpunkt:";
        lblVertex.Location = new Point(10, py);
        lblVertex.Size     = new Size(100, 22);
        lblVertex.Font     = fLbl;
        cmbVertex.Location    = new Point(115, py - 2);
        cmbVertex.Size        = new Size(255, 26);
        cmbVertex.Font        = fTxt;
        cmbVertex.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbVertex.SelectedIndexChanged += cmbVertex_SelectedIndexChanged;

        lblFixR.Text     = "R (fix):";
        lblFixR.Location = new Point(10, py);
        lblFixR.Size     = new Size(70, 22);
        lblFixR.Font     = fLbl;
        txtFixR.Location = new Point(85, py - 2);
        txtFixR.Size     = new Size(130, 24);
        txtFixR.Font     = fTxt;

        lblFixH.Text     = "H (fix):";
        lblFixH.Location = new Point(225, py);
        lblFixH.Size     = new Size(70, 22);
        lblFixH.Font     = fLbl;
        txtFixH.Location = new Point(300, py - 2);
        txtFixH.Size     = new Size(70, 24);
        txtFixH.Font     = fTxt;

        lblRichtung.Text     = "Richtung [gon]:";
        lblRichtung.Location = new Point(10, py);
        lblRichtung.Size     = new Size(120, 22);
        lblRichtung.Font     = fLbl;
        txtRichtung.Location = new Point(135, py - 2);
        txtRichtung.Size     = new Size(100, 24);
        txtRichtung.Font     = fTxt;
        txtRichtung.Text     = "0.0000";

        lblBreite.Text     = "Breite [m]:";
        lblBreite.Location = new Point(225, py);
        lblBreite.Size     = new Size(80, 22);
        lblBreite.Font     = fLbl;
        txtBreite.Location = new Point(310, py - 2);
        txtBreite.Size     = new Size(60, 24);
        txtBreite.Font     = fTxt;

        // Verhältnis-Panel
        lblRatio.Text     = "Verhältnis a:b";
        lblRatio.Location = new Point(10, py);
        lblRatio.Size     = new Size(100, 22);
        lblRatio.Font     = fLbl;

        pnlRatio.Location = new Point(115, py - 4);
        pnlRatio.Size     = new Size(255, 50);

        txtRatioA.Location = new Point(0, 2);
        txtRatioA.Size     = new Size(55, 24);
        txtRatioA.Font     = fTxt;
        txtRatioA.Text     = "1";

        lblRatioColon.Text     = ":";
        lblRatioColon.Location = new Point(58, 4);
        lblRatioColon.Size     = new Size(12, 22);
        lblRatioColon.Font     = fTit;

        txtRatioB.Location = new Point(73, 2);
        txtRatioB.Size     = new Size(55, 24);
        txtRatioB.Font     = fTxt;
        txtRatioB.Text     = "1";

        lblRatioBasis.Text     = "Basis:";
        lblRatioBasis.Location = new Point(0, 30);
        lblRatioBasis.Size     = new Size(45, 20);
        lblRatioBasis.Font     = fLbl;

        cmbRatioBasis.Location    = new Point(48, 28);
        cmbRatioBasis.Size        = new Size(200, 24);
        cmbRatioBasis.Font        = new Font("Segoe UI", 8.5F);
        cmbRatioBasis.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbRatioBasis.Items.AddRange(new object[]
        { "Parallele zur Grundseite", "Durch festen Punkt", "Gegebene Richtung" });
        cmbRatioBasis.SelectedIndex = 0;

        pnlRatio.Controls.AddRange(new Control[]
        { txtRatioA, lblRatioColon, txtRatioB, lblRatioBasis, cmbRatioBasis });

        // A_soll
        int aSollY = 162;
        lblASoll.Text     = "A₁ (Soll) [m²]:";
        lblASoll.Location = new Point(10, aSollY);
        lblASoll.Size     = new Size(110, 22);
        lblASoll.Font     = fLbl;
        txtASoll.Location = new Point(125, aSollY - 2);
        txtASoll.Size     = new Size(120, 24);
        txtASoll.Font     = fTxt;

        // ── Polygon-Tabelle ───────────────────────────────────────────────────
        lblPolygon.Text     = "Polygon-Eckpunkte (R, H):";
        lblPolygon.Location = new Point(10, 198);
        lblPolygon.Size     = new Size(280, 22);
        lblPolygon.Font     = fTit;

        dgvPolygon.Location              = new Point(10, 222);
        dgvPolygon.Size                  = new Size(365, 300);
        dgvPolygon.AllowUserToAddRows    = true;
        dgvPolygon.AllowUserToDeleteRows = true;
        dgvPolygon.RowHeadersWidth       = 40;
        dgvPolygon.Font                  = fTxt;
        dgvPolygon.DefaultCellStyle.Font = fTxt;
        dgvPolygon.CellEndEdit          += dgvPolygon_CellEndEdit;
        dgvPolygon.UserDeletedRow       += dgvPolygon_UserDeletedRow;
        dgvPolygon.ColumnHeadersDefaultCellStyle.Font = fTit;
        dgvPolygon.AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill;

        var colR = new DataGridViewTextBoxColumn { Name = "colR", HeaderText = "R [m]" };
        var colH = new DataGridViewTextBoxColumn { Name = "colH", HeaderText = "H [m]" };
        dgvPolygon.Columns.AddRange(colR, colH);

        lblAGesamt.Location  = new Point(10, 528);
        lblAGesamt.Size      = new Size(365, 22);
        lblAGesamt.Font      = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblAGesamt.Text      = "A_ges = –";
        lblAGesamt.ForeColor = Color.FromArgb(30, 60, 130);

        // ── Ergebnis-Labels ───────────────────────────────────────────────────
        int ry = 558;
        var fRes = new Font("Courier New", 9F);

        lblResA1soll.Text     = "A1 (Soll): –";
        lblResA1soll.Location = new Point(10, ry);
        lblResA1soll.Size     = new Size(365, 20);
        lblResA1soll.Font     = fRes;

        lblResA1ist.Text     = "A1 (Ist):  –";
        lblResA1ist.Location = new Point(10, ry + 22);
        lblResA1ist.Size     = new Size(365, 20);
        lblResA1ist.Font     = fRes;

        lblResA2.Text     = "A2:        –";
        lblResA2.Location = new Point(10, ry + 44);
        lblResA2.Size     = new Size(365, 20);
        lblResA2.Font     = fRes;

        lblResDiff.Text     = "Δ:         –";
        lblResDiff.Location = new Point(10, ry + 66);
        lblResDiff.Size     = new Size(365, 20);
        lblResDiff.Font     = fRes;

        // ── Vorschau ──────────────────────────────────────────────────────────
        mapPreview.Location = new Point(392, 10);
        mapPreview.Size     = new Size(820, 545);

        // ── Grenzpunkte-Tabelle ───────────────────────────────────────────────
        lblGrenzpunkte.Text     = "Neue Grenzpunkte:";
        lblGrenzpunkte.Location = new Point(392, 562);
        lblGrenzpunkte.Size     = new Size(200, 22);
        lblGrenzpunkte.Font     = fTit;

        dgvGrenzpunkte.Location = new Point(392, 585);
        dgvGrenzpunkte.Size     = new Size(820, 110);
        dgvGrenzpunkte.ReadOnly = true;
        dgvGrenzpunkte.AllowUserToAddRows = false;
        dgvGrenzpunkte.RowHeadersWidth    = 30;
        dgvGrenzpunkte.Font               = fTxt;
        dgvGrenzpunkte.DefaultCellStyle.Font = fTxt;
        dgvGrenzpunkte.ColumnHeadersDefaultCellStyle.Font = fTit;
        dgvGrenzpunkte.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        var gpNr = new DataGridViewTextBoxColumn { Name = "colGpNr", HeaderText = "Nr." };
        var gpR  = new DataGridViewTextBoxColumn { Name = "colGpR",  HeaderText = "R [m]" };
        var gpH  = new DataGridViewTextBoxColumn { Name = "colGpH",  HeaderText = "H [m]" };
        dgvGrenzpunkte.Columns.AddRange(gpNr, gpR, gpH);

        // ── Buttons ──────────────────────────────────────────────────────────
        var fBtn = new Font("Segoe UI", 9.5F);
        int by = 710;

        btnLaden.Text      = "Aus DXF laden…";
        btnLaden.Location  = new Point(10, by);
        btnLaden.Size      = new Size(160, 36);
        btnLaden.Font      = fBtn;
        btnLaden.BackColor = Color.FromArgb(215, 228, 248);
        btnLaden.FlatStyle = FlatStyle.Flat;
        btnLaden.Click    += btnLaden_Click;

        btnBerechnen.Text      = "Berechnen";
        btnBerechnen.Location  = new Point(180, by);
        btnBerechnen.Size      = new Size(160, 36);
        btnBerechnen.Font      = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnBerechnen.BackColor = Color.FromArgb(198, 224, 180);
        btnBerechnen.FlatStyle = FlatStyle.Flat;
        btnBerechnen.Click    += btnBerechnen_Click;

        btnAbsteckung.Text      = "→ Absteckung";
        btnAbsteckung.Location  = new Point(392, by);
        btnAbsteckung.Size      = new Size(160, 36);
        btnAbsteckung.Font      = fBtn;
        btnAbsteckung.BackColor = Color.FromArgb(255, 230, 153);
        btnAbsteckung.FlatStyle = FlatStyle.Flat;
        btnAbsteckung.Enabled   = false;
        btnAbsteckung.Click    += btnAbsteckung_Click;

        btnProtokoll.Text      = "Protokoll";
        btnProtokoll.Location  = new Point(562, by);
        btnProtokoll.Size      = new Size(160, 36);
        btnProtokoll.Font      = fBtn;
        btnProtokoll.BackColor = Color.FromArgb(252, 213, 180);
        btnProtokoll.FlatStyle = FlatStyle.Flat;
        btnProtokoll.Enabled   = false;
        btnProtokoll.Click    += btnProtokoll_Click;

        btnSchliessen.Text      = "Schließen";
        btnSchliessen.Location  = new Point(1084, by);
        btnSchliessen.Size      = new Size(128, 36);
        btnSchliessen.Font      = fBtn;
        btnSchliessen.BackColor = Color.FromArgb(238, 238, 238);
        btnSchliessen.FlatStyle = FlatStyle.Flat;
        btnSchliessen.Click    += btnSchliessen_Click;

        Controls.AddRange(new Control[]
        {
            lblVerfahren, cmbVerfahren, lblHint,
            lblEdge, cmbEdge,
            lblVertex, cmbVertex,
            lblFixR, txtFixR, lblFixH, txtFixH,
            lblRichtung, txtRichtung,
            lblBreite, txtBreite,
            lblRatio, pnlRatio,
            lblASoll, txtASoll,
            lblPolygon, dgvPolygon, lblAGesamt,
            lblResA1soll, lblResA1ist, lblResA2, lblResDiff,
            mapPreview,
            lblGrenzpunkte, dgvGrenzpunkte,
            btnLaden, btnBerechnen, btnAbsteckung, btnProtokoll, btnSchliessen,
        });

        ((System.ComponentModel.ISupportInitialize)dgvPolygon).EndInit();
        ((System.ComponentModel.ISupportInitialize)dgvGrenzpunkte).EndInit();
        ResumeLayout(false);
    }

    // ── Felder ────────────────────────────────────────────────────────────────
    private Label        lblVerfahren    = null!;
    private ComboBox     cmbVerfahren    = null!;
    private Label        lblHint         = null!;
    private Label        lblEdge         = null!;
    private ComboBox     cmbEdge         = null!;
    private Label        lblVertex       = null!;
    private ComboBox     cmbVertex       = null!;
    private Label        lblFixR         = null!;
    private TextBox      txtFixR         = null!;
    private Label        lblFixH         = null!;
    private TextBox      txtFixH         = null!;
    private Label        lblRichtung     = null!;
    private TextBox      txtRichtung     = null!;
    private Label        lblBreite       = null!;
    private TextBox      txtBreite       = null!;
    private Label        lblRatio        = null!;
    private Panel        pnlRatio        = null!;
    private TextBox      txtRatioA       = null!;
    private Label        lblRatioColon   = null!;
    private TextBox      txtRatioB       = null!;
    private Label        lblRatioBasis   = null!;
    private ComboBox     cmbRatioBasis   = null!;
    private Label        lblASoll        = null!;
    private TextBox      txtASoll        = null!;
    private Label        lblPolygon      = null!;
    private DataGridView dgvPolygon      = null!;
    private Label        lblAGesamt      = null!;
    private Label        lblResA1soll    = null!;
    private Label        lblResA1ist     = null!;
    private Label        lblResA2        = null!;
    private Label        lblResDiff      = null!;
    private AbsteckungMapPanel mapPreview = null!;
    private Label        lblGrenzpunkte  = null!;
    private DataGridView dgvGrenzpunkte  = null!;
    private Button       btnLaden        = null!;
    private Button       btnBerechnen    = null!;
    private Button       btnAbsteckung   = null!;
    private Button       btnProtokoll    = null!;
    private Button       btnSchliessen   = null!;
}
