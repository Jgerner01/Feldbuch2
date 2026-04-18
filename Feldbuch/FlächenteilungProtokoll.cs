namespace Feldbuch;

using System.Globalization;

public static class FlächenteilungProtokoll
{
    private static readonly string      VorlageName = "Flächenteilung_Protokoll.xml";
    private static readonly CultureInfo IC          = CultureInfo.InvariantCulture;

    public static void Schreiben(
        IList<(double R, double H)> polygon,
        FlächenteilungErgebnis erg)
    {
        if (!ProjektManager.ProtokollAktiv) return;
        string vorlagePfad = AppPfade.Get(VorlageName);
        if (!File.Exists(vorlagePfad)) return;

        string verzeichnis = ProjektManager.IstGeladen
            ? ProjektManager.ProjektVerzeichnis : AppPfade.Basis;

        try
        {
            var    jetzt   = DateTime.Now;
            string basis   = Path.Combine(verzeichnis, $"Flächenteilung_{jetzt:yyyy-MM-dd_HH-mm-ss}");
            string rtfPfad = basis + ".rtf";
            string pngPfad = basis + "_lageplan.png";

            var felder = new Dictionary<string, string>
            {
                ["Bearbeiter"]  = ProjektdatenManager.Bearbeiter,
                ["Zeitpunkt"]   = jetzt.ToString("dd.MM.yyyy    HH:mm:ss"),
                ["Verfahren"]   = erg.Verfahren,
                ["A_gesamt"]    = erg.A_gesamt.ToString("F4", IC) + " m²",
                ["A1_soll"]     = erg.A1_soll.ToString("F4", IC) + " m²",
                ["A1_ist"]      = erg.A1_ist.ToString("F4", IC) + " m²",
                ["A2"]          = erg.A2.ToString("F4", IC) + " m²",
                ["Differenz"]   = erg.Differenz.ToString("+0.0000;-0.0000;0.0000", IC) + " m²",
                ["Iterationen"] = erg.Iterationen.ToString(),
                ["LageplanHinweis"] = $"Lageplanskizze: {Path.GetFileName(pngPfad)}",
            };

            // Eckpunkte-Tabelle
            var zeilen = new List<Dictionary<string, string>>();
            for (int i = 0; i < polygon.Count; i++)
            {
                zeilen.Add(new Dictionary<string, string>
                {
                    ["Nr"] = (i + 1).ToString(),
                    ["R"]  = polygon[i].R.ToString("F3", IC),
                    ["H"]  = polygon[i].H.ToString("F3", IC),
                    ["Art"] = "",
                });
            }
            // Neue Grenzpunkte anhängen
            foreach (var (nr, r, h) in erg.NeueGrenzpunkte)
            {
                zeilen.Add(new Dictionary<string, string>
                {
                    ["Nr"]  = nr,
                    ["R"]   = r.ToString("F3", IC),
                    ["H"]   = h.ToString("F3", IC),
                    ["Art"] = "Grenzpunkt",
                });
            }

            RtfProtokollGenerator.Schreiben(vorlagePfad, felder, zeilen, rtfPfad);

            // Lageplan-PNG
            ExportLageplanPng(polygon, erg, pngPfad);

            MessageBox.Show(
                $"Protokoll gespeichert:\n{rtfPfad}\n\nLageplan:\n{pngPfad}",
                "Protokoll", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Protokoll konnte nicht geschrieben werden:\n{ex.Message}",
                "Protokoll-Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static void ExportLageplanPng(
        IList<(double R, double H)> polygon,
        FlächenteilungErgebnis erg,
        string pngPfad)
    {
        const int W = 800, H = 600;
        using var bmp = new System.Drawing.Bitmap(W, H);
        using var g   = System.Drawing.Graphics.FromImage(bmp);
        g.Clear(System.Drawing.Color.FromArgb(240, 240, 248));
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        if (polygon.Count < 2) { bmp.Save(pngPfad, System.Drawing.Imaging.ImageFormat.Png); return; }

        double rMin = polygon.Min(p => p.R), rMax = polygon.Max(p => p.R);
        double hMin = polygon.Min(p => p.H), hMax = polygon.Max(p => p.H);
        double span  = Math.Max(rMax - rMin, hMax - hMin);
        if (span < 1e-9) span = 1;
        double margin = 50.0;
        double scale  = (Math.Min(W, H) - 2 * margin) / span;
        double offR   = margin + (W - 2 * margin - (rMax - rMin) * scale) / 2 - rMin * scale;
        double offH   = H - margin - (H - 2 * margin - (hMax - hMin) * scale) / 2 + hMin * scale;

        System.Drawing.PointF Pt(double r, double h) =>
            new((float)(r * scale + offR), (float)(-h * scale + offH));
        System.Drawing.PointF[] Pts(IEnumerable<(double R, double H)> pts) =>
            pts.Select(p => Pt(p.R, p.H)).ToArray();

        // Teilflächen
        if (erg.Polygon1.Count >= 3)
        {
            using var br1 = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(110, 173, 216, 230));
            g.FillPolygon(br1, Pts(erg.Polygon1));
        }
        if (erg.Polygon2.Count >= 3)
        {
            using var br2 = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(110, 144, 238, 144));
            g.FillPolygon(br2, Pts(erg.Polygon2));
        }

        // Umriss
        if (polygon.Count >= 3)
        {
            using var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(30, 60, 130), 2f);
            g.DrawPolygon(pen, Pts(polygon));
        }

        // Trennlinie
        if (erg.NeueGrenzpunkte.Count >= 2)
        {
            using var penRed = new System.Drawing.Pen(System.Drawing.Color.Red, 2f)
            { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            g.DrawLine(penRed, Pt(erg.NeueGrenzpunkte[0].R, erg.NeueGrenzpunkte[0].H),
                               Pt(erg.NeueGrenzpunkte[1].R, erg.NeueGrenzpunkte[1].H));
        }

        // Grenzpunkte
        using var fntGp = new System.Drawing.Font("Arial Narrow", 9F, System.Drawing.FontStyle.Bold);
        foreach (var (nr, r, h) in erg.NeueGrenzpunkte)
        {
            var pt = Pt(r, h);
            g.FillEllipse(System.Drawing.Brushes.Yellow, pt.X - 7, pt.Y - 7, 14, 14);
            using var penY = new System.Drawing.Pen(System.Drawing.Color.DarkOrange, 1.5f);
            g.DrawEllipse(penY, pt.X - 7, pt.Y - 7, 14, 14);
            g.DrawString(nr, fntGp, System.Drawing.Brushes.DarkOrange, pt.X + 7, pt.Y - 9);
        }

        // Legende
        using var fntLeg = new System.Drawing.Font("Segoe UI", 9F);
        int lx = W - 200, ly = H - 80;
        g.FillRectangle(new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(200, 255, 255, 255)),
            lx - 5, ly - 5, 195, 70);
        g.FillRectangle(new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(110, 173, 216, 230)),
            lx, ly, 14, 14);
        g.DrawString($"A1 = {erg.A1_ist:F2} m²", fntLeg, System.Drawing.Brushes.DarkBlue, lx + 18, ly);
        g.FillRectangle(new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(110, 144, 238, 144)),
            lx, ly + 22, 14, 14);
        g.DrawString($"A2 = {erg.A2:F2} m²", fntLeg, System.Drawing.Brushes.DarkGreen, lx + 18, ly + 22);
        g.DrawString(erg.Verfahren, fntLeg, System.Drawing.Brushes.Black, lx, ly + 46);

        bmp.Save(pngPfad, System.Drawing.Imaging.ImageFormat.Png);
    }
}
