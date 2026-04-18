namespace Feldbuch;

using System.Globalization;

public static class HochpunktherablegungProtokoll
{
    private static readonly string     VorlageName = "Hochpunktherablegung_Protokoll.xml";
    private static readonly CultureInfo IC          = CultureInfo.InvariantCulture;

    public static void Schreiben(
        HochpunktErgebnis       erg,
        List<HochpunktMessung>  messungen,
        string                  punktnr)
    {
        if (!ProjektManager.ProtokollAktiv) return;

        string vorlagePfad = AppPfade.Get(VorlageName);
        if (!File.Exists(vorlagePfad))
        {
            System.Windows.Forms.MessageBox.Show(
                $"Protokollvorlage nicht gefunden:\n{vorlagePfad}",
                "Protokoll-Fehler",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Warning);
            return;
        }

        string verzeichnis = ProjektManager.IstGeladen
            ? ProjektManager.ProjektVerzeichnis
            : AppPfade.Basis;

        try
        {
            var    jetzt    = DateTime.Now;
            string zielPfad = Path.Combine(verzeichnis,
                $"Hochpunktherablegung_{jetzt:yyyy-MM-dd_HH-mm-ss}.rtf");

            var felder = BaueFelder(erg, punktnr, jetzt);
            var zeilen = BaueTabellenzeilen(erg, messungen);

            RtfProtokollGenerator.Schreiben(vorlagePfad, felder, zeilen, zielPfad);

            System.Windows.Forms.MessageBox.Show(
                $"Protokoll gespeichert:\n{zielPfad}",
                "Protokoll",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            System.Windows.Forms.MessageBox.Show(
                $"Protokoll konnte nicht geschrieben werden:\n{ex.Message}",
                "Protokoll-Fehler",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Warning);
        }
    }

    private static Dictionary<string, string> BaueFelder(
        HochpunktErgebnis erg, string punktnr, DateTime zeitpunkt)
    {
        string dirInfo = erg.RedundanzDir > 0
            ? $"Richtungen – s0 = {erg.s0Dir_mm:F2} mm    r = {erg.RedundanzDir}    {erg.Iterationen} Iter." +
              (erg.Konvergiert ? "" : "  !! NICHT konvergiert !!")
            : $"Richtungen – r = 0  (eindeutig bestimmt, {erg.Iterationen} Iter.)";
        string hInfo = erg.RedundanzH > 0
            ? $"Hoehe – s0 = {erg.s0H_mm:F2} mm    r = {erg.RedundanzH}"
            : "Hoehe – r = 0  (eindeutig bestimmt)";

        return new Dictionary<string, string>
        {
            ["Bearbeiter"] = ProjektdatenManager.Bearbeiter,
            ["Zeitpunkt"]  = zeitpunkt.ToString("dd.MM.yyyy    HH:mm:ss"),
            ["PunktNr"]    = punktnr,
            ["R"]          = erg.R.ToString("F3", IC),
            ["H"]          = erg.H.ToString("F3", IC),
            ["Hoehe"]      = erg.Hoehe.ToString("F3", IC),
            ["S0DirInfo"]  = dirInfo,
            ["S0HInfo"]    = hInfo,
        };
    }

    private static List<Dictionary<string, string>> BaueTabellenzeilen(
        HochpunktErgebnis      erg,
        List<HochpunktMessung> messungen)
    {
        var resDict = erg.Residuen.ToDictionary(r => r.PunktNr, StringComparer.OrdinalIgnoreCase);
        var zeilen  = new List<Dictionary<string, string>>();

        foreach (var m in messungen)
        {
            resDict.TryGetValue(m.PunktNr, out var res);

            string pNr    = m.PunktNr + (res != null && !res.AktivDir ? "*" : "");
            string sStr   = res != null ? res.s_horiz.ToString("F1", IC) : "";
            string vDir   = res != null && res.AktivDir
                ? res.vDir_cc.ToString("+0.0;-0.0;0.0", IC) : "-";
            string hPiStr = res != null && res.AktivH
                ? res.HoehePi.ToString("F3", IC) : "-";
            string vH     = res != null && res.AktivH
                ? res.vH_mm.ToString("+0.0;-0.0;0.0", IC) : "-";

            double absDir = res != null && res.AktivDir ? Math.Abs(res.vDir_cc) : 0;
            double absH   = res != null && res.AktivH   ? Math.Abs(res.vH_mm)   : 0;
            string ampDir = absDir > 60 ? "3" : absDir > 20 ? "2" : absDir > 5 ? "1" : "";
            string ampH   = absH   > 30 ? "3" : absH   > 10 ? "2" : absH  > 3 ? "1" : "";

            zeilen.Add(new Dictionary<string, string>
            {
                ["PunktNr"]  = pNr,
                ["R"]        = m.R.ToString("F3", IC),
                ["H"]        = m.H.ToString("F3", IC),
                ["Hoehe"]    = m.Hoehe.ToString("F3", IC),
                ["iH"]       = m.iH.ToString("F3", IC),
                ["Hz"]       = m.Hz.ToString("F4", IC),
                ["z"]        = m.z.ToString("F4", IC),
                ["V"]        = m.V.ToString("F4", IC),
                ["zh"]       = m.Zielhöhe.ToString("F3", IC),
                ["s"]        = sStr,
                ["vDir"]     = vDir,
                ["HPi"]      = hPiStr,
                ["vH"]       = vH,
                ["_ampel"]   = ampDir != "" ? ampDir : ampH,
            });
        }
        return zeilen;
    }
}
