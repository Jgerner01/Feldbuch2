namespace Feldbuch;

using System.Drawing;
using System.Drawing.Drawing2D;

// ──────────────────────────────────────────────────────────────────────────────
// GDI+-Zeichenmethoden für Absteckungsmodule
// ──────────────────────────────────────────────────────────────────────────────
public static class AbsteckungGrafik
{
    // ── Lageplan ──────────────────────────────────────────────────────────────
    public static void DrawLageplan(
        Graphics g, Rectangle bounds,
        StandpunktInfo? station,
        IReadOnlyList<AbsteckPunkt> punkte,
        int currentIdx)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(245, 248, 252));

        if (punkte.Count == 0 && station == null) return;

        // Bounding Box
        var allR = punkte.Select(p => p.R_soll).ToList();
        var allH = punkte.Select(p => p.H_soll).ToList();
        if (station != null) { allR.Add(station.R); allH.Add(station.H); }
        if (allR.Count == 0) return;

        double minR = allR.Min(), maxR = allR.Max();
        double minH = allH.Min(), maxH = allH.Max();
        double spanR = Math.Max(maxR - minR, 1.0);
        double spanH = Math.Max(maxH - minH, 1.0);
        double margin = 0.12;
        minR -= spanR * margin; maxR += spanR * margin;
        minH -= spanH * margin; maxH += spanH * margin;
        spanR = maxR - minR; spanH = maxH - minH;

        double scaleX = bounds.Width  / spanR;
        double scaleY = bounds.Height / spanH;
        double scale  = Math.Min(scaleX, scaleY);
        double offX   = bounds.Left + (bounds.Width  - spanR * scale) / 2;
        double offY   = bounds.Top  + (bounds.Height - spanH * scale) / 2;

        PointF Scr(double R, double H) => new PointF(
            (float)(offX + (R - minR) * scale),
            (float)(offY + (maxH - H) * scale));

        // Gitterlinien
        using var gridPen = new Pen(Color.FromArgb(210, 220, 230), 1f) { DashStyle = DashStyle.Dot };
        for (double r = Math.Ceiling(minR / 10) * 10; r <= maxR; r += 10)
        {
            var p1 = Scr(r, minH); var p2 = Scr(r, maxH);
            g.DrawLine(gridPen, p1, p2);
        }
        for (double h = Math.Ceiling(minH / 10) * 10; h <= maxH; h += 10)
        {
            var p1 = Scr(minR, h); var p2 = Scr(maxR, h);
            g.DrawLine(gridPen, p1, p2);
        }

        // Verbindungslinie Station → aktueller Punkt
        if (station != null && currentIdx >= 0 && currentIdx < punkte.Count)
        {
            using var linePen = new Pen(Color.Orange, 1.5f) { DashStyle = DashStyle.Dash };
            g.DrawLine(linePen, Scr(station.R, station.H),
                                Scr(punkte[currentIdx].R_soll, punkte[currentIdx].H_soll));
        }

        // Alle Sollpunkte
        using var fntLabel = new Font("Segoe UI", 6.5f);
        for (int i = 0; i < punkte.Count; i++)
        {
            var p  = punkte[i];
            var pt = Scr(p.R_soll, p.H_soll);
            bool isCurrent = (i == currentIdx);
            bool isDone    = p.Status == "abgesteckt";
            float rad = isCurrent ? 8f : 5f;
            Color fill = isDone ? Color.SeaGreen : isCurrent ? Color.Gold : Color.RoyalBlue;
            using var br = new SolidBrush(fill);
            g.FillEllipse(br, pt.X - rad, pt.Y - rad, 2 * rad, 2 * rad);
            using var pen = new Pen(isCurrent ? Color.DarkOrange : Color.DimGray, 1f);
            g.DrawEllipse(pen, pt.X - rad, pt.Y - rad, 2 * rad, 2 * rad);
            g.DrawString(p.PunktNr, fntLabel, Brushes.Black, pt.X + rad + 1f, pt.Y - 8f);
        }

        // Standpunkt (roter Stern)
        if (station != null)
        {
            var sp  = Scr(station.R, station.H);
            float sr = 8f;
            using var br = new SolidBrush(Color.Crimson);
            g.FillEllipse(br, sp.X - sr, sp.Y - sr, 2 * sr, 2 * sr);
            using var pen = new Pen(Color.DarkRed, 1.5f);
            g.DrawEllipse(pen, sp.X - sr, sp.Y - sr, 2 * sr, 2 * sr);
            g.DrawLine(pen, sp.X - sr - 5, sp.Y, sp.X - sr, sp.Y);
            g.DrawLine(pen, sp.X + sr, sp.Y, sp.X + sr + 5, sp.Y);
            g.DrawLine(pen, sp.X, sp.Y - sr - 5, sp.X, sp.Y - sr);
            g.DrawLine(pen, sp.X, sp.Y + sr, sp.X, sp.Y + sr + 5);
            using var fntSt = new Font("Segoe UI", 7f, FontStyle.Bold);
            g.DrawString(station.PunktNr, fntSt, Brushes.DarkRed, sp.X + sr + 2f, sp.Y - 9f);
        }

        // Rahmen
        using var borderPen = new Pen(Color.FromArgb(160, 170, 190), 1f);
        g.DrawRectangle(borderPen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
    }

    // Lageplan mit Polygonlinie (Schnurgerüst / Achse)
    public static void DrawLageplanMitPolygon(
        Graphics g, Rectangle bounds,
        StandpunktInfo? station,
        IReadOnlyList<AbsteckPunkt> sollPunkte,
        IReadOnlyList<(double R, double H)>? polygon,
        IReadOnlyList<AbsteckPunkt> sgPunkte,
        int currentIdx)
    {
        // Zeichne Basislageplan
        var allPts = sollPunkte.Concat(sgPunkte).ToList();
        DrawLageplan(g, bounds, station, allPts, currentIdx);
        if (polygon == null || polygon.Count < 2) return;

        // Alle Koordinaten für Transformation sammeln
        var allR = polygon.Select(p => p.R).Concat(allPts.Select(p => p.R_soll)).ToList();
        var allH = polygon.Select(p => p.H).Concat(allPts.Select(p => p.H_soll)).ToList();
        if (station != null) { allR.Add(station.R); allH.Add(station.H); }

        double minR = allR.Min(), maxR = allR.Max();
        double minH = allH.Min(), maxH = allH.Max();
        double spanR = Math.Max(maxR - minR, 1.0);
        double spanH = Math.Max(maxH - minH, 1.0);
        double margin = 0.12;
        minR -= spanR * margin; maxR += spanR * margin;
        minH -= spanH * margin; maxH += spanH * margin;
        spanR = maxR - minR; spanH = maxH - minH;
        double scale = Math.Min(bounds.Width / spanR, bounds.Height / spanH);
        double offX  = bounds.Left + (bounds.Width  - spanR * scale) / 2;
        double offY  = bounds.Top  + (bounds.Height - spanH * scale) / 2;
        PointF Scr(double R, double H) => new PointF(
            (float)(offX + (R - minR) * scale),
            (float)(offY + (maxH - H) * scale));

        // Gebäudepolygon (rot gestrichelt)
        using var polyPen = new Pen(Color.Red, 2f) { DashStyle = DashStyle.Solid };
        var polyPts = polygon.Select(p => Scr(p.R, p.H)).ToArray();
        if (polyPts.Length >= 2)
        {
            g.DrawPolygon(polyPen, polyPts);
        }
        // Schnurgerüst (blau)
        if (sgPunkte.Count >= 2)
        {
            using var sgPen = new Pen(Color.Blue, 1.5f) { DashStyle = DashStyle.Dash };
            var sgPts = sgPunkte.Select(p => Scr(p.R_soll, p.H_soll)).ToArray();
            g.DrawPolygon(sgPen, sgPts);
        }
    }

    // ── Einweiser-Animation ───────────────────────────────────────────────────
    public static void DrawEinweiser(
        Graphics g, Rectangle bounds,
        double Hz_soll_gon, double s_soll_m,
        double? deltaHz_gon = null, double? deltaS_mm = null,
        double toleranz_mm = 10.0)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(28, 32, 42));

        int cx = bounds.Left + bounds.Width  / 2;
        int cy = bounds.Top  + bounds.Height / 2;
        int r  = Math.Min(bounds.Width, bounds.Height) / 2 - 18;
        if (r < 30) return;

        bool hasDeviation = deltaHz_gon.HasValue && deltaS_mm.HasValue;
        bool inToleranz   = hasDeviation &&
                            Math.Abs(deltaHz_gon!.Value) < 0.05 &&
                            Math.Abs(deltaS_mm!.Value) < toleranz_mm;

        // Hintergrundkreis
        using var bgBrush = new SolidBrush(inToleranz
            ? Color.FromArgb(0, 60, 15)
            : Color.FromArgb(28, 32, 42));
        g.FillEllipse(bgBrush, cx - r, cy - r, 2 * r, 2 * r);

        // Ringe (10 cm, 5 cm, 2 cm, 0.5 cm relativ zu Toleranz)
        (int frac, Color col)[] rings = {
            (1,   Color.FromArgb(100, 110, 130)),
            (3,   Color.FromArgb(150, 160, 180)),
            (7,   Color.Yellow),
            (10,  Color.LimeGreen)
        };
        foreach (var (frac, col) in rings)
        {
            int rr = r * frac / 10;
            using var pen = new Pen(col, 1.2f);
            g.DrawEllipse(pen, cx - rr, cy - rr, 2 * rr, 2 * rr);
        }

        // Fadenkreuz
        using var chPen = new Pen(Color.FromArgb(140, 150, 170), 0.8f);
        g.DrawLine(chPen, cx - r, cy, cx + r, cy);
        g.DrawLine(chPen, cx, cy - r, cx, cy + r);

        // Zentrum
        g.FillEllipse(Brushes.White, cx - 4, cy - 4, 8, 8);

        if (inToleranz)
        {
            using var fnt = new Font("Segoe UI", 15f, FontStyle.Bold);
            string txt = "✓ IN TOLERANZ";
            var sz = g.MeasureString(txt, fnt);
            g.DrawString(txt, fnt, Brushes.LimeGreen,
                cx - sz.Width / 2, cy - sz.Height / 2);
        }
        else if (hasDeviation)
        {
            // Hz-Korrektur (links/rechts drehen)
            double dhz = deltaHz_gon!.Value;
            if (Math.Abs(dhz) >= 0.05)
            {
                string arrow = dhz > 0 ? "◄ rechts drehen" : "► links drehen";
                string val   = $"{Math.Abs(dhz):F2} gon";
                using var fntA = new Font("Segoe UI", 11f, FontStyle.Bold);
                g.DrawString(arrow, fntA, new SolidBrush(Color.Orange),
                    bounds.Left + 8, bounds.Top + 8);
                g.DrawString(val, fntA, new SolidBrush(Color.Orange),
                    bounds.Left + 8, bounds.Top + 30);
            }

            // Streckendifferenz (vor/zurück)
            double ds = deltaS_mm!.Value;
            if (Math.Abs(ds) >= toleranz_mm)
            {
                string arrow = ds > 0 ? "▲  vorwärts" : "▼  zurück";
                string val   = $"{Math.Abs(ds):F0} mm";
                using var fntA = new Font("Segoe UI", 11f, FontStyle.Bold);
                g.DrawString(arrow, fntA, new SolidBrush(Color.Cyan),
                    bounds.Left + 8, bounds.Bottom - 52);
                g.DrawString(val, fntA, new SolidBrush(Color.Cyan),
                    bounds.Left + 8, bounds.Bottom - 30);
            }
        }

        // Untere Infozeile: Hz_soll + s_soll
        using var fntInfo  = new Font("Courier New", 8.5f);
        using var brInfo   = new SolidBrush(Color.FromArgb(180, 185, 200));
        string info = $"Hz: {Hz_soll_gon:F4} gon    s: {s_soll_m:F3} m";
        g.DrawString(info, fntInfo, brInfo, bounds.Left + 4, bounds.Bottom - 17);

        // Außenrahmen
        using var framePen = new Pen(Color.FromArgb(80, 90, 110), 1f);
        g.DrawRectangle(framePen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Overlay-Listen für DxfCanvas.OverlayEntities
    // ────────────────────────────────────────────────────────────────────────────

    // ── Punktabsteckung ───────────────────────────────────────────────────────
    public static List<DxfEntity> ErzeugePunktabsteckungOverlay(
        StandpunktInfo? station,
        IReadOnlyList<AbsteckPunkt> punkte,
        int currentIdx)
    {
        var list = new List<DxfEntity>();

        // Verbindungslinie Station → aktueller Punkt
        if (station != null && currentIdx >= 0 && currentIdx < punkte.Count)
            list.Add(new AbsteckVerbindungslinieEntity(
                station.R, station.H,
                punkte[currentIdx].R_soll, punkte[currentIdx].H_soll));

        // Sollpunkte
        for (int i = 0; i < punkte.Count; i++)
        {
            var p = punkte[i];
            list.Add(new AbsteckSollPunktEntity(
                p.R_soll, p.H_soll, p.PunktNr,
                i == currentIdx, p.Status == "abgesteckt"));
        }

        // Standpunkt
        if (station != null)
            list.Add(new AbsteckStandpunktEntity(station.R, station.H, station.PunktNr));

        return list;
    }

    // ── Achsabsteckung ────────────────────────────────────────────────────────
    public static List<DxfEntity> ErzeugeAchsabsteckungOverlay(
        StandpunktInfo? station,
        IReadOnlyList<AbsteckPunkt> punkte,
        double rA, double hA, double rE, double hE,
        int currentIdx = -1)
    {
        var list = new List<DxfEntity>();

        // Achslinie
        list.Add(new AbsteckAchseEntity(rA, hA, rE, hE));

        // Sollpunkte (Stationspunkte + Querprofilpunkte)
        for (int i = 0; i < punkte.Count; i++)
        {
            var p = punkte[i];
            list.Add(new AbsteckSollPunktEntity(
                p.R_soll, p.H_soll, p.PunktNr,
                i == currentIdx, p.Status == "abgesteckt"));
        }

        // Standpunkt
        if (station != null)
            list.Add(new AbsteckStandpunktEntity(station.R, station.H, station.PunktNr));

        return list;
    }

    // ── Schnurgerüst V2 (mit klickbaren Kanten, Messpunkt, Offset) ──────────────
    public static List<DxfEntity> ErzeugeSchnurgeruestV2Overlay(
        StandpunktInfo? station,
        IReadOnlyList<(string? Nr, double R, double H)> polygon,
        double sgAbstand,
        bool fixiert,
        int selectedAxis,
        (double R, double H)? gemessenerPunkt)
    {
        var list = new List<DxfEntity>();
        int n    = polygon.Count;

        if (n >= 2)
        {
            bool isOpen = n == 2;

            if (fixiert)
            {
                // Klickbare Kanten
                int kantenZahl = isOpen ? n - 1 : n;
                for (int i = 0; i < kantenZahl; i++)
                {
                    int j = (i + 1) % n;
                    list.Add(new AbsteckSGKanteEntity(i,
                        polygon[i].R, polygon[i].H,
                        polygon[j].R, polygon[j].H,
                        i == selectedAxis));
                }

                // SG-Versatzlinie
                if (!isOpen && Math.Abs(sgAbstand) > 1e-6)
                {
                    var polySimple = polygon.Select(p => (p.R, p.H)).ToList();
                    var sgPunkte   = AbsteckungRechner.BerechneSchnurgeruest(polySimple, sgAbstand, null);
                    if (sgPunkte.Count >= 2)
                        list.Add(new AbsteckPolygonEntity(
                            sgPunkte.Select(p => (p.R_soll, p.H_soll)),
                            Color.Blue, 1.5f, close: true,
                            dash: System.Drawing.Drawing2D.DashStyle.Dash));
                }
                else if (isOpen && Math.Abs(sgAbstand) > 1e-6)
                {
                    double dR  = polygon[1].R - polygon[0].R;
                    double dH  = polygon[1].H - polygon[0].H;
                    double len = Math.Sqrt(dR * dR + dH * dH);
                    if (len > 1e-9)
                    {
                        double nR = dH / len, nH = -dR / len;
                        list.Add(new AbsteckPolygonEntity(
                            new[] {
                                (polygon[0].R + nR * sgAbstand, polygon[0].H + nH * sgAbstand),
                                (polygon[1].R + nR * sgAbstand, polygon[1].H + nH * sgAbstand),
                            },
                            Color.Blue, 1.5f, close: false,
                            dash: System.Drawing.Drawing2D.DashStyle.Dash));
                    }
                }
            }
            else
            {
                // Noch nicht fixiert: einfache Polylinie
                list.Add(new AbsteckPolygonEntity(
                    polygon.Select(p => (p.R, p.H)),
                    Color.Red, 2f, close: !isOpen,
                    dash: System.Drawing.Drawing2D.DashStyle.DashDot));
            }

            // Eckpunkte
            for (int i = 0; i < n; i++)
            {
                string nr = polygon[i].Nr ?? $"{i + 1}";
                list.Add(new AbsteckSollPunktEntity(
                    polygon[i].R, polygon[i].H, nr, false, false));
            }
        }

        // Gemessener Punkt + Lotstrecke
        if (gemessenerPunkt.HasValue)
        {
            double? aR1 = null, aH1 = null, aR2 = null, aH2 = null;
            if (selectedAxis >= 0 && n >= 2)
            {
                int j = (selectedAxis + 1) % n;
                aR1 = polygon[selectedAxis].R; aH1 = polygon[selectedAxis].H;
                aR2 = polygon[j].R;            aH2 = polygon[j].H;
            }
            list.Add(new AbsteckMesspunktEntity(
                gemessenerPunkt.Value.R, gemessenerPunkt.Value.H,
                aR1, aH1, aR2, aH2));
        }

        if (station != null)
            list.Add(new AbsteckStandpunktEntity(station.R, station.H, station.PunktNr));

        return list;
    }

    // ── Schnurgerüst (Altversion) ─────────────────────────────────────────────
    public static List<DxfEntity> ErzeugeSchnurgeruestOverlay(
        StandpunktInfo? station,
        IReadOnlyList<(double R, double H)> polygon,
        IReadOnlyList<AbsteckPunkt> sgPunkte,
        int currentIdx = -1)
    {
        var list = new List<DxfEntity>();

        // Gebäudepolygon (rot)
        if (polygon.Count >= 2)
            list.Add(new AbsteckPolygonEntity(polygon, Color.Red, 2f));

        // Schnurgerüst-Linien (blau gestrichelt)
        if (sgPunkte.Count >= 2)
            list.Add(new AbsteckPolygonEntity(
                sgPunkte.Select(p => (p.R_soll, p.H_soll)), Color.Blue, 1.5f,
                close: true, dash: System.Drawing.Drawing2D.DashStyle.Dash));

        // SG-Punkte
        for (int i = 0; i < sgPunkte.Count; i++)
        {
            var p = sgPunkte[i];
            list.Add(new AbsteckSollPunktEntity(
                p.R_soll, p.H_soll, p.PunktNr,
                i == currentIdx, p.Status == "abgesteckt"));
        }

        // Standpunkt
        if (station != null)
            list.Add(new AbsteckStandpunktEntity(station.R, station.H, station.PunktNr));

        return list;
    }

    // ── Rasterabsteckung ──────────────────────────────────────────────────────
    public static List<DxfEntity> ErzeugeRasterabsteckungOverlay(
        StandpunktInfo? station,
        IReadOnlyList<AbsteckPunkt> punkte,
        int currentIdx = -1)
        => ErzeugePunktabsteckungOverlay(station, punkte, currentIdx);

    // ── Profilabsteckung (keine Grafik, Tabelle im Vordergrund) ──────────────
    public static List<DxfEntity> ErzeugeProfilabsteckungOverlay(
        StandpunktInfo? station,
        double rA, double hA, double rE, double hE,
        IReadOnlyList<AbsteckPunkt> punkte,
        int currentIdx = -1)
        => ErzeugeAchsabsteckungOverlay(station, punkte, rA, hA, rE, hE, currentIdx);

    // ── Flächenteilung ────────────────────────────────────────────────────────
    public static List<DxfEntity> ErzeugeFlächenteilungOverlay(
        IList<(double R, double H)> polygon,
        FlächenteilungErgebnis? erg)
    {
        var list = new List<DxfEntity>();

        if (erg != null)
        {
            // A1-Fläche (hellblau)
            if (erg.Polygon1.Count >= 3)
                list.Add(new AbsteckFlächeEntity(erg.Polygon1,
                    Color.FromArgb(100, 173, 216, 230)));
            // A2-Fläche (hellgrün)
            if (erg.Polygon2.Count >= 3)
                list.Add(new AbsteckFlächeEntity(erg.Polygon2,
                    Color.FromArgb(100, 144, 238, 144)));
        }

        // Polygon-Umriss (dunkelblau)
        if (polygon.Count >= 3)
            list.Add(new AbsteckPolygonEntity(polygon,
                Color.FromArgb(30, 60, 130), 2f));

        // Trennlinie + Grenzpunkte
        if (erg != null)
        {
            if (erg.NeueGrenzpunkte.Count >= 2)
                list.Add(new AbsteckPolygonEntity(
                    erg.NeueGrenzpunkte.Select(g => (g.R, g.H)),
                    Color.Red, 2f, close: false,
                    dash: System.Drawing.Drawing2D.DashStyle.Dash));

            foreach (var (nr, r, h) in erg.NeueGrenzpunkte)
                list.Add(new AbsteckGrenzpunktEntity(r, h, nr));
        }

        return list;
    }

    // ── Lageplan als Bitmap exportieren (für Protokoll-PNG) ──────────────────
    public static Bitmap ExportLageplan(
        StandpunktInfo? station, IReadOnlyList<AbsteckPunkt> punkte,
        int width = 640, int height = 480)
    {
        var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        DrawLageplan(g, new Rectangle(0, 0, width, height), station, punkte, -1);
        return bmp;
    }
}
