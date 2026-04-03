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
        lblStandpunkt   = new Label();
        txtStandpunkt   = new TextBox();
        lblInstHoehe    = new Label();
        nudInstHoehe    = new NumericUpDown();
        dgvPunkte           = new DataGridView();
        btnTestdatenLaden   = new Button();
        btnSpeichern        = new Button();
        btnBerechnen        = new Button();
        btnRechenparameter  = new Button();

        SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)nudInstHoehe).BeginInit();
        ((System.ComponentModel.ISupportInitialize)dgvPunkte).BeginInit();

        // Fenster
        ClientSize = new Size(980, 720);
        Text = "Freie Stationierung";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;

        var labelFont  = new Font("Segoe UI", 10F);
        var inputFont  = new Font("Segoe UI", 10F);

        // Standpunktnummer
        lblStandpunkt.Text     = "Standpunktnummer:";
        lblStandpunkt.Location = new Point(20, 22);
        lblStandpunkt.Size     = new Size(145, 24);
        lblStandpunkt.Font     = labelFont;

        txtStandpunkt.Location = new Point(170, 19);
        txtStandpunkt.Size     = new Size(110, 28);
        txtStandpunkt.Font     = inputFont;
        txtStandpunkt.Text     = "9000";

        // Instrumentenhöhe
        lblInstHoehe.Text     = "Instrumentenhöhe (m):";
        lblInstHoehe.Location = new Point(310, 22);
        lblInstHoehe.Size     = new Size(170, 24);
        lblInstHoehe.Font     = labelFont;

        nudInstHoehe.Location     = new Point(485, 19);
        nudInstHoehe.Size         = new Size(100, 28);
        nudInstHoehe.Font         = inputFont;
        nudInstHoehe.Minimum      = 0m;
        nudInstHoehe.Maximum      = 10m;
        nudInstHoehe.DecimalPlaces = 3;
        nudInstHoehe.Increment    = 0.001m;
        nudInstHoehe.Value        = 1.500m;

        // DataGridView
        dgvPunkte.Location                 = new Point(20, 65);
        dgvPunkte.Size                     = new Size(940, 575);
        dgvPunkte.AllowUserToAddRows       = false;
        dgvPunkte.AllowUserToDeleteRows    = false;
        dgvPunkte.SelectionMode            = DataGridViewSelectionMode.CellSelect;
        dgvPunkte.Font                     = new Font("Segoe UI", 9F);
        dgvPunkte.RowHeadersWidth          = 32;
        dgvPunkte.AutoSizeColumnsMode      = DataGridViewAutoSizeColumnsMode.Fill;
        dgvPunkte.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvPunkte.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;

        // Buttons
        btnRechenparameter.Text      = "⚙ Rechenparameter";
        btnRechenparameter.Location  = new Point(20, 655);
        btnRechenparameter.Size      = new Size(200, 38);
        btnRechenparameter.Font      = labelFont;
        btnRechenparameter.Click    += btnRechenparameter_Click;

        btnTestdatenLaden.Text     = "Testdaten laden (Muster.csv)";
        btnTestdatenLaden.Location = new Point(240, 655);
        btnTestdatenLaden.Size     = new Size(220, 38);
        btnTestdatenLaden.Font     = labelFont;
        btnTestdatenLaden.Click   += btnTestdatenLaden_Click;

        btnSpeichern.Text      = "Speichern";
        btnSpeichern.Location  = new Point(575, 655);
        btnSpeichern.Size      = new Size(150, 38);
        btnSpeichern.Font      = labelFont;
        btnSpeichern.Click    += btnSpeichern_Click;

        btnBerechnen.Text     = "Berechnen";
        btnBerechnen.Location = new Point(740, 655);
        btnBerechnen.Size     = new Size(220, 38);
        btnBerechnen.Font     = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnBerechnen.BackColor = Color.SteelBlue;
        btnBerechnen.ForeColor = Color.White;
        btnBerechnen.FlatStyle = FlatStyle.Flat;
        btnBerechnen.Click    += btnBerechnen_Click;

        Controls.AddRange(new Control[]
        {
            lblStandpunkt, txtStandpunkt,
            lblInstHoehe, nudInstHoehe,
            dgvPunkte,
            btnRechenparameter, btnTestdatenLaden, btnSpeichern, btnBerechnen
        });

        ((System.ComponentModel.ISupportInitialize)nudInstHoehe).EndInit();
        ((System.ComponentModel.ISupportInitialize)dgvPunkte).EndInit();
        ResumeLayout(false);
    }

    private Label lblStandpunkt = null!;
    private TextBox txtStandpunkt = null!;
    private Label lblInstHoehe = null!;
    private NumericUpDown nudInstHoehe = null!;
    private DataGridView dgvPunkte = null!;
    private Button btnTestdatenLaden  = null!;
    private Button btnSpeichern       = null!;
    private Button btnBerechnen       = null!;
    private Button btnRechenparameter = null!;
}
