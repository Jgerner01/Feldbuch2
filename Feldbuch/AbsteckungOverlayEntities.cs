namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// DxfEntity-Unterklassen für Absteckungsmodule
// Werden in DxfCanvas.OverlayEntities eingehängt und zoom-invariant gezeichnet
// ──────────────────────────────────────────────────────────────────────────────

// ── Standpunkt (roter gefüllter Kreis mit Kreuzarmen) ────────────────────────
public class AbsteckStandpunktEntity : DxfEntity
{
    public double WX { get; }
    public double WY { get; }
    public string Nr { get; }

    public AbsteckStandpunktEntity(double wx, double wy, string nr)
    {
        WX = wx; WY = wy; Nr = nr; Layer = "Standpunkt";
    }

    public override BoundingBox GetBounds() => new(WX - 1, WY - 1, WX + 1, WY + 1);
    public override double DistanceTo(double wx, double wy) =>
        Math.Sqrt((wx - WX) * (wx - WX) + (wy - WY) * (wy - WY));
    public override (double x, double y) GetNearestPoint(double wx, double wy) => (WX, WY);
    public override (double x, double y) GetSnapPoint(double wx, double wy)    => (WX, WY);
    public override string GetInfo() => $"Standpunkt {Nr}  R={WX:F3}  H={WY:F3}";

    public override void Draw(Graphics g, Pen _, Func<double, double, PointF> ts, double scale)
    {
        var c = ts(WX, WY);
        const float r = 7f, arm = 12f;
        using var pen   = new Pen(Color.Crimson, 1.5f);
        using var brush = new SolidBrush(Color.Crimson);
        g.FillEllipse(brush, c.X - r, c.Y - r, 2 * r, 2 * r);
        g.DrawEllipse(new Pen(Color.DarkRed, 1f), c.X - r, c.Y - r, 2 * r, 2 * r);
        g.DrawLine(pen, c.X - r - arm, c.Y, c.X - r, c.Y);
        g.DrawLine(pen, c.X + r, c.Y, c.X + r + arm, c.Y);
        g.DrawLine(pen, c.X, c.Y - r - arm, c.X, c.Y - r);
        g.DrawLine(pen, c.X, c.Y + r, c.X, c.Y + r + arm);
        using var fnt = new Font("Arial", 8f, FontStyle.Bold, GraphicsUnit.Pixel);
        g.DrawString(Nr, fnt, Brushes.DarkRed, c.X + r + 3f, c.Y - 9f);
    }
}

// ── Soll-Punkt (offen=blau, aktuell=gold, abgesteckt=grün) ───────────────────
public class AbsteckSollPunktEntity : DxfEntity
{
    public double WX { get; }
    public double WY { get; }
    public string PunktNr { get; }
    public bool IsCurrent  { get; }
    public bool IsDone     { get; }

    public AbsteckSollPunktEntity(double wx, double wy, string nr,
                                   bool isCurrent, bool isDone)
    {
        WX = wx; WY = wy; PunktNr = nr;
        IsCurrent = isCurrent; IsDone = isDone;
        Layer = "AbsteckPunkt";
    }

    public override BoundingBox GetBounds() => new(WX - 1, WY - 1, WX + 1, WY + 1);
    public override double DistanceTo(double wx, double wy) =>
        Math.Sqrt((wx - WX) * (wx - WX) + (wy - WY) * (wy - WY));
    public override (double x, double y) GetNearestPoint(double wx, double wy) => (WX, WY);
    public override (double x, double y) GetSnapPoint(double wx, double wy)    => (WX, WY);
    public override string GetInfo() =>
        $"Sollpunkt {PunktNr}  R={WX:F3}  H={WY:F3}" +
        (IsDone ? "  [abgesteckt]" : IsCurrent ? "  [aktuell]" : "");

    public override void Draw(Graphics g, Pen _, Func<double, double, PointF> ts, double scale)
    {
        var c = ts(WX, WY);
        float r = IsCurrent ? 8f : 5f;
        Color fill = IsDone ? Color.SeaGreen : IsCurrent ? Color.Gold : Color.RoyalBlue;
        using var brush = new SolidBrush(fill);
        using var pen   = new Pen(IsCurrent ? Color.DarkOrange : Color.DimGray, 1f);
        g.FillEllipse(brush, c.X - r, c.Y - r, 2 * r, 2 * r);
        g.DrawEllipse(pen,   c.X - r, c.Y - r, 2 * r, 2 * r);
        using var fnt = new Font("Arial", 7f, FontStyle.Regular, GraphicsUnit.Pixel);
        g.DrawString(PunktNr, fnt, Brushes.Black, c.X + r + 2f, c.Y - 8f);
    }
}

// ── Verbindungslinie Station → aktueller Punkt ────────────────────────────────
public class AbsteckVerbindungslinieEntity : DxfEntity
{
    private readonly double _x1, _y1, _x2, _y2;

    public AbsteckVerbindungslinieEntity(double x1, double y1, double x2, double y2)
    {
        _x1 = x1; _y1 = y1; _x2 = x2; _y2 = y2; Layer = "Verbindungslinie";
    }

    public override BoundingBox GetBounds() =>
        new(Math.Min(_x1,_x2), Math.Min(_y1,_y2), Math.Max(_x1,_x2), Math.Max(_y1,_y2));
    public override double DistanceTo(double wx, double wy) => double.MaxValue;
    public override (double x, double y) GetNearestPoint(double wx, double wy) => (_x1, _y1);
    public override (double x, double y) GetSnapPoint(double wx, double wy)    => (_x1, _y1);
    public override string GetInfo() => "";

    public override void Draw(Graphics g, Pen _, Func<double, double, PointF> ts, double scale)
    {
        using var pen = new Pen(Color.Orange, 1.5f)
        { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
        g.DrawLine(pen, ts(_x1, _y1), ts(_x2, _y2));
    }
}

// ── Polygon-Umriss (für Schnurgerüst, Flächenteilung) ─────────────────────────
public class AbsteckPolygonEntity : DxfEntity
{
    private readonly List<(double R, double H)> _pts;
    private readonly Color _color;
    private readonly float _width;
    private readonly bool  _close;
    private readonly System.Drawing.Drawing2D.DashStyle _dash;

    public AbsteckPolygonEntity(IEnumerable<(double R, double H)> pts,
        Color color, float width = 2f, bool close = true,
        System.Drawing.Drawing2D.DashStyle dash =
            System.Drawing.Drawing2D.DashStyle.Solid)
    {
        _pts   = pts.ToList();
        _color = color; _width = width; _close = close; _dash = dash;
        Layer  = "AbsteckPolygon";
    }

    public override BoundingBox GetBounds()
    {
        if (_pts.Count == 0) return new(0, 0, 0, 0);
        return new(_pts.Min(p => p.R), _pts.Min(p => p.H),
                   _pts.Max(p => p.R), _pts.Max(p => p.H));
    }
    public override double DistanceTo(double wx, double wy) => double.MaxValue;
    public override (double x, double y) GetNearestPoint(double wx, double wy) =>
        _pts.Count > 0 ? (_pts[0].R, _pts[0].H) : (0, 0);
    public override (double x, double y) GetSnapPoint(double wx, double wy) =>
        GetNearestPoint(wx, wy);
    public override string GetInfo() => "";

    public override void Draw(Graphics g, Pen _, Func<double, double, PointF> ts, double scale)
    {
        if (_pts.Count < 2) return;
        using var pen = new Pen(_color, _width) { DashStyle = _dash };
        var scr = _pts.Select(p => ts(p.R, p.H)).ToArray();
        if (_close && scr.Length >= 3) g.DrawPolygon(pen, scr);
        else                           g.DrawLines(pen, scr);
    }
}

// ── Gefüllte Fläche (A1/A2 in Flächenteilung) ────────────────────────────────
public class AbsteckFlächeEntity : DxfEntity
{
    private readonly List<(double R, double H)> _pts;
    private readonly Color _fillColor;

    public AbsteckFlächeEntity(IEnumerable<(double R, double H)> pts, Color fillColor)
    {
        _pts       = pts.ToList();
        _fillColor = fillColor;
        Layer      = "AbsteckFläche";
    }

    public override BoundingBox GetBounds()
    {
        if (_pts.Count == 0) return new(0, 0, 0, 0);
        return new(_pts.Min(p => p.R), _pts.Min(p => p.H),
                   _pts.Max(p => p.R), _pts.Max(p => p.H));
    }
    public override double DistanceTo(double wx, double wy) => double.MaxValue;
    public override (double x, double y) GetNearestPoint(double wx, double wy) =>
        _pts.Count > 0 ? (_pts[0].R, _pts[0].H) : (0, 0);
    public override (double x, double y) GetSnapPoint(double wx, double wy) =>
        GetNearestPoint(wx, wy);
    public override string GetInfo() => "";

    public override void Draw(Graphics g, Pen _, Func<double, double, PointF> ts, double scale)
    {
        if (_pts.Count < 3) return;
        using var br = new SolidBrush(_fillColor);
        g.FillPolygon(br, _pts.Select(p => ts(p.R, p.H)).ToArray());
    }
}

// ── Neuer Grenzpunkt (gelber Kreis + Label) ───────────────────────────────────
public class AbsteckGrenzpunktEntity : DxfEntity
{
    public double WX { get; }
    public double WY { get; }
    public string Nr { get; }

    public AbsteckGrenzpunktEntity(double wx, double wy, string nr)
    {
        WX = wx; WY = wy; Nr = nr; Layer = "Grenzpunkt";
    }

    public override BoundingBox GetBounds() => new(WX - 1, WY - 1, WX + 1, WY + 1);
    public override double DistanceTo(double wx, double wy) =>
        Math.Sqrt((wx - WX) * (wx - WX) + (wy - WY) * (wy - WY));
    public override (double x, double y) GetNearestPoint(double wx, double wy) => (WX, WY);
    public override (double x, double y) GetSnapPoint(double wx, double wy)    => (WX, WY);
    public override string GetInfo() => $"Grenzpunkt {Nr}  R={WX:F3}  H={WY:F3}";

    public override void Draw(Graphics g, Pen _, Func<double, double, PointF> ts, double scale)
    {
        var c = ts(WX, WY);
        const float r = 7f;
        g.FillEllipse(Brushes.Yellow, c.X - r, c.Y - r, 2 * r, 2 * r);
        using var pen = new Pen(Color.DarkOrange, 1.5f);
        g.DrawEllipse(pen, c.X - r, c.Y - r, 2 * r, 2 * r);
        using var fnt = new Font("Arial", 8f, FontStyle.Bold, GraphicsUnit.Pixel);
        g.DrawString(Nr, fnt, Brushes.DarkOrange, c.X + r + 3f, c.Y - 9f);
    }
}

// ── Achsenlinie (gestrichelt, von A nach E) ───────────────────────────────────
public class AbsteckAchseEntity : DxfEntity
{
    private readonly double _x1, _y1, _x2, _y2;

    public AbsteckAchseEntity(double x1, double y1, double x2, double y2)
    {
        _x1 = x1; _y1 = y1; _x2 = x2; _y2 = y2; Layer = "Achse";
    }

    public override BoundingBox GetBounds() =>
        new(Math.Min(_x1,_x2), Math.Min(_y1,_y2), Math.Max(_x1,_x2), Math.Max(_y1,_y2));
    public override double DistanceTo(double wx, double wy) => double.MaxValue;
    public override (double x, double y) GetNearestPoint(double wx, double wy) => (_x1, _y1);
    public override (double x, double y) GetSnapPoint(double wx, double wy)    => (_x1, _y1);
    public override string GetInfo() => $"Achse  ({_x1:F3}/{_y1:F3}) → ({_x2:F3}/{_y2:F3})";

    public override void Draw(Graphics g, Pen _, Func<double, double, PointF> ts, double scale)
    {
        using var pen = new Pen(Color.FromArgb(180, 80, 0), 2f)
        { DashStyle = System.Drawing.Drawing2D.DashStyle.DashDot };
        g.DrawLine(pen, ts(_x1, _y1), ts(_x2, _y2));
        // Beschriftung an den Endpunkten
        using var fnt = new Font("Arial", 8f, FontStyle.Bold, GraphicsUnit.Pixel);
        var pa = ts(_x1, _y1); var pe = ts(_x2, _y2);
        g.DrawString("A", fnt, Brushes.DarkOrange, pa.X + 4, pa.Y - 12);
        g.DrawString("E", fnt, Brushes.DarkOrange, pe.X + 4, pe.Y - 12);
    }
}

// ── Raster-Label (Punkt-Nr. im Raster) ───────────────────────────────────────
// Rasterabsteckung benutzt AbsteckSollPunktEntity direkt – kein extra Typ nötig

// ── Schnurgerüst-Kante (klickbar für Achswahl, per Kante indiziert) ──────────
public class AbsteckSGKanteEntity : DxfEntity
{
    public int KanteIdx { get; }
    private readonly double _r1, _h1, _r2, _h2;
    private readonly bool   _isSelected;

    public AbsteckSGKanteEntity(int idx,
        double r1, double h1, double r2, double h2, bool isSelected)
    {
        KanteIdx = idx; _r1 = r1; _h1 = h1; _r2 = r2; _h2 = h2;
        _isSelected = isSelected;
        Layer = "SGKante";
    }

    public override BoundingBox GetBounds() =>
        new(Math.Min(_r1, _r2), Math.Min(_h1, _h2),
            Math.Max(_r1, _r2), Math.Max(_h1, _h2));

    public override double DistanceTo(double wx, double wy)
    {
        double dR = _r2 - _r1, dH = _h2 - _h1;
        double len2 = dR * dR + dH * dH;
        if (len2 < 1e-18)
            return Math.Sqrt((wx - _r1) * (wx - _r1) + (wy - _h1) * (wy - _h1));
        double t  = Math.Max(0, Math.Min(1, ((wx - _r1) * dR + (wy - _h1) * dH) / len2));
        double px = _r1 + t * dR, py = _h1 + t * dH;
        return Math.Sqrt((wx - px) * (wx - px) + (wy - py) * (wy - py));
    }

    public override (double x, double y) GetNearestPoint(double wx, double wy)
    {
        double d1 = Math.Sqrt((wx-_r1)*(wx-_r1) + (wy-_h1)*(wy-_h1));
        double d2 = Math.Sqrt((wx-_r2)*(wx-_r2) + (wy-_h2)*(wy-_h2));
        return d1 <= d2 ? (_r1, _h1) : (_r2, _h2);
    }

    public override (double x, double y) GetSnapPoint(double wx, double wy)
        => GetNearestPoint(wx, wy);

    public override string GetInfo() =>
        $"Gebäudeseite {KanteIdx + 1}  ({_r1:F3}/{_h1:F3}) → ({_r2:F3}/{_h2:F3})";

    public override void Draw(Graphics g, Pen _, Func<double, double, PointF> ts, double scale)
    {
        Color col = _isSelected ? Color.OrangeRed : Color.Red;
        float w   = _isSelected ? 3f : 2f;
        using var pen = new Pen(col, w);
        g.DrawLine(pen, ts(_r1, _h1), ts(_r2, _h2));

        if (_isSelected)
        {
            double dR = _r2 - _r1, dH = _h2 - _h1;
            double len = Math.Sqrt(dR * dR + dH * dH);
            if (len > 1e-9)
            {
                // Right normal in world coords (outward for CW polygon)
                double outnR = dH / len, outnH = -dR / len;
                double midR  = (_r1 + _r2) / 2, midH = (_h1 + _h2) / 2;
                // Pixel-adaptive offset
                double dist  = 25.0 / scale;
                var outPt = ts(midR + outnR * dist, midH + outnH * dist);
                var inPt  = ts(midR - outnR * dist, midH - outnH * dist);
                using var fnt = new Font("Arial", 11f, FontStyle.Bold, GraphicsUnit.Pixel);
                g.DrawString("+", fnt, Brushes.OrangeRed,     outPt.X - 6f, outPt.Y - 9f);
                g.DrawString("–", fnt, Brushes.DeepSkyBlue,   inPt.X  - 5f, inPt.Y  - 9f);
            }
        }
    }
}

// ── Gemessener Punkt (gelber Kreis + Lotstrecke zur Achse) ────────────────────
public class AbsteckMesspunktEntity : DxfEntity
{
    private readonly double  _pR, _pH;
    private readonly double? _aR1, _aH1, _aR2, _aH2;

    public AbsteckMesspunktEntity(double pR, double pH,
        double? aR1 = null, double? aH1 = null,
        double? aR2 = null, double? aH2 = null)
    {
        _pR = pR; _pH = pH;
        _aR1 = aR1; _aH1 = aH1; _aR2 = aR2; _aH2 = aH2;
        Layer = "GemessenerPunkt";
    }

    public override BoundingBox GetBounds() => new(_pR - 1, _pH - 1, _pR + 1, _pH + 1);
    public override double DistanceTo(double wx, double wy) =>
        Math.Sqrt((wx - _pR) * (wx - _pR) + (wy - _pH) * (wy - _pH));
    public override (double x, double y) GetNearestPoint(double wx, double wy) => (_pR, _pH);
    public override (double x, double y) GetSnapPoint(double wx, double wy)    => (_pR, _pH);
    public override string GetInfo() => $"Messung  R={_pR:F3}  H={_pH:F3}";

    public override void Draw(Graphics g, Pen _, Func<double, double, PointF> ts, double scale)
    {
        var c = ts(_pR, _pH);
        const float r = 7f;
        g.FillEllipse(Brushes.Yellow, c.X - r, c.Y - r, 2 * r, 2 * r);
        using var pen = new Pen(Color.DarkGoldenrod, 1.5f);
        g.DrawEllipse(pen, c.X - r, c.Y - r, 2 * r, 2 * r);
        using var fnt = new Font("Arial", 8f, FontStyle.Bold, GraphicsUnit.Pixel);
        g.DrawString("M", fnt, Brushes.DarkGoldenrod, c.X + r + 2f, c.Y - 8f);

        // Lotstrecke zur gewählten Achse
        if (_aR1.HasValue && _aR2.HasValue)
        {
            double dR   = _aR2.Value - _aR1.Value, dH = _aH2!.Value - _aH1!.Value;
            double len2 = dR * dR + dH * dH;
            if (len2 > 1e-18)
            {
                double t   = Math.Max(0, Math.Min(1,
                    ((_pR - _aR1.Value) * dR + (_pH - _aH1.Value) * dH) / len2));
                double lotR = _aR1.Value + t * dR;
                double lotH = _aH1.Value + t * dH;
                using var lotPen = new Pen(Color.Goldenrod, 1.5f)
                    { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
                g.DrawLine(lotPen, c, ts(lotR, lotH));
            }
        }
    }
}
