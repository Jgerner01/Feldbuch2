namespace Feldbuch;

using System.Globalization;

public partial class FormVorwaertsschnitt : Form
{
    private const int MAX_ROWS = 10;
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    private VorwaertsschnittErgebnis?       _letzteErgebnis;
    private List<VorwaertsschnittMessung>?  _letzteMessungen;
    private bool _protokollGeschrieben;

    public FormVorwaertsschnitt()
    {
        InitializeComponent();
        InitGrid();
        LadeAnschlusspunkte();
    }

    private void InitGrid()
    {
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn  { HeaderText = "PunktNr (Station)", Name = "PunktNr", FillWeight = 22 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn  { HeaderText = "R [m]",             Name = "R",       FillWeight = 20 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn  { HeaderText = "H [m]",             Name = "H",       FillWeight = 20 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn  { HeaderText = "Hz [gon]",          Name = "Hz",      FillWeight = 18 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn  { HeaderText = "z [gon]",           Name = "z",       FillWeight = 18 });
        dgvPunkte.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Aktiv",             Name = "Aktiv",   FillWeight = 10 });
        dgvPunkte.Rows.Add(MAX_ROWS);
        foreach (DataGridViewRow row in dgvPunkte.Rows)
        {
            row.Cells["z"].Value    = "0.0000";
            row.Cells["Aktiv"].Value = true;
        }
    }

    // ── Koordinaten der Stationen aus DXF-Viewer laden ───────────────────────
    private void LadeAnschlusspunkte()
    {
        string pfad = FormDxfViewer.AnschlusspunktePfad;
        if (!File.Exists(pfad)) return;

        var lines = File.ReadAllLines(pfad).Skip(1)
                        .Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        if (lines.Count == 0) return;

        foreach (DataGridViewRow row in dgvPunkte.Rows)
            foreach (DataGridViewCell cell in row.Cells) cell.Value = null;

        for (int i = 0; i < Math.Min(lines.Count, MAX_ROWS); i++)
        {
            var parts = lines[i].Split(',');
            if (parts.Length < 3) continue;
            dgvPunkte.Rows[i].Cells["PunktNr"].Value = parts[0].Trim();
            dgvPunkte.Rows[i].Cells["R"].Value        = parts[1].Trim();
            dgvPunkte.Rows[i].Cells["H"].Value        = parts[2].Trim();
            dgvPunkte.Rows[i].Cells["z"].Value        = "0.0000";
            dgvPunkte.Rows[i].Cells["Aktiv"].Value   = true;
        }
    }

    private void btnLaden_Click(object? sender, EventArgs e) => LadeAnschlusspunkte();

    // ── Orientierung aus Freier Stationierung in alle z-Zellen laden ─────────
    private void btnzLaden_Click(object? sender, EventArgs e)
    {
        // Lese Orientierung der letzten Freien Stationierung aus ProjektdatenManager
        // und frage, für welche Zeile sie gelten soll
        string? zStr = ProjektdatenManager.GetValue("Freie Stationierung", "Orientierung [gon]");
        if (string.IsNullOrWhiteSpace(zStr))
        {
            MessageBox.Show("Keine Orientierung aus Freier Stationierung verfügbar.",
                "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Alle aktiven z-Felder mit 0-Wert füllen (Benutzer kann selektiv ändern)
        foreach (DataGridViewRow row in dgvPunkte.Rows)
        {
            string? pnr = row.Cells["PunktNr"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(pnr)) continue;
            row.Cells["z"].Value = zStr;
        }
        MessageBox.Show($"z = {zStr} gon in alle Zeilen eingetragen.",
            "Orientierung geladen", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ── Berechnen ────────────────────────────────────────────────────────────
    private void btnBerechnen_Click(object? sender, EventArgs e)
    {
        var messungen = new List<VorwaertsschnittMessung>();
        var aktiv     = new List<bool>();

        foreach (DataGridViewRow row in dgvPunkte.Rows)
        {
            string? pnr = row.Cells["PunktNr"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(pnr)) continue;

            if (!TryParse(row.Cells["R"].Value,  out double r)  ||
                !TryParse(row.Cells["H"].Value,  out double h)  ||
                !TryParse(row.Cells["Hz"].Value, out double hz) ||
                !TryParse(row.Cells["z"].Value,  out double z))
            {
                MessageBox.Show($"Ungültige Eingabe in Zeile {row.Index + 1}.",
                    "Eingabefehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            messungen.Add(new VorwaertsschnittMessung { PunktNr = pnr, R = r, H = h, Hz = hz, z = z });
            aktiv.Add(row.Cells["Aktiv"].Value is true);
        }

        if (messungen.Count < 2)
        {
            MessageBox.Show("Mindestens 2 vollständige Zeilen erforderlich.",
                "Zu wenig Stationen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var erg = VorwaertsschnittRechner.Berechnen(messungen, aktiv.ToArray());
            _letzteErgebnis  = erg;
            _letzteMessungen = messungen;

            string neupunkt = txtNeupunkt.Text.Trim();
            ProjektdatenManager.SetValue("Vorwärtschnitt", "PunktNr",   neupunkt);
            ProjektdatenManager.SetValue("Vorwärtschnitt", "R [m]",     erg.R.ToString("F3", IC));
            ProjektdatenManager.SetValue("Vorwärtschnitt", "H [m]",     erg.H.ToString("F3", IC));
            ProjektdatenManager.SetValue("Vorwärtschnitt", "s0 [mm]",   erg.s0_mm.ToString("F2", IC));

            FeldbuchpunkteManager.AddOrUpdate(new FeldbuchPunkt
            {
                PunktNr         = neupunkt,
                Typ             = "Neupunkt",
                R               = erg.R,
                H               = erg.H,
                Hoehe           = 0,
                IstBerechnung3D = false,
                Datum           = DateTime.Now.ToString("yyyy-MM-dd"),
                Quelle          = "Vorwärtschnitt"
            });

            AktualisiereErgebnisAnzeige(erg);
            pnlErgebnis.Visible = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Berechnungsfehler: " + ex.Message,
                "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AktualisiereErgebnisAnzeige(VorwaertsschnittErgebnis erg)
    {
        lblR.Text  = $"R:  {erg.R:F3} m";
        lblH.Text  = $"H:  {erg.H:F3} m";
        lblS0.Text = erg.Redundanz > 0
            ? $"s\u2080 = {erg.s0_mm:F2} mm     r = {erg.Redundanz}     {erg.Iterationen} Iter." +
              (erg.Konvergiert ? "" : "  !! NICHT konvergiert !!")
            : $"r = 0  (eindeutig bestimmt, {erg.Iterationen} Iter.)";

        dgvResiduen.Rows.Clear();
        foreach (var res in erg.Residuen)
        {
            string v = res.Aktiv
                ? res.vWinkel_cc.ToString("+0.0;-0.0;0.0", IC)
                : "-";
            int idx = dgvResiduen.Rows.Add(res.PunktNr, $"{res.StreckeH:F1}", v);
            if (res.Aktiv)
                AmpelFarbe(dgvResiduen.Rows[idx].Cells[2], Math.Abs(res.vWinkel_cc));
        }
    }

    private static void AmpelFarbe(DataGridViewCell cell, double absVal_cc)
    {
        cell.Style.BackColor = absVal_cc > 60 ? Color.FromArgb(200,  60,  60) :
                               absVal_cc > 20 ? Color.FromArgb(220, 100,  60) :
                               absVal_cc >  5 ? Color.FromArgb(255, 200,  80) :
                                                Color.FromArgb(160, 220, 130);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (_letzteErgebnis != null && _letzteMessungen != null && !_protokollGeschrieben)
        {
            _protokollGeschrieben = true;
            VorwaertsschnittProtokoll.Schreiben(
                _letzteErgebnis, _letzteMessungen, txtNeupunkt.Text.Trim());
        }
    }

    private void btnSchliessen_Click(object? sender, EventArgs e) => Close();

    private static bool TryParse(object? val, out double result)
    {
        string? s = val?.ToString()?.Replace(',', '.');
        return double.TryParse(s, NumberStyles.Any, IC, out result);
    }
}
