namespace Feldbuch;

using System.Globalization;

// ─────────────────────────────────────────────────────────────────────────────
// KoordTransformationsProtokoll
//
// Dünner Wrapper: bereitet die Laufzeitdaten auf und delegiert die
// RTF-Erzeugung an den RtfProtokollGenerator.
//
// Vorlage: KoordTransformation_Protokoll.xml  (neben der EXE)
// Ausgabe: KoordTransformation_YYYY-MM-DD_HH-mm-ss.rtf  (Projektverzeichnis)
// ─────────────────────────────────────────────────────────────────────────────
public static class KoordTransformationsProtokoll
{
    private static readonly string    VorlageName = "KoordTransformation_Protokoll.xml";
    private static readonly CultureInfo IC        = CultureInfo.InvariantCulture;

    public static void Schreiben(
        TransformationsErgebnis  erg,
        List<TransformPunkt>     quellePasspunkte,   // alle gematchten Paare (_quelleZuletzt)
        List<TransformPunkt>     zielPasspunkte,     // korrespondierende Zielpunkte
        List<TransformPunkt>?    alleTransformiert)  // alle transformierten Quellpunkte
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
            var    jetzt   = DateTime.Now;
            string zielPfad = Path.Combine(verzeichnis,
                $"KoordTransformation_{jetzt:yyyy-MM-dd_HH-mm-ss}.rtf");

            var felder   = BaueFelder(erg, quellePasspunkte, zielPasspunkte,
                                      alleTransformiert, jetzt);
            var tabellen = BaueTabellenzeilen(erg, quellePasspunkte, zielPasspunkte,
                                              alleTransformiert);

            RtfProtokollGenerator.Schreiben(vorlagePfad, felder, tabellen, zielPfad);

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

    // ── Skalare Felder ────────────────────────────────────────────────────────
    private static Dictionary<string, string> BaueFelder(
        TransformationsErgebnis erg,
        List<TransformPunkt>    quellePasspunkte,
        List<TransformPunkt>    zielPasspunkte,
        List<TransformPunkt>?   alleTransformiert,
        DateTime                zeitpunkt)
    {
        bool ist3D = erg.Typ != TransformationsTyp.Helmert2D;

        // Benutzte / nicht benutzte Passpunkte ermitteln
        var usedNrs = new HashSet<string>(
            erg.Residuen.Select(r => r.PunktNr),
            StringComparer.OrdinalIgnoreCase);
        int hatNicht = quellePasspunkte.Count(p => !usedNrs.Contains(p.PunktNr));

        string massstab  = erg.FesterMassstab ? "[Maßstab fest m = 1]" : "[Maßstab frei]";
        string typName   = KoordTransformationsRechner.TypName(erg.Typ);
        string gueteinfo = erg.Redundanz > 0
            ? $"Anzahl Punktpaare: {quellePasspunkte.Count}     " +
              $"Redundanz: {erg.Redundanz}     " +
              $"Standardabw: {erg.S0_mm:F2} mm"
            : $"Anzahl Punktpaare: {quellePasspunkte.Count}     " +
              $"Redundanz: 0  (eindeutig bestimmt)";

        return new Dictionary<string, string>
        {
            ["TypUndMassstab"]    = $"Typ: {typName}   {massstab}",
            ["Bearbeiter"]        = ProjektdatenManager.Bearbeiter,
            ["Projekt"]           = ProjektManager.ProjektName,
            ["Zeitpunkt"]         = zeitpunkt.ToString("dd.MM.yyyy    HH:mm:ss"),
            ["Parameter"]         = BaueParameterText(erg),
            ["Gueteinfo"]         = gueteinfo,
            ["Ist3D"]             = ist3D ? "1" : "0",
            ["HatNichtBenutzte"]  = hatNicht > 0 ? "1" : "0",
        };
    }

    // ── Parametertexte (mehrzeilig, \n-getrennt) ──────────────────────────────
    private static string BaueParameterText(TransformationsErgebnis erg)
    {
        var p    = erg.Parameter;
        var zeilen = new List<string>();

        switch (erg.Typ)
        {
            case TransformationsTyp.Helmert2D:
                zeilen.Add($"  dx          =  {p.Dx,14:F4} m");
                zeilen.Add($"  dy          =  {p.Dy,14:F4} m");
                zeilen.Add($"  Rotation α  =  {p.Alpha_gon,14:F6} gon");
                if (erg.FesterMassstab)
                    zeilen.Add( "  Maßstab m   =           1.00000000  (fest)");
                else
                {
                    zeilen.Add($"  Maßstab m   =  {p.Massstab2D,14:F8}");
                    zeilen.Add($"  (a = {p.A:F8},  b = {p.B:F8})");
                }
                break;

            case TransformationsTyp.Helmert3D:
            case TransformationsTyp.Parameter7:
                zeilen.Add($"  dx    =  {p.Dx,14:F4} m");
                zeilen.Add($"  dy    =  {p.Dy,14:F4} m");
                zeilen.Add($"  dz    =  {p.Dz,14:F4} m");
                zeilen.Add($"  rx    =  {p.Rx_rad,14:F8} rad  ({p.Rx_rad*206264.806:+0.000;-0.000} cc)");
                zeilen.Add($"  ry    =  {p.Ry_rad,14:F8} rad  ({p.Ry_rad*206264.806:+0.000;-0.000} cc)");
                zeilen.Add($"  rz    =  {p.Rz_rad,14:F8} rad  ({p.Rz_rad*206264.806:+0.000;-0.000} cc)");
                zeilen.Add(erg.FesterMassstab
                    ? "  m     =              0.00000000  (fest, m = 1)"
                    : $"  m     =  {p.M,14:F8}       ({p.M*1e6:+0.00;-0.00} ppm)");
                break;

            case TransformationsTyp.Parameter9:
                zeilen.Add($"  dx    =  {p.Dx,14:F4} m");
                zeilen.Add($"  dy    =  {p.Dy,14:F4} m");
                zeilen.Add($"  dz    =  {p.Dz,14:F4} m");
                zeilen.Add($"  rx    =  {p.Rx_rad,14:F8} rad  ({p.Rx_rad*206264.806:+0.000;-0.000} cc)");
                zeilen.Add($"  ry    =  {p.Ry_rad,14:F8} rad  ({p.Ry_rad*206264.806:+0.000;-0.000} cc)");
                zeilen.Add($"  rz    =  {p.Rz_rad,14:F8} rad  ({p.Rz_rad*206264.806:+0.000;-0.000} cc)");
                if (erg.FesterMassstab)
                {
                    zeilen.Add("  mx    =              0.00000000  (fest, m = 1)");
                    zeilen.Add("  my    =              0.00000000  (fest, m = 1)");
                    zeilen.Add("  mz    =              0.00000000  (fest, m = 1)");
                }
                else
                {
                    zeilen.Add($"  mx    =  {p.Mx,14:F8}       ({p.Mx*1e6:+0.00;-0.00} ppm)");
                    zeilen.Add($"  my    =  {p.My,14:F8}       ({p.My*1e6:+0.00;-0.00} ppm)");
                    zeilen.Add($"  mz    =  {p.Mz,14:F8}       ({p.Mz*1e6:+0.00;-0.00} ppm)");
                }
                break;
        }

        return string.Join("\n", zeilen);
    }

    // ── Tabellenzeilen ────────────────────────────────────────────────────────
    private static Dictionary<string, List<Dictionary<string, string>>> BaueTabellenzeilen(
        TransformationsErgebnis erg,
        List<TransformPunkt>    quellePasspunkte,
        List<TransformPunkt>    zielPasspunkte,
        List<TransformPunkt>?   alleTransformiert)
    {
        bool ist3D = erg.Typ != TransformationsTyp.Helmert2D;

        var usedNrs = new HashSet<string>(
            erg.Residuen.Select(r => r.PunktNr),
            StringComparer.OrdinalIgnoreCase);
        var residuumDict = erg.Residuen.ToDictionary(
            r => r.PunktNr, StringComparer.OrdinalIgnoreCase);
        var allePassNrs = new HashSet<string>(
            quellePasspunkte.Select(p => p.PunktNr),
            StringComparer.OrdinalIgnoreCase);

        // ── BenutztePasspunkte ────────────────────────────────────────────────
        var benutzte = new List<Dictionary<string, string>>();
        for (int i = 0; i < quellePasspunkte.Count; i++)
        {
            var src = quellePasspunkte[i];
            var tgt = zielPasspunkte[i];
            if (!residuumDict.TryGetValue(src.PunktNr, out var r)) continue;

            double vX  = r.vX_mm      / 10.0;
            double vY  = r.vY_mm      / 10.0;
            double vZ  = r.vZ_mm      / 10.0;
            double vG  = r.vGesamt_mm / 10.0;
            string amp = vG > 9.0 ? "3" : vG > 3.0 ? "2" : vG > 1.0 ? "1" : "";

            benutzte.Add(new Dictionary<string, string>
            {
                ["PunktNr"] = src.PunktNr,
                ["XAus"]    = src.X.ToString("F3", IC),
                ["YAus"]    = src.Y.ToString("F3", IC),
                ["ZAus"]    = ist3D ? src.Z.ToString("F3", IC) : "",
                ["XZiel"]   = tgt.X.ToString("F3", IC),
                ["YZiel"]   = tgt.Y.ToString("F3", IC),
                ["ZZiel"]   = ist3D ? tgt.Z.ToString("F3", IC) : "",
                ["vX"]      = vX.ToString("+0.0;-0.0;0.0", IC),
                ["vY"]      = vY.ToString("+0.0;-0.0;0.0", IC),
                ["vZ"]      = ist3D ? vZ.ToString("+0.0;-0.0;0.0", IC) : "",
                ["vGes"]    = vG.ToString("F1", IC),
                ["_ampel"]  = amp,
            });
        }

        // ── NichtBenutztePasspunkte ───────────────────────────────────────────
        var nichtBenutzte = new List<Dictionary<string, string>>();
        for (int i = 0; i < quellePasspunkte.Count; i++)
        {
            var src = quellePasspunkte[i];
            var tgt = zielPasspunkte[i];
            if (usedNrs.Contains(src.PunktNr)) continue;

            nichtBenutzte.Add(new Dictionary<string, string>
            {
                ["PunktNr"] = src.PunktNr,
                ["XAus"]    = src.X.ToString("F3", IC),
                ["YAus"]    = src.Y.ToString("F3", IC),
                ["ZAus"]    = ist3D ? src.Z.ToString("F3", IC) : "",
                ["XZiel"]   = tgt.X.ToString("F3", IC),
                ["YZiel"]   = tgt.Y.ToString("F3", IC),
                ["ZZiel"]   = ist3D ? tgt.Z.ToString("F3", IC) : "",
            });
        }

        // ── TransformiertePunkte ──────────────────────────────────────────────
        var transformiert = new List<Dictionary<string, string>>();
        var quellePunkte  = alleTransformiert ?? erg.TransformiertePunkte;
        foreach (var p in quellePunkte)
        {
            transformiert.Add(new Dictionary<string, string>
            {
                ["PunktNr"] = p.PunktNr,
                ["X"]       = p.X.ToString("F3", IC),
                ["Y"]       = p.Y.ToString("F3", IC),
                ["Z"]       = ist3D ? p.Z.ToString("F3", IC) : "",
                ["Typ"]     = allePassNrs.Contains(p.PunktNr) ? "Passpunkt" : "Neupunkt",
            });
        }

        return new Dictionary<string, List<Dictionary<string, string>>>
        {
            ["BenutztePasspunkte"]      = benutzte,
            ["NichtBenutztePasspunkte"] = nichtBenutzte,
            ["TransformiertePunkte"]    = transformiert,
        };
    }
}
