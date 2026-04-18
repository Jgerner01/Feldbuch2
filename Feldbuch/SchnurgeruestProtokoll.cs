namespace Feldbuch;

using System.Globalization;

public static class SchnurgeruestProtokoll
{
    private static readonly string      VorlageName = "Schnurgeruest_Protokoll.xml";
    private static readonly CultureInfo IC          = CultureInfo.InvariantCulture;

    public static void Schreiben(
        StandpunktInfo? station,
        List<(double R, double H)> polygon,
        List<AbsteckPunkt> sgPunkte,
        string abstandStr)
    {
        if (!ProjektManager.ProtokollAktiv) return;
        string vorlagePfad = AppPfade.Get(VorlageName);
        if (!File.Exists(vorlagePfad)) return;

        string verzeichnis = ProjektManager.IstGeladen
            ? ProjektManager.ProjektVerzeichnis : AppPfade.Basis;

        try
        {
            var    jetzt   = DateTime.Now;
            string basis   = Path.Combine(verzeichnis, $"Schnurgeruest_{jetzt:yyyy-MM-dd_HH-mm-ss}");
            string rtfPfad = basis + ".rtf";
            string pngPfad = basis + "_skizze.png";

            var felder = new Dictionary<string, string>
            {
                ["Bearbeiter"]  = ProjektdatenManager.Bearbeiter,
                ["Zeitpunkt"]   = jetzt.ToString("dd.MM.yyyy    HH:mm:ss"),
                ["Standpunkt"]  = station != null
                    ? $"{station.PunktNr}   R={station.R:F3}   H={station.H:F3}   z={station.Orientierung_gon:F4} gon" : "–",
                ["Abstand"]     = $"{abstandStr} m",
                ["AnzahlEcken"] = polygon.Count.ToString(),
                ["LageplanHinweis"] = $"Skizze: {Path.GetFileName(pngPfad)}",
            };

            var zeilen = sgPunkte.Select(p => new Dictionary<string, string>
            {
                ["PunktNr"] = p.PunktNr,
                ["R"]       = p.R_soll.ToString("F3", IC),
                ["H"]       = p.H_soll.ToString("F3", IC),
                ["Hz"]      = station != null ? p.Hz_soll_gon.ToString("F4", IC) : "-",
                ["s"]       = station != null ? p.s_soll_m.ToString("F3", IC)    : "-",
            }).ToList();

            RtfProtokollGenerator.Schreiben(vorlagePfad, felder, zeilen, rtfPfad);

            // Skizze: Polygon (rot) + Schnurgerüst (blau)
            var allPunkte = sgPunkte.ToList();
            using var bmp = new System.Drawing.Bitmap(640, 480);
            using var g   = System.Drawing.Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            AbsteckungGrafik.DrawLageplanMitPolygon(g,
                new System.Drawing.Rectangle(0, 0, 640, 480),
                station, new List<AbsteckPunkt>(), polygon, sgPunkte, -1);
            bmp.Save(pngPfad, System.Drawing.Imaging.ImageFormat.Png);

            System.Windows.Forms.MessageBox.Show(
                $"Protokoll gespeichert:\n{rtfPfad}\n\nSkizze:\n{pngPfad}",
                "Protokoll", System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            System.Windows.Forms.MessageBox.Show(
                $"Protokoll konnte nicht geschrieben werden:\n{ex.Message}",
                "Protokoll-Fehler", System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Warning);
        }
    }
}
