namespace Feldbuch;

partial class FormKonvertierung
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        // Steuerelemente anlegen
        grid              = new DataGridView();
        colPunktNr        = new DataGridViewTextBoxColumn();
        colTyp            = new DataGridViewTextBoxColumn();
        colR              = new DataGridViewTextBoxColumn();
        colH              = new DataGridViewTextBoxColumn();
        colHoehe          = new DataGridViewTextBoxColumn();
        colHz             = new DataGridViewTextBoxColumn();
        colV              = new DataGridViewTextBoxColumn();
        colStrecke        = new DataGridViewTextBoxColumn();
        colZielhoehe      = new DataGridViewTextBoxColumn();
        colCode           = new DataGridViewTextBoxColumn();
        colBemerkung      = new DataGridViewTextBoxColumn();
        pnlExport         = new Panel();
        pnlImport         = new Panel();
        pnlStatus         = new Panel();
        btnAktualisieren  = new Button();
        btnCsv            = new Button();
        btnKor            = new Button();
        btnDat            = new Button();
        btnSchliessen     = new Button();
        btnDateiOeffnen   = new Button();
        lblImportDatei    = new Label();
        lblFormat         = new Label();
        lblStatus         = new Label();

        SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)grid).BeginInit();
        pnlExport.SuspendLayout();
        pnlImport.SuspendLayout();
        pnlStatus.SuspendLayout();

        // ── Fenster ───────────────────────────────────────────────────────────
        ClientSize    = new Size(1060, 640);
        MinimumSize   = new Size(800, 480);
        Text          = "Konvertierung";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;

        // ── Status-Panel (ganz unten, 24px) ───────────────────────────────────
        pnlStatus.Dock      = DockStyle.Bottom;
        pnlStatus.Height    = 24;
        pnlStatus.BackColor = Color.FromArgb(235, 240, 250);
        pnlStatus.Padding   = new Padding(6, 3, 6, 0);

        lblStatus.Dock      = DockStyle.Fill;
        lblStatus.Font      = new Font("Segoe UI", 8.5F, FontStyle.Italic);
        lblStatus.ForeColor = Color.FromArgb(60, 60, 120);
        lblStatus.Text      = "Bereit.";
        pnlStatus.Controls.Add(lblStatus);

        // ── Import-Panel (über Status, 46px) ──────────────────────────────────
        pnlImport.Dock      = DockStyle.Bottom;
        pnlImport.Height    = 46;
        pnlImport.BackColor = Color.FromArgb(245, 248, 252);
        pnlImport.Padding   = new Padding(8, 7, 8, 7);

        var lblImportCaption = new Label
        {
            Text      = "Import:",
            Location  = new Point(8, 13),
            Size      = new Size(52, 22),
            Font      = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(60, 80, 120)
        };

        btnDateiOeffnen.Text      = "Datei öffnen…";
        btnDateiOeffnen.Size      = new Size(120, 30);
        btnDateiOeffnen.Location  = new Point(66, 8);
        btnDateiOeffnen.Font      = new Font("Segoe UI", 9F);
        btnDateiOeffnen.Click    += btnDateiOeffnen_Click;

        lblImportDatei.Text      = "(keine Datei gewählt)";
        lblImportDatei.Location  = new Point(196, 13);
        lblImportDatei.Size      = new Size(400, 20);
        lblImportDatei.Font      = new Font("Segoe UI", 9F);
        lblImportDatei.ForeColor = Color.FromArgb(80, 80, 80);

        lblFormat.Text      = "";
        lblFormat.Location  = new Point(610, 13);
        lblFormat.Size      = new Size(220, 20);
        lblFormat.Font      = new Font("Segoe UI", 8.5F, FontStyle.Italic);
        lblFormat.ForeColor = Color.FromArgb(100, 100, 140);

        pnlImport.Controls.Add(lblImportCaption);
        pnlImport.Controls.Add(btnDateiOeffnen);
        pnlImport.Controls.Add(lblImportDatei);
        pnlImport.Controls.Add(lblFormat);

        // ── Export-Panel (über Import, 46px) ──────────────────────────────────
        pnlExport.Dock      = DockStyle.Bottom;
        pnlExport.Height    = 46;
        pnlExport.Padding   = new Padding(8, 7, 8, 7);

        btnAktualisieren.Text     = "Feldbuch laden";
        btnAktualisieren.Size     = new Size(120, 30);
        btnAktualisieren.Location = new Point(8, 8);
        btnAktualisieren.Font     = new Font("Segoe UI", 9F);
        btnAktualisieren.Click   += btnAktualisieren_Click;

        // CSV-Button (grün, Icon-Style)
        btnCsv.Text      = "⬇ CSV";
        btnCsv.Size      = new Size(80, 30);
        btnCsv.Anchor    = AnchorStyles.Right;
        btnCsv.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnCsv.BackColor = Color.FromArgb(34, 139, 34);
        btnCsv.ForeColor = Color.White;
        btnCsv.FlatStyle = FlatStyle.Flat;
        btnCsv.FlatAppearance.BorderColor = Color.FromArgb(20, 110, 20);
        btnCsv.Cursor    = Cursors.Hand;
        btnCsv.Click    += btnCsv_Click;

        btnKor.Text      = "KOR";
        btnKor.Size      = new Size(70, 30);
        btnKor.Anchor    = AnchorStyles.Right;
        btnKor.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnKor.BackColor = Color.FromArgb(60, 100, 160);
        btnKor.ForeColor = Color.White;
        btnKor.FlatStyle = FlatStyle.Flat;
        btnKor.FlatAppearance.BorderColor = Color.FromArgb(40, 80, 140);
        btnKor.Click    += btnKor_Click;

        btnDat.Text      = "DAT";
        btnDat.Size      = new Size(70, 30);
        btnDat.Anchor    = AnchorStyles.Right;
        btnDat.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnDat.BackColor = Color.FromArgb(140, 80, 20);
        btnDat.ForeColor = Color.White;
        btnDat.FlatStyle = FlatStyle.Flat;
        btnDat.FlatAppearance.BorderColor = Color.FromArgb(110, 60, 10);
        btnDat.Click    += btnDat_Click;

        btnSchliessen.Text   = "Schließen";
        btnSchliessen.Size   = new Size(90, 30);
        btnSchliessen.Anchor = AnchorStyles.Right;
        btnSchliessen.Font   = new Font("Segoe UI", 9F);
        btnSchliessen.Click += btnSchliessen_Click;

        // Rechts-Buttons werden in Resize positioniert
        pnlExport.Controls.Add(btnAktualisieren);
        pnlExport.Controls.Add(btnCsv);
        pnlExport.Controls.Add(btnKor);
        pnlExport.Controls.Add(btnDat);
        pnlExport.Controls.Add(btnSchliessen);
        pnlExport.Resize += (s, e) => PositioniereExportButtons();

        // ── DataGridView ──────────────────────────────────────────────────────
        grid.Dock                  = DockStyle.Fill;
        grid.AllowUserToAddRows    = false;
        grid.AllowUserToDeleteRows = true;
        grid.RowHeadersVisible     = false;
        grid.AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.None;
        grid.SelectionMode         = DataGridViewSelectionMode.FullRowSelect;
        grid.Font                  = new Font("Consolas", 9F);
        grid.ColumnHeadersHeight   = 28;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 255);
        grid.ColumnHeadersDefaultCellStyle.Font        = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.BackColor   = Color.FromArgb(230, 236, 248);

        colPunktNr.HeaderText  = "Punkt-Nr";  colPunktNr.Width  = 80;
        colTyp.HeaderText      = "Typ";        colTyp.Width      = 90;
        colR.HeaderText        = "R [m]";      colR.Width        = 100;
        colH.HeaderText        = "H [m]";      colH.Width        = 100;
        colHoehe.HeaderText    = "Höhe [m]";   colHoehe.Width    = 80;
        colHz.HeaderText       = "Hz [gon]";   colHz.Width       = 82;
        colV.HeaderText        = "V [gon]";    colV.Width        = 82;
        colStrecke.HeaderText  = "Str. [m]";   colStrecke.Width  = 74;
        colZielhoehe.HeaderText= "Zielh.[m]";  colZielhoehe.Width= 72;
        colCode.HeaderText     = "Code";        colCode.Width     = 60;
        colBemerkung.HeaderText= "Bemerkung";   colBemerkung.Width= 260;

        grid.Columns.AddRange(
            colPunktNr, colTyp, colR, colH, colHoehe,
            colHz, colV, colStrecke, colZielhoehe,
            colCode, colBemerkung);

        // ── Zusammenbau ───────────────────────────────────────────────────────
        Controls.Add(grid);
        Controls.Add(pnlExport);
        Controls.Add(pnlImport);
        Controls.Add(pnlStatus);

        ((System.ComponentModel.ISupportInitialize)grid).EndInit();
        pnlExport.ResumeLayout(false);
        pnlImport.ResumeLayout(false);
        pnlStatus.ResumeLayout(false);
        ResumeLayout(false);

        // Initiale Positionierung der Rechts-Buttons
        Load += (s, e) => PositioniereExportButtons();
    }

    private void PositioniereExportButtons()
    {
        int x = pnlExport.ClientSize.Width - 8;
        foreach (Button b in new[] { btnSchliessen, btnDat, btnKor, btnCsv })
        {
            x -= b.Width;
            b.Location = new Point(x, 8);
            x -= 6;
        }
    }

    // ── Felder ────────────────────────────────────────────────────────────────
    private DataGridView              grid             = null!;
    private DataGridViewTextBoxColumn colPunktNr       = null!;
    private DataGridViewTextBoxColumn colTyp           = null!;
    private DataGridViewTextBoxColumn colR             = null!;
    private DataGridViewTextBoxColumn colH             = null!;
    private DataGridViewTextBoxColumn colHoehe         = null!;
    private DataGridViewTextBoxColumn colHz            = null!;
    private DataGridViewTextBoxColumn colV             = null!;
    private DataGridViewTextBoxColumn colStrecke       = null!;
    private DataGridViewTextBoxColumn colZielhoehe     = null!;
    private DataGridViewTextBoxColumn colCode          = null!;
    private DataGridViewTextBoxColumn colBemerkung     = null!;
    private Panel                     pnlExport        = null!;
    private Panel                     pnlImport        = null!;
    private Panel                     pnlStatus        = null!;
    private Button                    btnAktualisieren = null!;
    private Button                    btnCsv           = null!;
    private Button                    btnKor           = null!;
    private Button                    btnDat           = null!;
    private Button                    btnSchliessen    = null!;
    private Button                    btnDateiOeffnen  = null!;
    private Label                     lblImportDatei   = null!;
    private Label                     lblFormat        = null!;
    private Label                     lblStatus        = null!;
}
