namespace Feldbuch;

using System.Globalization;

public partial class FormBogenschnitt : Form
{
    private const int MAX_ROWS = 10;
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    private BogenschnittErgebnis?      _letzteErgebnis;
    private List<BogenschnittMessung>? _letzteMessungen;
    private bool _protokollGeschrieben;

    public FormBogenschnitt()
    {
        InitializeComponent();
        InitGrid();
        LadeAnschlusspunkte();
    }

    private void InitGrid()
    {
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn  { HeaderText = "PunktNr (Station)", Name = "PunktNr",  FillWeight = 25 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn  { HeaderText = "R [m]",             Name = "R",        FillWeight = 22 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn  { HeaderText = "H [m]",             Name = "H",        FillWeight = 22 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn  { HeaderText = "Strecke [m]",       Name = "Strecke",  FillWeight = 21 });
        dgvPunkte.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Aktiv",             Name = "Aktiv",    FillWeight = 10 });
        dgvPunkte.Rows.Add(MAX_ROWS);
        foreach (DataGridViewRow row in dgvPunkte.Rows)
            row.Cells["Aktiv"].Value = true;
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
            dgvPunkte.Rows[i].Cells["Aktiv"].Value   = true;
        }
    }

    private void btnLaden_Click(object? sender, EventArgs e) => LadeAnschlusspunkte();

    private void btnBerechnen_Click(object? sender, EventArgs e)
    {
        var messungen = new List<BogenschnittMessung>();
        var aktiv     = new List<bool>();

        foreach (DataGridViewRow row in dgvPunkte.Rows)
        {
            string? pnr = row.Cells["PunktNr"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(pnr)) continue;

            if (!TryParse(row.Cells["R"].Value,       out double r)  ||
                !TryParse(row.Cells["H"].Value,       out double h)  ||
                !TryParse(row.Cells["Strecke"].Value, out double s))
            {
                MessageBox.Show($"Ungültige Eingabe in Zeile {row.Index + 1}.",
                    "Eingabefehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            messungen.Add(new BogenschnittMessung { PunktNr = pnr, R = r, H = h, Strecke = s });
            aktiv.Add(row.Cells["Aktiv"].Value is true);
        }

        if (messungen.Count < 2)
        {
            MessageBox.Show("Mindestens 2 vollständige Zeilen erforderlich.",
                "Zu wenig Punkte", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var erg = BogenschnittRechner.Berechnen(messungen, aktiv.ToArray());
            _letzteErgebnis  = erg;
            _letzteMessungen = messungen;

            string neupunkt = txtNeupunkt.Text.Trim();
            ProjektdatenManager.SetValue("Bogenschnitt", "PunktNr",  neupunkt);
            ProjektdatenManager.SetValue("Bogenschnitt", "R [m]",    erg.R.ToString("F3", IC));
            ProjektdatenManager.SetValue("Bogenschnitt", "H [m]",    erg.H.ToString("F3", IC));
            ProjektdatenManager.SetValue("Bogenschnitt", "s0 [mm]",  erg.s0_mm.ToString("F2", IC));

            FeldbuchpunkteManager.AddOrUpdate(new FeldbuchPunkt
            {
                PunktNr         = neupunkt,
                Typ             = "Neupunkt",
                R               = erg.R,
                H               = erg.H,
                Hoehe           = 0,
                IstBerechnung3D = false,
                Datum           = DateTime.Now.ToString("yyyy-MM-dd"),
                Quelle          = "Bogenschnitt"
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

    private void AktualisiereErgebnisAnzeige(BogenschnittErgebnis erg)
    {
        lblR.Text = $"R:  {erg.R:F3} m";
        lblH.Text = $"H:  {erg.H:F3} m";

        string s0Text = erg.Redundanz > 0
            ? $"s\u2080 = {erg.s0_mm:F2} mm     r = {erg.Redundanz}     {erg.Iterationen} Iter." +
              (erg.Konvergiert ? "" : "  !! NICHT konvergiert !!")
            : $"r = 0  (eindeutig bestimmt, {erg.Iterationen} Iter.)";

        if (erg.ZweiLoesungen)
            s0Text += $"\n2. Lösung: R = {erg.R2:F3} m   H = {erg.H2:F3} m  (3. Punkt für Eindeutigkeit empfohlen)";

        lblS0.Text = s0Text;

        dgvResiduen.Rows.Clear();
        foreach (var res in erg.Residuen)
        {
            string v = res.Aktiv
                ? res.vStrecke_mm.ToString("+0.0;-0.0;0.0", IC)
                : "-";
            int idx = dgvResiduen.Rows.Add(res.PunktNr, v);
            if (res.Aktiv)
                AmpelFarbe(dgvResiduen.Rows[idx].Cells[1], Math.Abs(res.vStrecke_mm));
        }
    }

    private static void AmpelFarbe(DataGridViewCell cell, double absVal_mm)
    {
        cell.Style.BackColor = absVal_mm > 30 ? Color.FromArgb(200,  60,  60) :
                               absVal_mm > 10 ? Color.FromArgb(220, 100,  60) :
                               absVal_mm >  3 ? Color.FromArgb(255, 200,  80) :
                                                Color.FromArgb(160, 220, 130);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (_letzteErgebnis != null && _letzteMessungen != null && !_protokollGeschrieben)
        {
            _protokollGeschrieben = true;
            BogenschnittProtokoll.Schreiben(
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
