namespace Feldbuch;

using System.Text.Json;
using System.Text.Json.Serialization;

// ══════════════════════════════════════════════════════════════════════════════
// AutoMatchPunkte  –  JSON-Verwaltung der automatisch gematchten Punkte
//
// Datei: {Projektordner}\AutoMatch_Punkte.json
// Enthält alle Auto-Match-Treffer mit vollem Kontext (vorhergesagte Position,
// Suchradius, Ergebnis).  Ergänzt die Anschlusspunkte.csv um Audit-Informationen
// und ermöglicht gezielte Rücknahme eines Matches.
// ══════════════════════════════════════════════════════════════════════════════
public record AutoMatchPunkt(
    string Zeitstempel,
    string StandpunktNr,
    string PunktNr,
    double R,
    double H,
    double Hoehe,
    [property: JsonPropertyName("Hz_gon")]   double Hz_gon,
    [property: JsonPropertyName("V_gon")]    double V_gon,
    [property: JsonPropertyName("D_m")]      double D_m,
    double E_pred,
    double N_pred,
    double Suchradius_m,
    double Abstand_m,
    string Ergebnis,
    string Quelle
);

public class AutoMatchPunkteDatei
{
    public int                  Version      { get; set; } = 1;
    public string               StandpunktNr { get; set; } = "";
    public List<AutoMatchPunkt> Punkte       { get; set; } = new();
}

public static class AutoMatchPunkte
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string GetPfad()
        => ProjektManager.GetPfad("AutoMatch_Punkte.json");

    public static AutoMatchPunkteDatei Laden()
    {
        string pfad = GetPfad();
        if (!File.Exists(pfad)) return new AutoMatchPunkteDatei();
        try
        {
            var json = File.ReadAllText(pfad, System.Text.Encoding.UTF8);
            return JsonSerializer.Deserialize<AutoMatchPunkteDatei>(json, JsonOpts)
                   ?? new AutoMatchPunkteDatei();
        }
        catch { return new AutoMatchPunkteDatei(); }
    }

    public static void Speichern(AutoMatchPunkteDatei datei)
    {
        string pfad = GetPfad();
        string json = JsonSerializer.Serialize(datei, JsonOpts);
        File.WriteAllText(pfad, json, System.Text.Encoding.UTF8);
    }

    /// <summary>Fügt einen Auto-Match-Treffer hinzu und speichert.</summary>
    public static void PunktHinzufuegen(AutoMatchPunkt punkt, string standpunktNr)
    {
        var datei = Laden();
        datei.StandpunktNr = standpunktNr;
        // Duplikat (gleiche PunktNr) überschreiben
        int idx = datei.Punkte.FindIndex(p => p.PunktNr == punkt.PunktNr);
        if (idx >= 0) datei.Punkte[idx] = punkt;
        else          datei.Punkte.Add(punkt);
        Speichern(datei);
    }

    /// <summary>Gibt alle bereits gematchten PunktNr als HashSet zurück.</summary>
    public static HashSet<string> GetBereitsGematchte()
    {
        var datei = Laden();
        return datei.Punkte
            .Select(p => p.PunktNr)
            .ToHashSet(StringComparer.Ordinal);
    }
}
