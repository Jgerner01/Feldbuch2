namespace Feldbuch;

public partial class FormAufnahmetest
{
    private void InitializeComponent()
    {
        SuspendLayout();

        // ── Formular ──────────────────────────────────────────────────────────
        Text            = "Aufnahmetest – Motorisierter Tachymeter";
        Size            = new Size(820, 560);
        MinimumSize     = new Size(640, 420);
        BackColor       = Color.FromArgb(32, 32, 32);
        ForeColor       = Color.White;
        Font            = new Font("Segoe UI", 9F);
        StartPosition   = FormStartPosition.CenterParent;

        var fntBtn  = new Font("Segoe UI", 9F, FontStyle.Bold);
        var fntSm   = new Font("Segoe UI", 8.5F);
        var colBase = Color.FromArgb(48, 48, 48);

        // ══ obere Leiste ══════════════════════════════════════════════════════
        pnlTop = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 46,
            BackColor = Color.FromArgb(40, 40, 40),
            Padding   = new Padding(6, 5, 6, 0)
        };

        lblVerbindung = new Label
        {
            AutoSize  = false,
            Width     = 260,
            Height    = 20,
            Location  = new Point(6, 6),
            Font      = fntSm,
            ForeColor = Color.FromArgb(120, 220, 120),
            Text      = "Nicht verbunden"
        };

        var lblPNr = new Label
        {
            Text     = "PunktNr:",
            AutoSize = true,
            Location = new Point(6, 28),
            Font     = fntSm,
            ForeColor = Color.FromArgb(190, 190, 190)
        };

        txtPunktNr = new TextBox
        {
            Location  = new Point(64, 25),
            Width     = 70,
            BackColor = Color.FromArgb(55, 55, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font      = fntSm
        };

        var lblCode = new Label
        {
            Text     = "Code:",
            AutoSize = true,
            Location = new Point(140, 28),
            Font     = fntSm,
            ForeColor = Color.FromArgb(190, 190, 190)
        };

        txtCode = new TextBox
        {
            Location  = new Point(180, 25),
            Width     = 70,
            BackColor = Color.FromArgb(55, 55, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font      = fntSm
        };

        btnMessen = new Button
        {
            Text      = "Messen",
            Location  = new Point(260, 9),
            Size      = new Size(80, 30),
            Font      = fntBtn,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(160, 60, 20),
            ForeColor = Color.White,
            Cursor    = Cursors.Hand
        };
        btnMessen.FlatAppearance.BorderColor = Color.FromArgb(120, 45, 15);
        btnMessen.Click += btnMessen_Click;

        lblMessInfo = new Label
        {
            AutoSize  = false,
            Location  = new Point(350, 4),
            Size      = new Size(440, 38),
            Font      = fntSm,
            ForeColor = Color.FromArgb(140, 200, 140),
            Text      = ""
        };

        pnlTop.Controls.AddRange(new Control[]
        {
            lblVerbindung, lblPNr, txtPunktNr, lblCode, txtCode,
            btnMessen, lblMessInfo
        });

        // ══ untere Leiste ══════════════════════════════════════════════════════
        pnlBottom = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 58,
            BackColor = Color.FromArgb(40, 40, 40),
            Padding   = new Padding(6, 6, 6, 4)
        };

        btnZeileHinzu = new Button
        {
            Text      = "+ Zeile",
            Location  = new Point(6, 8),
            Size      = new Size(72, 28),
            Font      = fntSm,
            FlatStyle = FlatStyle.Flat,
            BackColor = colBase,
            ForeColor = Color.White,
            Cursor    = Cursors.Hand
        };
        btnZeileHinzu.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
        btnZeileHinzu.Click += btnZeileHinzu_Click;

        btnZeileLoeschen = new Button
        {
            Text      = "− Zeile",
            Location  = new Point(84, 8),
            Size      = new Size(72, 28),
            Font      = fntSm,
            FlatStyle = FlatStyle.Flat,
            BackColor = colBase,
            ForeColor = Color.White,
            Cursor    = Cursors.Hand
        };
        btnZeileLoeschen.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
        btnZeileLoeschen.Click += btnZeileLoeschen_Click;

        var sep = new Label
        {
            Location  = new Point(168, 4),
            Size      = new Size(1, 48),
            BackColor = Color.FromArgb(70, 70, 70)
        };

        var lblV = new Label
        {
            Text      = "V [gon]:",
            AutoSize  = true,
            Location  = new Point(178, 16),
            Font      = fntSm,
            ForeColor = Color.FromArgb(190, 190, 190)
        };

        nudVGon = new NumericUpDown
        {
            Location       = new Point(242, 12),
            Width          = 80,
            DecimalPlaces  = 4,
            Minimum        = 50m,
            Maximum        = 150m,
            Value          = 100m,
            Increment      = 1m,
            BackColor      = Color.FromArgb(55, 55, 55),
            ForeColor      = Color.White,
            Font           = fntSm
        };

        btnAnfahren = new Button
        {
            Text      = "Anfahren",
            Location  = new Point(330, 8),
            Size      = new Size(90, 28),
            Font      = fntBtn,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(30, 100, 160),
            ForeColor = Color.White,
            Cursor    = Cursors.Hand
        };
        btnAnfahren.FlatAppearance.BorderColor = Color.FromArgb(20, 70, 120);
        btnAnfahren.Click += btnAnfahren_Click;

        btnStopp = new Button
        {
            Text      = "Stopp",
            Location  = new Point(426, 8),
            Size      = new Size(72, 28),
            Font      = fntBtn,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(90, 90, 90),
            ForeColor = Color.White,
            Cursor    = Cursors.Hand
        };
        btnStopp.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70);
        btnStopp.Click += btnStopp_Click;

        lblAnfahrenInfo = new Label
        {
            AutoSize  = false,
            Location  = new Point(506, 4),
            Size      = new Size(190, 48),
            Font      = fntSm,
            ForeColor = Color.FromArgb(140, 180, 220),
            Text      = ""
        };

        btnExportieren = new Button
        {
            Text      = "Exportieren",
            Location  = new Point(702, 8),
            Size      = new Size(96, 28),
            Font      = fntSm,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(40, 90, 50),
            ForeColor = Color.White,
            Cursor    = Cursors.Hand
        };
        btnExportieren.FlatAppearance.BorderColor = Color.FromArgb(30, 70, 40);
        btnExportieren.Click += btnExportieren_Click;

        pnlBottom.Controls.AddRange(new Control[]
        {
            btnZeileHinzu, btnZeileLoeschen, sep,
            lblV, nudVGon, btnAnfahren, btnStopp, lblAnfahrenInfo,
            btnExportieren
        });

        // ══ DataGridView ══════════════════════════════════════════════════════
        dgvPunkte = new DataGridView
        {
            Dock                  = DockStyle.Fill,
            BackgroundColor       = Color.FromArgb(28, 28, 28),
            GridColor             = Color.FromArgb(60, 60, 60),
            BorderStyle           = BorderStyle.None,
            RowHeadersVisible     = false,
            AllowUserToAddRows    = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.None,
            SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect           = false,
            Font                  = new Font("Consolas", 9F),
            ColumnHeadersHeight   = 24,
            RowTemplate           = { Height = 22 }
        };

        // Dunkles Theme
        dgvPunkte.DefaultCellStyle.BackColor      = Color.FromArgb(36, 36, 36);
        dgvPunkte.DefaultCellStyle.ForeColor      = Color.FromArgb(220, 220, 220);
        dgvPunkte.DefaultCellStyle.SelectionBackColor = Color.FromArgb(50, 90, 140);
        dgvPunkte.DefaultCellStyle.SelectionForeColor = Color.White;
        dgvPunkte.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(48, 48, 48);
        dgvPunkte.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(200, 200, 200);
        dgvPunkte.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        dgvPunkte.EnableHeadersVisualStyles = false;

        // ── Zusammenbau ───────────────────────────────────────────────────────
        Controls.Add(dgvPunkte);
        Controls.Add(pnlBottom);
        Controls.Add(pnlTop);

        ResumeLayout(false);
    }

    // ── Felder ────────────────────────────────────────────────────────────────
    private Panel            pnlTop           = null!;
    private Panel            pnlBottom        = null!;
    private Label            lblVerbindung    = null!;
    private TextBox          txtPunktNr       = null!;
    private TextBox          txtCode          = null!;
    private Button           btnMessen        = null!;
    private Label            lblMessInfo      = null!;
    private DataGridView     dgvPunkte        = null!;
    private Button           btnZeileHinzu    = null!;
    private Button           btnZeileLoeschen = null!;
    private NumericUpDown    nudVGon          = null!;
    private Button           btnAnfahren      = null!;
    private Button           btnStopp         = null!;
    private Label            lblAnfahrenInfo  = null!;
    private Button           btnExportieren   = null!;
}
