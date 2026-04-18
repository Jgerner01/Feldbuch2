namespace Feldbuch;

using System.Globalization;

public static class PunktabsteckungProtokoll
{
    private static readonly string      VorlageName = "Punktabsteckung_Protokoll.xml";
    private static readonly CultureInfo IC          = CultureInfo.InvariantCulture;

    public static void Schreiben(StandpunktInfo? station, List<AbsteckPunkt> punkte)
    {
        if (!ProjektManager.ProtokollAktiv) return;
        string vorlagePfad = AppPfade.Get(VorlageName);
        if (!File.Exists(vorlagePfad)) return;

        string verzeichnis = ProjektManager.IstGeladen
            ? ProjektManager.ProjektVerzeichnis : AppPfade.Basis;

        try
        {
            var    jetzt    = DateTime.Now;
            string basis    = Path.Combine(verzeichnis, $"Punktabsteckung_{jetzt:yyyy-MM-dd_HH-mm-ss}");
            string rtfPfad  = basis + ".rtf";
            string pngPfad  = basis + "_lageplan.png";

            var felder = new Dictionary<string, string>
            {
                ["Bearbeiter"]   = ProjektdatenManager.Bearbeiter,
                ["Zeitpunkt"]    = jetzt.ToString("dd.MM.yyyy    HH:mm:ss"),
                ["Standpunkt"]   = station != null
                    ? $"{station.PunktNr}   R={station.R:F3}   H={station.H:F3}   z={station.Orientierung_gon:F4} gon"
                    : "–",
                ["AnzahlPunkte"]  = punkte.Count.ToString(),
                ["LageplanHinweis"] = File.Exists(pngPfad)
                    ? $"Lageplanskizze: {Path.GetFileName(pngPfad)}"
                    : "",
            };

            var zeilen = punkte.Select(p => new Dictionary<string, string>
            {
                ["PunktNr"] = p.PunktNr,
                ["R"]       = p.R_soll.ToString("F3", IC),
                ["H"]       = p.H_soll.ToString("F3", IC),
                ["Hz"]      = station != null ? p.Hz_soll_gon.ToString("F4", IC) : "-",
                ["s"]       = station != null ? p.s_soll_m.ToString("F3", IC)    : "-",
                ["Status"]  = p.Status,
            }).ToList();

            RtfProtokollGenerator.Schreiben(vorlagePfad, felder, zeilen, rtfPfad);

            // Lageplan-PNG
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
