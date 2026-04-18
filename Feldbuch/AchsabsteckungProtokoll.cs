namespace Feldbuch;

using System.Globalization;

public static class AchsabsteckungProtokoll
{
    private static readonly string      VorlageName = "Achsabsteckung_Protokoll.xml";
    private static readonly CultureInfo IC          = CultureInfo.InvariantCulture;

    public static void Schreiben(
        StandpunktInfo? station, List<AbsteckPunkt> punkte,
        string rA, string hA, string rE, string hE,
        string intervall, string offsets)
    {
        if (!ProjektManager.ProtokollAktiv) return;
        string vorlagePfad = AppPfade.Get(VorlageName);
        if (!File.Exists(vorlagePfad)) return;

        string verzeichnis = ProjektManager.IstGeladen
            ? ProjektManager.ProjektVerzeichnis : AppPfade.Basis;

        try
        {
            var    jetzt   = DateTime.Now;
            string basis   = Path.Combine(verzeichnis, $"Achsabsteckung_{jetzt:yyyy-MM-dd_HH-mm-ss}");
            string rtfPfad = basis + ".rtf";
            string pngPfad = basis + "_lageplan.png";

            var felder = new Dictionary<string, string>
            {
                ["Bearbeiter"]  = ProjektdatenManager.Bearbeiter,
                ["Zeitpunkt"]   = jetzt.ToString("dd.MM.yyyy    HH:mm:ss"),
                ["Standpunkt"]  = station != null
                    ? $"{station.PunktNr}   R={station.R:F3}   H={station.H:F3}   z={station.Orientierung_gon:F4} gon" : "–",
                ["AchseA"]      = $"R={rA}  H={hA}",
                ["AchseE"]      = $"R={rE}  H={hE}",
                ["Intervall"]   = $"{intervall} m",
                ["Offsets"]     = offsets,
                ["AnzahlPunkte"] = punkte.Count.ToString(),
                ["LageplanHinweis"] = File.Exists(pngPfad) ? $"Lageplanskizze: {Path.GetFileName(pngPfad)}" : "",
            };

            var zeilen = punkte.Select(p => new Dictionary<string, string>
            {
                ["PunktNr"]  = p.PunktNr,
                ["Station"]  = p.Label,
                ["R"]        = p.R_soll.ToString("F3", IC),
                ["H"]        = p.H_soll.ToString("F3", IC),
                ["Abs"]      = p.Abszisse_m.HasValue ? p.Abszisse_m.Value.ToString("F3", IC) : "",
                ["Ord"]      = p.Ordinate_m.HasValue ? p.Ordinate_m.Value.ToString("F3", IC) : "",
                ["Hz"]       = station != null ? p.Hz_soll_gon.ToString("F4", IC) : "-",
                ["s"]        = station != null ? p.s_soll_m.ToString("F3", IC) : "-",
            }).ToList();

            RtfProtokollGenerator.Schreiben(vorlagePfad, felder, zeilen, rtfPfad);
            using var bmp = AbsteckungGrafik.ExportLageplan(station, punkte);
            bmp.Save(pngPfad, System.Drawing.Imaging.ImageFormat.Png);

            System.Windows.Forms.MessageBox.Show(
                $"Protokoll gespeichert:\n{rtfPfad}\n\nLageplan:\n{pngPfad}",
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
