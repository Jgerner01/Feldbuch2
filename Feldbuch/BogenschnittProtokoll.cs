namespace Feldbuch;

using System.Globalization;

public static class BogenschnittProtokoll
{
    private static readonly string     VorlageName = "Bogenschnitt_Protokoll.xml";
    private static readonly CultureInfo IC          = CultureInfo.InvariantCulture;

    public static void Schreiben(
        BogenschnittErgebnis        erg,
        List<BogenschnittMessung>   messungen,
        string                      neupunkt)
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
                $"Bogenschnitt_{jetzt:yyyy-MM-dd_HH-mm-ss}.rtf");

            var felder = BaueFelder(erg, neupunkt, jetzt);
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
        BogenschnittErgebnis erg, string neupunkt, DateTime zeitpunkt)
    {
        string s0Info = erg.Redundanz > 0
            ? $"Standardabw. s0:  {erg.s0_mm:F2} mm     " +
              $"Redundanz r:  {erg.Redundanz}     " +
              (erg.Konvergiert
                  ? $"Iterationen:  {erg.Iterationen}  (konvergiert)"
                  : $"Iterationen:  {erg.Iterationen}  !! NICHT konvergiert !!")
            : $"Standardabw. s0:  -  (Redundanz r = 0, eindeutig bestimmt)";

        string zweiteLoesung = erg.ZweiLoesungen
            ? $"2. Lösung: R = {erg.R2.ToString("F3", IC)} m   H = {erg.H2.ToString("F3", IC)} m"
            : "";

        return new Dictionary<string, string>
        {
            ["Bearbeiter"]    = ProjektdatenManager.Bearbeiter,
            ["Zeitpunkt"]     = zeitpunkt.ToString("dd.MM.yyyy    HH:mm:ss"),
            ["Neupunkt"]      = neupunkt,
            ["R"]             = erg.R.ToString("F3", IC),
            ["H"]             = erg.H.ToString("F3", IC),
            ["S0Info"]        = s0Info,
            ["ZweiteLoesung"] = zweiteLoesung,
            ["HatZwei"]       = erg.ZweiLoesungen ? "1" : "0",
        };
    }

    private static List<Dictionary<string, string>> BaueTabellenzeilen(
        BogenschnittErgebnis       erg,
        List<BogenschnittMessung>  messungen)
    {
        var resDict = erg.Residuen.ToDictionary(r => r.PunktNr, StringComparer.OrdinalIgnoreCase);
        var zeilen  = new List<Dictionary<string, string>>();

        foreach (var m in messungen)
        {
            resDict.TryGetValue(m.PunktNr, out var res);

            string pNr  = m.PunktNr + (res != null && !res.Aktiv ? "*" : "");
            string v_mm = res != null && res.Aktiv
                ? res.vStrecke_mm.ToString("+0.0;-0.0;0.0", IC) : "-";
            double absV = res != null && res.Aktiv ? Math.Abs(res.vStrecke_mm) : 0;
            string amp  = absV > 30 ? "3" : absV > 10 ? "2" : absV > 3 ? "1" : "";

            zeilen.Add(new Dictionary<string, string>
            {
                ["PunktNr"] = pNr,
                ["R"]       = m.R.ToString("F3", IC),
                ["H"]       = m.H.ToString("F3", IC),
                ["Strecke"] = m.Strecke.ToString("F3", IC),
                ["v"]       = v_mm,
                ["_ampel"]  = amp,
            });
        }
        return zeilen;
    }
}
