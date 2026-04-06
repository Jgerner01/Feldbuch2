namespace Feldbuch;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

// ──────────────────────────────────────────────────────────────────────────────
// ImportPunkteManager – verwaltet importierte Punkte (KOR / CSV / JSON).
//
// Persistenz: ImportPunkte.json im Projektverzeichnis (analog Feldbuchpunkte.json)
// Duplikat-Erkennung: gleiche PunktNr ODER (R,H) ±1mm
// ──────────────────────────────────────────────────────────────────────────────
public class ImportPunkt
{
    public string PunktNr { get; set; } = "";
    public double R       { get; set; }
    public double H       { get; set; }
    public double Hoehe   { get; set; }
    public string Quelle  { get; set; } = "";
    public string Datum   { get; set; } = "";
}

public class ImportPunkteDaten
{
    public List<ImportPunkt> Punkte { get; set; } = new();
}

public static class ImportPunkteManager
{
    private const double EPS = 0.001;
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    private static string            _pfad   = "";
    private static List<ImportPunkt> _punkte = new();

    public static IReadOnlyList<ImportPunkt> Punkte => _punkte;

    // ── Initialisierung ───────────────────────────────────────────────────────
    public static void Initialize(string pfad)
    {
        _pfad = pfad;
        _punkte = new();
        if (File.Exists(pfad)) Load();
    }

    // ── Punkte hinzufügen (mit Duplikat-Prüfung) ─────────────────────────────
    public static (int hinzugefuegt, int doppelt) AddRange(
        IEnumerable<ImportPunkt> neu)
    {
        int added = 0, dupl = 0;
        foreach (var p in neu)
        {
            if (IstDoppelt(p)) { dupl++; continue; }
            _punkte.Add(p);
            added++;
        }
        if (added > 0) Save();
        return (added, dupl);
    }

    public static void Clear()
    {
        _punkte.Clear();
        Save();
    }

    // ── Laden: KOR-Datei (.kor) ───────────────────────────────────────────────
    // Format: PunktNr  R  H  Hoehe  (Leerzeichen-getrennt, Kommentare mit %)
    public static List<ImportPunkt> LeseKor(string path)
    {
        var result = new List<ImportPunkt>();
        string quelle = Path.GetFileName(path);
        foreach (var rawLine in File.ReadAllLines(path, Encoding.UTF8))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('%') ||
                line.StartsWith('#') || line.StartsWith(';'))
                continue;

            var parts = line.Split(new char[]{' ','\t'}, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) continue;

            if (!double.TryParse(parts[1], NumberStyles.Any, IC, out double r)) continue;
            if (!double.TryParse(parts[2], NumberStyles.Any, IC, out double h)) continue;
            double hoehe = 0;
            if (parts.Length >= 4)
                double.TryParse(parts[3], NumberStyles.Any, IC, out hoehe);

            result.Add(new ImportPunkt
            {
                PunktNr = parts[0],
                R = r, H = h, Hoehe = hoehe,
                Quelle = quelle,
                Datum  = DateTime.Now.ToString("yyyy-MM-dd")
            });
        }
        return result;
    }

    // ── Laden: CSV-Datei (.csv) ───────────────────────────────────────────────
    // Unterstützt Komma- und Semikolon-Trennung.
    // Kopfzeile wird erkannt wenn erste Spalte nicht numerisch ist.
    // Minimal: PunktNr, R, H  –  optional: Hoehe
    public static List<ImportPunkt> LeseCsv(string path)
    {
        var result = new List<ImportPunkt>();
        string quelle = Path.GetFileName(path);
        var lines = File.ReadAllLines(path, Encoding.UTF8);

        // Trennzeichen bestimmen
        char sep = ',';
        if (lines.Length > 0 && lines[0].Contains(';')) sep = ';';

        // Kopfzeile prüfen und Spaltenindizes ermitteln
        int idxNr = 0, idxR = 1, idxH = 2, idxHoehe = 3;
        int startLine = 0;

        if (lines.Length > 0)
        {
            var header = lines[0].Split(sep);
            bool hatHeader = false;
            for (int i = 0; i < header.Length; i++)
            {
                string h = header[i].Trim().ToLowerInvariant();
                if (h == "punktnr" || h == "nr" || h == "point" || h == "name")
                { idxNr = i; hatHeader = true; }
                else if (h is "r" or "rechtswert" or "easting" or "x")
                    idxR = i;
                else if (h is "h" or "hochwert" or "northing" or "y")
                    idxH = i;
                else if (h is "hoehe" or "höhe" or "z" or "elevation" or "height")
                    idxHoehe = i;
            }
            if (hatHeader) startLine = 1;
        }

        for (int li = startLine; li < lines.Length; li++)
        {
            var line = lines[li].Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('%') ||
                line.StartsWith('#') || line.StartsWith(';'))
                continue;

            var parts = line.Split(sep);
            int maxIdx = Math.Max(idxR, Math.Max(idxH, idxNr));
            if (parts.Length <= maxIdx) continue;

            if (!double.TryParse(parts[idxR].Trim(), NumberStyles.Any, IC, out double r)) continue;
            if (!double.TryParse(parts[idxH].Trim(), NumberStyles.Any, IC, out double h)) continue;
            double hoehe = 0;
            if (idxHoehe < parts.Length)
                double.TryParse(parts[idxHoehe].Trim(), NumberStyles.Any, IC, out hoehe);

            result.Add(new ImportPunkt
            {
                PunktNr = parts[idxNr].Trim(),
                R = r, H = h, Hoehe = hoehe,
                Quelle = quelle,
                Datum  = DateTime.Now.ToString("yyyy-MM-dd")
            });
        }
        return result;
    }

    // ── Laden: JSON-Datei ─────────────────────────────────────────────────────
    // Unterstützt ImportPunkteDaten ({"Punkte":[...]}) und
    // FeldbuchDaten ({"Punkte":[...]}) mit FeldbuchPunkt-Feldern.
    public static List<ImportPunkt> LeseJson(string path)
    {
        var result  = new List<ImportPunkt>();
        string json = File.ReadAllText(path, Encoding.UTF8);
        string quelle = Path.GetFileName(path);
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Versuche zuerst ImportPunkteDaten
        try
        {
            var data = JsonSerializer.Deserialize<ImportPunkteDaten>(json, opts);
            if (data?.Punkte?.Count > 0)
            {
                foreach (var p in data.Punkte)
                {
                    if (string.IsNullOrEmpty(p.Quelle)) p.Quelle = quelle;
                    result.Add(p);
                }
                return result;
            }
        }
        catch { }

        // Fallback: FeldbuchDaten (Feldbuchpunkte.json)
        try
        {
            var data = JsonSerializer.Deserialize<FeldbuchDaten>(json, opts);
            if (data?.Punkte?.Count > 0)
            {
                foreach (var p in data.Punkte)
                    result.Add(new ImportPunkt
                    {
                        PunktNr = p.PunktNr,
                        R       = p.R,
                        H       = p.H,
                        Hoehe   = p.Hoehe,
                        Quelle  = quelle,
                        Datum   = p.Datum
                    });
                return result;
            }
        }
        catch { }

        return result;
    }

    // ── Laden / Speichern ─────────────────────────────────────────────────────
    static void Load()
    {
        try
        {
            string json = File.ReadAllText(_pfad, Encoding.UTF8);
            var opts    = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data    = JsonSerializer.Deserialize<ImportPunkteDaten>(json, opts);
            _punkte     = data?.Punkte ?? new();
        }
        catch { _punkte = new(); }
    }

    public static void Save()
    {
        if (string.IsNullOrEmpty(_pfad)) return;
        try
        {
            var opts = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(
                new ImportPunkteDaten { Punkte = _punkte }, opts);
            File.WriteAllText(_pfad, json, Encoding.UTF8);
        }
        catch { }
    }

    // ── Duplikat-Prüfung ──────────────────────────────────────────────────────
    static bool IstDoppelt(ImportPunkt neu)
    {
        foreach (var v in _punkte)
        {
            if (!string.IsNullOrEmpty(neu.PunktNr) && v.PunktNr == neu.PunktNr)
                return true;
            if (Math.Abs(v.R - neu.R) < EPS && Math.Abs(v.H - neu.H) < EPS &&
                (neu.R != 0 || neu.H != 0))
                return true;
        }
        return false;
    }
}
