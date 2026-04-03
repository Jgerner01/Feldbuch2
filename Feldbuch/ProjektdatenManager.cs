namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// ProjektdatenManager – liest und schreibt Projektdaten.csv
//
// CSV-Format (Kopfzeile + Einträge):
//   Datum,Uhrzeit,Bearbeiter,Kategorie,Parameter,Wert
//
// Beim ersten Start wird die Datei angelegt.
// Statuswerte (z.B. Snap) werden beim Ändern automatisch gespeichert.
// ──────────────────────────────────────────────────────────────────────────────
public static class ProjektdatenManager
{
    private static string _path = "";
    private static List<ProjektEintrag> _eintraege = new();

    public static string Bearbeiter { get; set; } = Environment.UserName;

    // ── Initialisierung ───────────────────────────────────────────────────────
    public static void Initialize(string path)
    {
        _path = path;
        if (!File.Exists(path))
        {
            _eintraege = new();
            Intern_Add("System", "Erstellt", "Projekt angelegt");
            Save();
        }
        else
        {
            Load();
        }
    }

    // ── Lesen ─────────────────────────────────────────────────────────────────
    public static string? GetValue(string kategorie, string parameter)
        => _eintraege
            .LastOrDefault(e =>
                string.Equals(e.Kategorie, kategorie, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(e.Parameter, parameter, StringComparison.OrdinalIgnoreCase))
            ?.Wert;

    public static List<ProjektEintrag> GetAll() => new List<ProjektEintrag>(_eintraege);

    // ── Schreiben ─────────────────────────────────────────────────────────────
    public static void SetValue(string kategorie, string parameter, string wert)
    {
        Intern_Add(kategorie, parameter, wert);
        Save();
    }

    // Ersetzt alle Einträge (für den Editor)
    public static void ReplaceAll(List<ProjektEintrag> list)
    {
        _eintraege = new List<ProjektEintrag>(list);
        Save();
    }

    // ── Interne Hilfsmethoden ─────────────────────────────────────────────────
    static void Intern_Add(string kat, string par, string wert)
    {
        _eintraege.Add(new ProjektEintrag(
            DateTime.Now.ToString("yyyy-MM-dd"),
            DateTime.Now.ToString("HH:mm:ss"),
            Bearbeiter, kat, par, wert));
    }

    static void Load()
    {
        _eintraege = new();
        if (!File.Exists(_path)) return;

        var lines = File.ReadAllLines(_path, System.Text.Encoding.UTF8);
        foreach (var line in lines.Skip(1))   // erste Zeile = Kopfzeile
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            // Wert-Feld darf Kommas enthalten → alle Felder ab Index 5 zusammenführen
            var parts = line.Split(',');
            if (parts.Length < 6) continue;
            _eintraege.Add(new ProjektEintrag(
                parts[0].Trim(), parts[1].Trim(), parts[2].Trim(),
                parts[3].Trim(), parts[4].Trim(),
                string.Join(",", parts.Skip(5)).Trim()));
        }
    }

    public static void Save()
    {
        if (string.IsNullOrEmpty(_path)) return;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Datum,Uhrzeit,Bearbeiter,Kategorie,Parameter,Wert");
        foreach (var e in _eintraege)
        {
            // Kommas im Wert-Feld in Anführungszeichen einschließen
            string wert = e.Wert.Contains(',') ? $"\"{e.Wert}\"" : e.Wert;
            sb.AppendLine($"{e.Datum},{e.Uhrzeit},{e.Bearbeiter},{e.Kategorie},{e.Parameter},{wert}");
        }
        File.WriteAllText(_path, sb.ToString(), System.Text.Encoding.UTF8);
    }
}

// ── Datensatz ─────────────────────────────────────────────────────────────────
public record ProjektEintrag(
    string Datum,
    string Uhrzeit,
    string Bearbeiter,
    string Kategorie,
    string Parameter,
    string Wert);
