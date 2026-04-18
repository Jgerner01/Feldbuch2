namespace Feldbuch;

using System.Globalization;

public partial class FormHochpunktherablegung : Form
{
    private const int MAX_ROWS = 10;
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    private HochpunktErgebnis?       _letzteErgebnis;
    private List<HochpunktMessung>?  _letzteMessungen;
    private bool _protokollGeschrieben;

    public FormHochpunktherablegung()
    {
        InitializeComponent();
        InitGrid();
        LadeAnschlusspunkte();
    }

    private void InitGrid()
    {
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn  { HeaderText = "PunktNr (Station)", Name = "PunktNr",  FillWeight = 18 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn  { HeaderText = "R [m]",             Name = "R",        FillWeight = 14 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn  { HeaderText = "H [m]",             Name = "H",        FillWeight = 14 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn  { HeaderText = "Höhe [m]",          Name = "Hoehe",    FillWeight = 12 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn  { HeaderText = "iH [m]",            Name = "iH",       FillWeight = 10 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn  { HeaderText = "Hz [gon]",          Name = "Hz",       FillWeight = 14 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn  { HeaderText = "z [gon]",           Name = "z",        FillWeight = 14 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn  { HeaderText = "V [gon]",           Name = "V",        FillWeight = 14 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn  { HeaderText = "zh [m]",            Name = "zh",       FillWeight = 10 });
        dgvPunkte.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Aktiv",             Name = "Aktiv",    FillWeight =  8 });
        dgvPunkte.Rows.Add(MAX_ROWS);
        foreach (DataGridViewRow row in dgvPunkte.Rows)
        {
            row.Cells["iH"].Value    = "0.000";
            row.Cells["z"].Value     = "0.0000";
            row.Cells["zh"].Value    = "0.000";
            row.Cells["Aktiv"].Value = true;
        }
    }

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
            if (parts.Length >= 4) dgvPunkte.Rows[i].Cells["Hoehe"].Value = parts[3].Trim();
            dgvPunkte.Rows[i].Cells["iH"].Value    = "0.000";
            dgvPunkte.Rows[i].Cells["z"].Value     = "0.0000";
            dgvPunkte.Rows[i].Cells["zh"].Value    = "0.000";
            dgvPunkte.Rows[i].Cells["Aktiv"].Value = true;
        }
    }

    private void btnLaden_Click(object? sender, EventArgs e) => LadeAnschlusspunkte();

    private void btnzLaden_Click(object? sender, EventArgs e)
    {
        string? zStr = ProjektdatenManager.GetValue("Freie Stationierung", "Orientierung [gon]");
        if (string.IsNullOrWhiteSpace(zStr))
        {
            MessageBox.Show("Keine Orientierung aus Freier Stationierung verfügbar.",
                "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
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
        var messungen = new List<HochpunktMessung>();
        var aktiv     = new List<bool>();

        foreach (DataGridViewRow row in dgvPunkte.Rows)
        {
            string? pnr = row.Cells["PunktNr"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(pnr)) continue;

            if (!TryParse(row.Cells["R"].Value,     out double r)    ||
                !TryParse(row.Cells["H"].Value,     out double h)    ||
                !TryParse(row.Cells["Hoehe"].Value, out double hoehe)||
                !TryParse(row.Cells["iH"].Value,    out double ih)   ||
                !TryParse(row.Cells["Hz"].Value,    out double hz)   ||
                !TryParse(row.Cells["z"].Value,     out double z)    ||
                !TryParse(row.Cells["V"].Value,     out double v)    ||
                !TryParse(row.Cells["zh"].Value,    out double zh))
            {
                MessageBox.Show($"Ungültige Eingabe in Zeile {row.Index + 1}.",
                    "Eingabefehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            messungen.Add(new HochpunktMessung
            {
                PunktNr  = pnr,
                R        = r,
                H        = h,
                Hoehe    = hoehe,
                iH       = ih,
                Hz       = hz,
                z        = z,
                V        = v,
                Zielhöhe = zh,
            });
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
            var erg = HochpunktherablegungRechner.Berechnen(messungen, aktiv.ToArray());
            _letzteErgebnis  = erg;
            _letzteMessungen = messungen;

            string punktnr = txtPunktNr.Text.Trim();
            ProjektdatenManager.SetValue("Hochpunktherablegung", "PunktNr",      punktnr);
            ProjektdatenManager.SetValue("Hochpunktherablegung", "R [m]",        erg.R.ToString("F3", IC));
            ProjektdatenManager.SetValue("Hochpunktherablegung", "H [m]",        erg.H.ToString("F3", IC));
            ProjektdatenManager.SetValue("Hochpunktherablegung", "Höhe [m]",     erg.Hoehe.ToString("F3", IC));
            ProjektdatenManager.SetValue("Hochpunktherablegung", "s0Dir [mm]",   erg.s0Dir_mm.ToString("F2", IC));
            ProjektdatenManager.SetValue("Hochpunktherablegung", "s0H [mm]",     erg.s0H_mm.ToString("F2", IC));

            FeldbuchpunkteManager.AddOrUpdate(new FeldbuchPunkt
            {
                PunktNr         = punktnr,
                Typ             = "Neupunkt",
                R               = erg.R,
                H               = erg.H,
                Hoehe           = erg.Hoehe,
                IstBerechnung3D = true,
                Datum           = DateTime.Now.ToString("yyyy-MM-dd"),
                Quelle          = "Hochpunktherablegung"
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

    private void AktualisiereErgebnisAnzeige(HochpunktErgebnis erg)
    {
        lblR.Text     = $"R:  {erg.R:F3} m";
        lblH.Text     = $"H:  {erg.H:F3} m";
        lblHoehe.Text = $"Höhe:  {erg.Hoehe:F3} m";

        string dirInfo = erg.RedundanzDir > 0
            ? $"Richtungen – s\u2080 = {erg.s0Dir_mm:F2} mm    r = {erg.RedundanzDir}    {erg.Iterationen} Iter." +
              (erg.Konvergiert ? "" : "  !! NICHT konvergiert !!")
            : $"Richtungen – r = 0  (eindeutig bestimmt, {erg.Iterationen} Iter.)";
        string hInfo = erg.RedundanzH > 0
            ? $"Höhe – s\u2080 = {erg.s0H_mm:F2} mm    r = {erg.RedundanzH}"
            : "Höhe – r = 0  (eindeutig bestimmt)";
        lblS0.Text = dirInfo + "     " + hInfo;

        dgvResiduen.Rows.Clear();
        foreach (var res in erg.Residuen)
        {
            string vDir = res.AktivDir
                ? res.vDir_cc.ToString("+0.0;-0.0;0.0", IC)
                : "-";
            string sStr   = res.AktivDir ? $"{res.s_horiz:F1}" : "-";
            string hPiStr = res.AktivH   ? $"{res.HoehePi:F3}" : "-";
            string vH     = res.AktivH   ? res.vH_mm.ToString("+0.0;-0.0;0.0", IC) : "-";

            int idx = dgvResiduen.Rows.Add(res.PunktNr, sStr, vDir, hPiStr, vH);
            if (res.AktivDir)
                AmpelFarbe(dgvResiduen.Rows[idx].Cells[2], Math.Abs(res.vDir_cc), isDir: true);
            if (res.AktivH)
                AmpelFarbe(dgvResiduen.Rows[idx].Cells[4], Math.Abs(res.vH_mm),   isDir: false);
        }
    }

    private static void AmpelFarbe(DataGridViewCell cell, double absVal, bool isDir)
    {
        if (isDir)
        {
            cell.Style.BackColor = absVal > 60 ? Color.FromArgb(200,  60,  60) :
                                   absVal > 20 ? Color.FromArgb(220, 100,  60) :
                                   absVal >  5 ? Color.FromArgb(255, 200,  80) :
                                                 Color.FromArgb(160, 220, 130);
        }
        else
        {
            cell.Style.BackColor = absVal > 30 ? Color.FromArgb(200,  60,  60) :
                                   absVal > 10 ? Color.FromArgb(220, 100,  60) :
                                   absVal >  3 ? Color.FromArgb(255, 200,  80) :
                                                 Color.FromArgb(160, 220, 130);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (_letzteErgebnis != null && _letzteMessungen != null && !_protokollGeschrieben)
        {
            _protokollGeschrieben = true;
            HochpunktherablegungProtokoll.Schreiben(
                _letzteErgebnis, _letzteMessungen, txtPunktNr.Text.Trim());
        }
    }

    private void btnSchliessen_Click(object? sender, EventArgs e) => Close();

    private static bool TryParse(object? val, out double result)
    {
        string? s = val?.ToString()?.Replace(',', '.');
        return double.TryParse(s, NumberStyles.Any, IC, out result);
    }
}
