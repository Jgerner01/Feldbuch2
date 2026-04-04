namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// Overlay-Entities für Feldbuchpunkte (Standpunkte und Neupunkte).
//
// Symbole werden in Bildschirm-Pixeln gezeichnet (zoom-invariant),
// ähnlich wie die Katasterpunkt-Symbole.
//
// Standpunkt:  Roter Kreis  +  diagonales Kreuz  +  PunktNr  +  Höhe
// Neupunkt:    Grünes ausgefülltes Dreieck  +  PunktNr  +  Höhe
// ──────────────────────────────────────────────────────────────────────────────

public abstract class FeldbuchOverlayEntity : DxfEntity
{
    public FeldbuchPunkt Punkt { get; }

    protected FeldbuchOverlayEntity(FeldbuchPunkt punkt)
    {
        Punkt = punkt;
        Layer = punkt.Typ;
    }

    public override BoundingBox GetBounds() =>
        new(Punkt.R - 1, Punkt.H - 1, Punkt.R + 1, Punkt.H + 1);

    public override double DistanceTo(double wx, double wy) =>
        Math.Sqrt((wx - Punkt.R) * (wx - Punkt.R) +
                  (wy - Punkt.H) * (wy - Punkt.H));

    public override (double x, double y) GetNearestPoint(double wx, double wy) =>
        (Punkt.R, Punkt.H);

    public override (double x, double y) GetSnapPoint(double wx, double wy) =>
        (Punkt.R, Punkt.H);

    public override string GetInfo() =>
        $"{Punkt.Typ}  {Punkt.PunktNr}  " +
        $"R={Punkt.R:F3}  H={Punkt.H:F3}" +
        (Punkt.IstBerechnung3D ? $"  Höhe={Punkt.Hoehe:F3}" : "");

    // Hilfsmethode: Beschriftungstext zeichnen
    protected static void DrawLabel(Graphics g, PointF center,
        string punktNr, FeldbuchPunkt p, Color col, float symbolRadius)
    {
        using var brush = new SolidBrush(col);
        using var fntNr = new Font("Arial", 9f, FontStyle.Bold,  GraphicsUnit.Pixel);
        using var fntH  = new Font("Arial", 8f, FontStyle.Regular, GraphicsUnit.Pixel);

        float gap = symbolRadius + 3f;

        // Punktnummer oben rechts
        g.DrawString(punktNr, fntNr, brush,
            center.X + gap * 0.4f,
            center.Y - gap - fntNr.Size);

        // Höhe unten rechts (nur bei 3D)
        if (p.IstBerechnung3D)
            g.DrawString($"{p.Hoehe:F3}", fntH, brush,
                center.X + gap * 0.4f,
                center.Y + gap);
    }
}

// ── Standpunkt ────────────────────────────────────────────────────────────────
// Symbol: Kreis (rot) + diagonale Kreuzlinien außerhalb
public class OverlayStandpunkt : FeldbuchOverlayEntity
{
    private static readonly Color Farbe = Color.FromArgb(200, 30, 30);
    const float R = 4f;   // Kreisradius [px]

    public OverlayStandpunkt(FeldbuchPunkt p) : base(p) { }

    public override void Draw(Graphics g, Pen _pen,
        Func<double, double, PointF> ts, double scale)
    {
        var c = ts(Punkt.R, Punkt.H);
        using var pen   = new Pen(Farbe, 1.8f);
        using var brush = new SolidBrush(Farbe);

        // Kreis
        g.DrawEllipse(pen, c.X - R, c.Y - R, R * 2, R * 2);

        // Kreuzlinien außerhalb des Kreises (wie DIN-Symbol Festpunkt)
        float arm = R + 2.5f;
        g.DrawLine(pen, c.X - arm, c.Y - arm, c.X - R * 0.6f, c.Y - R * 0.6f);
        g.DrawLine(pen, c.X + R * 0.6f, c.Y + R * 0.6f, c.X + arm, c.Y + arm);
        g.DrawLine(pen, c.X + arm, c.Y - arm, c.X + R * 0.6f, c.Y - R * 0.6f);
        g.DrawLine(pen, c.X - R * 0.6f, c.Y + R * 0.6f, c.X - arm, c.Y + arm);

        // gefüllter Mittelpunkt
        g.FillEllipse(brush, c.X - 1.25f, c.Y - 1.25f, 2.5f, 2.5f);

        DrawLabel(g, c, Punkt.PunktNr, Punkt, Farbe, R);
    }
}

// ── Neupunkt ──────────────────────────────────────────────────────────────────
// Symbol: ausgefülltes grünes Dreieck (nach oben zeigend)
public class OverlayNeupunkt : FeldbuchOverlayEntity
{
    private static readonly Color Farbe = Color.FromArgb(20, 150, 40);
    const float H = 5f;   // Dreieckshöhe [px]

    public OverlayNeupunkt(FeldbuchPunkt p) : base(p) { }

    public override void Draw(Graphics g, Pen _pen,
        Func<double, double, PointF> ts, double scale)
    {
        var c = ts(Punkt.R, Punkt.H);
        using var pen   = new Pen(Farbe, 1.5f);
        using var brush = new SolidBrush(Color.FromArgb(180, 20, 150, 40));

        // gleichseitiges Dreieck, nach oben zeigend
        float b = H * 1.155f;   // Basis = Höhe * 2/sqrt(3)
        var tri = new PointF[]
        {
            new(c.X,         c.Y - H * 0.67f),   // Spitze
            new(c.X + b / 2, c.Y + H * 0.33f),   // rechts unten
            new(c.X - b / 2, c.Y + H * 0.33f)    // links unten
        };
        g.FillPolygon(brush, tri);
        g.DrawPolygon(pen,   tri);

        DrawLabel(g, c, Punkt.PunktNr, Punkt, Farbe, H * 0.67f);
    }
}
