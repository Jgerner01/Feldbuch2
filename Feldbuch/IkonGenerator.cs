namespace Feldbuch;

using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

// ──────────────────────────────────────────────────────────────────────────────
// IkonGenerator – erstellt fehlende Toolbar-Icons per GDI+ beim Programmstart.
//
// Erzeugte Icons (36 × 36 px, transparent):
//   toolbar_edm_prisma.png  – Rückstrahler-Prisma (Dreiecks-Eckwürfel)
//   toolbar_edm_rl.png      – Reflektorlosmessung (Laserstrahl auf Wand)
//   toolbar_laser.png       – Laserpointer (horizontaler Strahl)
// ──────────────────────────────────────────────────────────────────────────────
internal static class IkonGenerator
{
    public static void EnsureIcons()
    {
        try
        {
            var dir = IconLoader.IconVerzeichnis;
            Directory.CreateDirectory(dir);
            ErstellePrismaIkon      (Path.Combine(dir, "toolbar_edm_prisma.png"));
            ErstelleReflektorlosIkon(Path.Combine(dir, "toolbar_edm_rl.png"));
            ErstelleLaserIkon       (Path.Combine(dir, "toolbar_laser.png"));
        }
        catch (Exception ex) { ErrorLogger.Log("IkonGenerator.EnsureIcons", ex); }
    }

    // ── Prisma-Icon ────────────────────────────────────────────────────────────
    // Zeichnet einen kreisförmigen Prismenhalter mit Eckwürfel-Retroreflektor:
    //   - Äußerer Kreis (Gehäuse)
    //   - Gleichseitiges Dreieck im Kreis (Reflexionsfläche)
    //   - Drei Linien vom Mittelpunkt zu den Ecken (Fasetten)
    //   - Glanzpunkt oben-rechts
    private static void ErstellePrismaIkon(string pfad)
    {
        if (File.Exists(pfad)) return;
        using var bmp = new Bitmap(36, 36);
        using var g   = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        float cx = 18f, cy = 18f;

        // Äußerer Kreis – Prismengehäuse
        float outerR = 15.5f;
        using (var fill = new SolidBrush(Color.FromArgb(25, 100, 160, 210)))
            g.FillEllipse(fill, cx - outerR, cy - outerR, outerR * 2, outerR * 2);
        using (var pen = new Pen(Color.FromArgb(180, 200, 240), 1.8f))
            g.DrawEllipse(pen, cx - outerR, cy - outerR, outerR * 2, outerR * 2);

        // Gleichseitiges Dreieck (Eckwürfelfläche), leicht nach oben versetzt
        float triR   = 11.5f;
        float sqr3h  = (float)(triR * Math.Sqrt(3.0) / 2.0);
        float offY   = 1.5f;
        var tri = new PointF[]
        {
            new(cx,            cy - triR   + offY),
            new(cx + sqr3h,    cy + triR / 2 + offY),
            new(cx - sqr3h,    cy + triR / 2 + offY)
        };

        // Dreieck-Füllung: Verlauf blau-weiß
        using var lgb = new LinearGradientBrush(
            new PointF(cx - triR, cy - triR + offY),
            new PointF(cx + triR, cy + triR + offY),
            Color.FromArgb(210, 220, 245, 255),
            Color.FromArgb(200, 70,  120, 200));
        g.FillPolygon(lgb, tri);

        // Dreieck-Kontur
        using (var pen = new Pen(Color.FromArgb(230, 245, 255), 1.5f))
            g.DrawPolygon(pen, tri);

        // Eckwürfel-Fasetten: drei Linien vom Mittelpunkt zu den Ecken
        var fCenter = new PointF(cx, cy + offY);
        using (var pen = new Pen(Color.FromArgb(160, 255, 255, 255), 1f))
        {
            g.DrawLine(pen, fCenter, tri[0]);
            g.DrawLine(pen, fCenter, tri[1]);
            g.DrawLine(pen, fCenter, tri[2]);
        }

        // Mittelpunkt (Eckwürfel-Spitze)
        using (var brush = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
            g.FillEllipse(brush, fCenter.X - 1.8f, fCenter.Y - 1.8f, 3.6f, 3.6f);

        // Glanzpunkt oben-rechts im Dreieck
        using (var brush = new SolidBrush(Color.FromArgb(170, 255, 255, 255)))
            g.FillEllipse(brush, cx + 2.5f, cy - triR * 0.5f + offY, 5f, 4f);

        bmp.Save(pfad, ImageFormat.Png);
    }

    // ── Reflektorlos-Icon ─────────────────────────────────────────────────────
    // Zeichnet eine Messung ohne Prisma:
    //   - Wand-/Oberflächenstruktur rechts (gemauerte Blöcke)
    //   - Roter Laserstrahl von links zur Wand
    //   - Leuchtender Auftreffpunkt
    //   - Kleiner Kreis als Geräte-Sender links
    private static void ErstelleReflektorlosIkon(string pfad)
    {
        if (File.Exists(pfad)) return;
        using var bmp = new Bitmap(36, 36);
        using var g   = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        // Wandfläche rechts
        using (var fill = new SolidBrush(Color.FromArgb(55, 140, 165, 210)))
            g.FillRectangle(fill, 26, 4, 9, 28);
        using (var pen = new Pen(Color.FromArgb(155, 175, 220), 1.5f))
            g.DrawRectangle(pen, 26, 4, 9, 28);
        // Ziegel-Linien
        using (var pen = new Pen(Color.FromArgb(70, 175, 200, 240), 1f))
        {
            g.DrawLine(pen, 26, 12, 35, 12);
            g.DrawLine(pen, 26, 20, 35, 20);
            g.DrawLine(pen, 26, 28, 35, 28);
            g.DrawLine(pen, 30,  4, 30, 12);
            g.DrawLine(pen, 30, 20, 30, 28);
        }

        // Geräte-Sender (kleiner Kreis links)
        float srcX = 4f, srcY = 15f;
        using (var fill = new SolidBrush(Color.FromArgb(160, 185, 225)))
            g.FillEllipse(fill, srcX, srcY, 7f, 7f);
        using (var pen = new Pen(Color.FromArgb(200, 220, 255), 1.2f))
            g.DrawEllipse(pen, srcX, srcY, 7f, 7f);

        // Laserstrahl (rot) vom Sender zur Wand
        float beamY = 18.5f;
        using (var pen = new Pen(Color.FromArgb(55, 255, 60, 30), 4.5f))   // Glow
            g.DrawLine(pen, srcX + 7, beamY, 26, beamY);
        using (var pen = new Pen(Color.FromArgb(220, 255, 50, 20), 1.8f))  // Kern
            g.DrawLine(pen, srcX + 7, beamY, 26, beamY);

        // Auftreffpunkt – Leuchtfleck
        using (var brush = new SolidBrush(Color.FromArgb(180, 255, 90, 50)))
            g.FillEllipse(brush, 23, beamY - 5, 10, 10);
        using (var brush = new SolidBrush(Color.FromArgb(255, 255, 220, 200)))
            g.FillEllipse(brush, 25.5f, beamY - 2.5f, 5, 5);

        bmp.Save(pfad, ImageFormat.Png);
    }

    // ── Laser-Icon ─────────────────────────────────────────────────────────────
    // Zeichnet den Laserpointer-Schalter:
    //   - Instrument-Körper (Rechteck) links
    //   - Teleskop-Schlitz
    //   - Horizontaler Laserstrahl (Rot, mit Glow)
    //   - Leuchtpunkt am Ende
    private static void ErstelleLaserIkon(string pfad)
    {
        if (File.Exists(pfad)) return;
        using var bmp = new Bitmap(36, 36);
        using var g   = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        // Instrument-Körper
        using (var fill = new SolidBrush(Color.FromArgb(145, 170, 220)))
            g.FillRectangle(fill, 2, 11, 13, 14);
        using (var pen = new Pen(Color.FromArgb(200, 220, 255), 1.5f))
            g.DrawRectangle(pen, 2, 11, 13, 14);

        // Teleskop-Sehschlitze (horizontale Linien)
        using (var pen = new Pen(Color.FromArgb(160, 195, 240), 1f))
        {
            g.DrawLine(pen, 4, 15, 12, 15);
            g.DrawLine(pen, 4, 21, 12, 21);
        }

        // Laser-Austrittsöffnung (kleine rote Ellipse rechts am Körper)
        using (var brush = new SolidBrush(Color.FromArgb(255, 255, 50, 20)))
            g.FillEllipse(brush, 14, 16.5f, 3, 3);

        // Laser-Glow (breiter, transparenter Kern)
        float beamY = 18f;
        using (var pen = new Pen(Color.FromArgb(50, 255, 70, 30), 6f))
            g.DrawLine(pen, 17, beamY, 33, beamY);

        // Laserstrahl (roter Kern)
        using (var pen = new Pen(Color.FromArgb(230, 255, 45, 15), 2f))
            g.DrawLine(pen, 17, beamY, 32, beamY);

        // Leuchtpunkt am Ende
        using (var brush = new SolidBrush(Color.FromArgb(180, 255, 80, 50)))
            g.FillEllipse(brush, 29.5f, beamY - 4.5f, 9, 9);
        using (var brush = new SolidBrush(Color.FromArgb(255, 255, 230, 220)))
            g.FillEllipse(brush, 31f, beamY - 2.5f, 5, 5);

        bmp.Save(pfad, ImageFormat.Png);
    }
}
