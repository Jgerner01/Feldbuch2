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

        grpDaten        = new GroupBox();
        txtDaten        = new RichTextBox();
        btnClear        = new Button();

        btnMessung      = new Button();
        btnModus        = new Button();
        btnWinkel       = new Button();
        btnLaser        = new Button();

        SuspendLayout();

        var fontNormal = new Font("Segoe UI", 9.5F);
        var fontMono   = new Font("Consolas", 9F);
        var bgColor    = Color.FromArgb(244, 246, 250);

        // ── Fenster ───────────────────────────────────────────────────────────
        ClientSize      = new Size(560, 558);
        Text            = "Testmessungen GeoCOM";
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        AutoScaleMode   = AutoScaleMode.Font;
        BackColor       = bgColor;

        // ═══ GroupBox: Verbindungsstatus ══════════════════════════════════════
        grpStatus.Text     = "Verbindungsstatus";
        grpStatus.Location = new Point(12, 12);
        grpStatus.Size     = new Size(534, 52);
        grpStatus.Font     = fontNormal;
        grpStatus.BackColor = bgColor;

        lblStatusDot.Location  = new Point(16, 22);
        lblStatusDot.Size      = new Size(14, 14);
        lblStatusDot.BackColor = Color.FromArgb(200, 50, 50);
        lblStatusDot.BorderStyle = BorderStyle.FixedSingle;

        lblStatusText.Location  = new Point(38, 18);
        lblStatusText.Size      = new Size(480, 22);
        lblStatusText.Font      = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblStatusText.ForeColor = Color.FromArgb(160, 40, 40);
        lblStatusText.Text      = "Kein Tachymeter verbunden";

        grpStatus.Controls.AddRange([lblStatusDot, lblStatusText]);

        // ═══ GroupBox: Empfangene Daten ════════════════════════════════════════
        grpDaten.Text     = "Daten auf der COM-Schnittstelle";
        grpDaten.Location = new Point(12, 76);
        grpDaten.Size     = new Size(534, 330);
        grpDaten.Font     = fontNormal;
        grpDaten.BackColor = bgColor;

        txtDaten.Location    = new Point(10, 22);
        txtDaten.Size        = new Size(514, 270);
        txtDaten.ReadOnly    = true;
        txtDaten.Font        = fontMono;
        txtDaten.BackColor   = Color.FromArgb(18, 20, 26);
        txtDaten.ForeColor   = Color.FromArgb(180, 220, 160);
        txtDaten.ScrollBars  = RichTextBoxScrollBars.Vertical;
        txtDaten.WordWrap    = false;

        btnClear.Text      = "Löschen";
        btnClear.Location  = new Point(10, 300);
        btnClear.Size      = new Size(80, 24);
        btnClear.Font      = new Font("Segoe UI", 8.5F);
        btnClear.FlatStyle = FlatStyle.Flat;
        btnClear.ForeColor = Color.FromArgb(80, 90, 110);
        btnClear.FlatAppearance.BorderColor = Color.FromArgb(180, 185, 200);
        btnClear.Click    += btnClear_Click;

        grpDaten.Controls.AddRange([txtDaten, btnClear]);

        // ═══ Buttons: Messung / Modus ══════════════════════════════════════════
        btnMessung.Text      = "Messung auslösen";
        btnMessung.Location  = new Point(12, 422);
        btnMessung.Size      = new Size(200, 48);
        btnMessung.Font      = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        btnMessung.BackColor = Color.FromArgb(42, 100, 170);
        btnMessung.ForeColor = Color.White;
        btnMessung.FlatStyle = FlatStyle.Flat;
        btnMessung.FlatAppearance.BorderColor = Color.FromArgb(28, 76, 140);
        btnMessung.Cursor    = Cursors.Hand;
        btnMessung.Click    += btnMessung_Click;

        btnModus.Text      = "Reflektormessung";
        btnModus.Location  = new Point(224, 422);
        btnModus.Size      = new Size(322, 48);
        btnModus.Font      = new Font("Segoe UI", 10F);
        btnModus.BackColor = Color.FromArgb(38, 110, 72);
        btnModus.ForeColor = Color.White;
        btnModus.FlatStyle = FlatStyle.Flat;
        btnModus.FlatAppearance.BorderColor = Color.FromArgb(26, 86, 54);
        btnModus.Cursor    = Cursors.Hand;
        btnModus.Click    += btnModus_Click;

        // ═══ Button: Winkel-Dauerübertragung (halbe Breite) ═══════════════════
        btnWinkel.Text      = "Winkel-Dauerübertragung starten";
        btnWinkel.Location  = new Point(12, 478);
        btnWinkel.Size      = new Size(260, 40);
        btnWinkel.Font      = new Font("Segoe UI", 9.5F);
        btnWinkel.BackColor = Color.FromArgb(80, 50, 130);
        btnWinkel.ForeColor = Color.White;
        btnWinkel.FlatStyle = FlatStyle.Flat;
        btnWinkel.FlatAppearance.BorderColor = Color.FromArgb(60, 30, 110);
        btnWinkel.Cursor    = Cursors.Hand;
        btnWinkel.Click    += btnWinkel_Click;

        // ═══ Button: Laserpointer ══════════════════════════════════════════════
        btnLaser.Text      = "Laserpointer  EIN";
        btnLaser.Location  = new Point(280, 478);
        btnLaser.Size      = new Size(266, 40);
        btnLaser.Font      = new Font("Segoe UI", 9.5F);
        btnLaser.BackColor = Color.FromArgb(75, 60, 60);
        btnLaser.ForeColor = Color.White;
        btnLaser.FlatStyle = FlatStyle.Flat;
        btnLaser.FlatAppearance.BorderColor = Color.FromArgb(60, 40, 40);
        btnLaser.Cursor    = Cursors.Hand;
        btnLaser.Click    += btnLaser_Click;

        Controls.AddRange([grpStatus, grpDaten, btnMessung, btnModus, btnWinkel, btnLaser]);

        ResumeLayout(false);
    }

    private GroupBox    grpStatus;
    private Label       lblStatusDot;
    private Label       lblStatusText;
    private GroupBox    grpDaten;
    private RichTextBox txtDaten;
    private Button      btnClear;
    private Button      btnMessung;
    private Button      btnModus;
    private Button      btnWinkel;
    private Button      btnLaser;
}
