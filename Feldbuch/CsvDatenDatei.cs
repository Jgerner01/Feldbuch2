namespace Feldbuch;

using System.Text;

// ──────────────────────────────────────────────────────────────────────────────
// CsvDatenDatei – Lesen/Schreiben des Standard-CSV-Formats
//
// Format:
//   # METADATA
//   Projekt: ...
//   Sensor: ...
//   Bearbeiter: ...
//   Datum: ...
//   ---
//   # DATENTYP
//   Koordinaten | Messdaten
//   # DATEN
//   Spalte1;Spalte2;...
//   Wert1;Wert2;...
// ──────────────────────────────────────────────────────────────────────────────
public class CsvDatenDatei
{
    public string Projekt    { get; set; } = "";
    public string Sensor     { get; set; } = "";
    public string Bearbeiter { get; set; } = "";
    public string Datum      { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
    public string Datentyp   { get; set; } = "Koordinaten";   // "Koordinaten" | "Messdaten"

    // Nur für Messdaten (DAT): Standpunkt-Zeile  -<PunktNr> <InstrHoehe> [Code]
    public string StandpunktNr       { get; set; } = "";
    public string InstrumentenHoehe  { get; set; } = "";
    public string StandpunktCode     { get; set; } = "";

    public List<string>       Spalten { get; set; } = new();
    public List<List<string>> Zeilen  { get; set; } = new();
    public string             Pfad    { get; set; } = "";

    // ── Standard-Spalten ─────────────────────────────────────────────────────
    public static readonly string[] StandardSpaltenKor =
        ["PunktNr", "X", "Y", "Z", "Code"];
    public static readonly string[] StandardSpaltenDat =
        ["PunktNr", "HZ_gon", "V_gon", "Strecke_m", "Zielhoehe_m", "Code", "Zeitstempel"];

    // ── Neues Objekt mit Defaults ─────────────────────────────────────────────
    public static CsvDatenDatei Neu(string datentyp)
    {
        var d = new CsvDatenDatei { Datentyp = datentyp };
        d.Spalten.AddRange(datentyp == "Messdaten" ? StandardSpaltenDat : StandardSpaltenKor);
        return d;
    }

    // ── Dateiname normalisieren ───────────────────────────────────────────────
    // Fügt -kor oder -dat vor .csv ein, wenn noch nicht vorhanden.
    public static string NormalisierePfad(string pfad, string datentyp)
    {
        string basis = Path.GetFileNameWithoutExtension(pfad);
        string verz  = Path.GetDirectoryName(pfad) ?? "";
        if (basis.EndsWith("-kor", StringComparison.OrdinalIgnoreCase) ||
            basis.EndsWith("-dat", StringComparison.OrdinalIgnoreCase))
            return pfad;
        string suffix = datentyp == "Messdaten" ? "-dat" : "-kor";
        return Path.Combine(verz, basis + suffix + ".csv");
    }

    // ── KOR-Import ────────────────────────────────────────────────────────────
    // Format: PunktNr  X  Y  Z  Code  (leerzeichengetrennt, kein Header)
    public static CsvDatenDatei ImportiereKor(string pfad)
    {
        var d = new CsvDatenDatei { Datentyp = "Koordinaten" };
        d.Pfad = NormalisierePfad(
            Path.Combine(Path.GetDirectoryName(pfad) ?? "",
                         Path.GetFileNameWithoutExtension(pfad) + ".csv"),
            "Koordinaten");
        d.Spalten.AddRange(StandardSpaltenKor);

        foreach (var raw in File.ReadAllLines(pfad, Encoding.UTF8))
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line) || line[0] is '%' or '#' or ';') continue;
            var p = line.Split(new char[]{' ','\t'}, StringSplitOptions.RemoveEmptyEntries);
            if (p.Length < 4) continue;
            d.Zeilen.Add([
                p[0],
                p[1],
                p[2],
                p[3],
                p.Length > 4 ? p[4] : ""
            ]);
        }
        return d;
    }

    // ── DAT-Import ────────────────────────────────────────────────────────────
    // Zeile 1 = Standpunkt: -<PunktNr> <InstrHoehe> [Code]
    // Folgezeilen:           PunktNr  HZ_gon  V_gon  Strecke_m  Zielhoehe_m  Code
    public static CsvDatenDatei ImportiereDat(string pfad)
    {
        var d = new CsvDatenDatei { Datentyp = "Messdaten" };
        d.Pfad = NormalisierePfad(
            Path.Combine(Path.GetDirectoryName(pfad) ?? "",
                         Path.GetFileNameWithoutExtension(pfad) + ".csv"),
            "Messdaten");
        d.Spalten.AddRange(StandardSpaltenDat);
        string zeitstempel = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        bool ersteZeile = true;
        foreach (var raw in File.ReadAllLines(pfad, Encoding.UTF8))
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            if (ersteZeile)
            {
                ersteZeile = false;
                // Standpunkt-Zeile: beginnt mit '-'
                if (line.StartsWith('-'))
                {
                    var p = line.TrimStart('-').Split(
                        new char[]{' ','\t'}, StringSplitOptions.RemoveEmptyEntries);
                    d.StandpunktNr      = p.Length > 0 ? p[0] : "";
                    d.InstrumentenHoehe = p.Length > 1 ? p[1] : "";
                    d.StandpunktCode    = p.Length > 2 ? p[2] : "";
                }
                continue;
            }

            var t = line.Split(new char[]{' ','\t'}, StringSplitOptions.RemoveEmptyEntries);
            if (t.Length < 5) continue;
            // Spalten: PunktNr, HZ_gon, V_gon, Strecke_m, Zielhoehe_m, Code, Zeitstempel
            d.Zeilen.Add([
                t[0], t[1], t[2], t[3], t[4],
                t.Length > 5 ? t[5] : "",
                zeitstempel
            ]);
        }
        return d;
    }

    // ── KOR-Export ────────────────────────────────────────────────────────────
    public void ExportiereKor(string pfad)
    {
        int iNr   = SpaltenIdx("PunktNr");
        int iX    = SpaltenIdx("X");
        int iY    = SpaltenIdx("Y");
        int iZ    = SpaltenIdx("Z");
        int iCode = SpaltenIdx("Code");

        var sb = new StringBuilder();
        foreach (var row in Zeilen)
        {
            string nr   = Zelle(row, iNr);
            string x    = Zelle(row, iX);
            string y    = Zelle(row, iY);
            string z    = Zelle(row, iZ);
            string code = Zelle(row, iCode);
            sb.AppendLine($"{nr,-8}{x,16}{y,16}{z,10}    {code}");
        }
        File.WriteAllText(pfad, sb.ToString(), Encoding.UTF8);
    }

    // ── DAT-Export ────────────────────────────────────────────────────────────
    public void ExportiereDat(string pfad)
    {
        int iNr      = SpaltenIdx("PunktNr");
        int iHZ      = SpaltenIdx("HZ_gon");
        int iV       = SpaltenIdx("V_gon");
        int iStrecke = SpaltenIdx("Strecke_m");
        int iZielh   = SpaltenIdx("Zielhoehe_m");
        int iCode    = SpaltenIdx("Code");

        var sb = new StringBuilder();

        // Standpunkt-Zeile: -<PunktNr> <InstrHoehe> [Code]
        string stpNr = string.IsNullOrEmpty(StandpunktNr) ? "STP" : StandpunktNr;
        string stpH  = string.IsNullOrEmpty(InstrumentenHoehe) ? "0.000" : InstrumentenHoehe;
        string stpC  = string.IsNullOrEmpty(StandpunktCode) ? "" : $" {StandpunktCode}";
        sb.AppendLine($"-{stpNr} {stpH}{stpC}");

        foreach (var row in Zeilen)
        {
            string nr   = Zelle(row, iNr);
            string hz   = Zelle(row, iHZ);
            string v    = Zelle(row, iV);
            string str  = Zelle(row, iStrecke);
            string zh   = Zelle(row, iZielh);
            string code = Zelle(row, iCode);
            sb.AppendLine($" {nr,3}   {hz,9}   {v,9}   {str,9}   {zh,7}  {code}");
        }
        File.WriteAllText(pfad, sb.ToString(), Encoding.UTF8);
    }

    // ── Hilfsmethoden ─────────────────────────────────────────────────────────
    int SpaltenIdx(string name) =>
        Spalten.FindIndex(s => s.Equals(name, StringComparison.OrdinalIgnoreCase));

    static string Zelle(List<string> row, int idx) =>
        idx >= 0 && idx < row.Count ? row[idx] : "";

    // ── Laden ─────────────────────────────────────────────────────────────────
    public static CsvDatenDatei Load(string pfad)
    {
        var d = new CsvDatenDatei { Pfad = pfad };
        var lines = File.ReadAllLines(pfad, Encoding.UTF8);
        string sektion    = "";
        bool   datenKopf  = false;

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            if (line.StartsWith('#'))
            {
                sektion   = line.TrimStart('#').Trim().ToUpperInvariant();
                datenKopf = false;
                continue;
            }
            if (line == "---") continue;

            switch (sektion)
            {
                case "METADATA":
                {
                    int idx = line.IndexOf(':');
                    if (idx < 0) break;
                    var key = line[..idx].Trim().ToLowerInvariant();
                    var val = line[(idx + 1)..].Trim();
                    switch (key)
                    {
                        case "projekt":    d.Projekt    = val; break;
                        case "sensor":     d.Sensor     = val; break;
                        case "bearbeiter": d.Bearbeiter = val; break;
                        case "datum":            d.Datum             = val; break;
                        case "standpunktnr":     d.StandpunktNr      = val; break;
                        case "instrumentenhoehe":d.InstrumentenHoehe = val; break;
                        case "standpunktcode":   d.StandpunktCode    = val; break;
                    }
                    break;
                }
                case "DATENTYP":
                    // erste nicht-leere Zeile die kein "Format:" ist
                    if (!line.StartsWith("Format", StringComparison.OrdinalIgnoreCase))
                        d.Datentyp = line;
                    break;

                case "DATEN":
                    if (!datenKopf)
                    {
                        // erste Zeile = Spalten-Header
                        d.Spalten.AddRange(line.Split(';').Select(s => s.Trim()));
                        datenKopf = true;
                    }
                    else
                    {
                        d.Zeilen.Add([.. line.Split(';').Select(s => s.Trim())]);
                    }
                    break;
            }
        }
        return d;
    }

    // ── Speichern ─────────────────────────────────────────────────────────────
    public void Save()
    {
        if (string.IsNullOrEmpty(Pfad)) return;
        var sb = new StringBuilder();
        sb.AppendLine("# METADATA");
        sb.AppendLine($"Projekt: {Projekt}");
        sb.AppendLine($"Sensor: {Sensor}");
        sb.AppendLine($"Bearbeiter: {Bearbeiter}");
        sb.AppendLine($"Datum: {Datum}");
        if (Datentyp == "Messdaten")
        {
            sb.AppendLine($"StandpunktNr: {StandpunktNr}");
            sb.AppendLine($"InstrumentenHoehe: {InstrumentenHoehe}");
            sb.AppendLine($"StandpunktCode: {StandpunktCode}");
        }
        sb.AppendLine("---");
        sb.AppendLine("# DATENTYP");
        sb.AppendLine(Datentyp);
        sb.AppendLine("# DATEN");
        if (Spalten.Count > 0)
            sb.AppendLine(string.Join("; ", Spalten));
        foreach (var row in Zeilen)
        {
            var cells = new List<string>(row);
            while (cells.Count < Spalten.Count) cells.Add("");
            sb.AppendLine(string.Join("; ", cells));
        }
        File.WriteAllText(Pfad, sb.ToString(), Encoding.UTF8);
    }
}
