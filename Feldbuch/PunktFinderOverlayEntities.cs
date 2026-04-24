namespace Feldbuch;

// ══════════════════════════════════════════════════════════════════════════════
// PunktFinder-Overlay-Entities
//
// Werden im DXF-Viewer eingeblendet wenn eine Benutzerbestätigung nötig ist.
// Alle Symbole sind zoom-invariant in Weltkoordinaten, aber pixelgenau lesbar.
//
//   PunktFinderKreisEntity   – Suchkreis (Außen) und Auto-Zone (Innen)
//   PunktFinderKandidatEntity – Markierung für gefundene DXF-Punkte
//   PunktFinderRichtungsEntity – Richtungsstrahl (Winkel-only-Modus)
// ══════════════════════════════════════════════════════════════════════════════

public class PunktFinderKreisEntity : DxfEntity
{
    public double WX      { get; }
    public double WY      { get; }
    public double Radius  { get; }      // Weltkoordinaten [m]

    private readonly Color _farbe;
    private readonly bool  _gestrichelt;

    public PunktFinderKreisEntity(double wx, double wy, double radius,
        Color farbe, bool gestrichelt = false)
    {
        WX           = wx;
        WY           = wy;
        Radius       = radius;
        _farbe       = farbe;
        _gestrichelt = gestrichelt;
        Layer        = "PunktFinder";
    }

    public override BoundingBox GetBounds() =>
        new(WX - Radius, WY - Radius, WX + Radius, WY + Radius);

    public override double DistanceTo(double wx, double wy) => double.MaxValue;
    public override (double x, double y) GetNearestPoint(double wx, double wy) => (WX, WY);
    public override (double x, double y) GetSnapPoint(double wx, double wy)    => (WX, WY);
    public override string GetInfo() => $"Suchkreis R={Radius:F3} m";

    public override void Draw(Graphics g, Pen _pen,
        Func<double, double, PointF> ts, double scale)
    {
        var c  = ts(WX, WY);
        var r1 = ts(WX + Radius, WY);
        float pxRadius = r1.X - c.X;
        if (pxRadius < 1f) return;

        using var pen = new Pen(_farbe, 1.5f);
        if (_gestrichelt)
            pen.DashPattern = new float[] { 5f, 3f };

        g.DrawEllipse(pen, c.X - pxRadius, c.Y - pxRadius, pxRadius * 2, pxRadius * 2);
    }
}

public class PunktFinderKandidatEntity : DxfEntity
{
    public string PunktNr { get; }
    public double WX      { get; }
    public double WY      { get; }

    private readonly Color _farbe;
    private const float ArmPx   = 6f;
    private const float FontSize = 11f;

    public PunktFinderKandidatEntity(string punktNr, double wx, double wy, Color farbe)
    {
        PunktNr = punktNr;
        WX      = wx;
        WY      = wy;
        _farbe  = farbe;
        Layer   = "PunktFinder";
    }

    public override BoundingBox GetBounds() => new(WX - 1, WY - 1, WX + 1, WY + 1);
    public override double DistanceTo(double wx, double wy) => double.MaxValue;
    public override (double x, double y) GetNearestPoint(double wx, double wy) => (WX, WY);
    public override (double x, double y) GetSnapPoint(double wx, double wy)    => (WX, WY);
    public override string GetInfo() => $"Kandidat {PunktNr}";

    public override void Draw(Graphics g, Pen _pen,
        Func<double, double, PointF> ts, double scale)
    {
        var c = ts(WX, WY);
        using var pen   = new Pen(_farbe, 2f);
        using var brush = new SolidBrush(_farbe);
        using var fnt   = new Font("Arial", FontSize, FontStyle.Bold, GraphicsUnit.Pixel);

        // Kreuzlinien
        g.DrawLine(pen, c.X - ArmPx, c.Y, c.X + ArmPx, c.Y);
        g.DrawLine(pen, c.X, c.Y - ArmPx, c.X, c.Y + ArmPx);

        // Kleiner Kreis
        g.DrawEllipse(pen, c.X - 3f, c.Y - 3f, 6f, 6f);

        // Punktnummer
        if (!string.IsNullOrEmpty(PunktNr) && PunktNr != "?")
            g.DrawString(PunktNr, fnt, brush,
                c.X + ArmPx + 2f, c.Y - ArmPx - FontSize);
    }
}

public class PunktFinderRichtungsEntity : DxfEntity
{
    public double StationR    { get; }
    public double StationH    { get; }
    public double Richtung_gon { get; }
    public double Toleranz_gon { get; }   // halber Keil-Öffnungswinkel [gon]
    public double Laenge_m    { get; }

    private readonly Color _farbe;
    private const double GON2RAD = Math.PI / 200.0;

    public PunktFinderRichtungsEntity(
        double stationR, double stationH,
        double richtung_gon, double toleranz_gon, double laenge_m,
        Color farbe)
    {
        StationR     = stationR;
        StationH     = stationH;
        Richtung_gon = richtung_gon;
        Toleranz_gon = toleranz_gon;
        Laenge_m     = laenge_m;
        _farbe       = farbe;
        Layer        = "PunktFinder";
    }

    public override BoundingBox GetBounds() =>
        new(StationR - Laenge_m, StationH - Laenge_m,
            StationR + Laenge_m, StationH + Laenge_m);

    public override double DistanceTo(double wx, double wy) => double.MaxValue;
    public override (double x, double y) GetNearestPoint(double wx, double wy) => (StationR, StationH);
    public override (double x, double y) GetSnapPoint(double wx, double wy)    => (StationR, StationH);
    public override string GetInfo() => $"Richtung {Richtung_gon:F4} gon ± {Toleranz_gon * 100:F0} cc";

    public override void Draw(Graphics g, Pen _pen,
        Func<double, double, PointF> ts, double scale)
    {
        var origin = ts(StationR, StationH);

        // Richtung + Toleranzgrenzen berechnen
        double alpha1 = (Richtung_gon - Toleranz_gon) * GON2RAD;
        double alpha2 = (Richtung_gon + Toleranz_gon) * GON2RAD;
        double alphaM = Richtung_gon * GON2RAD;

        // Endpunkte berechnen (Weltkoordinaten → Bildschirm)
        var end1 = ts(StationR + Laenge_m * Math.Sin(alpha1),
                      StationH + Laenge_m * Math.Cos(alpha1));
        var end2 = ts(StationR + Laenge_m * Math.Sin(alpha2),
                      StationH + Laenge_m * Math.Cos(alpha2));
        var endM = ts(StationR + Laenge_m * Math.Sin(alphaM),
                      StationH + Laenge_m * Math.Cos(alphaM));

        using var pen = new Pen(_farbe, 1.5f) { DashPattern = new float[] { 6f, 3f } };
        using var penM = new Pen(Color.FromArgb(180, _farbe.R, _farbe.G, _farbe.B), 1f);
        using var brush = new SolidBrush(Color.FromArgb(40, _farbe.R, _farbe.G, _farbe.B));

        // Keilfläche füllen
        var keil = new PointF[] { origin, end1, end2 };
        g.FillPolygon(brush, keil);

        // Begrenzungslinien
        g.DrawLine(pen, origin, end1);
        g.DrawLine(pen, origin, end2);

        // Mittelachse
        g.DrawLine(penM, origin, endM);
    }
}
