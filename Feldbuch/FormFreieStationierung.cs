namespace Feldbuch;

using System.Globalization;

public partial class FormFreieStationierung : Form
{
    private const int MAX_ROWS = 15;
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    public FormFreieStationierung()
    {
        InitializeComponent();
        InitGrid();
        LadeAnschlusspunkte();   // automatisch aus DXF-Viewer-Picks
        AktualisiereNeuerStandpunktButton();
    }

    // ── Button "Neuer Standpunkt" – Sichtbarkeit und Aktion ──────────────────
    private void AktualisiereNeuerStandpunktButton()
    {
        // Sichtbar, wenn Anschlusspunkte.csv existiert
        btnNeuerStandpunkt.Visible = File.Exists(FormDxfViewer.AnschlusspunktePfad);
    }

    private void btnNeuerStandpunkt_Click(object? sender, EventArgs e)
    {
        string pfad = FormDxfViewer.AnschlusspunktePfad;

        // Bestehende Datei umbenennen (Datum + Uhrzeit im Dateinamen)
        if (File.Exists(pfad))
        {
            string zeitstempel = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string dir         = Path.GetDirectoryName(pfad)!;
            string archivPfad  = Path.Combine(dir, $"Anschlusspunkte_{zeitstempel}.csv");
            File.Move(pfad, archivPfad);
        }

        // Alle Grid-Zellen leeren
        foreach (DataGridViewRow row in dgvPunkte.Rows)
            foreach (DataGridViewCell cell in row.Cells)
                cell.Value = null;

        AktualisiereNeuerStandpunktButton();
    }

    // ── Anschlusspunkte aus DXF-Viewer automatisch laden ─────────────────────
    private void LadeAnschlusspunkte()
    {
        string pfad = FormDxfViewer.AnschlusspunktePfad;
        if (!File.Exists(pfad)) return;

        var lines = File.ReadAllLines(pfad)
                        .Skip(1)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();
        if (lines.Count == 0) return;

        // Grid leeren
        foreach (DataGridViewRow row in dgvPunkte.Rows)
            foreach (DataGridViewCell cell in row.Cells)
                cell.Value = null;

        for (int i = 0; i < Math.Min(lines.Count, MAX_ROWS); i++)
        {
            var parts = lines[i].Split(',');
            if (parts.Length < 8) continue;
            for (int j = 0; j < 8; j++)
                dgvPunkte.Rows[i].Cells[j].Value = parts[j].Trim();
        }
    }

    private void InitGrid()
    {
        string[] headers = { "PunktNr", "R", "H", "Höhe", "HZ [gon]", "V [gon]", "Strecke [m]", "Zielhöhe [m]" };
        foreach (string h in headers)
            dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = h, Name = h });

        dgvPunkte.Rows.Add(MAX_ROWS);
    }

    private void btnTestdatenLaden_Click(object? sender, EventArgs e)
    {
        string pfad = Path.Combine(AppContext.BaseDirectory, "Muster.csv");
        if (!File.Exists(pfad))
        {
            MessageBox.Show("Muster.csv nicht gefunden.\nErwartet in: " + pfad,
                "Datei nicht gefunden", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Alle Zellen leeren
        foreach (DataGridViewRow row in dgvPunkte.Rows)
            foreach (DataGridViewCell cell in row.Cells)
                cell.Value = null;

        var lines = File.ReadAllLines(pfad).Skip(1).Take(MAX_ROWS).ToList();
        for (int i = 0; i < lines.Count; i++)
        {
            var parts = lines[i].Split(',');
            if (parts.Length < 8) continue;
            for (int j = 0; j < 8; j++)
                dgvPunkte.Rows[i].Cells[j].Value = parts[j].Trim();
        }
    }

    private void btnSpeichern_Click(object? sender, EventArgs e)
    {
        string pfad = Path.Combine(AppContext.BaseDirectory, "Muster.csv");

        var zeilen = new List<string>
        {
            "PunktNr,R,H,Hoehe,HZ,V,Strecke,Zielhoehe"
        };

        foreach (DataGridViewRow row in dgvPunkte.Rows)
        {
            string? pnr = row.Cells[0].Value?.ToString();
            if (string.IsNullOrWhiteSpace(pnr)) continue;

            var werte = new string[8];
            for (int j = 0; j < 8; j++)
                werte[j] = row.Cells[j].Value?.ToString()?.Trim() ?? "";

            zeilen.Add(string.Join(",", werte));
        }

        try
        {
            File.WriteAllLines(pfad, zeilen, System.Text.Encoding.UTF8);
            MessageBox.Show($"Gespeichert: {zeilen.Count - 1} Punkte\n{pfad}",
                "Gespeichert", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Fehler beim Speichern: " + ex.Message,
                "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnRechenparameter_Click(object? sender, EventArgs e)
    {
        using var form = new FormRechenparameter();
        form.ShowDialog(this);
    }

    private void btnBerechnen_Click(object? sender, EventArgs e)
    {
        var punkte = new List<StationierungsPunkt>();

        foreach (DataGridViewRow row in dgvPunkte.Rows)
        {
            string? pnr = row.Cells[0].Value?.ToString();
            if (string.IsNullOrWhiteSpace(pnr)) continue;

            if (!TryParseZeile(row, out var pkt, out string fehler))
            {
                MessageBox.Show($"Fehler in Zeile {row.Index + 1}: {fehler}",
                    "Eingabefehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            punkte.Add(pkt);
        }

        if (punkte.Count < 2)
        {
            MessageBox.Show("Mindestens 2 vollständige Zeilen erforderlich.",
                "Zu wenig Punkte", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            double iH         = (double)nudInstHoehe.Value;
            var    par        = RechenparameterManager.Params;
            var ergebnis      = FreieStationierungRechner.Berechnen(punkte, iH,
                                    freierMassstab:       par.FreierMassstab,
                                    berechnung3D:         par.Berechnung3D,
                                    fehlergrenzeMM_Hoehe: par.FehlergrenzeMM_Hoehe);

            // Ergebnis in Projektdaten protokollieren
            string standpunkt = txtStandpunkt.Text;
            ProjektdatenManager.SetValue("Freie Stationierung", "Standpunkt",         standpunkt);
            ProjektdatenManager.SetValue("Freie Stationierung", "R [m]",              ergebnis.R.ToString("F3", IC));
            ProjektdatenManager.SetValue("Freie Stationierung", "H [m]",              ergebnis.H.ToString("F3", IC));
            ProjektdatenManager.SetValue("Freie Stationierung", "Hoehe [m]",          ergebnis.Hoehe.ToString("F3", IC));
            ProjektdatenManager.SetValue("Freie Stationierung", "Orientierung [gon]", ergebnis.Orientierung_gon.ToString("F4", IC));
            ProjektdatenManager.SetValue("Freie Stationierung", "s0 [mm]",            ergebnis.s0_mm.ToString("F2", IC));

            // Standpunkt in Feldbuchpunkte.json speichern
            FeldbuchpunkteManager.AddOrUpdate(new FeldbuchPunkt
            {
                PunktNr          = standpunkt,
                Typ              = "Standpunkt",
                R                = ergebnis.R,
                H                = ergebnis.H,
                Hoehe            = ergebnis.Hoehe,
                Orientierung_gon = ergebnis.Orientierung_gon,
                IstBerechnung3D  = ergebnis.Berechnung3D,
                Datum            = DateTime.Now.ToString("yyyy-MM-dd"),
                Quelle           = "Freie Stationierung"
            });

            using var formErg = new FormErgebnis(standpunkt, iH, ergebnis, punkte);
            formErg.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Berechnungsfehler: " + ex.Message,
                "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static bool TryParseZeile(DataGridViewRow row, out StationierungsPunkt pkt, out string fehler)
    {
        pkt = new StationierungsPunkt { PunktNr = row.Cells[0].Value!.ToString()! };
        fehler = "";
        double r, h, hoe, hz, v, s, zh;

        if (!TryParse(row.Cells[1].Value, out r))  { fehler = "R ungültig";         return false; }
        if (!TryParse(row.Cells[2].Value, out h))  { fehler = "H ungültig";         return false; }
        if (!TryParse(row.Cells[3].Value, out hoe)){ fehler = "Höhe ungültig";      return false; }
        if (!TryParse(row.Cells[4].Value, out hz)) { fehler = "HZ ungültig";        return false; }
        if (!TryParse(row.Cells[5].Value, out v))  { fehler = "V ungültig";         return false; }
        if (!TryParse(row.Cells[6].Value, out s))  { fehler = "Strecke ungültig";   return false; }
        if (!TryParse(row.Cells[7].Value, out zh)) { fehler = "Zielhöhe ungültig";  return false; }

        pkt.R = r; pkt.H = h; pkt.Hoehe = hoe;
        pkt.HZ = hz; pkt.V = v; pkt.Strecke = s; pkt.Zielhoehe = zh;
        return true;
    }

    private static bool TryParse(object? val, out double result)
    {
        string? s = val?.ToString()?.Replace(',', '.');
        return double.TryParse(s, NumberStyles.Any, IC, out result);
    }
}
