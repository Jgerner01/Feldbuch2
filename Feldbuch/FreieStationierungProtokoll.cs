namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// FreieStationierungProtokoll
//
// Dünner Wrapper: bereitet die Laufzeitdaten auf und delegiert die
// RTF-Erzeugung an den RtfProtokollGenerator.
//
// Vorlage: FreieStationierung_Protokoll.xml  (neben der EXE)
// Ausgabe: FreieStationierung_YYYY-MM-DD_HH-mm-ss.rtf  (Projektverzeichnis)
// ──────────────────────────────────────────────────────────────────────────────
public static class FreieStationierungProtokoll
{
    private static readonly string VorlageName = "FreieStationierung_Protokoll.xml";

    public static void Schreiben(
        StationierungsErgebnis    erg,
        List<StationierungsPunkt> punkte,
        bool[]                    aktHz,
        bool[]                    aktStr,
        bool[]                    aktHoe,
        string                    standpunkt,
        double                    iH)
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
                $"FreieStationierung_{jetzt:yyyy-MM-dd_HH-mm-ss}.rtf");

            var felder = BaueFelder(erg, standpunkt, iH, jetzt, punkte.Count);
            var zeilen = BaueTabellenzeilen(erg, punkte, aktHz, aktStr, aktHoe);

            RtfProtokollGenerator.Schreiben(vorlagePfad, felder, zeilen, zielPfad);
        }
        catch (Exception ex)
        {
            System.Windows.Forms.MessageBox.Show(
                $"Protokoll konnte nicht geschrieben werden:\n{ex.Message}\n\nZiel: {verzeichnis}",
                "Protokoll-Fehler",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Warning);
        }
    }

    // ── Einfache Felder ───────────────────────────────────────────────────────
    private static Dictionary<string, string> BaueFelder(
        StationierungsErgebnis erg,
        string standpunkt, double iH,
        DateTime zeitpunkt, int punkteAnzahl)
    {
        var ic = System.Globalization.CultureInfo.InvariantCulture;

        string s0Info;
        if (punkteAnzahl == 2)
        {
            s0Info = "Aehnlichkeitstransformation  (2 Punkte, Direktloesung, r = 0)";
        }
        else if (erg.Redundanz > 0)
        {
            s0Info = $"Standardabw. s0:  {erg.s0_mm:F2} mm     " +
                     $"Redundanz r:  {erg.Redundanz}     " +
                     (erg.Konvergiert
                         ? $"Iterationen:  {erg.Iterationen}  (konvergiert)"
                         : $"Iterationen:  {erg.Iterationen}  !! NICHT konvergiert !!");
        }
        else
        {
            s0Info = "Standardabw. s0:  -  (Redundanz r = 0, eindeutig bestimmt)";
        }

        return new Dictionary<string, string>
        {
            ["Bearbeiter"]   = ProjektdatenManager.Bearbeiter,
            ["Zeitpunkt"]    = zeitpunkt.ToString("dd.MM.yyyy    HH:mm:ss"),
            ["Standpunktnr"] = standpunkt,
            ["IH"]           = iH.ToString("F3", ic),
            ["R"]            = erg.R.ToString("F3", ic),
            ["H"]            = erg.H.ToString("F3", ic),
            ["Hoehe"]        = erg.Berechnung3D ? erg.Hoehe.ToString("F3", ic) : "-",
            ["Orientierung"] = erg.Orientierung_gon.ToString("F4", ic),
            ["Massstab"]     = RechenparameterManager.Params.FreierMassstab
                               ? erg.Massstab.ToString("F6", ic)
                               : "1.000000  [fixiert]",
            ["S0Info"]       = s0Info,
        };
    }

    // ── Tabellenzeilen ────────────────────────────────────────────────────────
    private static List<Dictionary<string, string>> BaueTabellenzeilen(
        StationierungsErgebnis    erg,
        List<StationierungsPunkt> punkte,
        bool[]                    aktHz,
        bool[]                    aktStr,
        bool[]                    aktHoe)
    {
        var ic      = System.Globalization.CultureInfo.InvariantCulture;
        var resDict = erg.Residuen.ToDictionary(r => r.PunktNr);
        var zeilen  = new List<Dictionary<string, string>>();

        for (int i = 0; i < punkte.Count; i++)
        {
            var p = punkte[i];
            resDict.TryGetValue(p.PunktNr, out var res);

            // Aktivierungs-Marker (*) falls eine Beobachtung deaktiviert ist
            bool inaktiv = !aktHz[i] || !aktStr[i] || !aktHoe[i];
            string pNr   = p.PunktNr + (inaktiv ? "*" : "");

            string vQ = res != null && res.RichtungAktiv && !double.IsNaN(res.vQuer_mm)
                        ? res.vQuer_mm.ToString("+0.0;-0.0;0.0", ic) : "-";
            string vL = res != null && res.StreckeAktiv  && !double.IsNaN(res.vStrecke_mm)
                        ? res.vStrecke_mm.ToString("+0.0;-0.0;0.0", ic) : "-";
            string vH = res != null && res.HoeheAktiv    && !double.IsNaN(res.vHoehe_mm)
                        ? res.vHoehe_mm.ToString("+0.0;-0.0;0.0", ic) : "-";

            zeilen.Add(new Dictionary<string, string>
            {
                ["PunktNr"]   = pNr,
                ["R"]         = p.R.ToString("F3", ic),
                ["H"]         = p.H.ToString("F3", ic),
                ["Hoehe"]     = p.Hoehe     != 0.0 ? p.Hoehe.ToString("F3", ic)     : "-",
                ["HZ"]        = p.HZ.ToString("F4", ic),
                ["V"]         = p.V.ToString("F4", ic),
                ["Strecke"]   = p.Strecke   != 0.0 ? p.Strecke.ToString("F3", ic)   : "-",
                ["Zielhoehe"] = p.Zielhoehe != 0.0 ? p.Zielhoehe.ToString("F3", ic) : "-",
                ["vQuer"]     = vQ,
                ["vLaengs"]   = vL,
                ["vHoehe"]    = vH,
            });
        }
        return zeilen;
    }
}
