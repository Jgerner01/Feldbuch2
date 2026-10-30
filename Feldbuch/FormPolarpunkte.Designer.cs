// Zweck:    Oberfläche der Polarpunktberechnung (Layout; Logik in FormPolarpunkte.cs).
// Hardware: keine
// Autor:    Johann Gerner

namespace Feldbuch;

partial class FormPolarpunkte
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        var textFont  = new Font("Segoe UI", 9.5F);
        var gridFont  = new Font("Segoe UI", 9.5F);
        var monoFont  = new Font("Consolas", 10F);
        var btnFont   = new Font("Segoe UI", 9.5F);

        pnlKopf              = new Panel();
        lblStandpunkt        = new Label();
        cmbStandpunkt        = new ComboBox();
        lblInstrHoehe        = new Label();
        txtInstrHoehe        = new TextBox();
        lblQuelle            = new Label();
        rbQuelleStation      = new RadioButton();
        rbQuelleDat          = new RadioButton();
        btnRechenparameter   = new Button();
        btnNeuLaden          = new Button();
        lblHinweise          = new Label();
        splitMain            = new SplitContainer();
        grpAnschluss         = new GroupBox();
        dgvAnschluss         = new DataGridView();
        lblErgebnis          = new Label();
        grpPolar             = new GroupBox();
        dgvPolar             = new DataGridView();
        pnlUnten             = new Panel();
        lblStatus            = new Label();
        btnNeuBerechnen      = new Button();
        btnSpeichern         = new Button();
        btnKorCsv            = new Button();
        btnProtokoll         = new Button();
        btnSchliessen        = new Button();

        ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
        splitMain.Panel1.SuspendLayout();
        splitMain.Panel2.SuspendLayout();
        splitMain.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvAnschluss).BeginInit();
        ((System.ComponentModel.ISupportInitialize)dgvPolar).BeginInit();
        SuspendLayout();

        // ── Fenster ───────────────────────────────────────────────────────────
        Text            = "Polarpunkte";
        ClientSize      = new Size(1180, 780);
        MinimumSize     = new Size(1150, 600);
        StartPosition   = FormStartPosition.CenterParent;
        AutoScaleMode   = AutoScaleMode.Font;
        BackColor       = Color.FromArgb(245, 245, 245);
        Font            = textFont;

        // ── Kopfbereich ───────────────────────────────────────────────────────
        pnlKopf.Dock   = DockStyle.Top;
        pnlKopf.Height = 78;

        lblStandpunkt.Text     = "Standpunkt:";
        lblStandpunkt.Location = new Point(14, 14);
        lblStandpunkt.Size     = new Size(80, 24);
        lblStandpunkt.TextAlign = ContentAlignment.MiddleLeft;

        cmbStandpunkt.Location      = new Point(98, 12);
        cmbStandpunkt.Size          = new Size(130, 26);
        cmbStandpunkt.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbStandpunkt.Font          = monoFont;

        lblInstrHoehe.Text     = "Instrumentenhöhe [m]:";
        lblInstrHoehe.Location = new Point(250, 14);
        lblInstrHoehe.Size     = new Size(150, 24);
        lblInstrHoehe.TextAlign = ContentAlignment.MiddleLeft;

        txtInstrHoehe.Location  = new Point(404, 12);
        txtInstrHoehe.Size      = new Size(80, 26);
        txtInstrHoehe.Font      = monoFont;
        txtInstrHoehe.TextAlign = HorizontalAlignment.Right;

        lblQuelle.Text     = "Orientierung aus:";
        lblQuelle.Location = new Point(14, 46);
        lblQuelle.Size     = new Size(120, 24);
        lblQuelle.TextAlign = ContentAlignment.MiddleLeft;

        rbQuelleStation.Text     = "gespeicherter Station";
        rbQuelleStation.Location = new Point(138, 46);
        rbQuelleStation.AutoSize = true;

        rbQuelleDat.Text     = "neu aus dat.csv";
        rbQuelleDat.Location = new Point(330, 46);
        rbQuelleDat.AutoSize = true;

        btnRechenparameter.Text     = "Rechenparameter…";
        btnRechenparameter.Font     = btnFont;
        btnRechenparameter.Location = new Point(520, 10);
        btnRechenparameter.Size     = new Size(160, 30);

        btnNeuLaden.Text     = "Neu laden";
        btnNeuLaden.Font     = btnFont;
        btnNeuLaden.Location = new Point(690, 10);
        btnNeuLaden.Size     = new Size(100, 30);

        pnlKopf.Controls.AddRange(new Control[]
        {
            lblStandpunkt, cmbStandpunkt, lblInstrHoehe, txtInstrHoehe,
            lblQuelle, rbQuelleStation, rbQuelleDat, btnRechenparameter, btnNeuLaden
        });

        // ── Hinweisbereich ────────────────────────────────────────────────────
        lblHinweise.Dock      = DockStyle.Top;
        lblHinweise.AutoSize  = false;
        lblHinweise.Height    = 0;
        lblHinweise.Padding   = new Padding(14, 4, 14, 4);
        lblHinweise.BackColor = Color.FromArgb(255, 244, 200);
        lblHinweise.ForeColor = Color.FromArgb(90, 60, 0);
        lblHinweise.Visible   = false;

        // ── Geteilter Hauptbereich ────────────────────────────────────────────
        splitMain.Dock        = DockStyle.Fill;
        splitMain.Orientation = Orientation.Horizontal;
        splitMain.Panel1MinSize = 120;
        splitMain.Panel2MinSize = 120;

        // Obere Tabelle: Orientierung / Anschlusspunkte
        grpAnschluss.Text = "Orientierung / Anschlusspunkte";
        grpAnschluss.Dock = DockStyle.Fill;
        grpAnschluss.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

        KonfiguriereGrid(dgvAnschluss, gridFont);
        dgvAnschluss.Columns.AddRange(new DataGridViewColumn[]
        {
            TextSpalte(COL_NR,        "Punkt",          14),
            TextSpalte(COL_STRECKEH,  "Str.h [m]",      11),
            TextSpalte(COL_VWINKEL,   "v Winkel [cc]",  12),
            TextSpalte(COL_VQUER,     "v Quer [mm]",    11),
            CheckSpalte(COL_AKT_HZ,   "Hz",              6),
            TextSpalte(COL_VLAENGS,   "v Längs [mm]",   11),
            CheckSpalte(COL_AKT_STR,  "Str",             6),
            TextSpalte(COL_VHOEHE,    "v Höhe [mm]",    11),
            CheckSpalte(COL_AKT_HOE,  "Hoe",             6),
            TextSpalte(COL_GEWICHT,   "Gewicht",        10)
        });

        lblErgebnis.Dock      = DockStyle.Bottom;
        lblErgebnis.AutoSize  = false;
        lblErgebnis.Height    = 46;
        lblErgebnis.Font      = monoFont;
        lblErgebnis.Padding   = new Padding(6, 4, 6, 2);
        lblErgebnis.BackColor = Color.FromArgb(232, 238, 248);
        lblErgebnis.TextAlign = ContentAlignment.MiddleLeft;

        grpAnschluss.Controls.Add(dgvAnschluss);
        grpAnschluss.Controls.Add(lblErgebnis);
        splitMain.Panel1.Controls.Add(grpAnschluss);

        // Untere Tabelle: Polarpunkte
        grpPolar.Text = "Polarpunkte";
        grpPolar.Dock = DockStyle.Fill;
        grpPolar.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

        KonfiguriereGrid(dgvPolar, gridFont);
        dgvPolar.Columns.AddRange(new DataGridViewColumn[]
        {
            CheckSpalte(COL_P_BERECHNEN, "Berechnen", 8),
            TextSpalte(COL_P_NR,      "Punkt",          9),
            TextSpalte(COL_P_HZ,      "Hz [gon]",       9),
            TextSpalte(COL_P_V,       "V [gon]",        9),
            TextSpalte(COL_P_S,       "Strecke [m]",    9),
            TextSpalte(COL_P_ZH,      "ZH [m]",         6),
            EditierbareSpalte(COL_P_EXZL, "Exz längs [m]", 8),
            EditierbareSpalte(COL_P_EXZQ, "Exz quer [m]",  8),
            TextSpalte(COL_P_R,       "R [m]",         11),
            TextSpalte(COL_P_H,       "H [m]",         11),
            TextSpalte(COL_P_HOEHE,   "Höhe [m]",       8),
            TextSpalte(COL_P_CODE,    "Code",           6),
            TextSpalte(COL_P_ZEIT,    "Zeit",          11)
        });

        grpPolar.Controls.Add(dgvPolar);
        splitMain.Panel2.Controls.Add(grpPolar);

        // ── Fußbereich ────────────────────────────────────────────────────────
        pnlUnten.Dock   = DockStyle.Bottom;
        pnlUnten.Height = 54;

        lblStatus.Location  = new Point(14, 8);
        lblStatus.Size      = new Size(420, 38);
        lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        lblStatus.Anchor    = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;

        btnNeuBerechnen.Text = "Neu berechnen";
        btnNeuBerechnen.Size = new Size(130, 36);
        btnSpeichern.Text    = "Speichern";
        btnSpeichern.Size    = new Size(110, 36);
        btnKorCsv.Text       = "Als KOR-CSV speichern";
        btnKorCsv.Size       = new Size(180, 36);
        btnProtokoll.Text    = "Protokoll anzeigen";
        btnProtokoll.Size    = new Size(150, 36);
        btnSchliessen.Text   = "Schließen";
        btnSchliessen.Size   = new Size(100, 36);

        foreach (var b in new[] { btnNeuBerechnen, btnSpeichern, btnKorCsv, btnProtokoll, btnSchliessen })
        {
            b.Font   = btnFont;
            b.Anchor = AnchorStyles.Left | AnchorStyles.Top;      // Lage setzt PositioniereButtons() je nach Panelbreite
        }
        btnSpeichern.BackColor = Color.FromArgb(46, 125, 50);
        btnSpeichern.ForeColor = Color.White;
        btnSpeichern.FlatStyle = FlatStyle.Flat;

        // Positionen von rechts (Anchor Right: bei Größenänderung mitgeführt)
        int x = ClientSize.Width - 14;
        btnSchliessen.Location   = new Point(x - btnSchliessen.Width, 9);   x -= btnSchliessen.Width + 8;
        btnKorCsv.Location       = new Point(x - btnKorCsv.Width, 9);       x -= btnKorCsv.Width + 8;
        btnProtokoll.Location    = new Point(x - btnProtokoll.Width, 9);    x -= btnProtokoll.Width + 8;
        btnSpeichern.Location    = new Point(x - btnSpeichern.Width, 9);    x -= btnSpeichern.Width + 8;
        btnNeuBerechnen.Location = new Point(x - btnNeuBerechnen.Width, 9);

        pnlUnten.Controls.AddRange(new Control[]
        {
            lblStatus, btnNeuBerechnen, btnSpeichern, btnKorCsv, btnProtokoll, btnSchliessen
        });

        // Reihenfolge: Fill-Steuerelement zuerst, Dock-Ränder danach
        Controls.Add(splitMain);
        Controls.Add(lblHinweise);
        Controls.Add(pnlKopf);
        Controls.Add(pnlUnten);

        AcceptButton = null;

        ((System.ComponentModel.ISupportInitialize)dgvAnschluss).EndInit();
        ((System.ComponentModel.ISupportInitialize)dgvPolar).EndInit();
        splitMain.Panel1.ResumeLayout(false);
        splitMain.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
        splitMain.ResumeLayout(false);
        ResumeLayout(false);
    }

    // ── Spaltennamen ──────────────────────────────────────────────────────────
    private const string COL_NR       = "PunktNr";
    private const string COL_STRECKEH = "StreckeH";
    private const string COL_VWINKEL  = "vWinkel";
    private const string COL_VQUER    = "vQuer";
    private const string COL_AKT_HZ   = "colAktivHz";
    private const string COL_VLAENGS  = "vLaengs";
    private const string COL_AKT_STR  = "colAktivStr";
    private const string COL_VHOEHE   = "vHoehe";
    private const string COL_AKT_HOE  = "colAktivHoehe";
    private const string COL_GEWICHT  = "Gewicht";

    private const string COL_P_BERECHNEN = "colBerechnen";
    private const string COL_P_NR     = "pPunktNr";
    private const string COL_P_HZ     = "pHz";
    private const string COL_P_V      = "pV";
    private const string COL_P_S      = "pStrecke";
    private const string COL_P_ZH     = "pZh";
    private const string COL_P_EXZL   = "pExzL";
    private const string COL_P_EXZQ   = "pExzQ";
    private const string COL_P_R      = "pR";
    private const string COL_P_H      = "pH";
    private const string COL_P_HOEHE  = "pHoehe";
    private const string COL_P_CODE   = "pCode";
    private const string COL_P_ZEIT   = "pZeit";

    // ── Hilfen für Tabellen ───────────────────────────────────────────────────
    private static void KonfiguriereGrid(DataGridView dgv, Font font)
    {
        dgv.Dock                      = DockStyle.Fill;
        dgv.Font                      = font;
        dgv.AllowUserToAddRows        = false;
        dgv.AllowUserToDeleteRows     = false;
        dgv.AllowUserToResizeRows     = false;
        dgv.RowHeadersVisible         = false;
        dgv.BackgroundColor           = Color.White;
        dgv.SelectionMode             = DataGridViewSelectionMode.CellSelect;
        dgv.AutoSizeColumnsMode       = DataGridViewAutoSizeColumnsMode.Fill;
        dgv.ColumnHeadersHeight       = 32;
        dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dgv.RowTemplate.Height        = 26;
        dgv.EnableHeadersVisualStyles = false;
        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(60, 100, 160);
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
        dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgv.DefaultCellStyle.Alignment              = DataGridViewContentAlignment.MiddleRight;
    }

    private static DataGridViewTextBoxColumn TextSpalte(string name, string kopf, float gewicht)
        => new() { Name = name, HeaderText = kopf, ReadOnly = true, FillWeight = gewicht, SortMode = DataGridViewColumnSortMode.NotSortable };

    private static DataGridViewTextBoxColumn EditierbareSpalte(string name, string kopf, float gewicht)
        => new() { Name = name, HeaderText = kopf, ReadOnly = false, FillWeight = gewicht, SortMode = DataGridViewColumnSortMode.NotSortable };

    private static DataGridViewCheckBoxColumn CheckSpalte(string name, string kopf, float gewicht)
        => new() { Name = name, HeaderText = kopf, FillWeight = gewicht, SortMode = DataGridViewColumnSortMode.NotSortable };

    // ── Steuerelemente ────────────────────────────────────────────────────────
    private Panel          pnlKopf            = null!;
    private Label          lblStandpunkt      = null!;
    private ComboBox       cmbStandpunkt      = null!;
    private Label          lblInstrHoehe      = null!;
    private TextBox        txtInstrHoehe      = null!;
    private Label          lblQuelle          = null!;
    private RadioButton    rbQuelleStation    = null!;
    private RadioButton    rbQuelleDat        = null!;
    private Button         btnRechenparameter = null!;
    private Button         btnNeuLaden        = null!;
    private Label          lblHinweise        = null!;
    private SplitContainer splitMain          = null!;
    private GroupBox       grpAnschluss       = null!;
    private DataGridView   dgvAnschluss       = null!;
    private Label          lblErgebnis        = null!;
    private GroupBox       grpPolar           = null!;
    private DataGridView   dgvPolar           = null!;
    private Panel          pnlUnten           = null!;
    private Label          lblStatus          = null!;
    private Button         btnNeuBerechnen    = null!;
    private Button         btnSpeichern       = null!;
    private Button         btnKorCsv          = null!;
    private Button         btnProtokoll       = null!;
    private Button         btnSchliessen      = null!;
}
