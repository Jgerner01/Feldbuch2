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
    [JsonRequired]
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
        _punkte = new();
        if (!File.Exists(pfad)) DateiSpeicher.LadenErfolgreich(pfad);
        if (File.Exists(pfad)) Load();
        else _punkte = new();
    }

    // ── Punkt hinzufügen oder aktualisieren ───────────────────────────────────
    public static void AddOrUpdate(FeldbuchPunkt punkt)
    {
        DateiSpeicher.PruefeSchreibzugriff(_pfad);
        var vorher = new List<FeldbuchPunkt>(_punkte);
        int idx = _punkte.FindIndex(p =>
            string.Equals(p.PunktNr, punkt.PunktNr, StringComparison.Ordinal) &&
            string.Equals(p.Typ,     punkt.Typ,     StringComparison.Ordinal));
        if (idx >= 0) _punkte[idx] = punkt;
        else          _punkte.Add(punkt);
        try { Save(); }
        catch { _punkte = vorher; throw; }
    }

    public static void Remove(string punktNr, string typ = "")
    {
        DateiSpeicher.PruefeSchreibzugriff(_pfad);
        var vorher = new List<FeldbuchPunkt>(_punkte);
        _punkte.RemoveAll(p => p.PunktNr == punktNr &&
                               (string.IsNullOrEmpty(typ) || p.Typ == typ));
        try { Save(); }
        catch { _punkte = vorher; throw; }
    }

    /// <summary>Löscht alle Punkte die das Prädikat erfüllen. Gibt Anzahl zurück.</summary>
    public static int RemoveWhere(Func<FeldbuchPunkt, bool> predicate)
    {
        DateiSpeicher.PruefeSchreibzugriff(_pfad);
        var vorher = new List<FeldbuchPunkt>(_punkte);
        int count = _punkte.RemoveAll(p => predicate(p));
        try { if (count > 0) Save(); }
        catch { _punkte = vorher; throw; }
        return count;
    }

    // ── Laden / Speichern ─────────────────────────────────────────────────────
    static void Load()
    {
        try
        {
            string json = File.ReadAllText(_pfad, Encoding.UTF8);
            var data = JsonSerializer.Deserialize<FeldbuchDaten>(json,
                           DateiSpeicher.JsonOptionen);
            _punkte = data?.Punkte ?? throw new JsonException("Fehlende Punktliste.");
            if (_punkte.Any(p => p == null)) throw new JsonException("Ungültiger Punkteintrag.");
            DateiSpeicher.LadenErfolgreich(_pfad);
        }
        catch (Exception ex) { DateiSpeicher.MeldeLadefehler(_pfad, ex); _punkte = new(); }
    }

    public static void Save()
    {
        if (string.IsNullOrEmpty(_pfad)) return;
        try
        {
            var opts = DateiSpeicher.JsonOptionen;
            string json = JsonSerializer.Serialize(new FeldbuchDaten { Punkte = _punkte }, opts);
            DateiSpeicher.SchreibeText(_pfad, json, Encoding.UTF8);
        }
        catch (Exception ex) { ErrorLogger.Log("FeldbuchpunkteManager.Save", ex); throw; }
    }
}
