namespace Feldbuch;

using System.Globalization;
using System.Text;

// ──────────────────────────────────────────────────────────────────────────────
// Konvertierung: editierbare Tabelle mit Feldbuchpunkten und Importfunktion.
//
// Spalten: PunktNr | Typ | R | H | Höhe | HZ | V | Strecke | Zielh. | Code | Bemerkung
//
// Import: GSI (Leica), DXF (Linien + Symbole)
// Export: CSV (;-getrennt), KOR (Koordinatenliste), DAT (Felddatenformat)
// Duplikat-Erkennung: gleicher PunktNr oder gleiche (R,H) innerhalb ±1mm
// Auto-Nummerierung: beim Import wird PunktNr fortlaufend vergeben
// ──────────────────────────────────────────────────────────────────────────────
public partial class FormKonvertierung : Form
{
    private static readonly CultureInfo IC  = CultureInfo.InvariantCulture;
    private const           double      EPS = 0.001;   // Duplikat-Toleranz [m]

    // Spaltenindizes
    private const int COL_NR      = 0;
    private const int COL_TYP     = 1;
    private const int COL_R       = 2;
    private const int COL_H       = 3;
    private const int COL_HOEHE   = 4;
    private const int COL_HZ      = 5;
    private const int COL_V       = 6;
    private const int COL_STRECKE = 7;
    private const int COL_ZIELH   = 8;
    private const int COL_CODE    = 9;
    private const int COL_BEM     = 10;

    private string _importDatei = "";   // zuletzt gewählte Importdatei

    public FormKonvertierung()
    {
        InitializeComponent();
        LadePunkte();
    }

    // ── Feldbuchpunkte laden ──────────────────────────────────────────────────
    private void LadePunkte()
    {
        grid.Rows.Clear();
        foreach (var p in FeldbuchpunkteManager.Punkte)
        {
            grid.Rows.Add(
                p.PunktNr,
                p.Typ,
                p.R.ToString("F3", IC),
                p.H.ToString("F3", IC),
                p.Hoehe.ToString("F3", IC),
                "", "", "", "",   // HZ, V, Strecke, Zielhoehe leer
                "",               // Punktcode
                $"Quelle={p.Quelle}  Datum={p.Datum}");
        }
        AktualisiereStatus($"{grid.Rows.Count} Punkte geladen.");
    }

    private void btnAktualisieren_Click(object? sender, EventArgs e)
        => LadePunkte();

    // ── Import: Datei wählen ──────────────────────────────────────────────────
    private void btnDateiOeffnen_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title  = "Datei zum Importieren wählen",
            Filter = "Alle unterstützten Formate|*.gsi;*.dxf" +
                     "|GSI-Datei (Leica)|*.gsi" +
                     "|DXF-Datei|*.dxf" +
                     "|Alle Dateien|*.*"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        _importDatei = dlg.FileName;
        lblImportDatei.Text = Path.GetFileName(_importDatei);

        string ext = Path.GetExtension(_importDatei).ToLowerInvariant();
        lblFormat.Text = ext switch
        {
            ".gsi" => "Format: GSI (Leica)",
            ".dxf" => "Format: DXF",
            _      => "Format: unbekannt"
        };

        // sofort einlesen
        ImportiereDate();
    }

    // ── Import: Einlesen ──────────────────────────────────────────────────────
    private void ImportiereDate()
    {
        if (string.IsNullOrEmpty(_importDatei) || !File.Exists(_importDatei))
        {
            AktualisiereStatus("Keine gültige Importdatei ausgewählt.");
            return;
        }

        string ext = Path.GetExtension(_importDatei).ToLowerInvariant();
        List<KonvertierungPunkt> punkte;

        try
        {
            punkte = ext switch
            {
                ".gsi" => GsiParser.Parse(_importDatei),
                ".dxf" => DxfKoordImporter.Import(_importDatei),
                _      => throw new NotSupportedException($"Format '{ext}' wird nicht unterstützt.")
            };
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Einlesen:\n{ex.Message}", "Importfehler",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        int hinzugefuegt = 0;
        int doppelt      = 0;

        foreach (var p in punkte)
        {
            if (IstDoppelt(p))
            {
                doppelt++;
                continue;
            }

            p.PunktNr = NaechstePunktNr(p.PunktNr);
            FuegeZeileHinzu(p);
            hinzugefuegt++;
        }

        AktualisiereStatus(
            $"Import: {hinzugefuegt} Punkte hinzugefügt" +
            (doppelt > 0 ? $", {doppelt} Duplikat(e) übersprungen." : "."));
    }

    // ── Duplikat-Prüfung ──────────────────────────────────────────────────────
    private bool IstDoppelt(KonvertierungPunkt neu)
    {
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow) continue;

            // Vergleich per PunktNr (wenn beide nicht leer)
            string vorhandeneNr = row.Cells[COL_NR].Value?.ToString() ?? "";
            if (!string.IsNullOrEmpty(neu.PunktNr) && !string.IsNullOrEmpty(vorhandeneNr) &&
                vorhandeneNr == neu.PunktNr)
                return true;

            // Vergleich per (R, H) innerhalb EPS
            if (double.TryParse(row.Cells[COL_R].Value?.ToString(), NumberStyles.Any, IC, out double vR) &&
                double.TryParse(row.Cells[COL_H].Value?.ToString(), NumberStyles.Any, IC, out double vH))
            {
                if (Math.Abs(vR - neu.R) < EPS && Math.Abs(vH - neu.H) < EPS &&
                    (neu.R != 0 || neu.H != 0))   // (0,0) nicht als Duplikat zählen
                    return true;
            }
        }
        return false;
    }

    // ── Nächste freie Punktnummer ─────────────────────────────────────────────
    // Versucht die vorhandene PunktNr zu erhalten; ist sie schon vergeben oder
    // leer, wird die nächste freie fortlaufende Nummer vergeben.
    private string NaechstePunktNr(string vorschlag)
    {
        // Höchste bereits verwendete Ganzzahl-Nummer ermitteln
        int maxNr = 0;
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow) continue;
            string v = row.Cells[COL_NR].Value?.ToString() ?? "";
            if (int.TryParse(v, out int n) && n > maxNr) maxNr = n;
        }

        // Vorschlag prüfen
        if (!string.IsNullOrEmpty(vorschlag))
        {
            bool vergeben = false;
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                if ((row.Cells[COL_NR].Value?.ToString() ?? "") == vorschlag)
                { vergeben = true; break; }
            }
            if (!vergeben) return vorschlag;
        }

        // Nächste freie Nummer
        return (maxNr + 1).ToString();
    }

    // ── Zeile in Grid einfügen ────────────────────────────────────────────────
    private void FuegeZeileHinzu(KonvertierungPunkt p)
    {
        grid.Rows.Add(
            p.PunktNr,
            p.Typ,
            p.R      != 0 ? p.R.ToString("F3", IC)      : "",
            p.H      != 0 ? p.H.ToString("F3", IC)      : "",
            p.Hoehe  != 0 ? p.Hoehe.ToString("F3", IC)  : "",
            p.HZ     != 0 ? p.HZ.ToString("F4", IC)     : "",
            p.V      != 0 ? p.V.ToString("F4", IC)      : "",
            p.Strecke!= 0 ? p.Strecke.ToString("F3",IC) : "",
            p.Zielhoehe!=0? p.Zielhoehe.ToString("F3",IC):"",
            p.Punktcode,
            p.Bemerkung);
    }

    // ── Status ────────────────────────────────────────────────────────────────
    private void AktualisiereStatus(string text)
        => lblStatus.Text = text;

    // ── Zeilen lesen ──────────────────────────────────────────────────────────
    private List<KonvertierungPunkt> LeseZeilen()
    {
        var liste = new List<KonvertierungPunkt>();
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow) continue;
            string Str(int i) => row.Cells[i].Value?.ToString()?.Trim() ?? "";
            double Dbl(int i) { double.TryParse(Str(i), NumberStyles.Any, IC, out double v); return v; }

            liste.Add(new KonvertierungPunkt
            {
                PunktNr   = Str(COL_NR),
                Typ       = Str(COL_TYP),
                R         = Dbl(COL_R),
                H         = Dbl(COL_H),
                Hoehe     = Dbl(COL_HOEHE),
                HZ        = Dbl(COL_HZ),
                V         = Dbl(COL_V),
                Strecke   = Dbl(COL_STRECKE),
                Zielhoehe = Dbl(COL_ZIELH),
                Punktcode = Str(COL_CODE),
                Bemerkung = Str(COL_BEM)
            });
        }
        return liste;
    }

    // ── CSV Export ────────────────────────────────────────────────────────────
    private void btnCsv_Click(object? sender, EventArgs e)
    {
        using var dlg = new SaveFileDialog
        {
            Title    = "CSV speichern",
            Filter   = "CSV-Datei|*.csv",
            FileName = "Punkte.csv"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var sb = new StringBuilder();
        sb.AppendLine("PunktNr;Typ;R;H;Hoehe;HZ;V;Strecke;Zielhoehe;Punktcode;Bemerkung");
        foreach (var z in LeseZeilen())
            sb.AppendLine(string.Join(";",
                z.PunktNr, z.Typ,
                z.R.ToString("F3", IC), z.H.ToString("F3", IC), z.Hoehe.ToString("F3", IC),
                z.HZ.ToString("F4", IC), z.V.ToString("F4", IC),
                z.Strecke.ToString("F3", IC), z.Zielhoehe.ToString("F3", IC),
                z.Punktcode, z.Bemerkung));

        File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
        AktualisiereStatus($"CSV gespeichert: {Path.GetFileName(dlg.FileName)}");
        MessageBox.Show($"CSV gespeichert:\n{dlg.FileName}", "Export",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ── KOR Export ────────────────────────────────────────────────────────────
    private void btnKor_Click(object? sender, EventArgs e)
    {
        using var dlg = new SaveFileDialog
        {
            Title    = "KOR-Datei speichern",
            Filter   = "Koordinatendatei|*.kor|Alle Dateien|*.*",
            FileName = "Punkte.kor"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var sb = new StringBuilder();
        sb.AppendLine("% Koordinatenliste – Feldbuch Export");
        sb.AppendLine($"% Datum:   {DateTime.Now:yyyy-MM-dd}");
        sb.AppendLine($"% Projekt: {(ProjektManager.IstGeladen ? ProjektManager.ProjektName : "–")}");
        sb.AppendLine("%");
        sb.AppendLine("% PunktNr        Rechtswert [m]    Hochwert [m]      Hoehe [m]    Code  Bemerkung");

        foreach (var z in LeseZeilen())
        {
            sb.AppendLine(
                $"{z.PunktNr,-16}" +
                $"{z.R.ToString("F3", IC),16}" +
                $"{z.H.ToString("F3", IC),16}" +
                $"{z.Hoehe.ToString("F3", IC),14}" +
                $"  {z.Punktcode,-8}" +
                $"  {z.Bemerkung}");
        }

        File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
        AktualisiereStatus($"KOR gespeichert: {Path.GetFileName(dlg.FileName)}");
        MessageBox.Show($"KOR-Datei gespeichert:\n{dlg.FileName}", "Export",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ── DAT Export ────────────────────────────────────────────────────────────
    private void btnDat_Click(object? sender, EventArgs e)
    {
        using var dlg = new SaveFileDialog
        {
            Title    = "DAT-Datei speichern",
            Filter   = "Felddatendatei|*.dat|Alle Dateien|*.*",
            FileName = "Punkte.dat"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var zeilen = LeseZeilen();

        // Standpunkt suchen
        var stp = zeilen.FirstOrDefault(z =>
            string.Equals(z.Typ, "Standpunkt", StringComparison.OrdinalIgnoreCase));

        string stpNr    = stp?.PunktNr ?? "STP1";
        string stpHoehe = stp != null ? stp.Hoehe.ToString("F3", IC) : "0.000";
        const string stpCode = "005";

        var sb = new StringBuilder();
        sb.AppendLine("% Feldbuch Export – DAT Format");
        sb.AppendLine($"% Datum:   {DateTime.Now:yyyy-MM-dd}");
        sb.AppendLine($"% Projekt: {(ProjektManager.IstGeladen ? ProjektManager.ProjektName : "–")}");
        sb.AppendLine("%");
        sb.AppendLine($"STANDPUNKT {stpNr} {stpHoehe} {stpCode}");
        sb.AppendLine("%");
        sb.AppendLine("% PunktNr        Rechtswert [m]    Hochwert [m]      Hoehe [m]    Code  Typ");

        foreach (var z in zeilen)
        {
            sb.AppendLine(
                $"{z.PunktNr,-16}" +
                $"{z.R.ToString("F3", IC),16}" +
                $"{z.H.ToString("F3", IC),16}" +
                $"{z.Hoehe.ToString("F3", IC),14}" +
                $"  {z.Punktcode,-8}" +
                $"  {z.Typ}");
        }

        File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);

        string hinweis = stp == null
            ? "\n\nHinweis: Kein Standpunkt gefunden – Standardwerte verwendet (STP1 / 0.000 / 005)."
            : "";
        AktualisiereStatus($"DAT gespeichert: {Path.GetFileName(dlg.FileName)}{(stp == null ? " [Standardstandpunkt]" : "")}");
        MessageBox.Show($"DAT-Datei gespeichert:\n{dlg.FileName}{hinweis}", "Export",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void btnSchliessen_Click(object? sender, EventArgs e)
        => Close();
}
