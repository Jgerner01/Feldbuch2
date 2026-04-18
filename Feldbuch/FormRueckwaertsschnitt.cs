namespace Feldbuch;

using System.Globalization;

public partial class FormRueckwaertsschnitt : Form
{
    private const int MAX_ROWS = 12;
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    private RueckwaertsschnittErgebnis? _letzteErgebnis;
    private List<RueckwaertsschnittPunkt>? _letztePunkte;
    private bool _protokollGeschrieben;

    public FormRueckwaertsschnitt()
    {
        InitializeComponent();
        InitGrid();
        LadeAnschlusspunkte();
    }

    // ── Grid initialisieren ──────────────────────────────────────────────────
    private void InitGrid()
    {
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn     { HeaderText = "PunktNr",  Name = "PunktNr",  FillWeight = 18 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn     { HeaderText = "R [m]",    Name = "R",        FillWeight = 20 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn     { HeaderText = "H [m]",    Name = "H",        FillWeight = 20 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn     { HeaderText = "HZ [gon]", Name = "HZ",       FillWeight = 22 });
        dgvPunkte.Columns.Add(new DataGridViewCheckBoxColumn    { HeaderText = "Aktiv",    Name = "Aktiv",    FillWeight = 10 });
        dgvPunkte.Rows.Add(MAX_ROWS);

        foreach (DataGridViewRow row in dgvPunkte.Rows)
            row.Cells["Aktiv"].Value = true;
    }

    // ── Anschlusspunkte aus DXF-Viewer laden (Koordinaten, kein Hz) ─────────
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
            // HZ muss manuell eingetragen werden
            dgvPunkte.Rows[i].Cells["Aktiv"].Value   = true;
        }
    }

    private void btnAnschlusspunkteLaden_Click(object? sender, EventArgs e) => LadeAnschlusspunkte();

    // ── Berechnen ────────────────────────────────────────────────────────────
    private void btnBerechnen_Click(object? sender, EventArgs e)
    {
        var punkte = new List<RueckwaertsschnittPunkt>();
        var aktiv  = new List<bool>();

        foreach (DataGridViewRow row in dgvPunkte.Rows)
        {
            string? pnr = row.Cells["PunktNr"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(pnr)) continue;

            if (!TryParse(row.Cells["R"].Value,  out double r) ||
                !TryParse(row.Cells["H"].Value,  out double h) ||
                !TryParse(row.Cells["HZ"].Value, out double hz))
            {
                MessageBox.Show($"Ungültige Eingabe in Zeile {row.Index + 1}.",
                    "Eingabefehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            punkte.Add(new RueckwaertsschnittPunkt { PunktNr = pnr, R = r, H = h, HZ = hz });
            aktiv.Add(row.Cells["Aktiv"].Value is true);
        }

        if (punkte.Count < 3)
        {
            MessageBox.Show("Mindestens 3 vollständige Zeilen erforderlich.",
                "Zu wenig Punkte", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var erg = RueckwaertsschnittRechner.Berechnen(punkte, aktiv.ToArray());
            _letzteErgebnis = erg;
            _letztePunkte   = punkte;

            // Ergebnisse in Projektdaten speichern
            string standpunkt = txtStandpunkt.Text.Trim();
            ProjektdatenManager.SetValue("Rückwärtschnitt", "Standpunkt",         standpunkt);
            ProjektdatenManager.SetValue("Rückwärtschnitt", "R [m]",              erg.R.ToString("F3", IC));
            ProjektdatenManager.SetValue("Rückwärtschnitt", "H [m]",              erg.H.ToString("F3", IC));
            ProjektdatenManager.SetValue("Rückwärtschnitt", "Orientierung [gon]", erg.Orientierung_gon.ToString("F4", IC));
            ProjektdatenManager.SetValue("Rückwärtschnitt", "s0 [mm]",            erg.s0_mm.ToString("F2", IC));

            // Standpunkt in Feldbuchpunkte speichern
            FeldbuchpunkteManager.AddOrUpdate(new FeldbuchPunkt
            {
                PunktNr          = standpunkt,
                Typ              = "Standpunkt",
                R                = erg.R,
                H                = erg.H,
                Hoehe            = 0,
                Orientierung_gon = erg.Orientierung_gon,
                IstBerechnung3D  = false,
                Datum            = DateTime.Now.ToString("yyyy-MM-dd"),
                Quelle           = "Rückwärtschnitt"
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

    // ── Ergebnisanzeige ───────────────────────────────────────────────────────
    private void AktualisiereErgebnisAnzeige(RueckwaertsschnittErgebnis erg)
    {
        lblR.Text    = $"R:  {erg.R:F3} m";
        lblH.Text    = $"H:  {erg.H:F3} m";
        lblZ.Text    = $"Orientierung z:  {erg.Orientierung_gon:F4} gon";
        lblS0.Text   = erg.Redundanz > 0
            ? $"s\u2080 = {erg.s0_mm:F2} mm     r = {erg.Redundanz}     {erg.Iterationen} Iter." +
              (erg.Konvergiert ? "" : "  !! NICHT konvergiert !!")
            : $"r = 0  (eindeutig bestimmt, {erg.Iterationen} Iter.)";

        bool hatWarnung = !string.IsNullOrEmpty(erg.KritischerKreis);
        lblKreis.Text      = hatWarnung ? "⚠  " + erg.KritischerKreis : "";
        lblKreis.Visible   = hatWarnung;
        lblKreis.ForeColor = Color.DarkRed;

        // Residuentabelle
        dgvResiduen.Rows.Clear();
        foreach (var res in erg.Residuen)
        {
            string v = res.Aktiv
                ? res.vWinkel_cc.ToString("+0.0;-0.0;0.0", IC)
                : "-";
            int rowIdx = dgvResiduen.Rows.Add(res.PunktNr, $"{res.StreckeH:F1}", v);
            if (res.Aktiv)
                AmpelFarbe(dgvResiduen.Rows[rowIdx].Cells[2], Math.Abs(res.vWinkel_cc));
        }
    }

    private static void AmpelFarbe(DataGridViewCell cell, double absVal_cc)
    {
        cell.Style.BackColor = absVal_cc > 60  ? Color.FromArgb(200,  60,  60) :
                               absVal_cc > 20  ? Color.FromArgb(220, 100,  60) :
                               absVal_cc >  5  ? Color.FromArgb(255, 200,  80) :
                                                 Color.FromArgb(160, 220, 130);
    }

    // ── Protokoll ────────────────────────────────────────────────────────────
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (_letzteErgebnis != null && _letztePunkte != null && !_protokollGeschrieben)
        {
            _protokollGeschrieben = true;
            RueckwaertsschnittProtokoll.Schreiben(
                _letzteErgebnis, _letztePunkte, txtStandpunkt.Text.Trim());
        }
    }

    private void btnSchliessen_Click(object? sender, EventArgs e) => Close();

    // ── Hilfsmethoden ────────────────────────────────────────────────────────
    private static bool TryParse(object? val, out double result)
    {
        string? s = val?.ToString()?.Replace(',', '.');
        return double.TryParse(s, NumberStyles.Any, IC, out result);
    }
}
