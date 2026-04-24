namespace Feldbuch;

using System.Globalization;

// ══════════════════════════════════════════════════════════════════════════════
// AutoMatchProtokoll  –  Append-only CSV-Protokoll aller Suchereignisse
//
// Dateiname: {Projektordner}\AutoMatch_{StandpunktNr}.csv
// Enthält sowohl erfolgreiche Matches als auch Fehlschläge.
// ══════════════════════════════════════════════════════════════════════════════
public enum AutoMatchErgebnis
{
    AutoMatch,
    Bestaetigt,
    Abgelehnt,
    KeinTreffer,
    MehrereTreffer
}

public record AutoMatchEreignis(
    DateTime          Zeitstempel,
    double            StationE,
    double            StationN,
    double            StationH,
    double            Orientierung_gon,
    double            s0_mm,
    int               nPunkte,
    double            Hz_gon,
    double            V_gon,
    double            D_m,
    double            E_pred,
    double            N_pred,
    double            Radius_m,
    int               AnzahlTreffer,
    string            GewaehlterPunkt,
    double            AbstandGewählt_m,
    AutoMatchErgebnis Ergebnis
);

public static class AutoMatchProtokoll
{
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;
    private const string Header =
        "Zeitstempel;StationE;StationN;StationH;θ_gon;s0_mm;nPkt;" +
        "Hz_gon;V_gon;D_m;E_pred;N_pred;R_suche_m;" +
        "nTreffer;GewähltPunkt;AbstandGew_m;Ergebnis";

    public static string GetPfad(string standpunktNr)
    {
        string sicher = string.Join("_",
            standpunktNr.Split(Path.GetInvalidFileNameChars()));
        return ProjektManager.GetPfad($"AutoMatch_{sicher}.csv");
    }

    public static void Schreiben(AutoMatchEreignis e, string standpunktNr)
    {
        string pfad = GetPfad(standpunktNr);
        bool   neu  = !File.Exists(pfad);
        using var sw = new StreamWriter(pfad, append: true, System.Text.Encoding.UTF8);
        if (neu) sw.WriteLine(Header);
        sw.WriteLine(string.Join(";", new[]
        {
            e.Zeitstempel.ToString("yyyy-MM-dd HH:mm:ss"),
            e.StationE.ToString("F3", IC),
            e.StationN.ToString("F3", IC),
            e.StationH.ToString("F3", IC),
            e.Orientierung_gon.ToString("F4", IC),
            e.s0_mm.ToString("F2", IC),
            e.nPunkte.ToString(),
            e.Hz_gon.ToString("F4", IC),
            e.V_gon.ToString("F4", IC),
            e.D_m.ToString("F3", IC),
            e.E_pred.ToString("F3", IC),
            e.N_pred.ToString("F3", IC),
            e.Radius_m.ToString("F3", IC),
            e.AnzahlTreffer.ToString(),
            e.GewaehlterPunkt,
            e.AbstandGewählt_m >= 0 ? e.AbstandGewählt_m.ToString("F3", IC) : "-1",
            e.Ergebnis.ToString()
        }));
    }

    public static List<AutoMatchEreignis> Laden(string standpunktNr)
    {
        string pfad = GetPfad(standpunktNr);
        if (!File.Exists(pfad)) return new();
        var result = new List<AutoMatchEreignis>();
        foreach (var line in File.ReadAllLines(pfad, System.Text.Encoding.UTF8).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var p = line.Split(';');
            if (p.Length < 17) continue;
            try
            {
                result.Add(new AutoMatchEreignis(
                    DateTime.Parse(p[0]),
                    double.Parse(p[1], IC), double.Parse(p[2], IC), double.Parse(p[3], IC),
                    double.Parse(p[4], IC),
                    double.Parse(p[5], IC), int.Parse(p[6]),
                    double.Parse(p[7], IC), double.Parse(p[8], IC), double.Parse(p[9], IC),
                    double.Parse(p[10], IC), double.Parse(p[11], IC), double.Parse(p[12], IC),
                    int.Parse(p[13]),
                    p[14],
                    double.Parse(p[15], IC),
                    Enum.Parse<AutoMatchErgebnis>(p[16])
                ));
            }
            catch { /* fehlerhafte Zeilen überspringen */ }
        }
        return result;
    }
}
