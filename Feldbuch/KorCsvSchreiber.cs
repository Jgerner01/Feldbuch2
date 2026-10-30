// Zweck:    Schreibt und ergänzt Koordinatendateien im Format {Projekt}-kor.csv
//           (Metadaten-Header, Spalten "PunktNr; X; Y; Z; Code", Trennzeichen "; ", Dezimalpunkt).
//           X = Rechtswert R, Y = Hochwert H, Z = Höhe (leer bei 2D).
// Hardware: keine
// Autor:    Johann Gerner

using System.Globalization;
using System.Text;

namespace Feldbuch;

public record KorPunkt(string PunktNr, double R, double H, double? Hoehe, string Code);

public static class KorCsvSchreiber
{
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    private const string SPALTENZEILE = "PunktNr; X; Y; Z; Code";
    private const string FORMAT       = "F3";
    private const int    SPALTE_NR    = 0;
    private const int    SPALTE_X     = 1;
    private const int    SPALTE_Y     = 2;
    private const int    SPALTE_Z     = 3;
    private const int    SPALTE_CODE  = 4;

    /// <summary>Erzeugt den vollständigen Dateitext.</summary>
    public static string Erzeuge(string projekt, string sensor, string datum, IEnumerable<KorPunkt> punkte)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# METADATA");
        sb.AppendLine($"Projekt: {projekt}");
        sb.AppendLine($"Sensor: {sensor}");
        sb.AppendLine("Bearbeiter: ");
        sb.AppendLine($"Datum: {datum}");
        sb.AppendLine("---");
        sb.AppendLine("# DATENTYP");
        sb.AppendLine("Koordinaten");
        sb.AppendLine("# DATEN");
        sb.AppendLine(SPALTENZEILE);
        foreach (var p in punkte)
            sb.AppendLine(string.Join("; ",
                p.PunktNr,
                p.R.ToString(FORMAT, IC),
                p.H.ToString(FORMAT, IC),
                p.Hoehe.HasValue ? p.Hoehe.Value.ToString(FORMAT, IC) : "",
                p.Code));
        return sb.ToString();
    }

    /// <summary>Liest die Datenzeilen einer vorhandenen -kor.csv (Zeilen mit lesbaren X/Y).</summary>
    public static List<KorPunkt> LesePunkte(string pfad)
    {
        var liste = new List<KorPunkt>();
        bool inDaten = false, kopfGelesen = false;

        foreach (string raw in File.ReadAllLines(pfad, Encoding.UTF8))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line == "---") continue;
            if (line.StartsWith('#'))
            {
                inDaten     = line.TrimStart('#').Trim().Equals("DATEN", StringComparison.OrdinalIgnoreCase);
                kopfGelesen = false;
                continue;
            }
            if (!inDaten) continue;
            if (!kopfGelesen) { kopfGelesen = true; continue; }

            string[] t = line.Split(';').Select(s => s.Trim()).ToArray();
            if (t.Length <= SPALTE_Y) continue;
            if (!double.TryParse(t[SPALTE_X], NumberStyles.Float, IC, out double x) ||
                !double.TryParse(t[SPALTE_Y], NumberStyles.Float, IC, out double y)) continue;

            double? z = t.Length > SPALTE_Z && double.TryParse(t[SPALTE_Z], NumberStyles.Float, IC, out double zz) ? zz : null;
            string code = t.Length > SPALTE_CODE ? t[SPALTE_CODE] : "";
            liste.Add(new KorPunkt(t[SPALTE_NR], x, y, z, code));
        }
        return liste;
    }

    /// <summary>
    /// Schreibt die Punkte in die Datei. Existiert sie und ist <paramref name="vorhandeneErhalten"/> gesetzt,
    /// ersetzen neue Punkte gleicher Nummer die alten (an gleicher Stelle), alle anderen bleiben erhalten.
    /// Gibt die Anzahl der Punkte in der geschriebenen Datei zurück.
    /// </summary>
    public static int Schreibe(string pfad, string projekt, string sensor, IEnumerable<KorPunkt> neu, bool vorhandeneErhalten)
    {
        var ergebnis = vorhandeneErhalten && File.Exists(pfad) ? LesePunkte(pfad) : new List<KorPunkt>();

        foreach (var p in neu)
        {
            int i = ergebnis.FindIndex(e => string.Equals(e.PunktNr, p.PunktNr, StringComparison.OrdinalIgnoreCase));
            if (i >= 0) ergebnis[i] = p;
            else        ergebnis.Add(p);
        }

        File.WriteAllText(pfad,
            Erzeuge(projekt, sensor, DateTime.Today.ToString("yyyy-MM-dd"), ergebnis),
            Encoding.UTF8);
        return ergebnis.Count;
    }
}
