namespace Feldbuch;

using System.Drawing.Drawing2D;

// ──────────────────────────────────────────────────────────────────────────────
// FormPrismenkonstante – Auswahl von Leica-Prismen.
//
// Leica-Konvention (TPS1200):
//   Das Standard-Prisma GPH1P/GPR1 hat physikalisch -34,4 mm Prismenkonstante.
//   Leica definiert dies als Referenz → GPR1 = 0,0 mm im Instrument.
//   Alle anderen Prismen sind relativ zum GPR1 angegeben:
//     Mini-Prisma GMP111: −17,5 mm (kleiner als Referenz)
//
//   Reflektorlos wird über den EDM-Umschalter in der Toolbar gewählt.
// ──────────────────────────────────────────────────────────────────────────────
public partial class FormPrismenkonstante : Form
{
    // ── Öffentliche Ergebnisfelder ────────────────────────────────────────────
    public decimal GewähltePrismenkonstante { get; private set; } = 0m;
    public string  GewählterPrismaName      { get; private set; } = "GPR1 Standard";

    // ── Prismen-Daten ─────────────────────────────────────────────────────────
    private record PrismaInfo(
        string  Name,           // Anzeigename
        decimal Konstante_mm,   // Prismenkonstante [mm], Leica-Konvention
        string  Beschreibung);  // Tooltip / Unterzeile

    private static readonly PrismaInfo[] _prismen =
    [
        new("GPR1 / GPH1P",        0.0m,   "Standard-Vollprisma"),
        new("GRZ4 / GRZ122",       0.0m,   "360°-Rundprisma"),
        new("GMP111 / GMP101",    -17.5m,  "Miniaturprisma"),
        new("GPH3P / GPR3",        0.0m,   "Dreifach-Prismensatz"),
        new("Folie / Kleintarget", 34.4m,  "Reflexfolie (EDM Tape)"),
    ];

    private int _ausgewaehltIndex = 0;
    private Button[] _prismaButtons = [];

    // ── Konstruktor ───────────────────────────────────────────────────────────
    public FormPrismenkonstante(decimal aktuelleKonstante_mm)
    {
        InitializeComponent();
        // Vorauswahl ermitteln
        for (int i = 0; i < _prismen.Length; i++)
        {
            if (_prismen[i].Konstante_mm == aktuelleKonstante_mm)
            {
                _ausgewaehltIndex = i;
                break;
            }
        }
        nudKonstante.Value = Math.Clamp(aktuelleKonstante_mm, -50m, 50m);
        AktualisiereAuswahl(_ausgewaehltIndex);
    }

    // ── Auswahl-Aktualisierung ────────────────────────────────────────────────
    private void AktualisiereAuswahl(int idx)
    {
        _ausgewaehltIndex = idx;
        var p = _prismen[idx];

        nudKonstante.Value = Math.Clamp(p.Konstante_mm, -50m, 50m);
        lblStatus.Text     = $"Prisma: {p.Name}  –  {p.Konstante_mm:+0.0;-0.0;0.0} mm";

        // Buttons neu einfärben
        for (int i = 0; i < _prismaButtons.Length; i++)
        {
            bool aktiv = (i == idx);
            _prismaButtons[i].BackColor = aktiv
                ? Color.FromArgb(40, 100, 175)
                : Color.FromArgb(52, 64, 90);
            _prismaButtons[i].FlatAppearance.BorderColor = aktiv
                ? Color.FromArgb(120, 180, 255)
                : Color.FromArgb(80, 95, 130);
            _prismaButtons[i].Invalidate();
        }
    }

    // ── Button-Klick ─────────────────────────────────────────────────────────
    private void PrismaButton_Click(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.Tag is int idx)
            AktualisiereAuswahl(idx);
    }

    // ── Manual NUD ───────────────────────────────────────────────────────────
    private void nudKonstante_ValueChanged(object? sender, EventArgs e)
    {
        lblStatus.Text = $"Manuell: {nudKonstante.Value:+0.0;-0.0;0.0} mm";
        // Vorauswahl aufheben wenn manuelle Eingabe nicht exakt passt
        bool passt = false;
        for (int i = 0; i < _prismen.Length; i++)
        {
            if (_prismen[i].Konstante_mm == nudKonstante.Value)
            { passt = true; break; }
        }
        if (!passt)
        {
            _ausgewaehltIndex = -1;
            foreach (var b in _prismaButtons)
            {
                b.BackColor = Color.FromArgb(52, 64, 90);
                b.FlatAppearance.BorderColor = Color.FromArgb(80, 95, 130);
                b.Invalidate();
            }
        }
    }

    // ── OK ────────────────────────────────────────────────────────────────────
    private void btnÜbernehmen_Click(object? sender, EventArgs e)
    {
        GewähltePrismenkonstante = nudKonstante.Value;
        GewählterPrismaName      = _ausgewaehltIndex >= 0
            ? _prismen[_ausgewaehltIndex].Name : "Manuell";
        DialogResult = DialogResult.OK;
        Close();
    }

    // ── Owner-Draw: Prismen-Icon ──────────────────────────────────────────────
    // Zeichnet ein kleines Prismen- oder RL-Symbol oben im Button.
    private void PrismaButton_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not int idx) return;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var p = _prismen[idx];
        bool aktiv = (idx == _ausgewaehltIndex);

        // Zeichenfläche
        var r = btn.ClientRectangle;
        int iconH = 38;

        if (p.Name.Contains("360") || p.Name.Contains("GRZ"))
            Zeichne360Prisma(g, r.Width / 2, 8, iconH, aktiv);
        else if (p.Name.Contains("Mini") || p.Name.Contains("GMP"))
            ZeichneMiniPrisma(g, r.Width / 2, 8, iconH, aktiv);
        else
            ZeichneStandardPrisma(g, r.Width / 2, 8, iconH, aktiv);

        // Prisma-Name
        var nameFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        var descFont = new Font("Segoe UI", 6.5f);
        Color textCol = aktiv ? Color.White : Color.FromArgb(200, 215, 240);

        // Name oben
        var nameSz = g.MeasureString(p.Name, nameFont);
        g.DrawString(p.Name, nameFont, new SolidBrush(textCol),
            (r.Width - nameSz.Width) / 2, iconH + 6);

        // Konstante
        string konstStr = p.Konstante_mm == 0
            ? "0,0 mm"
            : $"{p.Konstante_mm:+0.0;-0.0} mm";
        var kSz = g.MeasureString(konstStr, descFont);
        Color kCol = p.Konstante_mm > 0 ? Color.FromArgb(255, 160, 80)
                   : p.Konstante_mm < 0 ? Color.FromArgb(130, 200, 255)
                   : Color.FromArgb(180, 220, 255);
        g.DrawString(konstStr, descFont, new SolidBrush(kCol),
            (r.Width - kSz.Width) / 2, iconH + 22);

        nameFont.Dispose();
        descFont.Dispose();
    }

    // ── Prismen-Symbole ───────────────────────────────────────────────────────

    // Standard-Vollprisma: Kreis + Eckwürfel-Dreieck
    private static void ZeichneStandardPrisma(Graphics g, float cx, float top, int h, bool aktiv)
    {
        float cy   = top + h * 0.5f;
        float outerR = h * 0.46f;
        float triR   = h * 0.36f;

        using var circlePen = new Pen(aktiv ? Color.FromArgb(210, 230, 255) : Color.FromArgb(140, 165, 215), 1.5f);
        g.DrawEllipse(circlePen, cx - outerR, cy - outerR, outerR * 2, outerR * 2);

        float sq3h = (float)(triR * Math.Sqrt(3.0) / 2.0);
        var tri = new PointF[]
        {
            new(cx,        cy - triR),
            new(cx + sq3h, cy + triR / 2f),
            new(cx - sq3h, cy + triR / 2f)
        };
        using var fill = new SolidBrush(aktiv ? Color.FromArgb(160, 180, 220, 255) : Color.FromArgb(80, 120, 180, 230));
        g.FillPolygon(fill, tri);
        using var triPen = new Pen(aktiv ? Color.White : Color.FromArgb(160, 190, 235), 1.2f);
        g.DrawPolygon(triPen, tri);
        using var facetPen = new Pen(Color.FromArgb(120, 255, 255, 255), 0.8f);
        var fc = new PointF(cx, cy);
        g.DrawLine(facetPen, fc, tri[0]);
        g.DrawLine(facetPen, fc, tri[1]);
        g.DrawLine(facetPen, fc, tri[2]);
        // Glanz
        using var shine = new SolidBrush(Color.FromArgb(140, 255, 255, 255));
        g.FillEllipse(shine, cx + 1.5f, cy - triR * 0.6f, 4, 3);
    }

    // 360°-Prisma: Kreis mit Kreuz
    private static void Zeichne360Prisma(Graphics g, float cx, float top, int h, bool aktiv)
    {
        float cy = top + h * 0.5f;
        float r  = h * 0.46f;
        using var fill = new SolidBrush(aktiv ? Color.FromArgb(40, 100, 180, 255) : Color.FromArgb(20, 80, 140, 200));
        g.FillEllipse(fill, cx - r, cy - r, r * 2, r * 2);
        using var circlePen = new Pen(aktiv ? Color.FromArgb(210, 230, 255) : Color.FromArgb(140, 165, 215), 1.5f);
        g.DrawEllipse(circlePen, cx - r, cy - r, r * 2, r * 2);
        // Innere Ringe (360°-Symbol)
        float r2 = r * 0.65f;
        using var innerPen = new Pen(aktiv ? Color.FromArgb(200, 225, 255) : Color.FromArgb(120, 160, 220), 1f);
        g.DrawEllipse(innerPen, cx - r2, cy - r2, r2 * 2, r2 * 2);
        // Kreuz
        using var crossPen = new Pen(aktiv ? Color.White : Color.FromArgb(160, 190, 235), 1.2f);
        g.DrawLine(crossPen, cx, cy - r, cx, cy + r);
        g.DrawLine(crossPen, cx - r, cy, cx + r, cy);
        // Zentralpunkt
        using var dot = new SolidBrush(aktiv ? Color.White : Color.FromArgb(200, 210, 240));
        g.FillEllipse(dot, cx - 2.5f, cy - 2.5f, 5, 5);
    }

    // Mini-Prisma: Kleines Prisma-Symbol mit Halterung
    private static void ZeichneMiniPrisma(Graphics g, float cx, float top, int h, bool aktiv)
    {
        float cy    = top + h * 0.55f;
        float sR    = h * 0.28f;  // Kleiner
        float stX1  = cx - 2f;
        float stTop = cy + sR;

        // Halterungsstiel
        using var stPen = new Pen(aktiv ? Color.FromArgb(180, 200, 240) : Color.FromArgb(110, 140, 200), 2f);
        g.DrawLine(stPen, cx, stTop, cx, stTop + h * 0.15f);

        // Kleines Prisma (Kreis + Dreieck)
        using var fill = new SolidBrush(aktiv ? Color.FromArgb(30, 100, 170, 255) : Color.FromArgb(15, 70, 120, 190));
        g.FillEllipse(fill, cx - sR, cy - sR, sR * 2, sR * 2);
        using var circlePen = new Pen(aktiv ? Color.FromArgb(200, 225, 255) : Color.FromArgb(130, 160, 215), 1.5f);
        g.DrawEllipse(circlePen, cx - sR, cy - sR, sR * 2, sR * 2);
        float sq3 = (float)(sR * 0.78f * Math.Sqrt(3.0) / 2.0);
        float triR2 = sR * 0.78f;
        var tri = new PointF[]
        {
            new(cx, cy - triR2),
            new(cx + sq3, cy + triR2 / 2f),
            new(cx - sq3, cy + triR2 / 2f)
        };
        using var tfill = new SolidBrush(aktiv ? Color.FromArgb(140, 170, 210, 255) : Color.FromArgb(70, 110, 170, 230));
        g.FillPolygon(tfill, tri);
        using var triPen = new Pen(aktiv ? Color.White : Color.FromArgb(155, 185, 235), 1f);
        g.DrawPolygon(triPen, tri);

        // Kleines "M" für Mini
        using var font = new Font("Segoe UI", 6.5f, FontStyle.Bold);
        var sz = g.MeasureString("M", font);
        g.DrawString("M", font, new SolidBrush(Color.FromArgb(255, 200, 80)),
            cx - sz.Width / 2, top + h - sz.Height + 1);
    }

}
