namespace Feldbuch;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

// ──────────────────────────────────────────────────────────────────────────────
// FeldbuchpunkteManager – verwaltet berechnete Punkte (Standpunkte, Neupunkte)
// Persistenz: Feldbuchpunkte.json im Projektverzeichnis
// ──────────────────────────────────────────────────────────────────────────────

public class FeldbuchPunkt
{
    public string PunktNr           { get; set; } = "";
    public string Typ               { get; set; } = "Neupunkt";   // "Standpunkt" | "Neupunkt"
    public double R                 { get; set; }
    public double H                 { get; set; }
    public double Hoehe             { get; set; }
    public double Orientierung_gon  { get; set; }   // nur bei Standpunkt relevant
    public bool   IstBerechnung3D   { get; set; } = true;
    public string Datum             { get; set; } = "";
    public string Quelle            { get; set; } = "";
}

public class FeldbuchDaten
{
    public List<FeldbuchPunkt> Punkte { get; set; } = new();
}

public static class FeldbuchpunkteManager
{
    private static string              _pfad   = "";
    private static List<FeldbuchPunkt> _punkte = new();

    public static IReadOnlyList<FeldbuchPunkt> Punkte => _punkte;

    // ── Initialisierung ───────────────────────────────────────────────────────
    public static void Initialize(string pfad)
    {
        _pfad = pfad;
        if (File.Exists(pfad)) Load();
        else _punkte = new();
    }

    // ── Punkt hinzufügen oder aktualisieren ───────────────────────────────────
    public static void AddOrUpdate(FeldbuchPunkt punkt)
    {
        int idx = _punkte.FindIndex(p =>
            string.Equals(p.PunktNr, punkt.PunktNr, StringComparison.Ordinal) &&
            string.Equals(p.Typ,     punkt.Typ,     StringComparison.Ordinal));
        if (idx >= 0) _punkte[idx] = punkt;
        else          _punkte.Add(punkt);
        Save();
    }

    public static void Remove(string punktNr, string typ = "")
    {
        _punkte.RemoveAll(p => p.PunktNr == punktNr &&
                               (string.IsNullOrEmpty(typ) || p.Typ == typ));
        Save();
    }

    // ── Laden / Speichern ─────────────────────────────────────────────────────
    static void Load()
    {
        try
        {
            string json = File.ReadAllText(_pfad, Encoding.UTF8);
            var data = JsonSerializer.Deserialize<FeldbuchDaten>(json,
                           new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            _punkte = data?.Punkte ?? new();
        }
        catch { _punkte = new(); }
    }

    public static void Save()
    {
        if (string.IsNullOrEmpty(_pfad)) return;
        try
        {
            var opts = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(new FeldbuchDaten { Punkte = _punkte }, opts);
            File.WriteAllText(_pfad, json, Encoding.UTF8);
        }
        catch { /* Schreibfehler ignorieren */ }
    }
}
