namespace Feldbuch;

using System.Drawing.Drawing2D;

/// <summary>
/// Zeichnet eine animierte digitale Dosenlibelle mit Gerätedreifuß.
///
/// Koordinatenkonvention (GeoCOM TMC_GetAngle1 / RPC 2003):
///   CrossIncline  (Querachse)  positiv → Instrument kippt nach rechts  → Blase wandert links
///   LengthIncline (Längsachse) positiv → Instrument kippt nach vorne   → Blase wandert oben
///
/// Blasenfarbe:
///   Grün   – innerhalb des Toleranzrings (gut zentriert)
///   Gelb   – außerhalb Toleranz, noch im Libellenglas
///   Rot    – Neigung größer als MaxNeigung (Randbereich / überschritten)
/// </summary>
public sealed class DosenlibellControl : Panel
{
    // ── Neigungswerte ─────────────────────────────────────────────────────────
    private double _kreuz   = 0.0;   // CrossIncline  [rad]
    private double _laengs  = 0.0;   // LengthIncline [rad]
    private bool   _gueltig = false; // false = Kompensator außerhalb Messbereich

    // Vollausschlag: Blase am Rand bei dieser Neigung
    // Dosenlibelle TPS1200: typisch ±8' ≈ ±0.0023 rad; wir zeigen ±6' ≈ 0.0017 rad
    // Für deutlich sichtbare Reaktion auf echte Geländekippungen: ±0.005 rad (≈ ±17')
    private const double MaxNeigung = 0.005;

    // Toleranzring-Radius relativ zum Libellen-Radius (z. B. 0.15 ≈ 1.5')
    private const double TolRel = 0.15;

    public DosenlibellControl()
    {
        DoubleBuffered = true;
        BackColor      = Color.FromArgb(22, 25, 34);
        MinimumSize    = new Size(80, 80);
    }

    /// <summary>Setzt neue Neigungswerte und löst Neuzeichnen aus.</summary>
    public void SetzeNeigung(double kreuzNeigung_rad, double laengsNeigung_rad,
                             bool gueltig = true)
    {
        _kreuz   = kreuzNeigung_rad;
        _laengs  = laengsNeigung_rad;
        _gueltig = gueltig;
        Invalidate();
    }

    /// <summary>
    /// Behält die letzte bekannte Position, markiert aber als ungültig
    /// (z. B. wenn Kompensator außerhalb seines Messbereichs ist).
    /// </summary>
    public void MarciereUngueltig()
    {
        _gueltig = false;
        Invalidate();
    }

    /// <summary>Setzt die Libelle in die Mitte (kein Gerät verbunden).</summary>
    public void Zuruecksetzen()
    {
        _kreuz   = 0;
        _laengs  = 0;
        _gueltig = false;
        Invalidate();
    }

    // ── Zeichnen ──────────────────────────────────────────────────────────────

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode      = SmoothingMode.AntiAlias;
        g.PixelOffsetMode    = PixelOffsetMode.HighQuality;
        g.InterpolationMode  = InterpolationMode.HighQualityBicubic;

        int seite = Math.Min(Width, Height);
        float cx  = Width  / 2f;
        float cy  = Height / 2f;

        // Außenradius des Dreiecks (lässt 4px Rand)
        float rAussen = seite / 2f - 5f;

        // ── Dreifuß-Dreieck ───────────────────────────────────────────────────
        // Gleichseitiges Dreieck, Spitze oben (−90°), dann je +120°
        var dreieck = DreieckPunkte(cx, cy, rAussen);

        // Hintergrund des Dreiecks (leicht aufgehellt)
        using var brushBg = new SolidBrush(Color.FromArgb(32, 38, 52));
        g.FillPolygon(brushBg, dreieck);

        // Dreifuß-Kontur
        using var penDreieck = new Pen(Color.FromArgb(110, 145, 190), 2.0f);
        g.DrawPolygon(penDreieck, dreieck);

        // Dreifuß-Fußschrauben: kleine Kreise an den Ecken
        foreach (var pt in dreieck)
        {
            using var brushFuss = new SolidBrush(Color.FromArgb(80, 120, 170));
            g.FillEllipse(brushFuss, pt.X - 5, pt.Y - 5, 10, 10);
            g.DrawEllipse(penDreieck, pt.X - 5, pt.Y - 5, 10, 10);
        }

        // ── Libellenring ──────────────────────────────────────────────────────
        // Kreis, eingeschrieben in das Dreieck (Inkreisradius = rAussen / √3)
        float rLibelle = rAussen * 0.68f;   // ≈ Inkreisradius des gleichseitigen Dreiecks

        // Libellen-Glashintergrund (leicht bläulich)
        using var brushGlas = new SolidBrush(Color.FromArgb(18, 30, 50));
        g.FillEllipse(brushGlas, cx - rLibelle, cy - rLibelle, rLibelle * 2, rLibelle * 2);

        // Äußerer Libellenring
        using var penLibelle = new Pen(Color.FromArgb(80, 150, 230), 2.5f);
        g.DrawEllipse(penLibelle, cx - rLibelle, cy - rLibelle, rLibelle * 2, rLibelle * 2);

        // Innerer Glanzring (Highlights für Glasoptik)
        using var penGlanz = new Pen(Color.FromArgb(40, 160, 200, 255), 1.0f);
        float rGlanz = rLibelle * 0.93f;
        g.DrawArc(penGlanz, cx - rGlanz, cy - rGlanz, rGlanz * 2, rGlanz * 2, 200, 130);

        // Toleranzring (gestrichelt)
        float rTol = rLibelle * (float)TolRel;
        using var penTol = new Pen(Color.FromArgb(120, 160, 180), 1.0f)
            { DashStyle = DashStyle.Dot };
        g.DrawEllipse(penTol, cx - rTol, cy - rTol, rTol * 2, rTol * 2);

        // ── Fadenkreuz ────────────────────────────────────────────────────────
        using var penKreuz = new Pen(Color.FromArgb(55, 80, 110), 1.0f);
        float kLen = rLibelle * 0.9f;
        g.DrawLine(penKreuz, cx - kLen, cy, cx + kLen, cy);
        g.DrawLine(penKreuz, cx, cy - kLen, cx, cy + kLen);

        // Mittelkreuz (kleine Marke in der Mitte)
        float kMid = 3f;
        using var penMid = new Pen(Color.FromArgb(80, 100, 130), 1.0f);
        g.DrawLine(penMid, cx - kMid, cy, cx + kMid, cy);
        g.DrawLine(penMid, cx, cy - kMid, cx, cy + kMid);

        // ── Libellen-Blase ────────────────────────────────────────────────────
        // CrossIncline > 0 → Gerät kippt rechts → Blase links → dx negativ
        // LengthIncline > 0 → Gerät kippt vorne → Blase hinten → in Bildschirm: oben → dy negativ
        double normiert_x = -_kreuz  / MaxNeigung;
        double normiert_y = -_laengs / MaxNeigung;

        double abstand = Math.Sqrt(normiert_x * normiert_x + normiert_y * normiert_y);
        if (abstand > 1.0)
        {
            normiert_x /= abstand;
            normiert_y /= abstand;
            abstand     = 1.0;
        }

        float rBlase   = rLibelle * 0.19f;
        float spielR   = rLibelle - rBlase * 1.1f;   // maximaler Mittelpunkt-Abstand
        float bx = cx + (float)(normiert_x * spielR);
        float by = cy + (float)(normiert_y * spielR);

        // Blasenfarbe
        Color blaseFarbe;
        if (abstand < TolRel)
            blaseFarbe = Color.FromArgb(30, 210, 80);    // grün: gut zentriert
        else if (abstand < 0.65)
            blaseFarbe = Color.FromArgb(230, 200, 20);   // gelb: leichte Abweichung
        else
            blaseFarbe = Color.FromArgb(230, 60, 40);    // rot: zu weit außen

        // Blase: Radialverlauf mit Glanzpunkt
        var blaseRect = new RectangleF(bx - rBlase, by - rBlase, rBlase * 2, rBlase * 2);
        using var pathBlase = new GraphicsPath();
        pathBlase.AddEllipse(blaseRect);

        using var brushBlase = new PathGradientBrush(pathBlase)
        {
            CenterColor    = Color.FromArgb(230, 255, 255, 255),
            SurroundColors = [blaseFarbe],
            CenterPoint    = new PointF(bx - rBlase * 0.25f, by - rBlase * 0.25f)
        };
        g.FillEllipse(brushBlase, blaseRect);

        using var penBlase = new Pen(Color.FromArgb(180, blaseFarbe), 1.2f);
        g.DrawEllipse(penBlase, blaseRect);

        // ── Beschriftung Himmelsrichtungen ─────────────────────────────────────
        // N oben, S unten (Längsachse ≙ N–S)
        using var fontRicht = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        using var brushRicht = new SolidBrush(Color.FromArgb(90, 120, 160));
        float abst = rLibelle + 10f;
        g.DrawString("N", fontRicht, brushRicht, cx - 4.5f, cy - abst - 9f);
        g.DrawString("S", fontRicht, brushRicht, cx - 4f,   cy + abst + 1f);

        // ── Warnanzeige: Kompensator außerhalb Messbereich ────────────────────
        if (!_gueltig)
        {
            // Orangefarbener Randring
            using var penWarn = new Pen(Color.FromArgb(200, 220, 100, 20), 3.5f);
            g.DrawEllipse(penWarn,
                cx - rLibelle - 2, cy - rLibelle - 2,
                (rLibelle + 2) * 2, (rLibelle + 2) * 2);

            // Gedimmte Überlagerung
            using var brushDim = new SolidBrush(Color.FromArgb(100, 10, 10, 10));
            g.FillEllipse(brushDim, cx - rLibelle, cy - rLibelle, rLibelle * 2, rLibelle * 2);

            // Warnsymbol "!"
            using var fontWarn  = new Font("Segoe UI", 14f, FontStyle.Bold);
            using var brushWarnT = new SolidBrush(Color.FromArgb(220, 180, 60));
            var warnText = "!";
            var textSize = g.MeasureString(warnText, fontWarn);
            g.DrawString(warnText, fontWarn, brushWarnT,
                cx - textSize.Width / 2, cy - textSize.Height / 2 - 8);

            using var fontKlein  = new Font("Segoe UI", 7f);
            using var brushKlein = new SolidBrush(Color.FromArgb(200, 200, 120, 40));
            var subText = "außerh. Bereich";
            var subSize = g.MeasureString(subText, fontKlein);
            g.DrawString(subText, fontKlein, brushKlein,
                cx - subSize.Width / 2, cy + 4f);
        }
    }

    // ── Hilfsmethoden ─────────────────────────────────────────────────────────

    private static PointF[] DreieckPunkte(float cx, float cy, float r)
    {
        // Gleichseitiges Dreieck: Spitze bei -90° (oben), Uhrzeigersinn +120°
        const double start = -Math.PI / 2.0;
        return
        [
            Punkt(cx, cy, r, start),
            Punkt(cx, cy, r, start + 2 * Math.PI / 3),
            Punkt(cx, cy, r, start + 4 * Math.PI / 3),
        ];
    }

    private static PointF Punkt(float cx, float cy, float r, double winkelRad) =>
        new(cx + r * (float)Math.Cos(winkelRad),
            cy + r * (float)Math.Sin(winkelRad));
}
