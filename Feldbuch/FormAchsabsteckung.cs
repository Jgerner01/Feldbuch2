namespace Feldbuch;

using System.Globalization;

public partial class FormAchsabsteckung : Form
{
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;
    private StandpunktInfo? _station;
    private List<AbsteckPunkt> _punkte = new();
    private int _currentIdx = -1;

    public FormAchsabsteckung()
    {
        InitializeComponent();
        LadeStation();

        dgvAchse.CellEndEdit     += (_, __) => RefreshGrafik();
        dgvAchse.CellMouseClick  += DgvAchse_CellMouseClick;
        dgvPunkte.CellClick      += (s, e) => { if (e.RowIndex >= 0) SelectPoint(e.RowIndex); };
        dgvPunkte.CellMouseClick += DgvPunkte_CellMouseClick;

        mapLageplan.PunktGefangen += OnPunktAusKarteGefangen;
        einweiser.PunktMarkiert   += OnPunktMarkiert;
    }

    // ── Standpunkt ────────────────────────────────────────────────────────────
    private void LadeStation()
    {
        _station = AbsteckungRechner.LadeStandpunkt();
        if (_station != null)
        {
            lblStation.Text = $"Standpunkt: {_station.PunktNr}   R={_station.R:F3}   H={_station.H:F3}   z={_station.Orientierung_gon:F4} gon";
            mapLageplan.SetHeaderText($"Standpunkt: {_station.PunktNr}   R = {_station.R:F3}   H = {_station.H:F3}   z = {_station.Orientierung_gon:F4} gon");
        }
        else
        {
            lblStation.Text = "Kein Standpunkt geladen.";
            mapLageplan.SetHeaderText("Achsabsteckung – kein Standpunkt geladen");
        }
    }

    // ── Achskoordinaten lesen ─────────────────────────────────────────────────
    private bool LeseAchse(out double rA, out double hA, out double rE, out double hE)
    {
        rA = hA = rE = hE = 0;
        return TryParseCell(0, "R", out rA) && TryParseCell(0, "H", out hA)
            && TryParseCell(1, "R", out rE) && TryParseCell(1, "H", out hE);
    }

    private bool TryParseCell(int row, string col, out double val)
        => TryParse(dgvAchse.Rows[row].Cells[col].Value?.ToString(), out val);

    // ── Punkt aus Karte → Achsdefinition ─────────────────────────────────────
    private void OnPunktAusKarteGefangen(double r, double h, string? punktNr)
    {
        int rowIdx = dgvAchse.CurrentRow?.Index ?? -1;
        if (rowIdx < 0)
            for (int i = 0; i < 2; i++)
                if (string.IsNullOrWhiteSpace(dgvAchse.Rows[i].Cells["R"].Value?.ToString()))
                { rowIdx = i; break; }
        if (rowIdx < 0) rowIdx = 0;

        dgvAchse.Rows[rowIdx].Cells["PunktNr"].Value = punktNr ?? "";
        dgvAchse.Rows[rowIdx].Cells["R"].Value        = r.ToString("F3", IC);
        dgvAchse.Rows[rowIdx].Cells["H"].Value        = h.ToString("F3", IC);

        int next = rowIdx + 1;
        if (next < 2) dgvAchse.CurrentCell = dgvAchse.Rows[next].Cells["PunktNr"];

        RefreshGrafik();
    }

    // ── Berechnen ─────────────────────────────────────────────────────────────
    private void btnBerechnen_Click(object? sender, EventArgs e)
    {
        LadeStation();
        if (!LeseAchse(out double rA, out double hA, out double rE, out double hE))
        {
            MessageBox.Show("Anfangs- und Endpunkt der Achse eingeben.", "Eingabe",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!TryParse(txtIntervall.Text, out double intervall) || intervall < 0.01)
        {
            MessageBox.Show("Gültiges Intervall eingeben.", "Eingabe",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        double[] offsets = ParseOffsets(txtOffsets.Text);
        _punkte = AbsteckungRechner.BerechneAchspunkte(rA, hA, rE, hE, intervall, offsets, _station);
        _currentIdx = -1;
        FuelleTabelle();
        if (_punkte.Count > 0) SelectPoint(0);
        else RefreshGrafik();
    }

    private static double[] ParseOffsets(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<double>();
        var list = new List<double>();
        foreach (var p in text.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            if (TryParse(p, out double o)) list.Add(o);
        return list.ToArray();
    }

    // ── Tabelle füllen ────────────────────────────────────────────────────────
    private void FuelleTabelle()
    {
        dgvPunkte.Rows.Clear();
        foreach (var p in _punkte)
        {
            string hz = _station != null ? p.Hz_soll_gon.ToString("F4", IC) : "-";
            string s  = _station != null ? p.s_soll_m.ToString("F3", IC)    : "-";
            string ab = p.Abszisse_m.HasValue ? p.Abszisse_m.Value.ToString("F3", IC) : "";
            string or = p.Ordinate_m.HasValue  ? p.Ordinate_m.Value.ToString("F3", IC)  : "";
            dgvPunkte.Rows.Add(p.PunktNr, p.Label, p.R_soll.ToString("F3", IC),
                p.H_soll.ToString("F3", IC), ab, or, hz, s, p.Status);
        }
    }

    // ── Punkt auswählen (Absteckziel setzen) ──────────────────────────────────
    private void SelectPoint(int rowIdx)
    {
        if (rowIdx >= _punkte.Count) return;
        _currentIdx = rowIdx;
        var p = _punkte[rowIdx];

        einweiser.SetzeZielPunkt(
            p.PunktNr,
            _station != null ? p.Hz_soll_gon : (double?)null,
            _station != null ? p.s_soll_m    : (double?)null);

        RefreshGrafik();
    }

    // ── Punkt markiert ────────────────────────────────────────────────────────
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
        if (next >= 0) SelectPoint(next);
        else RefreshGrafik();
    }

    // ── Kontextmenü dgvAchse (Zeile leeren) ──────────────────────────────────
    private void DgvAchse_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right) return;
        if (e.RowIndex < 0 || e.ColumnIndex != 0) return;

        dgvAchse.CurrentCell = dgvAchse.Rows[e.RowIndex].Cells["PunktNr"];
        string typ = dgvAchse.Rows[e.RowIndex].Cells["Typ"].Value?.ToString() ?? "";
        var menu = new ContextMenuStrip();
        menu.Items.Add($"Punkt '{typ}' löschen", null, (_, __) => AchszeileLeeren(e.RowIndex));
        menu.Show(dgvAchse, dgvAchse.PointToClient(Cursor.Position));
    }

    private void AchszeileLeeren(int rowIdx)
    {
        dgvAchse.Rows[rowIdx].Cells["PunktNr"].Value = "";
        dgvAchse.Rows[rowIdx].Cells["R"].Value        = "";
        dgvAchse.Rows[rowIdx].Cells["H"].Value        = "";
        _punkte.Clear();
        _currentIdx = -1;
        FuelleTabelle();
        einweiser.Zuruecksetzen();
        RefreshGrafik();
    }

    // ── Kontextmenü dgvPunkte (Punkt entfernen) ───────────────────────────────
    private void DgvPunkte_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right) return;
        if (e.RowIndex < 0 || e.ColumnIndex != 0) return;
        if (e.RowIndex >= _punkte.Count) return;

        dgvPunkte.CurrentCell = dgvPunkte.Rows[e.RowIndex].Cells["PunktNr"];
        string nr = _punkte[e.RowIndex].PunktNr;
        var menu = new ContextMenuStrip();
        menu.Items.Add($"Punkt '{nr}' entfernen", null, (_, __) => PunktEntfernen(e.RowIndex));
        menu.Show(dgvPunkte, dgvPunkte.PointToClient(Cursor.Position));
    }

    private void PunktEntfernen(int rowIdx)
    {
        if (rowIdx >= _punkte.Count) return;
        _punkte.RemoveAt(rowIdx);
        dgvPunkte.Rows.RemoveAt(rowIdx);
        _currentIdx = -1;
        einweiser.Zuruecksetzen();
        RefreshGrafik();
    }

    // ── Grafik ────────────────────────────────────────────────────────────────
    private void RefreshGrafik()
    {
        double rA = 0, hA = 0, rE = 0, hE = 0;
        bool achseOk = LeseAchse(out rA, out hA, out rE, out hE);

        mapLageplan.Canvas.OverlayEntities = achseOk
            ? AbsteckungGrafik.ErzeugeAchsabsteckungOverlay(_station, _punkte, rA, hA, rE, hE, _currentIdx)
            : AbsteckungGrafik.ErzeugePunktabsteckungOverlay(_station, _punkte, _currentIdx);
        mapLageplan.Canvas.Invalidate();
    }

    // ── Protokoll ─────────────────────────────────────────────────────────────
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        mapLageplan.SpeichereZoom();
        base.OnFormClosing(e);
        if (_punkte.Count > 0)
        {
            string rA = dgvAchse.Rows[0].Cells["R"].Value?.ToString() ?? "";
            string hA = dgvAchse.Rows[0].Cells["H"].Value?.ToString() ?? "";
            string rE = dgvAchse.Rows[1].Cells["R"].Value?.ToString() ?? "";
            string hE = dgvAchse.Rows[1].Cells["H"].Value?.ToString() ?? "";
            AchsabsteckungProtokoll.Schreiben(_station, _punkte,
                rA, hA, rE, hE, txtIntervall.Text, txtOffsets.Text);
        }
    }

    private void btnSchliessen_Click(object? sender, EventArgs e) => Close();

    private static bool TryParse(string? s, out double r) =>
        double.TryParse(s?.Replace(',', '.'), NumberStyles.Any, IC, out r);
}
