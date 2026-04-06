namespace Feldbuch;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        pnlHeader             = new Panel();
        lblTitel              = new Label();
        lblProjektInfo        = new Label();
        pnlBody               = new Panel();
        lblSekProjekt         = new Label();
        btnProjekt            = new Button();
        btnProjektdaten       = new Button();
        lblSekFeldarbeit      = new Label();
        btnDxfViewer          = new Button();
        btnFreieStationierung = new Button();
        btnTachymeterKommunikation = new Button();
        lblSekDaten           = new Label();
        btnKonvertierung      = new Button();
        grpOptionen           = new GroupBox();
        chkProtokoll          = new CheckBox();
        chkAutoBackup         = new CheckBox();
        chkKoordTooltip       = new CheckBox();
        chkTon                = new CheckBox();
        chkErwProto           = new CheckBox();
        btnInfo               = new Button();

        SuspendLayout();
        pnlBody.SuspendLayout();
        grpOptionen.SuspendLayout();

        // ── Farben & Schriften ────────────────────────────────────────────────
        var bgColor    = Color.FromArgb(244, 246, 250);
        var secColor   = Color.FromArgb(100, 120, 160);
        var btnFont    = new Font("Segoe UI", 10F);
        var btnFontBig = new Font("Segoe UI", 11F, FontStyle.Bold);

        // ── Fenster ───────────────────────────────────────────────────────────
        ClientSize      = new Size(460, 530);
        Text            = "Feldbuch";
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        AutoScaleMode   = AutoScaleMode.Font;
        BackColor       = bgColor;

        // ── Header-Panel ──────────────────────────────────────────────────────
        pnlHeader.Dock      = DockStyle.Top;
        pnlHeader.Height    = 58;
        pnlHeader.BackColor = Color.FromArgb(42, 72, 130);
        pnlHeader.Padding   = new Padding(14, 6, 14, 0);

        lblTitel.Text      = "Feldbuch";
        lblTitel.Font      = new Font("Segoe UI", 15F, FontStyle.Bold);
        lblTitel.ForeColor = Color.White;
        lblTitel.Location  = new Point(14, 6);
        lblTitel.AutoSize  = true;

        lblProjektInfo.Text      = "Kein Projekt gewählt";
        lblProjektInfo.Font      = new Font("Segoe UI", 8F, FontStyle.Italic);
        lblProjektInfo.ForeColor = Color.FromArgb(190, 210, 240);
        lblProjektInfo.Location  = new Point(14, 33);
        lblProjektInfo.Size      = new Size(432, 18);

        pnlHeader.Controls.Add(lblTitel);
        pnlHeader.Controls.Add(lblProjektInfo);

        // ── Body-Panel ────────────────────────────────────────────────────────
        pnlBody.Location  = new Point(0, 58);
        pnlBody.Size      = new Size(460, 432);
        pnlBody.BackColor = bgColor;
        pnlBody.Padding   = new Padding(14, 10, 14, 0);

        int x = 14, w = 432, bh = 42, half = 208, gap = 16;
        int y = 10;

        // ─── Sektion: PROJEKT ─────────────────────────────────────────────────
        lblSekProjekt = BaueSektion("PROJEKT", x, y, w, secColor);
        y += 24;

        btnProjekt.Text      = "Projekt wählen";
        btnProjekt.Size      = new Size(half, bh);
        btnProjekt.Location  = new Point(x, y);
        btnProjekt.Font      = btnFont;
        BlauerButton(btnProjekt);
        btnProjekt.Click    += btnProjekt_Click;

        btnProjektdaten.Text    = "Projektdaten";
        btnProjektdaten.Size    = new Size(half, bh);
        btnProjektdaten.Location= new Point(x + half + gap, y);
        btnProjektdaten.Font    = btnFont;
        GrauButton(btnProjektdaten);
        btnProjektdaten.Click  += btnProjektdaten_Click;
        y += bh + 14;

        // ─── Sektion: FELDARBEIT ──────────────────────────────────────────────
        lblSekFeldarbeit = BaueSektion("FELDARBEIT", x, y, w, secColor);
        y += 24;

        btnDxfViewer.Text      = "DXF-Viewer  /  Aufnahme";
        btnDxfViewer.Size      = new Size(w, 50);
        btnDxfViewer.Location  = new Point(x, y);
        btnDxfViewer.Font      = btnFontBig;
        BlauerButton(btnDxfViewer);
        btnDxfViewer.BackColor = Color.FromArgb(34, 90, 160);
        btnDxfViewer.Click    += btnDxfViewer_Click;
        y += 50 + 8;

        btnFreieStationierung.Text     = "Freie Stationierung";
        btnFreieStationierung.Size     = new Size(half, bh);
        btnFreieStationierung.Location = new Point(x, y);
        btnFreieStationierung.Font     = btnFont;
        GrauButton(btnFreieStationierung);
        btnFreieStationierung.Click   += btnFreieStationierung_Click;

        btnTachymeterKommunikation.Text     = "Tachymeter";
        btnTachymeterKommunikation.Size     = new Size(half, bh);
        btnTachymeterKommunikation.Location = new Point(x + half + gap, y);
        btnTachymeterKommunikation.Font     = btnFont;
        GruenerButton(btnTachymeterKommunikation);
        btnTachymeterKommunikation.Click   += btnTachymeterKommunikation_Click;
        y += bh + 14;

        // ─── Sektion: DATEN ───────────────────────────────────────────────────
        lblSekDaten = BaueSektion("DATEN", x, y, w, secColor);
        y += 24;

        btnKonvertierung.Text     = "Konvertierung";
        btnKonvertierung.Size     = new Size(w, bh);
        btnKonvertierung.Location = new Point(x, y);
        btnKonvertierung.Font     = btnFont;
        GrauButton(btnKonvertierung);
        btnKonvertierung.Click   += btnKonvertierung_Click;
        y += bh + 14;

        // ─── Optionen ─────────────────────────────────────────────────────────
        grpOptionen.Text     = "Optionen";
        grpOptionen.Location = new Point(x, y);
        grpOptionen.Size     = new Size(w, 68);
        grpOptionen.Font     = new Font("Segoe UI", 8.5F);
        grpOptionen.BackColor = bgColor;
        grpOptionen.FlatStyle = GroupBoxStyle(grpOptionen);

        int cx1 = 8, cx2 = 160, cx3 = 316;
        int cy1 = 16, cy2 = 38;

        SetChk(chkProtokoll,   "Protokoll",          cx1, cy1);
        SetChk(chkAutoBackup,  "Auto-Backup",         cx2, cy1);
        SetChk(chkKoordTooltip,"Koordinaten-Tooltip", cx3, cy1);
        SetChk(chkTon,         "Ton",                 cx1, cy2);
        SetChk(chkErwProto,    "Erw. Protokoll",      cx2, cy2);

        chkProtokoll.CheckedChanged    += chkProtokoll_CheckedChanged;
        chkAutoBackup.CheckedChanged   += chkOption_CheckedChanged;
        chkKoordTooltip.CheckedChanged += chkOption_CheckedChanged;
        chkTon.CheckedChanged          += chkOption_CheckedChanged;
        chkErwProto.CheckedChanged     += chkOption_CheckedChanged;

        grpOptionen.Controls.AddRange(new Control[]
            { chkProtokoll, chkAutoBackup, chkKoordTooltip, chkTon, chkErwProto });

        pnlBody.Controls.AddRange(new Control[]
        {
            lblSekProjekt, btnProjekt, btnProjektdaten,
            lblSekFeldarbeit, btnDxfViewer, btnFreieStationierung, btnTachymeterKommunikation,
            lblSekDaten, btnKonvertierung,
            grpOptionen
        });

        // ── Info-Button (unten links) ─────────────────────────────────────────
        btnInfo.Text      = "?";
        btnInfo.Size      = new Size(30, 30);
        btnInfo.Location  = new Point(10, 494);
        btnInfo.Font      = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnInfo.FlatStyle = FlatStyle.Flat;
        btnInfo.ForeColor = Color.FromArgb(100, 100, 120);
        btnInfo.FlatAppearance.BorderColor = Color.FromArgb(180, 185, 200);
        btnInfo.Click    += btnInfo_Click;

        Controls.Add(pnlHeader);
        Controls.Add(pnlBody);
        Controls.Add(btnInfo);

        pnlBody.ResumeLayout(false);
        grpOptionen.ResumeLayout(false);
        ResumeLayout(false);
    }

    // ── Hilfsmethoden ─────────────────────────────────────────────────────────
    private static Label BaueSektion(string text, int x, int y, int w, Color col)
    {
        var lbl = new Label
        {
            Text      = text,
            Location  = new Point(x, y),
            Size      = new Size(w, 18),
            Font      = new Font("Segoe UI", 7.5F, FontStyle.Bold),
            ForeColor = col
        };
        return lbl;
    }

    private static void BlauerButton(Button b)
    {
        b.BackColor = Color.FromArgb(52, 100, 175);
        b.ForeColor = Color.White;
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderColor = Color.FromArgb(36, 78, 150);
        b.Cursor    = Cursors.Hand;
    }

    private static void GrauButton(Button b)
    {
        b.BackColor = Color.FromArgb(224, 228, 238);
        b.ForeColor = Color.FromArgb(40, 50, 80);
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderColor = Color.FromArgb(185, 192, 210);
        b.Cursor    = Cursors.Hand;
    }

    private static void GruenerButton(Button b)
    {
        b.BackColor = Color.FromArgb(38, 110, 72);
        b.ForeColor = Color.White;
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderColor = Color.FromArgb(26, 90, 56);
        b.Cursor    = Cursors.Hand;
    }

    private static void SetChk(CheckBox chk, string text, int x, int y)
    {
        chk.Text      = text;
        chk.Location  = new Point(x, y);
        chk.AutoSize  = true;
        chk.Font      = new Font("Segoe UI", 8.5F);
        chk.ForeColor = Color.FromArgb(50, 55, 80);
    }

    // Kein eigener GroupBox-FlatStyle-Rückgabewert nötig – Dummy für Lesbarkeit
    private static FlatStyle GroupBoxStyle(GroupBox g)
    {
        g.FlatStyle = FlatStyle.Flat;
        return FlatStyle.Flat;
    }

    // ── Felder ────────────────────────────────────────────────────────────────
    private Panel     pnlHeader                   = null!;
    private Panel     pnlBody                     = null!;
    private Label     lblTitel                    = null!;
    private Label     lblProjektInfo              = null!;
    private Label     lblSekProjekt               = null!;
    private Label     lblSekFeldarbeit            = null!;
    private Label     lblSekDaten                 = null!;
    private Button    btnProjekt                  = null!;
    private Button    btnProjektdaten             = null!;
    private Button    btnDxfViewer                = null!;
    private Button    btnFreieStationierung        = null!;
    private Button    btnTachymeterKommunikation   = null!;
    private Button    btnKonvertierung             = null!;
    private Button    btnInfo                     = null!;
    private GroupBox  grpOptionen                 = null!;
    private CheckBox  chkProtokoll                = null!;
    private CheckBox  chkAutoBackup               = null!;
    private CheckBox  chkKoordTooltip             = null!;
    private CheckBox  chkTon                      = null!;
    private CheckBox  chkErwProto                 = null!;
}
