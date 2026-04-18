namespace Feldbuch;

using System.Globalization;

public static class RueckwaertsschnittProtokoll
{
    private static readonly string     VorlageName = "Rueckwaertsschnitt_Protokoll.xml";
    private static readonly CultureInfo IC          = CultureInfo.InvariantCulture;

    public static void Schreiben(
        RueckwaertsschnittErgebnis       erg,
        List<RueckwaertsschnittPunkt>    punkte,
        string                           standpunkt)
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
                $"Rueckwaertsschnitt_{jetzt:yyyy-MM-dd_HH-mm-ss}.rtf");

            var felder = BaueFelder(erg, standpunkt, jetzt);
            var zeilen = BaueTabellenzeilen(erg, punkte);

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
        RueckwaertsschnittErgebnis erg, string standpunkt, DateTime zeitpunkt)
    {
        string s0Info = erg.Redundanz > 0
            ? $"Standardabw. s0:  {erg.s0_mm:F2} mm     " +
              $"Redundanz r:  {erg.Redundanz}     " +
              (erg.Konvergiert
                  ? $"Iterationen:  {erg.Iterationen}  (konvergiert)"
                  : $"Iterationen:  {erg.Iterationen}  !! NICHT konvergiert !!")
            : $"Standardabw. s0:  -  (Redundanz r = 0, eindeutig bestimmt)";

        return new Dictionary<string, string>
        {
            ["Bearbeiter"]     = ProjektdatenManager.Bearbeiter,
            ["Zeitpunkt"]      = zeitpunkt.ToString("dd.MM.yyyy    HH:mm:ss"),
            ["Standpunktnr"]   = standpunkt,
            ["R"]              = erg.R.ToString("F3", IC),
            ["H"]              = erg.H.ToString("F3", IC),
            ["Orientierung"]   = erg.Orientierung_gon.ToString("F4", IC),
            ["S0Info"]         = s0Info,
            ["KritKreis"]      = erg.KritischerKreis,
            ["HatKritKreis"]   = string.IsNullOrEmpty(erg.KritischerKreis) ? "0" : "1",
        };
    }

    private static List<Dictionary<string, string>> BaueTabellenzeilen(
        RueckwaertsschnittErgebnis    erg,
        List<RueckwaertsschnittPunkt> punkte)
    {
        var resDict = erg.Residuen.ToDictionary(r => r.PunktNr, StringComparer.OrdinalIgnoreCase);
        var zeilen  = new List<Dictionary<string, string>>();

        foreach (var p in punkte)
        {
            resDict.TryGetValue(p.PunktNr, out var res);

            string pNr  = p.PunktNr + (res != null && !res.Aktiv ? "*" : "");
            string v_cc = res != null && res.Aktiv
                ? res.vWinkel_cc.ToString("+0.0;-0.0;0.0", IC) : "-";
            double absV = res != null && res.Aktiv ? Math.Abs(res.vWinkel_cc) : 0;
            string amp  = absV > 60 ? "3" : absV > 20 ? "2" : absV > 5 ? "1" : "";

            zeilen.Add(new Dictionary<string, string>
            {
                ["PunktNr"] = pNr,
                ["R"]       = p.R.ToString("F3", IC),
                ["H"]       = p.H.ToString("F3", IC),
                ["HZ"]      = p.HZ.ToString("F4", IC),
                ["s"]       = res != null ? res.StreckeH.ToString("F1", IC) : "",
                ["v"]       = v_cc,
                ["_ampel"]  = amp,
            });
        }
        return zeilen;
    }
}
