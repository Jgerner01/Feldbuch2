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
    // Einheitliche Höhen-Farbe (dunkelbraun, passend zu ACI 30 im DXF-Export)
    internal static readonly Color FarbeHoehe = Color.FromArgb(120, 60, 0);

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

    // Hilfsmethode: Beschriftungstext zeichnen (Höhe immer dunkelbraun)
    protected static void DrawLabel(Graphics g, PointF center,
        string punktNr, FeldbuchPunkt p, Color col, float symbolRadius)
    {
        using var brush  = new SolidBrush(col);
        using var brushH = new SolidBrush(FarbeHoehe);
        using var fntNr  = new Font("Arial", 9f, FontStyle.Bold,    GraphicsUnit.Pixel);
        using var fntH   = new Font("Arial", 8f, FontStyle.Regular, GraphicsUnit.Pixel);

        float gap = symbolRadius + 3f;

        // Punktnummer oben rechts
        g.DrawString(punktNr, fntNr, brush,
            center.X + gap * 0.4f,
            center.Y - gap - fntNr.Size);

        // Höhe unten rechts (nur bei 3D, dunkelbraun)
        if (p.IstBerechnung3D)
            g.DrawString($"{p.Hoehe:F3}", fntH, brushH,
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

// ── DXF-Punkt-Marker (Beschriftung für DXF-Koordinaten) ──────────────────────
// Zeigt die fortlaufende Punktnummer aus dem DxfPunktIndex als Beschriftung
// über den原有的 DXF-Symbolen (Insert, Circle, Line, Point).
// Symbol: kleiner orangefarbener Kreis + Nummer in Grün (analog OverlayImportPunkt).
public class DxfPunktMarker : DxfEntity
{
    public string PunktNr { get; }
    public double WX { get; }
    public double WY { get; }

    private static readonly Color FarbeSymbol = Color.FromArgb(200, 140, 30);    // Orange
    private static readonly Color FarbeBeschr = Color.FromArgb(0,   160, 60);    // Grün
    const float R = 2.5f;   // Radius [px]
    const float FontSize = 12f;  // Schriftgröße [px] – 50% größer als Standard (8pt)

    public DxfPunktMarker(string punktNr, double wx, double wy)
    {
        PunktNr = punktNr;
        WX = wx;
        WY = wy;
        Layer = "DXF-Punkte";
    }

    public override BoundingBox GetBounds() => new(WX - 0.5, WY - 0.5, WX + 0.5, WY + 0.5);
    public override double DistanceTo(double wx, double wy) =>
        Math.Sqrt((wx - WX) * (wx - WX) + (wy - WY) * (wy - WY));
    public override (double x, double y) GetNearestPoint(double wx, double wy) => (WX, WY);
    public override (double x, double y) GetSnapPoint(double wx, double wy) => (WX, WY);
    public override string GetInfo() =>
        $"DXF-Punkt {PunktNr}  R={WX:F3}  H={WY:F3}";

    public override void Draw(Graphics g, Pen _pen,
        Func<double, double, PointF> ts, double scale)
    {
        var c = ts(WX, WY);
        using var penSym   = new Pen(FarbeSymbol, 1f);
        using var brushSym = new SolidBrush(FarbeSymbol);
        using var brushLbl = new SolidBrush(FarbeBeschr);
        using var fntNr    = new Font("Arial", FontSize, FontStyle.Bold, GraphicsUnit.Pixel);

        // Kleiner Kreis
        g.DrawEllipse(penSym, c.X - R, c.Y - R, R * 2, R * 2);
        g.FillEllipse(brushSym, c.X - 1f, c.Y - 1f, 2f, 2f);

        // Punktnummer rechts-oben (wie OverlayImportPunkt)
        float gap = R + 2f;
        g.DrawString(PunktNr, fntNr, brushLbl,
            c.X + gap * 0.4f, c.Y - gap - FontSize);
    }
}

// ── Import-Punkt ──────────────────────────────────────────────────────────────
// Symbol: roter Kreis (etwas kleiner als Standpunkt), Beschriftung grün.
// Wird für KOR/CSV/JSON-Import verwendet.
public class OverlayImportPunkt : DxfEntity
{
    public string PunktNr { get; }
    public double WX      { get; }
    public double WY      { get; }
    public double Hoehe   { get; }

    private static readonly Color FarbeSymbol = Color.FromArgb(200, 30, 30);     // Rot
    private static readonly Color FarbeBeschr = Color.FromArgb(0,   160, 60);    // Grün
    const float R = 3f;   // Radius [px] – kleiner als Standpunkt (4px)

    public OverlayImportPunkt(string punktNr, double wx, double wy, double hoehe)
    {
        PunktNr = punktNr;
        WX      = wx;
        WY      = wy;
        Hoehe   = hoehe;
        Layer   = "Import";
    }

    public override BoundingBox GetBounds() => new(WX - 1, WY - 1, WX + 1, WY + 1);
    public override double DistanceTo(double wx, double wy) =>
        Math.Sqrt((wx - WX) * (wx - WX) + (wy - WY) * (wy - WY));
    public override (double x, double y) GetNearestPoint(double wx, double wy) => (WX, WY);
    public override (double x, double y) GetSnapPoint(double wx, double wy)    => (WX, WY);
    public override string GetInfo() =>
        $"Import  {PunktNr}  R={WX:F3}  H={WY:F3}  Höhe={Hoehe:F3}";

    public override void Draw(Graphics g, Pen _pen,
        Func<double, double, PointF> ts, double scale)
    {
        var c = ts(WX, WY);
        using var penSym   = new Pen(FarbeSymbol, 1.5f);
        using var brushSym = new SolidBrush(FarbeSymbol);
        using var brushLbl = new SolidBrush(FarbeBeschr);
        using var brushH   = new SolidBrush(FeldbuchOverlayEntity.FarbeHoehe);
        using var fntNr    = new Font("Arial", 8f, FontStyle.Bold,    GraphicsUnit.Pixel);
        using var fntH     = new Font("Arial", 7f, FontStyle.Regular, GraphicsUnit.Pixel);

        // Kreis
        g.DrawEllipse(penSym, c.X - R, c.Y - R, R * 2, R * 2);
        // gefüllter Mittelpunkt
        g.FillEllipse(brushSym, c.X - 1f, c.Y - 1f, 2f, 2f);

        // Punktnummer grün
        float gap = R + 2f;
        g.DrawString(PunktNr, fntNr, brushLbl,
            c.X + gap * 0.4f, c.Y - gap - fntNr.Size);
        // Höhe dunkelbraun
        if (Hoehe != 0)
            g.DrawString($"{Hoehe:F3}", fntH, brushH,
                c.X + gap * 0.4f, c.Y + gap);
    }
}

// ── Neupunkt (aus Feldbuchpunkte.json) ───────────────────────────────────────
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

// ── Gemessener Neupunkt (aus NeupunkteManager) ───────────────────────────────
// Symbol: blaues ausgefülltes Quadrat (zoom-invariant)
// Beschriftung: PunktNr + Code + Höhe (wenn 3D)
public class OverlayGemessenerNeupunkt : DxfEntity
{
    public NeupunktErgebnis Ergebnis { get; }
    private static readonly Color FarbeSymbol = Color.FromArgb(20,  80, 200);   // Blau
    private static readonly Color FarbeBeschr = Color.FromArgb(10,  60, 160);   // Dunkelblau
    const float S = 4f;   // Hälfte der Quadratseite [px]

    public OverlayGemessenerNeupunkt(NeupunktErgebnis e)
    {
        Ergebnis = e;
        Layer    = "Neupunkt";
    }

    public override BoundingBox GetBounds() =>
        new(Ergebnis.R - 1, Ergebnis.H - 1, Ergebnis.R + 1, Ergebnis.H + 1);

    public override double DistanceTo(double wx, double wy) =>
        Math.Sqrt((wx - Ergebnis.R) * (wx - Ergebnis.R) +
                  (wy - Ergebnis.H) * (wy - Ergebnis.H));

    public override (double x, double y) GetNearestPoint(double wx, double wy) =>
        (Ergebnis.R, Ergebnis.H);

    public override (double x, double y) GetSnapPoint(double wx, double wy) =>
        (Ergebnis.R, Ergebnis.H);

    public override string GetInfo()
    {
        string basis = $"Neupunkt  {Ergebnis.PunktNr}  " +
                       $"R={Ergebnis.R:F3}  H={Ergebnis.H:F3}";
        if (Ergebnis.Ist3D)  basis += $"  Höhe={Ergebnis.Hoehe:F3}";
        if (!string.IsNullOrEmpty(Ergebnis.Code)) basis += $"  Code={Ergebnis.Code}";
        basis += $"  SP={Ergebnis.StandpunktNr}";
        return basis;
    }

    public override void Draw(Graphics g, Pen _pen,
        Func<double, double, PointF> ts, double scale)
    {
        var c = ts(Ergebnis.R, Ergebnis.H);
        using var penSym  = new Pen(FarbeSymbol, 1.5f);
        using var brSym   = new SolidBrush(Color.FromArgb(160, FarbeSymbol));
        using var brLbl   = new SolidBrush(FarbeBeschr);
        using var brH     = new SolidBrush(FeldbuchOverlayEntity.FarbeHoehe);
        using var fntNr   = new Font("Arial", 9f,  FontStyle.Bold,    GraphicsUnit.Pixel);
        using var fntCode = new Font("Arial", 8f,  FontStyle.Regular, GraphicsUnit.Pixel);
        using var fntH    = new Font("Arial", 7.5f, FontStyle.Regular, GraphicsUnit.Pixel);

        // Quadrat
        g.FillRectangle(brSym, c.X - S, c.Y - S, S * 2, S * 2);
        g.DrawRectangle(penSym, c.X - S, c.Y - S, S * 2, S * 2);

        // Mittelpunkt
        using var brDot = new SolidBrush(FarbeSymbol);
        g.FillEllipse(brDot, c.X - 1.2f, c.Y - 1.2f, 2.4f, 2.4f);

        // Punktnummer + Code blau
        float gap = S + 3f;
        float y   = c.Y - gap - fntNr.Size - 1;

        g.DrawString(Ergebnis.PunktNr, fntNr, brLbl, c.X + gap * 0.4f, y);
        y += fntNr.Size + 1f;

        if (!string.IsNullOrEmpty(Ergebnis.Code))
        {
            g.DrawString(Ergebnis.Code, fntCode, brLbl, c.X + gap * 0.4f, y);
            y += fntCode.Size + 1f;
        }

        // Höhe dunkelbraun
        if (Ergebnis.Ist3D)
            g.DrawString($"{Ergebnis.Hoehe:F3}", fntH, brH, c.X + gap * 0.4f, c.Y + gap);
    }
}

// ── Residual-Marker für Anschlusspunkte ───────────────────────────────────────
// Zeigt am Anschlusspunkt einen farbigen Kreis, dessen Farbe und Radius
// proportional zum Residuum ist.  Tooltip zeigt Werte.
public class OverlayResidualPunkt : DxfEntity
{
    public PunktResidum Residuum { get; }
    public double WX { get; }
    public double WY { get; }

    private readonly Color _farbe;
    private readonly float _radius;

    public OverlayResidualPunkt(PunktResidum res, double wx, double wy)
    {
        Residuum = res;
        WX       = wx;
        WY       = wy;
        Layer    = "Residual";

        // Farbe nach Gesamtabweichung in mm
        double vGes = Math.Sqrt(res.vQuer_mm * res.vQuer_mm +
                                res.vStrecke_mm * res.vStrecke_mm);
        if      (vGes >  20) { _farbe = Color.FromArgb(220, 20, 20); _radius = 6f; }
        else if (vGes >   8) { _farbe = Color.FromArgb(220, 140, 0); _radius = 5f; }
        else if (vGes >   3) { _farbe = Color.FromArgb(180, 200, 0); _radius = 4f; }
        else                 { _farbe = Color.FromArgb( 40, 180, 50); _radius = 3f; }
    }

    public override BoundingBox GetBounds() => new(WX - 1, WY - 1, WX + 1, WY + 1);
    public override double DistanceTo(double wx, double wy) =>
        Math.Sqrt((wx - WX) * (wx - WX) + (wy - WY) * (wy - WY));
    public override (double x, double y) GetNearestPoint(double wx, double wy) => (WX, WY);
    public override (double x, double y) GetSnapPoint  (double wx, double wy) => (WX, WY);

    public override string GetInfo() =>
        $"Anschlusspunkt {Residuum.PunktNr}  " +
        $"vQuer={Residuum.vQuer_mm:+0.0;-0.0} mm  " +
        $"vStr={Residuum.vStrecke_mm:+0.0;-0.0} mm  " +
        $"D={Residuum.StreckeH:F1} m";

    public override void Draw(Graphics g, Pen _pen,
        Func<double, double, PointF> ts, double scale)
    {
        var c = ts(WX, WY);
        float r = _radius;
        using var pen   = new Pen(_farbe, 1.5f);
        using var brush = new SolidBrush(Color.FromArgb(120, _farbe));
        using var fnt   = new Font("Arial", 7.5f, FontStyle.Regular, GraphicsUnit.Pixel);
        using var lbl   = new SolidBrush(_farbe);

        // Äußerer Residual-Ring
        g.DrawEllipse(pen, c.X - r - 2, c.Y - r - 2, (r + 2) * 2, (r + 2) * 2);
        g.FillEllipse(brush, c.X - r,   c.Y - r,     r * 2,        r * 2);

        // Kurzinfo (vGes in mm)
        double vGes = Math.Sqrt(Residuum.vQuer_mm * Residuum.vQuer_mm +
                                Residuum.vStrecke_mm * Residuum.vStrecke_mm);
        g.DrawString($"{vGes:F1}", fnt, lbl, c.X + r + 2, c.Y - fnt.Size / 2);
    }
}

// ── Fehlerellipse ──────────────────────────────────────────────────────────────
// Stellt Residuen als skalierbare Ellipse dar.
// Längsachse (a) = vStrecke in radialer Richtung (Station→Punkt),
// Querachse  (b) = vQuer senkrecht dazu.
// Skalierung wird vom DxfCanvas per Mausrad gesteuert.
public class OverlayFehlerEllipse : DxfEntity
{
    public string PunktNr     { get; }
    public double R           { get; }
    public double H           { get; }
    public double VStrecke_mm { get; }
    public double VQuer_mm    { get; }
    public double Phi_rad     { get; }   // Richtung Station→Punkt im Weltsystem [rad]
    public double Skalierung  { get; set; } = 1.0;

    private static readonly Color Farbe = Color.FromArgb(20, 80, 200);
    const int N = 36;

    public OverlayFehlerEllipse(string punktNr, double r, double h,
        double vStrecke_mm, double vQuer_mm, double phi_rad)
    {
        PunktNr     = punktNr;
        R           = r;  H = h;
        VStrecke_mm = vStrecke_mm;
        VQuer_mm    = vQuer_mm;
        Phi_rad     = phi_rad;
        Layer       = "FehlerEllipse";
    }

    public override BoundingBox GetBounds()
    {
        double d = Math.Max(Math.Abs(VStrecke_mm), Math.Abs(VQuer_mm)) * 0.001 * Skalierung + 0.1;
        return new(R - d, H - d, R + d, H + d);
    }

    public override double DistanceTo(double wx, double wy) => double.MaxValue;
    public override (double, double) GetNearestPoint(double wx, double wy) => (R, H);
    public override (double, double) GetSnapPoint(double wx, double wy)    => (R, H);
    public override string GetInfo() =>
        $"Fehlerellipse {PunktNr}  vL={VStrecke_mm:+0.0;-0.0} mm  vQ={VQuer_mm:+0.0;-0.0} mm";

    public override void Draw(Graphics g, Pen _,
        Func<double, double, PointF> ts, double scale)
    {
        double a = Math.Abs(VStrecke_mm) * 0.001 * Skalierung;
        double b = Math.Abs(VQuer_mm)    * 0.001 * Skalierung;
        double minR = 0.5 / scale;
        a = Math.Max(a, minR);
        b = Math.Max(b, minR);

        double cosPhi = Math.Cos(Phi_rad);
        double sinPhi = Math.Sin(Phi_rad);
        var pts = new PointF[N];

        for (int i = 0; i < N; i++)
        {
            double t  = 2 * Math.PI * i / N;
            double lx = a * Math.Cos(t);   // entlang Längsachse (radial)
            double ly = b * Math.Sin(t);   // entlang Querachse
            pts[i] = ts(R + lx * cosPhi - ly * sinPhi,
                        H + lx * sinPhi + ly * cosPhi);
        }

        using var pen = new Pen(Farbe, 1.5f);
        try { g.DrawPolygon(pen, pts); } catch { }

        // Mittelpunkt-Kreuz
        var c = ts(R, H);
        const float d = 3.5f;
        g.DrawLine(pen, c.X - d, c.Y, c.X + d, c.Y);
        g.DrawLine(pen, c.X, c.Y - d, c.X, c.Y + d);
    }
}
