namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// DXF-Parser: unterstützt LINE, CIRCLE, ARC, LWPOLYLINE, POINT, TEXT, MTEXT
// Koordinatensystem: DXF = Y-nach-oben (mathematisch), Bildschirm = Y-nach-unten
// ──────────────────────────────────────────────────────────────────────────────

public record BoundingBox(double MinX, double MinY, double MaxX, double MaxY)
{
    public double Width  => MaxX - MinX;
    public double Height => MaxY - MinY;
    public double CenterX => (MinX + MaxX) / 2;
    public double CenterY => (MinY + MaxY) / 2;

    public BoundingBox Expand(BoundingBox other) => new(
        Math.Min(MinX, other.MinX), Math.Min(MinY, other.MinY),
        Math.Max(MaxX, other.MaxX), Math.Max(MaxY, other.MaxY));
}

// ── Basis-Entität ─────────────────────────────────────────────────────────────
public abstract class DxfEntity
{
    public string Layer { get; set; } = "0";
    public Color  Color { get; set; } = Color.Empty;   // Empty = layer color

    public abstract BoundingBox  GetBounds();
    public abstract double       DistanceTo(double wx, double wy);
    public abstract string       GetInfo();
    public abstract (double x, double y) GetNearestPoint(double wx, double wy);
    // Snap: Mittelpunkt (Kreis) oder Endpunkt (Linie/Bogen/Polyline) nächst dem Cursor
    public abstract (double x, double y) GetSnapPoint(double wx, double wy);
    public abstract void Draw(Graphics g, Pen pen, Func<double, double, PointF> toScreen, double scale);
}

// ── LINE ──────────────────────────────────────────────────────────────────────
public class DxfLine : DxfEntity
{
    public double X1, Y1, X2, Y2;

    public override BoundingBox GetBounds() =>
        new(Math.Min(X1,X2), Math.Min(Y1,Y2), Math.Max(X1,X2), Math.Max(Y1,Y2));

    public override double DistanceTo(double wx, double wy)
    {
        double dx = X2 - X1, dy = Y2 - Y1;
        double len2 = dx*dx + dy*dy;
        if (len2 < 1e-20) return Math.Sqrt((wx-X1)*(wx-X1)+(wy-Y1)*(wy-Y1));
        double t = Math.Clamp(((wx-X1)*dx + (wy-Y1)*dy) / len2, 0, 1);
        double px = X1 + t*dx, py = Y1 + t*dy;
        return Math.Sqrt((wx-px)*(wx-px)+(wy-py)*(wy-py));
    }

    public override (double x, double y) GetNearestPoint(double wx, double wy)
    {
        double dx = X2-X1, dy = Y2-Y1, len2 = dx*dx+dy*dy;
        if (len2 < 1e-20) return (X1, Y1);
        double t = Math.Clamp(((wx-X1)*dx+(wy-Y1)*dy)/len2, 0, 1);
        return (X1+t*dx, Y1+t*dy);
    }

    // Snap: nächster Endpunkt (Anfang oder Ende der Linie)
    public override (double x, double y) GetSnapPoint(double wx, double wy)
    {
        double d1 = Math.Sqrt((wx-X1)*(wx-X1)+(wy-Y1)*(wy-Y1));
        double d2 = Math.Sqrt((wx-X2)*(wx-X2)+(wy-Y2)*(wy-Y2));
        return d1 <= d2 ? (X1, Y1) : (X2, Y2);
    }

    public override string GetInfo() =>
        $"LINE  von ({X1:F3} / {Y1:F3})  nach ({X2:F3} / {Y2:F3})";

    public override void Draw(Graphics g, Pen pen, Func<double, double, PointF> ts, double scale)
        => g.DrawLine(pen, ts(X1, Y1), ts(X2, Y2));
}

// ── CIRCLE ────────────────────────────────────────────────────────────────────
public class DxfCircle : DxfEntity
{
    public double CX, CY, Radius;

    public override BoundingBox GetBounds() =>
        new(CX-Radius, CY-Radius, CX+Radius, CY+Radius);

    public override double DistanceTo(double wx, double wy)
        => Math.Abs(Math.Sqrt((wx-CX)*(wx-CX)+(wy-CY)*(wy-CY)) - Radius);

    public override (double x, double y) GetNearestPoint(double wx, double wy)
    {
        double dx = wx-CX, dy = wy-CY;
        double d  = Math.Sqrt(dx*dx+dy*dy);
        if (d < 1e-10) return (CX+Radius, CY);
        return (CX + dx/d*Radius, CY + dy/d*Radius);
    }

    // Snap: Mittelpunkt des Kreises
    public override (double x, double y) GetSnapPoint(double wx, double wy) => (CX, CY);

    public override string GetInfo() =>
        $"CIRCLE  Mitte ({CX:F3} / {CY:F3})  R = {Radius:F3}";

    public override void Draw(Graphics g, Pen pen, Func<double, double, PointF> ts, double scale)
    {
        float r = (float)(Radius * scale);
        var c   = ts(CX, CY);
        g.DrawEllipse(pen, c.X - r, c.Y - r, r * 2, r * 2);
    }
}

// ── ARC ───────────────────────────────────────────────────────────────────────
public class DxfArc : DxfEntity
{
    public double CX, CY, Radius, StartAngle, EndAngle;   // Winkel in Grad, CCW

    public override BoundingBox GetBounds()
    {
        // Näherung: prüfe Start, End und alle Vielfachen von 90°
        var pts = new List<(double x, double y)>();
        double a = StartAngle;
        double sweep = (EndAngle - StartAngle + 360) % 360;
        if (sweep < 0.001) sweep = 360;
        for (double deg = 0; deg < sweep; deg += Math.Min(sweep, 90))
        {
            double rad = (a + deg) * Math.PI / 180.0;
            pts.Add((CX + Radius * Math.Cos(rad), CY + Radius * Math.Sin(rad)));
        }
        double endRad = EndAngle * Math.PI / 180.0;
        pts.Add((CX + Radius * Math.Cos(endRad), CY + Radius * Math.Sin(endRad)));

        return new(pts.Min(p=>p.x), pts.Min(p=>p.y),
                   pts.Max(p=>p.x), pts.Max(p=>p.y));
    }

    public override double DistanceTo(double wx, double wy)
    {
        double dx = wx-CX, dy = wy-CY;
        double d  = Math.Sqrt(dx*dx+dy*dy);
        return Math.Abs(d - Radius);   // Näherung (ignoriert Bogenbegrenzung)
    }

    public override (double x, double y) GetNearestPoint(double wx, double wy)
    {
        double ang = Math.Atan2(wy-CY, wx-CX) * 180.0 / Math.PI;
        if (ang < 0) ang += 360;
        double rad = ang * Math.PI / 180.0;
        return (CX + Radius * Math.Cos(rad), CY + Radius * Math.Sin(rad));
    }

    // Snap: nächster Endpunkt des Bogens (Anfangs- oder Endpunkt)
    public override (double x, double y) GetSnapPoint(double wx, double wy)
    {
        double sr = StartAngle * Math.PI / 180.0;
        double er = EndAngle   * Math.PI / 180.0;
        double sx = CX + Radius * Math.Cos(sr), sy = CY + Radius * Math.Sin(sr);
        double ex = CX + Radius * Math.Cos(er), ey = CY + Radius * Math.Sin(er);
        double d1 = Math.Sqrt((wx-sx)*(wx-sx)+(wy-sy)*(wy-sy));
        double d2 = Math.Sqrt((wx-ex)*(wx-ex)+(wy-ey)*(wy-ey));
        return d1 <= d2 ? (sx, sy) : (ex, ey);
    }

    public override string GetInfo() =>
        $"ARC  Mitte ({CX:F3} / {CY:F3})  R={Radius:F3}  {StartAngle:F1}°–{EndAngle:F1}°";

    public override void Draw(Graphics g, Pen pen, Func<double, double, PointF> ts, double scale)
    {
        float r     = (float)(Radius * scale);
        var   c     = ts(CX, CY);
        // DXF CCW → Bildschirm (Y-gespiegelt) → GDI+ CW negiert
        double dxfSweep = (EndAngle - StartAngle + 360) % 360;
        if (dxfSweep < 0.001) dxfSweep = 360;
        float gdiStart = -(float)StartAngle;
        float gdiSweep = -(float)dxfSweep;
        g.DrawArc(pen, c.X - r, c.Y - r, r * 2, r * 2, gdiStart, gdiSweep);
    }
}

// ── LWPOLYLINE ────────────────────────────────────────────────────────────────
public class DxfLwPolyline : DxfEntity
{
    public List<(double x, double y)> Vertices { get; } = new();
    public bool Closed { get; set; }

    public override BoundingBox GetBounds()
    {
        if (Vertices.Count == 0) return new(0,0,0,0);
        return new(Vertices.Min(v=>v.x), Vertices.Min(v=>v.y),
                   Vertices.Max(v=>v.x), Vertices.Max(v=>v.y));
    }

    public override double DistanceTo(double wx, double wy)
    {
        double min = double.MaxValue;
        int n = Vertices.Count;
        for (int i = 0; i < n - 1 + (Closed ? 1 : 0); i++)
        {
            var a = Vertices[i]; var b = Vertices[(i+1) % n];
            double dx = b.x-a.x, dy = b.y-a.y, len2 = dx*dx+dy*dy;
            double t  = len2 < 1e-20 ? 0 : Math.Clamp(((wx-a.x)*dx+(wy-a.y)*dy)/len2,0,1);
            double px = a.x+t*dx, py = a.y+t*dy;
            min = Math.Min(min, Math.Sqrt((wx-px)*(wx-px)+(wy-py)*(wy-py)));
        }
        return min;
    }

    public override (double x, double y) GetNearestPoint(double wx, double wy)
        => Vertices.Count == 0 ? (0, 0) :
           Vertices.MinBy(v => Math.Sqrt((v.x-wx)*(v.x-wx)+(v.y-wy)*(v.y-wy)));

    // Snap: nächster Eckpunkt der Polylinie
    public override (double x, double y) GetSnapPoint(double wx, double wy)
        => GetNearestPoint(wx, wy);

    public override string GetInfo() =>
        $"POLYLINE  {Vertices.Count} Punkte{(Closed?" (geschlossen)":"")}";

    public override void Draw(Graphics g, Pen pen, Func<double, double, PointF> ts, double scale)
    {
        if (Vertices.Count < 2) return;
        var pts = Vertices.Select(v => ts(v.x, v.y)).ToArray();
        g.DrawLines(pen, pts);
        if (Closed) g.DrawLine(pen, pts[^1], pts[0]);
    }
}

// ── POINT ─────────────────────────────────────────────────────────────────────
public class DxfPoint : DxfEntity
{
    public double X, Y;

    public override BoundingBox GetBounds() => new(X, Y, X, Y);
    public override double DistanceTo(double wx, double wy)
        => Math.Sqrt((wx-X)*(wx-X)+(wy-Y)*(wy-Y));
    public override (double x, double y) GetNearestPoint(double wx, double wy) => (X, Y);
    public override (double x, double y) GetSnapPoint(double wx, double wy) => (X, Y);
    public override string GetInfo() => $"POINT  ({X:F3} / {Y:F3})";

    public override void Draw(Graphics g, Pen pen, Func<double, double, PointF> ts, double scale)
    {
        float sz = Math.Max(4f, (float)(2.0 * scale / 100));
        var c = ts(X, Y);
        g.DrawLine(pen, c.X - sz, c.Y, c.X + sz, c.Y);
        g.DrawLine(pen, c.X, c.Y - sz, c.X, c.Y + sz);
    }
}

// ── TEXT / MTEXT ──────────────────────────────────────────────────────────────
public class DxfText : DxfEntity
{
    public double X, Y, Height;
    public string Text { get; set; } = "";

    public override BoundingBox GetBounds() => new(X, Y, X, Y + Height);
    public override double DistanceTo(double wx, double wy)
        => Math.Sqrt((wx-X)*(wx-X)+(wy-Y)*(wy-Y));
    public override (double x, double y) GetNearestPoint(double wx, double wy) => (X, Y);
    public override (double x, double y) GetSnapPoint(double wx, double wy) => (X, Y);
    public override string GetInfo() => $"TEXT  \"{Text}\"  ({X:F3} / {Y:F3})";

    public override void Draw(Graphics g, Pen pen, Func<double, double, PointF> ts, double scale)
    {
        float px = Math.Max(6f, (float)(Height * scale));
        using var fnt = new Font("Arial", px, GraphicsUnit.Pixel);
        var c = ts(X, Y);
        g.DrawString(Text, fnt, new SolidBrush(pen.Color), c);
    }
}

// ── INSERT (Block-Referenz) ───────────────────────────────────────────────────
// Wird für Katasterpunkte verwendet (Grenzpunkte, Katasterfestpunkte).
// Symbole werden in Bildschirm-Pixel (unabhängig vom Zoom) gezeichnet.
public enum KatasterPunktTyp
{
    GrenzpunktGenau,      // Layer: grenzpunkt_genau    → Doppelkreis (rot)
    GrenzpunktSonstiger,  // Layer: grenzpunkt_sonstiger → gestrichelter Kreis (braun)
    Katasterfestpunkt,    // Layer: katasterfestpunkt   → Kreis + Kreuz (blau)
    Sonstig               // alle anderen INSERT-Layer  → kleine Raute
}

public class DxfInsert : DxfEntity
{
    public string BlockName { get; set; } = "";
    public double X, Y;

    public KatasterPunktTyp PunktTyp => Layer.ToLowerInvariant() switch
    {
        "grenzpunkt_genau"     => KatasterPunktTyp.GrenzpunktGenau,
        "grenzpunkt_sonstiger" => KatasterPunktTyp.GrenzpunktSonstiger,
        "katasterfestpunkt"    => KatasterPunktTyp.Katasterfestpunkt,
        _                      => KatasterPunktTyp.Sonstig
    };

    public bool IstKatasterPunkt => PunktTyp != KatasterPunktTyp.Sonstig;

    public override BoundingBox GetBounds() => new(X, Y, X, Y);
    public override double DistanceTo(double wx, double wy)
        => Math.Sqrt((wx - X) * (wx - X) + (wy - Y) * (wy - Y));
    public override (double x, double y) GetNearestPoint(double wx, double wy) => (X, Y);
    public override (double x, double y) GetSnapPoint(double wx, double wy) => (X, Y);
    public override string GetInfo()
        => $"{Layer}  ({X:F3} / {Y:F3})  [{BlockName}]";

    public override void Draw(Graphics g, Pen pen,
        Func<double, double, PointF> ts, double scale)
    {
        var c = ts(X, Y);
        switch (PunktTyp)
        {
            case KatasterPunktTyp.GrenzpunktGenau:
                DrawGrenzpunktGenau(g, c);
                break;
            case KatasterPunktTyp.GrenzpunktSonstiger:
                DrawGrenzpunktSonstiger(g, c);
                break;
            case KatasterPunktTyp.Katasterfestpunkt:
                DrawKatasterfestpunkt(g, c);
                break;
            default:
                DrawSonstig(g, c, pen);
                break;
        }
    }

    // Grenzpunkt genau: Außenkreis + gefüllter Innenkreis (dunkelrot, wie ABM_1000)
    static void DrawGrenzpunktGenau(Graphics g, PointF c)
    {
        using var pen   = new Pen(Color.FromArgb(170, 30, 30), 1.5f);
        using var brush = new SolidBrush(Color.FromArgb(170, 30, 30));
        const float outer = 4f, inner = 1.25f;
        g.DrawEllipse(pen, c.X - outer, c.Y - outer, outer * 2, outer * 2);
        g.FillEllipse(brush, c.X - inner, c.Y - inner, inner * 2, inner * 2);
    }

    // Grenzpunkt sonstiger: gestrichelter Kreis + Mittelpunkt (braun, wie ABM_9500)
    static void DrawGrenzpunktSonstiger(Graphics g, PointF c)
    {
        using var pen = new Pen(Color.FromArgb(140, 90, 20), 1.5f)
            { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };
        using var brush = new SolidBrush(Color.FromArgb(140, 90, 20));
        const float r = 3.5f;
        g.DrawEllipse(pen, c.X - r, c.Y - r, r * 2, r * 2);
        g.FillEllipse(brush, c.X - 1f, c.Y - 1f, 2f, 2f);
    }

    // Katasterfestpunkt: Kreis mit durchgehendem Kreuz (blau, wie Aufnahmepunkt)
    static void DrawKatasterfestpunkt(Graphics g, PointF c)
    {
        using var pen = new Pen(Color.FromArgb(20, 60, 170), 1.8f);
        const float r = 4f;
        g.DrawEllipse(pen, c.X - r, c.Y - r, r * 2, r * 2);
        g.DrawLine(pen, c.X, c.Y - r - 2, c.X, c.Y + r + 2);
        g.DrawLine(pen, c.X - r - 2, c.Y, c.X + r + 2, c.Y);
    }

    // Sonstige INSERT-Entities: kleine Raute
    static void DrawSonstig(Graphics g, PointF c, Pen pen)
    {
        const float h = 2f;
        g.DrawLine(pen, c.X, c.Y - h, c.X + h, c.Y);
        g.DrawLine(pen, c.X + h, c.Y, c.X, c.Y + h);
        g.DrawLine(pen, c.X, c.Y + h, c.X - h, c.Y);
        g.DrawLine(pen, c.X - h, c.Y, c.X, c.Y - h);
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// DXF-Reader: liest Gruppe-Code / Wert-Paare und extrahiert Entitäten
// ──────────────────────────────────────────────────────────────────────────────
public static class DxfReader
{
    public static List<DxfEntity> Read(string path)
    {
        var groups = ReadGroups(path).ToList();
        return ParseEntities(groups);
    }

    static IEnumerable<(int code, string val)> ReadGroups(string path)
    {
        using var r = new System.IO.StreamReader(path, System.Text.Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);
        while (!r.EndOfStream)
        {
            string? c = r.ReadLine()?.Trim();
            string? v = r.ReadLine()?.Trim() ?? "";
            if (c == null) break;
            if (int.TryParse(c, out int code)) yield return (code, v);
        }
    }

    static List<DxfEntity> ParseEntities(List<(int code, string val)> groups)
    {
        var result = new List<DxfEntity>();
        bool inEntities = false;
        int i = 0;

        while (i < groups.Count)
        {
            var (code, val) = groups[i];

            if (code == 0 && val == "SECTION") { i++; continue; }
            if (code == 2 && val == "ENTITIES") { inEntities = true; i++; continue; }
            if (code == 0 && val == "ENDSEC") { inEntities = false; i++; continue; }
            if (code == 0 && val == "EOF") break;

            if (!inEntities) { i++; continue; }

            // Entität lesen
            if (code == 0)
            {
                string type = val;
                i++;
                int start = i;
                // sammle alle Gruppen bis zur nächsten code-0-Zeile
                while (i < groups.Count && groups[i].code != 0) i++;
                var props = groups.Skip(start).Take(i - start).ToList();
                var entity = ParseEntity(type, props);
                if (entity != null) result.Add(entity);
            }
            else i++;
        }
        return result;
    }

    static DxfEntity? ParseEntity(string type, List<(int code, string val)> props)
    {
        string layer = Get(props, 8, "0");
        Color  color = GetAciColor(props);

        switch (type)
        {
            case "LINE":
                return new DxfLine
                {
                    Layer = layer, Color = color,
                    X1 = GetD(props, 10), Y1 = GetD(props, 20),
                    X2 = GetD(props, 11), Y2 = GetD(props, 21)
                };

            case "CIRCLE":
                return new DxfCircle
                {
                    Layer = layer, Color = color,
                    CX = GetD(props, 10), CY = GetD(props, 20),
                    Radius = GetD(props, 40)
                };

            case "ARC":
                return new DxfArc
                {
                    Layer = layer, Color = color,
                    CX = GetD(props, 10), CY = GetD(props, 20),
                    Radius = GetD(props, 40),
                    StartAngle = GetD(props, 50),
                    EndAngle   = GetD(props, 51)
                };

            case "LWPOLYLINE":
            {
                int flags = GetI(props, 70, 0);
                var poly  = new DxfLwPolyline { Layer = layer, Color = color, Closed = (flags & 1) != 0 };
                // Vertices: je ein Code-10 und Code-20 Paar
                double? vx = null;
                foreach (var (c, v) in props)
                {
                    if (c == 10) { vx = ParseD(v); }
                    else if (c == 20 && vx.HasValue)
                    { poly.Vertices.Add((vx.Value, ParseD(v))); vx = null; }
                }
                return poly.Vertices.Count > 0 ? poly : null;
            }

            case "POINT":
                return new DxfPoint
                {
                    Layer = layer, Color = color,
                    X = GetD(props, 10), Y = GetD(props, 20)
                };

            case "TEXT":
            case "MTEXT":
                return new DxfText
                {
                    Layer = layer, Color = color,
                    X = GetD(props, 10), Y = GetD(props, 20),
                    Height = GetD(props, 40),
                    Text = Get(props, 1, "")
                };

            case "INSERT":
                return new DxfInsert
                {
                    Layer     = layer,
                    Color     = color,
                    BlockName = Get(props, 2, ""),
                    X         = GetD(props, 10),
                    Y         = GetD(props, 20)
                };

            case "ATTRIB":
            case "SEQEND":
                return null;   // Attribute und Sequenzende werden ignoriert

            default: return null;
        }
    }

    // ── Hilfsmethoden ─────────────────────────────────────────────────────────
    static string Get(List<(int c, string v)> props, int code, string def)
        => props.LastOrDefault(p => p.c == code).v ?? def;

    static double GetD(List<(int c, string v)> props, int code, double def = 0)
    {
        var v = props.LastOrDefault(p => p.c == code).v;
        return v != null && double.TryParse(v,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out double d) ? d : def;
    }

    static int GetI(List<(int c, string v)> props, int code, int def = 0)
    {
        var v = props.LastOrDefault(p => p.c == code).v;
        return v != null && int.TryParse(v, out int d) ? d : def;
    }

    static double ParseD(string s)
    {
        double.TryParse(s, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out double d);
        return d;
    }

    static Color GetAciColor(List<(int c, string v)> props)
    {
        int aci = GetI(props, 62, -1);
        return aci >= 0 ? AciToColor(aci) : Color.Empty;
    }

    // AutoCAD Color Index → .NET Color (für weißen Hintergrund angepasst)
    static Color AciToColor(int aci) => aci switch
    {
        1  => Color.Red,                          2  => Color.FromArgb(180, 140, 0),  // Yellow → dunkler
        3  => Color.Green,                        4  => Color.Teal,
        5  => Color.Blue,                         6  => Color.Purple,
        7  => Color.Black,                        // White → Black (weißer Hintergrund)
        8  => Color.FromArgb(80,  80,  80),       9  => Color.FromArgb(120, 120, 120),
        250 => Color.FromArgb(100, 100, 100),   251 => Color.FromArgb(120, 120, 120),
        252 => Color.FromArgb(150, 150, 150),   253 => Color.FromArgb(170, 170, 170),
        254 => Color.FromArgb(190, 190, 190),   255 => Color.Black,
        _ => Color.Empty
    };
}
