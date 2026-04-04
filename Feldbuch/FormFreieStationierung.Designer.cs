namespace Feldbuch;

partial class FormFreieStationierung
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        lblStandpunkt       = new Label();
        txtStandpunkt       = new TextBox();
        lblInstHoehe        = new Label();
        nudInstHoehe        = new NumericUpDown();
        dgvPunkte           = new DataGridView();
        btnNeuerStandpunkt  = new Button();
        btnPlatz1           = new Button();
        btnPlatz2           = new Button();
        btnRechenparameter  = new Button();
        btnTestdatenLaden   = new Button();
        btnSpeichern        = new Button();
        btnBerechnen        = new Button();

        SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)nudInstHoehe).BeginInit();
        ((System.ComponentModel.ISupportInitialize)dgvPunkte).BeginInit();

        // ── Fenster ───────────────────────────────────────────────────────────
        ClientSize    = new Size(980, 718);
        Text          = "Freie Stationierung";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;

        var labelFont = new Font("Segoe UI", 10F);

        // ── Kopfzeile ─────────────────────────────────────────────────────────
        lblStandpunkt.Text     = "Standpunktnummer:";
        lblStandpunkt.Location = new Point(20, 22);
        lblStandpunkt.Size     = new Size(145, 24);
        lblStandpunkt.Font     = labelFont;

        txtStandpunkt.Location = new Point(170, 19);
        txtStandpunkt.Size     = new Size(110, 28);
        txtStandpunkt.Font     = new Font("Segoe UI", 10F);
        txtStandpunkt.Text     = "9000";

        lblInstHoehe.Text     = "Instrumentenhöhe (m):";
        lblInstHoehe.Location = new Point(310, 22);
        lblInstHoehe.Size     = new Size(170, 24);
        lblInstHoehe.Font     = labelFont;

        nudInstHoehe.Location      = new Point(485, 19);
        nudInstHoehe.Size          = new Size(100, 28);
        nudInstHoehe.Font          = new Font("Segoe UI", 10F);
        nudInstHoehe.Minimum       = 0m;
        nudInstHoehe.Maximum       = 10m;
        nudInstHoehe.DecimalPlaces = 3;
        nudInstHoehe.Increment     = 0.001m;
        nudInstHoehe.Value         = 1.500m;

        // ── Tabelle ───────────────────────────────────────────────────────────
        dgvPunkte.Location                    = new Point(20, 58);
        dgvPunkte.Size                        = new Size(940, 545);
        dgvPunkte.AllowUserToAddRows          = false;
        dgvPunkte.AllowUserToDeleteRows       = false;
        dgvPunkte.SelectionMode               = DataGridViewSelectionMode.CellSelect;
        dgvPunkte.Font                        = new Font("Segoe UI", 9F);
        dgvPunkte.RowHeadersWidth             = 32;
        dgvPunkte.AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill;
        dgvPunkte.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvPunkte.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;

        // ── Button-Zeile 1 ────────────────────────────────────────────────────
        // Positionen: [Neuer Standpunkt] [Platz1] [Platz2]  ...  [Rechenparameter]

        btnNeuerStandpunkt.Text      = "Neuer Standpunkt";
        btnNeuerStandpunkt.Location  = new Point(20, 614);
        btnNeuerStandpunkt.Size      = new Size(195, 36);
        btnNeuerStandpunkt.Font      = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnNeuerStandpunkt.BackColor = Color.FromArgb(220, 120, 20);
        btnNeuerStandpunkt.ForeColor = Color.White;
        btnNeuerStandpunkt.FlatStyle = FlatStyle.Flat;
        btnNeuerStandpunkt.FlatAppearance.BorderColor = Color.FromArgb(180, 90, 10);
        btnNeuerStandpunkt.Visible   = false;   // Sichtbarkeit wird im Konstruktor gesetzt
        btnNeuerStandpunkt.Click    += btnNeuerStandpunkt_Click;

        // Platzhalter für zukünftige Buttons (hidden – Layout reserviert)
        btnPlatz1.Text      = "";
        btnPlatz1.Location  = new Point(225, 614);
        btnPlatz1.Size      = new Size(195, 36);
        btnPlatz1.Font      = labelFont;
        btnPlatz1.Visible   = false;

        btnPlatz2.Text      = "";
        btnPlatz2.Location  = new Point(430, 614);
        btnPlatz2.Size      = new Size(195, 36);
        btnPlatz2.Font      = labelFont;
        btnPlatz2.Visible   = false;

        btnRechenparameter.Text     = "⚙ Rechenparameter";
        btnRechenparameter.Location = new Point(730, 614);
        btnRechenparameter.Size     = new Size(230, 36);
        btnRechenparameter.Font     = labelFont;
        btnRechenparameter.Click   += btnRechenparameter_Click;

        // ── Button-Zeile 2 ────────────────────────────────────────────────────
        // [Testdaten laden]   ...   [Speichern]  [Berechnen]

        btnTestdatenLaden.Text     = "Testdaten laden (Muster.csv)";
        btnTestdatenLaden.Location = new Point(20, 660);
        btnTestdatenLaden.Size     = new Size(240, 36);
        btnTestdatenLaden.Font     = labelFont;
        btnTestdatenLaden.Click   += btnTestdatenLaden_Click;

        btnSpeichern.Text      = "Speichern";
        btnSpeichern.Location  = new Point(500, 660);
        btnSpeichern.Size      = new Size(200, 36);
        btnSpeichern.Font      = labelFont;
        btnSpeichern.Click    += btnSpeichern_Click;

        btnBerechnen.Text      = "Berechnen";
        btnBerechnen.Location  = new Point(710, 660);
        btnBerechnen.Size      = new Size(250, 36);
        btnBerechnen.Font      = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnBerechnen.BackColor = Color.SteelBlue;
        btnBerechnen.ForeColor = Color.White;
        btnBerechnen.FlatStyle = FlatStyle.Flat;
        btnBerechnen.Click    += btnBerechnen_Click;

        Controls.AddRange(new Control[]
        {
            lblStandpunkt, txtStandpunkt,
            lblInstHoehe, nudInstHoehe,
            dgvPunkte,
            btnNeuerStandpunkt, btnPlatz1, btnPlatz2,
            btnRechenparameter,
            btnTestdatenLaden, btnSpeichern, btnBerechnen
        });

        ((System.ComponentModel.ISupportInitialize)nudInstHoehe).EndInit();
        ((System.ComponentModel.ISupportInitialize)dgvPunkte).EndInit();
        ResumeLayout(false);
    }

    private Label          lblStandpunkt      = null!;
    private TextBox        txtStandpunkt      = null!;
    private Label          lblInstHoehe       = null!;
    private NumericUpDown  nudInstHoehe       = null!;
    private DataGridView   dgvPunkte          = null!;
    private Button         btnNeuerStandpunkt = null!;
    private Button         btnPlatz1          = null!;
    private Button         btnPlatz2          = null!;
    private Button         btnRechenparameter = null!;
    private Button         btnTestdatenLaden  = null!;
    private Button         btnSpeichern       = null!;
    private Button         btnBerechnen       = null!;
}
