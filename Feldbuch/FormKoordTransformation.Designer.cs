namespace Feldbuch;

partial class FormKoordTransformation
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        pnlTop            = new Panel();
        lblTitel          = new Label();
        lblTyp            = new Label();
        cmbTyp            = new ComboBox();
        chkFesterMassstab = new CheckBox();
        btnBerechnen      = new Button();
        btnTransformieren = new Button();
        btnProtokoll      = new Button();
        btnExportKor      = new Button();
        splitH          = new SplitContainer();
        // Ausgangssystem (links → Quelle)
        pnlQuelle       = new Panel();
        lblQuelle       = new Label();
        dgvQuelle       = new DataGridView();
        pnlQuelleTools  = new Panel();
        btnImportQuelleKor  = new Button();
        btnImportQuelleCsv  = new Button();
        btnLoeschenQuelle   = new Button();
        // Zielsystem (rechts → Ziel)
        pnlZiel         = new Panel();
        lblZiel         = new Label();
        dgvZiel         = new DataGridView();
        pnlZielTools    = new Panel();
        btnImportZielKor    = new Button();
        btnImportZielCsv    = new Button();
        btnLoeschenZiel     = new Button();
        btnPunkteZuordnen   = new Button();
        // Ergebnis-Panel
        pnlErgebnis     = new Panel();
        lblErgebnisTitel= new Label();
        lblErgebnisInfo = new Label();
        dgvResiduen     = new DataGridView();
        splitter        = new Splitter();

        SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitH).BeginInit();
        splitH.Panel1.SuspendLayout();
        splitH.Panel2.SuspendLayout();
        splitH.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvQuelle).BeginInit();
        ((System.ComponentModel.ISupportInitialize)dgvZiel).BeginInit();
        ((System.ComponentModel.ISupportInitialize)dgvResiduen).BeginInit();

        // ── Fenster ───────────────────────────────────────────────────────────
        ClientSize    = new Size(1280, 820);
        Text          = "Koordinatentransformation";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        MinimumSize   = new Size(960, 600);
        BackColor     = Color.FromArgb(240, 242, 248);

        // ── Toolbar oben (56px) ───────────────────────────────────────────────
        pnlTop.Dock      = DockStyle.Top;
        pnlTop.Height    = 56;
        pnlTop.BackColor = Color.FromArgb(42, 72, 130);
        pnlTop.Padding   = new Padding(10, 0, 10, 0);

        lblTitel.Text      = "Koordinatentransformation";
        lblTitel.Font      = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblTitel.ForeColor = Color.White;
        lblTitel.AutoSize  = true;
        lblTitel.Location  = new Point(12, 14);

        lblTyp.Text      = "Typ:";
        lblTyp.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblTyp.ForeColor = Color.FromArgb(185, 205, 240);
        lblTyp.AutoSize  = true;
        lblTyp.Location  = new Point(280, 18);

        cmbTyp.Items.AddRange(new object[]
        {
            "2D Helmert  (4 Parameter)",
            "3D Helmert  (7 Parameter)",
            "7-Parameter  (Bursa-Wolf)",
            "9-Parameter  (getrennte Maßstäbe)"
        });
        cmbTyp.SelectedIndex  = 0;
        cmbTyp.DropDownStyle  = ComboBoxStyle.DropDownList;
        cmbTyp.Size           = new Size(260, 28);
        cmbTyp.Location       = new Point(314, 14);
        cmbTyp.Font           = new Font("Segoe UI", 9.5F);

        chkFesterMassstab.Text      = "Maßstab fest (m = 1)";
        chkFesterMassstab.Font      = new Font("Segoe UI", 8.5F);
        chkFesterMassstab.ForeColor = Color.FromArgb(210, 225, 255);
        chkFesterMassstab.Location  = new Point(592, 19);
        chkFesterMassstab.AutoSize  = true;
        chkFesterMassstab.Cursor    = Cursors.Hand;

        btnBerechnen.Text      = "▶  Berechnen";
        btnBerechnen.Size      = new Size(130, 34);
        btnBerechnen.Location  = new Point(790, 11);
        btnBerechnen.Font      = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnBerechnen.BackColor = Color.FromArgb(160, 60, 20);
        btnBerechnen.ForeColor = Color.White;
        btnBerechnen.FlatStyle = FlatStyle.Flat;
        btnBerechnen.FlatAppearance.BorderColor = Color.FromArgb(120, 40, 10);
        btnBerechnen.Cursor    = Cursors.Hand;
        btnBerechnen.Click    += btnBerechnen_Click;

        btnTransformieren.Text      = "Transformieren →";
        btnTransformieren.Size      = new Size(132, 34);
        btnTransformieren.Location  = new Point(932, 11);
        btnTransformieren.Font      = new Font("Segoe UI", 9F);
        btnTransformieren.BackColor = Color.FromArgb(38, 110, 72);
        btnTransformieren.ForeColor = Color.White;
        btnTransformieren.FlatStyle = FlatStyle.Flat;
        btnTransformieren.FlatAppearance.BorderColor = Color.FromArgb(26, 90, 56);
        btnTransformieren.Cursor    = Cursors.Hand;
        btnTransformieren.Enabled   = false;
        btnTransformieren.Click    += btnTransformieren_Click;

        btnProtokoll.Text      = "Protokoll";
        btnProtokoll.Size      = new Size(90, 34);
        btnProtokoll.Location  = new Point(1076, 11);
        btnProtokoll.Font      = new Font("Segoe UI", 9F);
        btnProtokoll.BackColor = Color.FromArgb(55, 88, 150);
        btnProtokoll.ForeColor = Color.White;
        btnProtokoll.FlatStyle = FlatStyle.Flat;
        btnProtokoll.FlatAppearance.BorderColor = Color.FromArgb(36, 65, 120);
        btnProtokoll.Cursor    = Cursors.Hand;
        btnProtokoll.Enabled   = false;
        btnProtokoll.Click    += btnProtokoll_Click;

        btnExportKor.Text      = "Export KOR";
        btnExportKor.Size      = new Size(90, 34);
        btnExportKor.Location  = new Point(1178, 11);
        btnExportKor.Font      = new Font("Segoe UI", 9F);
        btnExportKor.BackColor = Color.FromArgb(40, 100, 60);
        btnExportKor.ForeColor = Color.White;
        btnExportKor.FlatStyle = FlatStyle.Flat;
        btnExportKor.FlatAppearance.BorderColor = Color.FromArgb(26, 78, 44);
        btnExportKor.Cursor    = Cursors.Hand;
        btnExportKor.Enabled   = false;
        btnExportKor.Click    += btnExportKor_Click;

        pnlTop.Controls.AddRange(new Control[]
            { lblTitel, lblTyp, cmbTyp, chkFesterMassstab,
              btnBerechnen, btnTransformieren, btnProtokoll, btnExportKor });

        // ── SplitContainer (Quelle links, Ziel rechts) ────────────────────────
        splitH.Dock          = DockStyle.Fill;
        splitH.Orientation   = Orientation.Vertical;
        splitH.SplitterWidth = 6;
        splitH.BackColor     = Color.FromArgb(180, 185, 200);

        // ─── Panel Quelle (Ausgangssystem) ────────────────────────────────────
        pnlQuelle.Dock      = DockStyle.Fill;
        pnlQuelle.Padding   = new Padding(4);
        pnlQuelle.BackColor = Color.FromArgb(240, 242, 248);

        lblQuelle.Text      = "AUSGANGSSYSTEM  (Quelle)";
        lblQuelle.Font      = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblQuelle.ForeColor = Color.FromArgb(52, 100, 175);
        lblQuelle.Dock      = DockStyle.Top;
        lblQuelle.Height    = 22;
        lblQuelle.TextAlign = ContentAlignment.MiddleLeft;
        lblQuelle.Padding   = new Padding(2, 0, 0, 0);

        DgvStyle(dgvQuelle);
        dgvQuelle.Dock = DockStyle.Fill;

        pnlQuelleTools.Dock      = DockStyle.Bottom;
        pnlQuelleTools.Height    = 38;
        pnlQuelleTools.BackColor = Color.FromArgb(228, 231, 240);

        ToolBtn(btnImportQuelleKor, "↓ KOR",   4,  3, Color.FromArgb(55, 88, 150));
        ToolBtn(btnImportQuelleCsv, "↓ CSV",  86,  3, Color.FromArgb(55, 88, 150));
        ToolBtn(btnLoeschenQuelle,  "Löschen",168, 3, Color.FromArgb(140, 30, 30));
        btnImportQuelleKor.Click += (s,e) => ImportKor(isQuelle: true);
        btnImportQuelleCsv.Click += (s,e) => ImportCsv(isQuelle: true);
        btnLoeschenQuelle.Click  += (s,e) => DgvLeeren(dgvQuelle);
        pnlQuelleTools.Controls.AddRange(new Control[]
            { btnImportQuelleKor, btnImportQuelleCsv, btnLoeschenQuelle });

        pnlQuelle.Controls.Add(dgvQuelle);
        pnlQuelle.Controls.Add(lblQuelle);
        pnlQuelle.Controls.Add(pnlQuelleTools);
        splitH.Panel1.Controls.Add(pnlQuelle);

        // ─── Panel Ziel (Zielsystem) ──────────────────────────────────────────
        pnlZiel.Dock      = DockStyle.Fill;
        pnlZiel.Padding   = new Padding(4);
        pnlZiel.BackColor = Color.FromArgb(240, 242, 248);

        lblZiel.Text      = "ZIELSYSTEM  (Soll)";
        lblZiel.Font      = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblZiel.ForeColor = Color.FromArgb(36, 100, 60);
        lblZiel.Dock      = DockStyle.Top;
        lblZiel.Height    = 22;
        lblZiel.TextAlign = ContentAlignment.MiddleLeft;
        lblZiel.Padding   = new Padding(2, 0, 0, 0);

        DgvStyle(dgvZiel);
        dgvZiel.Dock = DockStyle.Fill;

        pnlZielTools.Dock      = DockStyle.Bottom;
        pnlZielTools.Height    = 38;
        pnlZielTools.BackColor = Color.FromArgb(228, 231, 240);

        ToolBtn(btnImportZielKor,   "↓ KOR",   4,  3, Color.FromArgb(36, 100, 60));
        ToolBtn(btnImportZielCsv,   "↓ CSV",  86,  3, Color.FromArgb(36, 100, 60));
        ToolBtn(btnLoeschenZiel,    "Löschen",168, 3, Color.FromArgb(140, 30, 30));
        ToolBtn(btnPunkteZuordnen,  "Zuordnen ↔", 254, 3, Color.FromArgb(100, 70, 0));
        btnImportZielKor.Click    += (s,e) => ImportKor(isQuelle: false);
        btnImportZielCsv.Click    += (s,e) => ImportCsv(isQuelle: false);
        btnLoeschenZiel.Click     += (s,e) => DgvLeeren(dgvZiel);
        btnPunkteZuordnen.Click   += btnPunkteZuordnen_Click;
        new ToolTip().SetToolTip(btnPunkteZuordnen,
            "Punkte nach Punktnummer automatisch zuordnen (Reihenfolge angleichen)");

        pnlZielTools.Controls.AddRange(new Control[]
            { btnImportZielKor, btnImportZielCsv, btnLoeschenZiel, btnPunkteZuordnen });

        pnlZiel.Controls.Add(dgvZiel);
        pnlZiel.Controls.Add(lblZiel);
        pnlZiel.Controls.Add(pnlZielTools);
        splitH.Panel2.Controls.Add(pnlZiel);

        // ── Ergebnis-Panel (unten, Splitter) ──────────────────────────────────
        splitter.Dock     = DockStyle.Bottom;
        splitter.Height   = 5;
        splitter.BackColor= Color.FromArgb(180, 185, 200);
        splitter.Cursor   = Cursors.HSplit;

        pnlErgebnis.Dock      = DockStyle.Bottom;
        pnlErgebnis.Height    = 0;    // wird nach Berechnung eingeblendet
        pnlErgebnis.BackColor = Color.FromArgb(230, 233, 242);
        pnlErgebnis.Padding   = new Padding(8, 4, 8, 4);

        lblErgebnisTitel.Text      = "ERGEBNIS";
        lblErgebnisTitel.Font      = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblErgebnisTitel.ForeColor = Color.FromArgb(42, 72, 130);
        lblErgebnisTitel.Dock      = DockStyle.Top;
        lblErgebnisTitel.Height    = 20;

        lblErgebnisInfo.Text      = "";
        lblErgebnisInfo.Font      = new Font("Consolas", 8.5F);
        lblErgebnisInfo.ForeColor = Color.FromArgb(30, 30, 30);
        lblErgebnisInfo.Dock      = DockStyle.Top;
        lblErgebnisInfo.Height    = 52;

        DgvStyle(dgvResiduen);
        dgvResiduen.Dock               = DockStyle.Fill;
        dgvResiduen.AllowUserToAddRows = false;

        pnlErgebnis.Controls.Add(dgvResiduen);
        pnlErgebnis.Controls.Add(lblErgebnisInfo);
        pnlErgebnis.Controls.Add(lblErgebnisTitel);

        // ── Zusammenbau ───────────────────────────────────────────────────────
        Controls.Add(splitH);
        Controls.Add(splitter);
        Controls.Add(pnlErgebnis);
        Controls.Add(pnlTop);

        Shown += (s,e) =>
        {
            splitH.Panel1MinSize    = 300;
            splitH.Panel2MinSize    = 300;
            splitH.SplitterDistance = (splitH.Width - splitH.SplitterWidth) / 2;
        };

        ((System.ComponentModel.ISupportInitialize)splitH).EndInit();
        splitH.Panel1.ResumeLayout(false);
        splitH.Panel2.ResumeLayout(false);
        splitH.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvQuelle).EndInit();
        ((System.ComponentModel.ISupportInitialize)dgvZiel).EndInit();
        ((System.ComponentModel.ISupportInitialize)dgvResiduen).EndInit();
        ResumeLayout(false);
    }

    // ── Hilfsmethoden Designer ────────────────────────────────────────────────
    private static void DgvStyle(DataGridView dgv)
    {
        dgv.AllowUserToAddRows          = true;
        dgv.AllowUserToDeleteRows       = true;
        dgv.SelectionMode               = DataGridViewSelectionMode.FullRowSelect;
        dgv.Font                        = new Font("Consolas", 9F);
        dgv.RowHeadersWidth             = 28;
        dgv.AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill;
        dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 253);
        dgv.BackgroundColor             = Color.White;
        dgv.BorderStyle                 = BorderStyle.None;
        dgv.GridColor                   = Color.FromArgb(210, 215, 230);
    }

    private static void ToolBtn(Button b, string text, int x, int y, Color back)
    {
        b.Text      = text;
        b.Size      = new Size(78, 30);
        b.Location  = new Point(x, y);
        b.Font      = new Font("Segoe UI", 8.5F);
        b.BackColor = back;
        b.ForeColor = Color.White;
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderColor = Color.FromArgb(
            Math.Max(0, back.R - 20),
            Math.Max(0, back.G - 20),
            Math.Max(0, back.B - 20));
        b.Cursor = Cursors.Hand;
    }

    // ── Felder ────────────────────────────────────────────────────────────────
    private Panel            pnlTop          = null!;
    private Label            lblTitel        = null!;
    private Label            lblTyp          = null!;
    private ComboBox         cmbTyp          = null!;
    private CheckBox         chkFesterMassstab   = null!;
    private Button           btnBerechnen        = null!;
    private Button           btnTransformieren   = null!;
    private Button           btnProtokoll        = null!;
    private Button           btnExportKor        = null!;
    private SplitContainer   splitH          = null!;
    private Panel            pnlQuelle       = null!;
    private Label            lblQuelle       = null!;
    private DataGridView     dgvQuelle       = null!;
    private Panel            pnlQuelleTools  = null!;
    private Button           btnImportQuelleKor  = null!;
    private Button           btnImportQuelleCsv  = null!;
    private Button           btnLoeschenQuelle   = null!;
    private Panel            pnlZiel         = null!;
    private Label            lblZiel         = null!;
    private DataGridView     dgvZiel         = null!;
    private Panel            pnlZielTools    = null!;
    private Button           btnImportZielKor    = null!;
    private Button           btnImportZielCsv    = null!;
    private Button           btnLoeschenZiel     = null!;
    private Button           btnPunkteZuordnen   = null!;
    private Panel            pnlErgebnis     = null!;
    private Label            lblErgebnisTitel= null!;
    private Label            lblErgebnisInfo = null!;
    private DataGridView     dgvResiduen     = null!;
    private Splitter         splitter        = null!;
}
