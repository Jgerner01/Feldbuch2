namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// LokalesSystemOverlay – Zeichenelemente für das lokale Koordinatensystem
// des DXF-Viewers (keine DXF-Daten, Standpunkt bei R=0 / H=0 / Höhe=0).
//
//   OverlayLokalerStandpunkt – Standpunktsymbol im Koordinatenursprung
//   OverlayZielrichtung      – rote gestrichelte Linie in Richtung des
//                              aktuellen Horizontalwinkels (Hz), läuft mit
//                              der Fernrohrbewegung mit
//
// Orientierung im lokalen System: 0 gon, d. h. Hz = Richtung vom Ursprung
// (Hz = 0 gon zeigt nach Norden / oben, im Uhrzeigersinn).
// Hardware: keine   Autor: Johann Gerner
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>Standpunktsymbol im Ursprung des lokalen Systems.</summary>
public class OverlayLokalerStandpunkt : DxfEntity
{
    private static readonly Color Farbe = Color.FromArgb(200, 30, 30);
    private const float SymbolRadiusPx = 6f;
    private const float LinienBreitePx = 1.8f;
    private const float SchriftPx      = 11f;

    /// <summary>Beschriftung, in der Regel die Standpunktnummer.</summary>
    public string Name { get; set; }

    public OverlayLokalerStandpunkt(string name)
    {
        Name  = name;
        Layer = "Standpunkt";
    }

    public override BoundingBox GetBounds() => new(-1, -1, 1, 1);

    public override double DistanceTo(double wx, double wy) =>
        Math.Sqrt(wx * wx + wy * wy);

    public override (double x, double y) GetNearestPoint(double wx, double wy) => (0, 0);

    public override (double x, double y) GetSnapPoint(double wx, double wy) => (0, 0);

    public override string GetInfo() =>
        $"Standpunkt  {Name}  R=0.000  H=0.000  Höhe=0.000  (lokales System)";

    public override void Draw(Graphics g, Pen _pen,
        Func<double, double, PointF> ts, double scale)
    {
        var c = ts(0, 0);
        using var pen    = new Pen(Farbe, LinienBreitePx);
        using var brush  = new SolidBrush(Farbe);
        using var fnt    = new Font("Arial", SchriftPx, FontStyle.Bold, GraphicsUnit.Pixel);

        // Dreieck (Standpunktsymbol) mit gefülltem Mittelpunkt
        float r = SymbolRadiusPx;
        var dreieck = new[]
        {
            new PointF(c.X,                 c.Y - r * 1.2f),
            new PointF(c.X - r * 1.05f,     c.Y + r * 0.7f),
            new PointF(c.X + r * 1.05f,     c.Y + r * 0.7f)
        };
        g.DrawPolygon(pen, dreieck);
        g.FillEllipse(brush, c.X - 1.5f, c.Y - 1.5f, 3f, 3f);

        g.DrawString(Name, fnt, brush, c.X + r + 3f, c.Y + 2f);
    }
}

/// <summary>Rote gestrichelte Zielrichtung vom Standpunkt, folgt dem Hz-Winkel.</summary>
public class OverlayZielrichtung : DxfEntity
{
    private static readonly Color Farbe = Color.FromArgb(220, 30, 30);
    private const float LinienBreitePx  = 1.6f;
    private const float LaengePx        = 4000f;   // länger als jedes Fenster
    private const float TextAbstandPx   = 80f;
    private const float SchriftPx       = 11f;
    private const double GON2RAD        = Math.PI / 200.0;

    /// <summary>Aktueller Horizontalwinkel [gon]; null = keine Anzeige.</summary>
    public double? HzGon { get; set; }

    public OverlayZielrichtung()
    {
        Layer = "Zielrichtung";
    }

    public override BoundingBox GetBounds() => new(0, 0, 0, 0);

    // Nie per Klick auswählbar
    public override double DistanceTo(double wx, double wy) => double.MaxValue;

    public override (double x, double y) GetNearestPoint(double wx, double wy) => (0, 0);

    public override (double x, double y) GetSnapPoint(double wx, double wy) => (0, 0);

    public override string GetInfo() =>
        HzGon.HasValue ? $"Zielrichtung  Hz={HzGon.Value:F4} gon" : "Zielrichtung";

    public override void Draw(Graphics g, Pen _pen,
        Func<double, double, PointF> ts, double scale)
    {
        if (!HzGon.HasValue || scale <= 0) return;

        double hz  = HzGon.Value * GON2RAD;
        double len = LaengePx / scale;                 // Länge in Weltkoordinaten
        var p0 = ts(0, 0);
        var p1 = ts(len * Math.Sin(hz), len * Math.Cos(hz));

        using var pen = new Pen(Farbe, LinienBreitePx)
        {
            DashStyle = System.Drawing.Drawing2D.DashStyle.Dash
        };
        g.DrawLine(pen, p0, p1);

        // Winkelanzeige ein Stück entlang der Linie
        double dx = p1.X - p0.X, dy = p1.Y - p0.Y;
        double l  = Math.Sqrt(dx * dx + dy * dy);
        if (l < 1e-6) return;
        float tx = p0.X + (float)(dx / l * TextAbstandPx);
        float ty = p0.Y + (float)(dy / l * TextAbstandPx);
        using var fnt   = new Font("Consolas", SchriftPx, FontStyle.Bold, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(Farbe);
        g.DrawString($"Hz {HzGon.Value:F4}", fnt, brush, tx + 4f, ty - SchriftPx - 2f);
    }
}
