namespace Feldbuch;

using System.Globalization;

public partial class FormPunktabsteckung : Form
{
    private const int MAX_ROWS = 20;
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    private StandpunktInfo? _station;
    private List<AbsteckPunkt> _punkte = new();
    private int _currentIdx = -1;

    public FormPunktabsteckung()
    {
        InitializeComponent();
        LadeStation();
        InitGrid();
    }

    public FormPunktabsteckung(List<AbsteckPunkt> punkte, StandpunktInfo? station)
    {
        InitializeComponent();
        _station = station;
        UpdateStationLabel();
        InitGrid();
        _punkte = punkte;
        FillGridFromList();
    }

    private void FillGridFromList()
    {
        for (int i = 0; i < Math.Min(_punkte.Count, MAX_ROWS); i++)
        {
            var p = _punkte[i];
            dgvPunkte.Rows[i].Cells["PunktNr"].Value = p.PunktNr;
            dgvPunkte.Rows[i].Cells["R"].Value        = p.R_soll.ToString("F3", IC);
            dgvPunkte.Rows[i].Cells["H"].Value        = p.H_soll.ToString("F3", IC);
            dgvPunkte.Rows[i].Cells["Hz"].Value       = _station != null ? p.Hz_soll_gon.ToString("F4", IC) : "-";
            dgvPunkte.Rows[i].Cells["s"].Value        = _station != null ? p.s_soll_m.ToString("F3", IC)    : "-";
            dgvPunkte.Rows[i].Cells["Status"].Value   = p.Status;
        }
        if (_punkte.Count > 0) SelectPoint(0);
    }

    // ── Standpunkt ────────────────────────────────────────────────────────────
    private void LadeStation()
    {
        _station = AbsteckungRechner.LadeStandpunkt();
        UpdateStationLabel();
    }

    private void UpdateStationLabel()
    {
        if (_station != null)
        {
            lblStation.Text = $"Standpunkt:  {_station.PunktNr}    R = {_station.R:F3}    H = {_station.H:F3}    z = {_station.Orientierung_gon:F4} gon";
            mapLageplan.SetHeaderText($"Standpunkt: {_station.PunktNr}   R = {_station.R:F3}   H = {_station.H:F3}   z = {_station.Orientierung_gon:F4} gon");
        }
        else
        {
            lblStation.Text = "Kein Standpunkt – bitte Rückwärtschnitt oder Freie Stationierung durchführen.";
            mapLageplan.SetHeaderText("Kein Standpunkt geladen");
        }
    }

    // ── Grid ──────────────────────────────────────────────────────────────────
    private void InitGrid()
    {
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PunktNr",    Name = "PunktNr", FillWeight = 18 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "R_soll [m]", Name = "R",       FillWeight = 22 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "H_soll [m]", Name = "H",       FillWeight = 22 });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Hz [gon]",   Name = "Hz",      FillWeight = 18, ReadOnly = true });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "s [m]",      Name = "s",       FillWeight = 14, ReadOnly = true });
        dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status",     Name = "Status",  FillWeight = 14, ReadOnly = true });
        dgvPunkte.Rows.Add(MAX_ROWS);

        dgvPunkte.CellClick      += (s, e) => { if (e.RowIndex >= 0) SelectPoint(e.RowIndex); };
        dgvPunkte.CellMouseClick += DgvPunkte_CellMouseClick;

        mapLageplan.PunktGefangen  += OnPunktAusKarteGefangen;
        einweiser.PunktMarkiert    += OnPunktMarkiert;
    }

    // ── Punkt auswählen ───────────────────────────────────────────────────────
    private void SelectPoint(int rowIdx)
    {
        string? pnr = dgvPunkte.Rows[rowIdx].Cells["PunktNr"].Value?.ToString();
        if (pnr == null) return;
        int pIdx = _punkte.FindIndex(p => p.PunktNr == pnr);
        if (pIdx < 0) return;

        _currentIdx = pIdx;
        var p = _punkte[pIdx];

        einweiser.SetzeZielPunkt(
            p.PunktNr,
            _station != null ? p.Hz_soll_gon : (double?)null,
            _station != null ? p.s_soll_m    : (double?)null);

        RefreshGrafik();
    }

    // ── Punkt aus Karte ───────────────────────────────────────────────────────
    private void OnPunktAusKarteGefangen(double r, double h, string? punktNr)
    {
        int rowIdx = dgvPunkte.CurrentRow?.Index ?? -1;
        if (rowIdx < 0)
            for (int i = 0; i < dgvPunkte.Rows.Count; i++)
                if (string.IsNullOrWhiteSpace(dgvPunkte.Rows[i].Cells["PunktNr"].Value?.ToString()))
                { rowIdx = i; break; }
        if (rowIdx < 0) return;

        dgvPunkte.Rows[rowIdx].Cells["PunktNr"].Value = punktNr ?? "";
        dgvPunkte.Rows[rowIdx].Cells["R"].Value        = r.ToString("F3", IC);
        dgvPunkte.Rows[rowIdx].Cells["H"].Value        = h.ToString("F3", IC);
        BerechneAlleAbsteckdaten();

        int next = rowIdx + 1;
        if (next < dgvPunkte.Rows.Count)
            dgvPunkte.CurrentCell = dgvPunkte.Rows[next].Cells["PunktNr"];
    }

    // ── Absteckdaten berechnen ────────────────────────────────────────────────
    private void BerechneAlleAbsteckdaten()
    {
        _punkte.Clear();
        foreach (DataGridViewRow row in dgvPunkte.Rows)
        {
            string? pnr = row.Cells["PunktNr"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(pnr)) continue;
            if (!TryParse(row.Cells["R"].Value, out double r) ||
                !TryParse(row.Cells["H"].Value, out double h)) continue;

            var p = new AbsteckPunkt { PunktNr = pnr, R_soll = r, H_soll = h };
            if (_station != null) AbsteckungRechner.BerechnePolareAbsteckung(_station, p);
            _punkte.Add(p);

            row.Cells["Hz"].Value     = _station != null ? p.Hz_soll_gon.ToString("F4", IC) : "-";
            row.Cells["s"].Value      = _station != null ? p.s_soll_m.ToString("F3", IC) : "-";
            row.Cells["Status"].Value = "offen";
        }

        if (_currentIdx >= _punkte.Count) _currentIdx = -1;
        if (_punkte.Count > 0 && _currentIdx < 0) SelectPoint(0);
        else RefreshGrafik();
    }

    // ── Punkt markiert (vom Einweiser-Event) ──────────────────────────────────
    private void OnPunktMarkiert(string punktNr)
    {
        int pIdx = _punkte.FindIndex(p => p.PunktNr == punktNr);
        if (pIdx < 0) return;

        _punkte[pIdx].Status = "abgesteckt";
        foreach (DataGridViewRow row in dgvPunkte.Rows)
        {
            if (row.Cells["PunktNr"].Value?.ToString() == punktNr)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(180, 230, 180);
                row.Cells["Status"].Value = "abgesteckt";
                break;
            }
        }

        int next = _punkte.FindIndex(_currentIdx + 1, p => p.Status == "offen");
        if (next < 0) next = _punkte.FindIndex(p => p.Status == "offen");
        if (next >= 0)
        {
            foreach (DataGridViewRow row in dgvPunkte.Rows)
                if (row.Cells["PunktNr"].Value?.ToString() == _punkte[next].PunktNr)
                { SelectPoint(row.Index); break; }
        }
        else RefreshGrafik();
    }

    // ── Zeile löschen (Rechtsklick Spalte 0) ─────────────────────────────────
    private void DgvPunkte_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right) return;
        if (e.RowIndex < 0 || e.ColumnIndex != 0) return;
        if (string.IsNullOrWhiteSpace(dgvPunkte.Rows[e.RowIndex].Cells["PunktNr"].Value?.ToString())) return;

        dgvPunkte.CurrentCell = dgvPunkte.Rows[e.RowIndex].Cells["PunktNr"];
        var menu = new ContextMenuStrip();
        menu.Items.Add($"Zeile {e.RowIndex + 1} löschen", null, (_, __) => ZeileLöschen(e.RowIndex));
        menu.Show(dgvPunkte, dgvPunkte.PointToClient(Cursor.Position));
    }

    private void ZeileLöschen(int rowIdx)
    {
        for (int i = rowIdx; i < dgvPunkte.Rows.Count - 1; i++)
        {
            var src = dgvPunkte.Rows[i + 1];
            var dst = dgvPunkte.Rows[i];
            foreach (DataGridViewColumn col in dgvPunkte.Columns)
                dst.Cells[col.Name].Value = src.Cells[col.Name].Value;
            dst.DefaultCellStyle.BackColor = src.DefaultCellStyle.BackColor;
        }
        var last = dgvPunkte.Rows[dgvPunkte.Rows.Count - 1];
        foreach (DataGridViewColumn col in dgvPunkte.Columns) last.Cells[col.Name].Value = null;
        last.DefaultCellStyle.BackColor = Color.Empty;

        _currentIdx = -1;
        BerechneAlleAbsteckdaten();
    }

    // ── Buttons ───────────────────────────────────────────────────────────────
    private void btnLaden_Click(object? sender, EventArgs e)
    {
        string pfad = FormDxfViewer.AnschlusspunktePfad;
        if (!File.Exists(pfad)) { MessageBox.Show("Keine Punkte verfügbar.", "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

        var lines = File.ReadAllLines(pfad).Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        foreach (DataGridViewRow row in dgvPunkte.Rows) foreach (DataGridViewCell c in row.Cells) c.Value = null;

        for (int i = 0; i < Math.Min(lines.Count, MAX_ROWS); i++)
        {
            var parts = lines[i].Split(',');
            if (parts.Length < 3) continue;
            dgvPunkte.Rows[i].Cells["PunktNr"].Value = parts[0].Trim();
            dgvPunkte.Rows[i].Cells["R"].Value        = parts[1].Trim();
            dgvPunkte.Rows[i].Cells["H"].Value        = parts[2].Trim();
        }
        BerechneAlleAbsteckdaten();
    }

    private void btnBerechnen_Click(object? sender, EventArgs e)
    {
        LadeStation();
        BerechneAlleAbsteckdaten();
    }

    // ── Grafik ────────────────────────────────────────────────────────────────
    private void RefreshGrafik()
    {
        mapLageplan.Canvas.OverlayEntities =
            AbsteckungGrafik.ErzeugePunktabsteckungOverlay(_station, _punkte, _currentIdx);
        mapLageplan.Canvas.Invalidate();
    }

    // ── Protokoll ─────────────────────────────────────────────────────────────
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        mapLageplan.SpeichereZoom();
        base.OnFormClosing(e);
        if (_punkte.Count > 0)
            PunktabsteckungProtokoll.Schreiben(_station, _punkte);
    }

    private void btnSchliessen_Click(object? sender, EventArgs e) => Close();

    private static bool TryParse(object? val, out double result)
        => double.TryParse(val?.ToString()?.Replace(',', '.'), NumberStyles.Any, IC, out result);

    private static bool TryParse(string? s, out double result)
        => double.TryParse(s?.Replace(',', '.'), NumberStyles.Any, IC, out result);
}
