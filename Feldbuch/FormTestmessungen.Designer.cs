namespace Feldbuch;

partial class FormTestmessungen
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        grpStatus       = new GroupBox();
        lblStatusDot    = new Label();
        lblStatusText   = new Label();
        lblProtokollHinweis = new Label();

        grpDaten        = new GroupBox();
        txtDaten        = new RichTextBox();
        btnClear        = new Button();

        grpSteuerung    = new GroupBox();
        btnMessung      = new Button();
        btnModus        = new Button();
        btnWinkel       = new Button();
        btnLaser        = new Button();

        grpMesswerte    = new GroupBox();
        lblHzLabel      = new Label();
        lblHz           = new Label();
        lblVLabel       = new Label();
        lblV            = new Label();
        lblSdLabel      = new Label();
        lblSd           = new Label();
        lblHdLabel      = new Label();
        lblHd           = new Label();
        lblDhLabel      = new Label();
        lblDh           = new Label();

        grpLibelle      = new GroupBox();
        pnlLibelle      = new DosenlibellControl();
        lblNeigung      = new Label();
        lblPPM          = new Label();
        btnLibelle      = new Button();

        SuspendLayout();

        var fontNormal = new Font("Segoe UI", 11F);
        var fontMono   = new Font("Consolas", 10F);
        var fontBold   = new Font("Segoe UI", 11F, FontStyle.Bold);
        var bgColor    = Color.FromArgb(244, 246, 250);

        // ── Fenster ───────────────────────────────────────────────────────────
        ClientSize      = new Size(1200, 800);
        Text            = "Messwerte / Testmessungen";
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        AutoScaleMode   = AutoScaleMode.Font;
        BackColor       = bgColor;

        // ═══ GroupBox: Verbindungsstatus (volle Breite) ════════════════════════
        grpStatus.Text      = "Verbindungsstatus";
        grpStatus.Location  = new Point(12, 8);
        grpStatus.Size      = new Size(1176, 56);
        grpStatus.Font      = fontNormal;
        grpStatus.BackColor = bgColor;

        lblStatusDot.Location    = new Point(16, 22);
        lblStatusDot.Size        = new Size(16, 16);
        lblStatusDot.BackColor   = Color.FromArgb(200, 50, 50);
        lblStatusDot.BorderStyle = BorderStyle.FixedSingle;

        lblStatusText.Location  = new Point(40, 18);
        lblStatusText.Size      = new Size(700, 26);
        lblStatusText.Font      = fontBold;
        lblStatusText.ForeColor = Color.FromArgb(160, 40, 40);
        lblStatusText.Text      = "Kein Tachymeter verbunden";

        lblProtokollHinweis.Location  = new Point(760, 18);
        lblProtokollHinweis.Size      = new Size(400, 26);
        lblProtokollHinweis.Font      = new Font("Segoe UI", 9.5F, FontStyle.Italic);
        lblProtokollHinweis.ForeColor = Color.FromArgb(100, 120, 160);
        lblProtokollHinweis.Text      = "";
        lblProtokollHinweis.TextAlign = ContentAlignment.MiddleRight;

        grpStatus.Controls.AddRange([lblStatusDot, lblStatusText, lblProtokollHinweis]);

        // ═══ LINKE SPALTE: Terminal ════════════════════════════════════════════
        // x: 12, y: 72, w: 784, h: 716

        grpDaten.Text      = "Daten auf der COM-Schnittstelle";
        grpDaten.Location  = new Point(12, 72);
        grpDaten.Size      = new Size(784, 718);
        grpDaten.Font      = fontNormal;
        grpDaten.BackColor = bgColor;

        txtDaten.Location   = new Point(10, 28);
        txtDaten.Size       = new Size(764, 648);
        txtDaten.ReadOnly   = true;
        txtDaten.Font       = fontMono;
        txtDaten.BackColor  = Color.FromArgb(18, 20, 26);
        txtDaten.ForeColor  = Color.FromArgb(180, 220, 160);
        txtDaten.ScrollBars = RichTextBoxScrollBars.Vertical;
        txtDaten.WordWrap   = false;

        btnClear.Text      = "Terminal löschen";
        btnClear.Location  = new Point(10, 682);
        btnClear.Size      = new Size(150, 32);
        btnClear.Font      = new Font("Segoe UI", 9.5F);
        btnClear.FlatStyle = FlatStyle.Flat;
        btnClear.ForeColor = Color.FromArgb(80, 90, 110);
        btnClear.FlatAppearance.BorderColor = Color.FromArgb(180, 185, 200);
        btnClear.Click    += btnClear_Click;

        grpDaten.Controls.AddRange([txtDaten, btnClear]);

        // ═══ RECHTE SPALTE ════════════════════════════════════════════════════
        // x: 808, y: 72, w: 380

        // ── GroupBox: Steuerung ───────────────────────────────────────────────
        grpSteuerung.Text      = "Steuerung";
        grpSteuerung.Location  = new Point(808, 72);
        grpSteuerung.Size      = new Size(380, 318);
        grpSteuerung.Font      = fontNormal;
        grpSteuerung.BackColor = bgColor;

        btnMessung.Text      = "Messung auslösen";
        btnMessung.Location  = new Point(10, 26);
        btnMessung.Size      = new Size(360, 62);
        btnMessung.Font      = new Font("Segoe UI", 12F, FontStyle.Bold);
        btnMessung.BackColor = Color.FromArgb(42, 100, 170);
        btnMessung.ForeColor = Color.White;
        btnMessung.FlatStyle = FlatStyle.Flat;
        btnMessung.FlatAppearance.BorderColor = Color.FromArgb(28, 76, 140);
        btnMessung.Cursor    = Cursors.Hand;
        btnMessung.Click    += btnMessung_Click;

        btnModus.Text      = "Modus: Prisma  →  weiter: RL Kurz";
        btnModus.Location  = new Point(10, 98);
        btnModus.Size      = new Size(360, 52);
        btnModus.Font      = new Font("Segoe UI", 10.5F);
        btnModus.BackColor = Color.FromArgb(38, 110, 72);
        btnModus.ForeColor = Color.White;
        btnModus.FlatStyle = FlatStyle.Flat;
        btnModus.FlatAppearance.BorderColor = Color.FromArgb(26, 86, 54);
        btnModus.Cursor    = Cursors.Hand;
        btnModus.Click    += btnModus_Click;

        btnWinkel.Text      = "Winkel-Live starten";
        btnWinkel.Location  = new Point(10, 160);
        btnWinkel.Size      = new Size(360, 52);
        btnWinkel.Font      = new Font("Segoe UI", 10.5F);
        btnWinkel.BackColor = Color.FromArgb(80, 50, 130);
        btnWinkel.ForeColor = Color.White;
        btnWinkel.FlatStyle = FlatStyle.Flat;
        btnWinkel.FlatAppearance.BorderColor = Color.FromArgb(60, 30, 110);
        btnWinkel.Cursor    = Cursors.Hand;
        btnWinkel.Click    += btnWinkel_Click;

        btnLaser.Text      = "Laserpointer  EIN";
        btnLaser.Location  = new Point(10, 222);
        btnLaser.Size      = new Size(360, 52);
        btnLaser.Font      = new Font("Segoe UI", 10.5F);
        btnLaser.BackColor = Color.FromArgb(75, 60, 60);
        btnLaser.ForeColor = Color.White;
        btnLaser.FlatStyle = FlatStyle.Flat;
        btnLaser.FlatAppearance.BorderColor = Color.FromArgb(60, 40, 40);
        btnLaser.Cursor    = Cursors.Hand;
        btnLaser.Click    += btnLaser_Click;

        grpSteuerung.Controls.AddRange([btnMessung, btnModus, btnWinkel, btnLaser]);

        // ── GroupBox: Letzte Messung ──────────────────────────────────────────
        grpMesswerte.Text      = "Letzte Messung";
        grpMesswerte.Location  = new Point(808, 400);
        grpMesswerte.Size      = new Size(380, 190);
        grpMesswerte.Font      = fontNormal;
        grpMesswerte.BackColor = bgColor;

        var monoWert = new Font("Consolas", 11F, FontStyle.Bold);
        var colFarbe = Color.FromArgb(100, 200, 255);

        void AddMesswertZeile(Label lbl, Label lbl2, string beschr, int y)
        {
            lbl.Text      = beschr;
            lbl.Location  = new Point(10, y);
            lbl.Size      = new Size(80, 26);
            lbl.Font      = fontNormal;
            lbl.ForeColor = Color.FromArgb(140, 155, 180);

            lbl2.Text      = "–";
            lbl2.Location  = new Point(95, y);
            lbl2.Size      = new Size(270, 26);
            lbl2.Font      = monoWert;
            lbl2.ForeColor = colFarbe;
        }

        AddMesswertZeile(lblHzLabel, lblHz, "Hz:",  28);
        AddMesswertZeile(lblVLabel,  lblV,  "V:",   60);
        AddMesswertZeile(lblSdLabel, lblSd, "D:",   92);
        AddMesswertZeile(lblHdLabel, lblHd, "Dh:", 124);
        AddMesswertZeile(lblDhLabel, lblDh, "Δh:", 156);

        grpMesswerte.Controls.AddRange([
            lblHzLabel, lblHz, lblVLabel, lblV,
            lblSdLabel, lblSd, lblHdLabel, lblHd,
            lblDhLabel, lblDh
        ]);

        // ── GroupBox: Dosenlibelle ────────────────────────────────────────────
        grpLibelle.Text      = "Dosenlibelle  (Kompensator)";
        grpLibelle.Location  = new Point(808, 598);
        grpLibelle.Size      = new Size(380, 140);
        grpLibelle.Font      = fontNormal;
        grpLibelle.BackColor = bgColor;

        pnlLibelle.Location = new Point(10, 24);
        pnlLibelle.Size     = new Size(120, 104);
        pnlLibelle.Cursor   = Cursors.Default;

        lblNeigung.Location  = new Point(136, 24);
        lblNeigung.Size      = new Size(232, 50);
        lblNeigung.Font      = new Font("Consolas", 9F);
        lblNeigung.ForeColor = Color.FromArgb(130, 160, 200);
        lblNeigung.Text      = "Quer:    –\r\nLängs:   –";
        lblNeigung.TextAlign = ContentAlignment.TopLeft;

        lblPPM.Location  = new Point(136, 78);
        lblPPM.Size      = new Size(232, 26);
        lblPPM.Font      = new Font("Consolas", 9F);
        lblPPM.ForeColor = Color.FromArgb(200, 160, 80);
        lblPPM.Text      = "PPM: –";
        lblPPM.TextAlign = ContentAlignment.TopLeft;

        btnLibelle.Text      = "Libelle Live  starten";
        btnLibelle.Location  = new Point(808, 746);
        btnLibelle.Size      = new Size(380, 44);
        btnLibelle.Font      = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnLibelle.BackColor = Color.FromArgb(50, 90, 70);
        btnLibelle.ForeColor = Color.White;
        btnLibelle.FlatStyle = FlatStyle.Flat;
        btnLibelle.FlatAppearance.BorderColor = Color.FromArgb(35, 70, 52);
        btnLibelle.Cursor    = Cursors.Hand;
        btnLibelle.Click    += btnLibelle_Click;

        grpLibelle.Controls.AddRange([pnlLibelle, lblNeigung, lblPPM]);

        Controls.AddRange([
            grpStatus, grpDaten,
            grpSteuerung, grpMesswerte, grpLibelle, btnLibelle
        ]);

        ResumeLayout(false);
    }

    private GroupBox           grpStatus;
    private Label              lblStatusDot;
    private Label              lblStatusText;
    private Label              lblProtokollHinweis;
    private GroupBox           grpDaten;
    private RichTextBox        txtDaten;
    private Button             btnClear;
    private GroupBox           grpSteuerung;
    private Button             btnMessung;
    private Button             btnModus;
    private Button             btnWinkel;
    private Button             btnLaser;
    private GroupBox           grpMesswerte;
    private Label              lblHzLabel;
    private Label              lblHz;
    private Label              lblVLabel;
    private Label              lblV;
    private Label              lblSdLabel;
    private Label              lblSd;
    private Label              lblHdLabel;
    private Label              lblHd;
    private Label              lblDhLabel;
    private Label              lblDh;
    private GroupBox           grpLibelle;
    private DosenlibellControl pnlLibelle;
    private Label              lblNeigung;
    private Label              lblPPM;
    private Button             btnLibelle;
}
