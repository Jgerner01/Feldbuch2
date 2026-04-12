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
        ClientSize      = new Size(512, 330);
        Text            = "Prismenkonstante wählen";
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        AutoScaleMode   = AutoScaleMode.Font;
        BackColor       = Color.FromArgb(38, 44, 62);

        // ── Erklärungs-Label ─────────────────────────────────────────────────
        var lblHinweis = new Label
        {
            Text      = "Leica-Konvention: Standard-Prisma (GPR1) = 0,0 mm  ·  Reflektorlos = +34,4 mm",
            Location  = new Point(10, 8),
            Size      = new Size(492, 18),
            Font      = new Font("Segoe UI", 7.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(160, 180, 220),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter
        };

        // ── Prismen-Buttons (2 × 3) ──────────────────────────────────────────
        // Button-Größe: 156 × 95, Abstände: 8px, Beginn bei x=14, y=30
        const int btnW  = 156;
        const int btnH  = 95;
        const int gapX  = 8;
        const int gapY  = 8;
        const int startX = 14;
        const int startY = 30;

        for (int i = 0; i < 5; i++)
        {
            // Zeile 1 (i=0..2): 3 Buttons; Zeile 2 (i=3..4): 2 Buttons zentriert
            int col = i < 3 ? i : i - 3;
            int row = i < 3 ? 0 : 1;
            // Zweite Zeile: 2 Buttons, um einen halben Buttonabstand eingerückt
            int xOffset = (row == 1) ? (btnW + gapX) / 2 : 0;
            var btn = _prismaButtons[i];

            btn.Tag       = i;
            btn.Size      = new Size(btnW, btnH);
            btn.Location  = new Point(startX + xOffset + col * (btnW + gapX),
                                      startY + row * (btnH + gapY));
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = Color.FromArgb(52, 64, 90);
            btn.ForeColor = Color.White;
            btn.FlatAppearance.BorderColor = Color.FromArgb(80, 95, 130);
            btn.FlatAppearance.BorderSize  = 1;
            btn.Cursor    = Cursors.Hand;
            btn.Text  = "";   // Alles über Paint
            btn.Paint += PrismaButton_Paint;
            btn.Click   += PrismaButton_Click;

            string tooltip = $"{_prismen[i].Name}  {_prismen[i].Konstante_mm:+0.0;-0.0;0.0} mm  –  {_prismen[i].Beschreibung}";
            new ToolTip().SetToolTip(btn, tooltip);
        }

        // ── Status-Label ─────────────────────────────────────────────────────
        lblStatus.Text      = "";
        lblStatus.Location  = new Point(14, 240);
        lblStatus.Size      = new Size(380, 22);
        lblStatus.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
        lblStatus.ForeColor = Color.FromArgb(180, 215, 255);
        lblStatus.BackColor = Color.Transparent;

        // ── Manuell-Eingabe ──────────────────────────────────────────────────
        var lblManuell = new Label
        {
            Text      = "Manuell (mm):",
            Location  = new Point(14, 270),
            Size      = new Size(110, 26),
            Font      = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(180, 200, 240),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft
        };

        nudKonstante.Minimum       = -50m;
        nudKonstante.Maximum       = 50m;
        nudKonstante.DecimalPlaces = 1;
        nudKonstante.Increment     = 0.5m;
        nudKonstante.Value         = 0m;
        nudKonstante.Location      = new Point(130, 268);
        nudKonstante.Size          = new Size(80, 28);
        nudKonstante.Font          = new Font("Segoe UI", 10f);
        nudKonstante.BackColor     = Color.FromArgb(55, 68, 100);
        nudKonstante.ForeColor     = Color.White;
        nudKonstante.ValueChanged += nudKonstante_ValueChanged;
        new ToolTip().SetToolTip(nudKonstante, "Prismenkonstante manuell eingeben [mm]");

        // ── OK-Button ────────────────────────────────────────────────────────
        btnÜbernehmen.Text      = "Übernehmen";
        btnÜbernehmen.Size      = new Size(120, 34);
        btnÜbernehmen.Location  = new Point(378, 262);
        btnÜbernehmen.Font      = new Font("Segoe UI", 10f, FontStyle.Bold);
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

    private NumericUpDown nudKonstante = null!;
    private Label         lblStatus    = null!;
    private Button        btnÜbernehmen = null!;
}
