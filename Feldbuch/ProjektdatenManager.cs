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
        _eintraege = new();
        if (!File.Exists(path))
        {
            DateiSpeicher.LadenErfolgreich(path);
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
        SetValues(kategorie, new Dictionary<string, string> { [parameter] = wert });
    }

    /// <summary>Schreibt zusammengehörige Parameter in einem Dateivorgang.</summary>
    public static void SetValues(string kategorie, IReadOnlyDictionary<string, string> werte)
    {
        DateiSpeicher.PruefeSchreibzugriff(_path);
        var vorher = new List<ProjektEintrag>(_eintraege);
        try
        {
            foreach (var (parameter, wert) in werte) Intern_Add(kategorie, parameter, wert);
            Save();
        }
        catch { _eintraege = vorher; throw; }
    }

    // Ersetzt alle Einträge (für den Editor)
    public static void ReplaceAll(List<ProjektEintrag> list)
    {
        DateiSpeicher.PruefeSchreibzugriff(_path);
        var vorher = _eintraege;
        _eintraege = new List<ProjektEintrag>(list);
        try { Save(); }
        catch { _eintraege = vorher; throw; }
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

        try
        {
            using var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(_path, System.Text.Encoding.UTF8)
            {
                TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true,
                TrimWhiteSpace = false
            };
            parser.SetDelimiters(",");
            var kopf = parser.ReadFields();
            if (kopf == null || !kopf.SequenceEqual(new[] { "Datum", "Uhrzeit", "Bearbeiter", "Kategorie", "Parameter", "Wert" }))
                throw new FormatException("Ungültige Kopfzeile in Projektdaten.csv.");
            var eintraege = new List<ProjektEintrag>();
            while (!parser.EndOfData)
            {
                var parts = parser.ReadFields();
                if (parts == null) continue;
                if (parts.Length < 6) throw new FormatException("Unvollständiger Projektdatensatz.");
                // Ältere Dateien konnten ein unquotiertes Komma im letzten Feld enthalten.
                eintraege.Add(new ProjektEintrag(parts[0], parts[1], parts[2], parts[3], parts[4],
                    string.Join(",", parts.Skip(5))));
            }
            _eintraege = eintraege;
            DateiSpeicher.LadenErfolgreich(_path);
        }
        catch (Exception ex) { DateiSpeicher.MeldeLadefehler(_path, ex); }
    }

    public static void Save()
    {
        if (string.IsNullOrEmpty(_path)) return;
        DateiSpeicher.PruefeSchreibzugriff(_path);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Datum,Uhrzeit,Bearbeiter,Kategorie,Parameter,Wert");
        foreach (var e in _eintraege)
        {
            sb.AppendLine(string.Join(",", new[] { e.Datum, e.Uhrzeit, e.Bearbeiter, e.Kategorie, e.Parameter, e.Wert }
                .Select(Quote)));
        }
        DateiSpeicher.SchreibeText(_path, sb.ToString(), System.Text.Encoding.UTF8);
    }

    private static string Quote(string text) => text.IndexOfAny([',', '"', '\r', '\n']) >= 0
        ? "\"" + text.Replace("\"", "\"\"") + "\"" : text;
}

// ── Datensatz ─────────────────────────────────────────────────────────────────
public record ProjektEintrag(
    string Datum,
    string Uhrzeit,
    string Bearbeiter,
    string Kategorie,
    string Parameter,
    string Wert);
