namespace Feldbuch;

using System.Globalization;

public static class ProfilabsteckungProtokoll
{
    private static readonly string      VorlageName = "Profilabsteckung_Protokoll.xml";
    private static readonly CultureInfo IC          = CultureInfo.InvariantCulture;

    public static void Schreiben(
        StandpunktInfo? station, List<ProfilAbsteckPunkt> profile,
        string rA, string hA, string rE, string hE,
        string intervall, string halbbreite, string boesch)
    {
        if (!ProjektManager.ProtokollAktiv) return;
        string vorlagePfad = AppPfade.Get(VorlageName);
        if (!File.Exists(vorlagePfad)) return;

        string verzeichnis = ProjektManager.IstGeladen
            ? ProjektManager.ProjektVerzeichnis : AppPfade.Basis;

        try
        {
            var    jetzt   = DateTime.Now;
            string rtfPfad = Path.Combine(verzeichnis, $"Profilabsteckung_{jetzt:yyyy-MM-dd_HH-mm-ss}.rtf");

            var felder = new Dictionary<string, string>
            {
                ["Bearbeiter"]  = ProjektdatenManager.Bearbeiter,
                ["Zeitpunkt"]   = jetzt.ToString("dd.MM.yyyy    HH:mm:ss"),
                ["Standpunkt"]  = station != null
                    ? $"{station.PunktNr}   R={station.R:F3}   H={station.H:F3}   z={station.Orientierung_gon:F4} gon" : "–",
                ["AchseA"]      = $"R={rA}  H={hA}",
                ["AchseE"]      = $"R={rE}  H={hE}",
                ["Intervall"]   = $"{intervall} m",
                ["Halbbreite"]  = $"{halbbreite} m",
                ["Boesch"]      = $"1:{boesch}",
            };

            var zeilen = profile.Select(p =>
            {
                string dH     = p.DeltaH_m.HasValue
                    ? p.DeltaH_m.Value.ToString("+0.000;-0.000;0.000", IC) : "-";
                string typ    = p.DeltaH_m.HasValue
                    ? (p.DeltaH_m.Value >= 0 ? "Auftrag" : "Aushub") : "-";
                string boesch2 = p.BoeschLinks_m.HasValue
                    ? p.BoeschLinks_m.Value.ToString("F2", IC) : "-";
                string hGel   = p.H_Gelaende.HasValue
                    ? p.H_Gelaende.Value.ToString("F3", IC) : "-";
                string hz     = station != null ? p.Hz_soll_gon.ToString("F4", IC) : "-";
                string s      = station != null ? p.s_soll_m.ToString("F3", IC)    : "-";

                return new Dictionary<string, string>
                {
                    ["PunktNr"]  = p.PunktNr,
                    ["Station"]  = p.Station_m.ToString("F2", IC),
                    ["H_Plan"]   = p.H_plan.ToString("F3", IC),
                    ["H_Gel"]    = hGel,
                    ["DeltaH"]   = dH,
                    ["Typ"]      = typ,
                    ["Boesch"]   = boesch2,
                    ["Hz"]       = hz,
                    ["s"]        = s,
                };
            }).ToList();

            RtfProtokollGenerator.Schreiben(vorlagePfad, felder, zeilen, rtfPfad);

            System.Windows.Forms.MessageBox.Show(
                $"Protokoll gespeichert:\n{rtfPfad}",
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
