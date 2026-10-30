// Zweck:    Sucht Koordinaten zu einer Punktnummer in Importpunkten, Feldbuchpunkten und
//           Anschlusspunkte.csv (gleiche Reihenfolge wie die Suche im DXF-Viewer).
//           Liefert null, wenn der Punkt nirgends vorkommt (statt 0/0/0).
// Hardware: keine
// Autor:    Johann Gerner

using System.Globalization;

namespace Feldbuch;

public static class AnschlusspunktSuche
{
    private const int MIN_SPALTEN_ANSCHLUSSDATEI = 3;

    public static (double R, double H, double Hoehe)? Finde(string punktNr)
    {
        if (string.IsNullOrWhiteSpace(punktNr)) return null;

        var imp = ImportPunkteManager.Punkte
            .FirstOrDefault(p => string.Equals(p.PunktNr, punktNr, StringComparison.OrdinalIgnoreCase));
        if (imp != null) return (imp.R, imp.H, imp.Hoehe);

        var fb = FeldbuchpunkteManager.Punkte
            .FirstOrDefault(p => string.Equals(p.PunktNr, punktNr, StringComparison.OrdinalIgnoreCase));
        if (fb != null) return (fb.R, fb.H, fb.Hoehe);

        try
        {
            string pfad = FormDxfViewer.AnschlusspunktePfad;
            if (File.Exists(pfad))
            {
                var ic = CultureInfo.InvariantCulture;
                foreach (var line in File.ReadAllLines(pfad).Skip(1))
                {
                    var t = line.Split(',');
                    if (t.Length < MIN_SPALTEN_ANSCHLUSSDATEI ||
                        !string.Equals(t[0].Trim(), punktNr, StringComparison.OrdinalIgnoreCase)) continue;

                    if (!double.TryParse(t[1].Trim(), NumberStyles.Any, ic, out double r) ||
                        !double.TryParse(t[2].Trim(), NumberStyles.Any, ic, out double h)) continue;
                    double hoe = t.Length > MIN_SPALTEN_ANSCHLUSSDATEI &&
                                 double.TryParse(t[3].Trim(), NumberStyles.Any, ic, out double ho) ? ho : 0.0;
                    return (r, h, hoe);
                }
            }
        }
        catch (Exception ex) { ErrorLogger.Log("AnschlusspunktSuche.Finde", ex); }

        return null;
    }
}
