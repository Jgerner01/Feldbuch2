namespace Feldbuch;

partial class FormPrismenkonstante
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        nudKonstante  = new NumericUpDown();
        lblStatus     = new Label();
        btnÜbernehmen = new Button();
        _prismaButtons = new Button[5];
        for (int i = 0; i < 5; i++) _prismaButtons[i] = new Button();

        SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)nudKonstante).BeginInit();

        // ── Fenster ──────────────────────────────────────────────────────────
        ClientSize      = new Size(1200, 800);
        Text            = "Prismenkonstante wählen";
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        AutoScaleMode   = AutoScaleMode.Font;
        BackColor       = Color.FromArgb(38, 44, 62);

        var fontNormal = new Font("Segoe UI", 11F);
        var fontBold   = new Font("Segoe UI", 11F, FontStyle.Bold);

        // ── Erklärungs-Label ─────────────────────────────────────────────────
        var lblHinweis = new Label
        {
            Text      = "Leica-Konvention: Standard-Prisma (GPR1) = 0,0 mm  ·  Reflektorlos = +34,4 mm",
            Location  = new Point(12, 10),
            Size      = new Size(1176, 28),
            Font      = new Font("Segoe UI", 10F, FontStyle.Italic),
            ForeColor = Color.FromArgb(160, 180, 220),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter
        };

        // ── Prismen-Buttons (2 Zeilen: 3 + 2) ───────────────────────────────
        // Verfügbare Breite 1200 px:
        //   3 Buttons à 356 px + 2 Lücken à 18 px = 1068 + 36 = 1104 → Rand je 48 px
        const int btnW   = 356;
        const int btnH   = 284;
        const int gapX   = 18;
        const int gapY   = 18;
        const int startY = 46;
        // Zeile 1 – 3 Buttons, linksbündig ab x=48
        const int startX1 = 48;
        // Zeile 2 – 2 Buttons zentriert: (1200 - 2*356 - 18) / 2 = 235
        const int startX2 = 235;

        for (int i = 0; i < 5; i++)
        {
            int row = i < 3 ? 0 : 1;
            int col = i < 3 ? i : i - 3;
            int x   = (row == 0 ? startX1 : startX2) + col * (btnW + gapX);
            int y   = startY + row * (btnH + gapY);

            var btn = _prismaButtons[i];
            btn.Tag      = i;
            btn.Size     = new Size(btnW, btnH);
            btn.Location = new Point(x, y);

            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = Color.FromArgb(52, 64, 90);
            btn.ForeColor = Color.White;
            btn.FlatAppearance.BorderColor = Color.FromArgb(80, 95, 130);
            btn.FlatAppearance.BorderSize  = 1;
            btn.Cursor    = Cursors.Hand;
            btn.Text      = "";   // Alles über Paint
            btn.Paint    += PrismaButton_Paint;
            btn.Click    += PrismaButton_Click;

            string tooltip = $"{_prismen[i].Name}  {_prismen[i].Konstante_mm:+0.0;-0.0;0.0} mm  –  {_prismen[i].Beschreibung}";
            new ToolTip().SetToolTip(btn, tooltip);
        }

        // Layout-Anker nach Buttons: y = startY + 2*btnH + gapY = 46 + 568 + 18 = 632

        // ── Status-Label ─────────────────────────────────────────────────────
        lblStatus.Text      = "";
        lblStatus.Location  = new Point(12, 638);
        lblStatus.Size      = new Size(780, 34);
        lblStatus.Font      = fontBold;
        lblStatus.ForeColor = Color.FromArgb(180, 215, 255);
        lblStatus.BackColor = Color.Transparent;
        lblStatus.TextAlign = ContentAlignment.MiddleLeft;

        // ── Manuell-Eingabe ──────────────────────────────────────────────────
        var lblManuell = new Label
        {
            Text      = "Manuell (mm):",
            Location  = new Point(12, 692),
            Size      = new Size(160, 44),
            Font      = fontNormal,
            ForeColor = Color.FromArgb(180, 200, 240),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft
        };

        nudKonstante.Minimum       = -50m;
        nudKonstante.Maximum       = 50m;
        nudKonstante.DecimalPlaces = 1;
        nudKonstante.Increment     = 0.5m;
        nudKonstante.Value         = 0m;
        nudKonstante.Location      = new Point(178, 694);
        nudKonstante.Size          = new Size(110, 40);
        nudKonstante.Font          = new Font("Segoe UI", 11F);
        nudKonstante.BackColor     = Color.FromArgb(55, 68, 100);
        nudKonstante.ForeColor     = Color.White;
        nudKonstante.ValueChanged += nudKonstante_ValueChanged;
        new ToolTip().SetToolTip(nudKonstante, "Prismenkonstante manuell eingeben [mm]");

        // ── OK-Button ────────────────────────────────────────────────────────
        btnÜbernehmen.Text      = "Übernehmen";
        btnÜbernehmen.Size      = new Size(200, 52);
        btnÜbernehmen.Location  = new Point(988, 686);
        btnÜbernehmen.Font      = fontBold;
        btnÜbernehmen.BackColor = Color.FromArgb(40, 100, 175);
        btnÜbernehmen.ForeColor = Color.White;
        btnÜbernehmen.FlatStyle = FlatStyle.Flat;
        btnÜbernehmen.FlatAppearance.BorderColor = Color.FromArgb(80, 140, 220);
        btnÜbernehmen.Cursor    = Cursors.Hand;
        btnÜbernehmen.Click    += btnÜbernehmen_Click;

        // ── Zusammenbau ──────────────────────────────────────────────────────
        Controls.Add(lblHinweis);
        Controls.Add(lblStatus);
        Controls.Add(lblManuell);
        Controls.Add(nudKonstante);
        Controls.Add(btnÜbernehmen);
        foreach (var b in _prismaButtons) Controls.Add(b);

        AcceptButton = btnÜbernehmen;

        ((System.ComponentModel.ISupportInitialize)nudKonstante).EndInit();
        ResumeLayout(false);
    }

    private NumericUpDown nudKonstante  = null!;
    private Label         lblStatus     = null!;
    private Button        btnÜbernehmen = null!;
}
