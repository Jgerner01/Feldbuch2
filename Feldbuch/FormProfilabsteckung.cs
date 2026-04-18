namespace Feldbuch;

using System.Globalization;

public partial class FormProfilabsteckung : Form
{
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;
    private StandpunktInfo? _station;
    private List<ProfilAbsteckPunkt> _profile = new();
    private double _planumHalbbreite = 3.0;
    private double _boeschNeigung    = 1.5;
    private double _rA, _hA, _rE, _hE;

    public FormProfilabsteckung()
    {
        InitializeComponent();
        LadeStation();
        InitGrid();
    }

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
            mapLageplan.SetHeaderText("Profilabsteckung – kein Standpunkt geladen");
        }
    }

    private void InitGrid()
    {
        dgvProfile.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Station",      Name = "Station",     FillWeight = 10, ReadOnly = true });
        dgvProfile.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "R [m]",        Name = "R",           FillWeight = 12, ReadOnly = true });
        dgvProfile.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "H [m]",        Name = "H",           FillWeight = 12, ReadOnly = true });
        dgvProfile.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "H_Plan [m]",   Name = "H_Plan",      FillWeight = 13 });
        dgvProfile.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "H_Gel. [m]",   Name = "H_Gelände",   FillWeight = 13 });
        dgvProfile.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ΔH [m]",       Name = "DeltaH",      FillWeight = 10, ReadOnly = true });
        dgvProfile.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Bösch. [m]",   Name = "Boesch",      FillWeight = 11, ReadOnly = true });
        dgvProfile.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Hz [gon]",     Name = "Hz",          FillWeight = 13, ReadOnly = true });
        dgvProfile.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "s [m]",        Name = "s",           FillWeight = 10, ReadOnly = true });

        dgvProfile.CellEndEdit += dgvProfile_CellEndEdit;
    }

    private void btnGenerieren_Click(object? sender, EventArgs e)
    {
        LadeStation();
        if (!TryParse(txtR_A.Text, out double rA) || !TryParse(txtH_A.Text, out double hA) ||
            !TryParse(txtR_E.Text, out double rE) || !TryParse(txtH_E.Text, out double hE) ||
            !TryParse(txtIntervall.Text, out double intervall) || intervall < 0.01 ||
            !TryParse(txtHalbbreite.Text, out _planumHalbbreite) ||
            !TryParse(txtBoesch.Text, out _boeschNeigung))
        {
            MessageBox.Show("Achse A/E, Intervall, Planumhalbbreite und Böschungsneigung eingeben.",
                "Eingabe", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // H_Plan aus vorhandenen Zellen übernehmen
        var hPlanListe = new List<double>();
        foreach (DataGridViewRow row in dgvProfile.Rows)
            if (TryParse(row.Cells["H_Plan"].Value, out double hp)) hPlanListe.Add(hp);

        _rA = rA; _hA = hA; _rE = rE; _hE = hE;
        _profile = AbsteckungRechner.BerechneProfilpunkte(rA, hA, rE, hE,
            intervall, hPlanListe, _planumHalbbreite, _boeschNeigung, _station);

        FuelleTabelle();
        RefreshGrafik();
    }

    private void FuelleTabelle()
    {
        dgvProfile.Rows.Clear();
        foreach (var p in _profile)
        {
            string hz    = _station != null ? p.Hz_soll_gon.ToString("F4", IC) : "-";
            string s     = _station != null ? p.s_soll_m.ToString("F3", IC) : "-";
            string hGel  = p.H_Gelaende.HasValue ? p.H_Gelaende.Value.ToString("F3", IC) : "";
            string dH    = p.DeltaH_m.HasValue ? p.DeltaH_m.Value.ToString("+0.000;-0.000;0.000", IC) : "";
            string boesch = p.BoeschLinks_m.HasValue ? p.BoeschLinks_m.Value.ToString("F2", IC) : "";
            int idx = dgvProfile.Rows.Add(
                p.Station_m.ToString("F2", IC),
                p.R.ToString("F3", IC),
                p.H.ToString("F3", IC),
                p.H_plan.ToString("F3", IC),
                hGel, dH, boesch, hz, s);

            // ΔH einfärben
            if (p.DeltaH_m.HasValue)
            {
                var cell = dgvProfile.Rows[idx].Cells["DeltaH"];
                cell.Style.BackColor = p.DeltaH_m.Value > 0
                    ? Color.FromArgb(200, 230, 200)  // Auftrag = grün
                    : Color.FromArgb(255, 210, 200);  // Aushub = rot
            }
        }
    }

    private void RefreshGrafik()
    {
        var achsPunkte = _profile.Select(p => new AbsteckPunkt
        {
            PunktNr     = p.Station_m.ToString("F2", IC),
            R_soll      = p.R,
            H_soll      = p.H,
            Hz_soll_gon = p.Hz_soll_gon,
            s_soll_m    = p.s_soll_m
        }).ToList();
        mapLageplan.Canvas.OverlayEntities =
            AbsteckungGrafik.ErzeugeProfilabsteckungOverlay(_station, _rA, _hA, _rE, _hE, achsPunkte);
        mapLageplan.Canvas.Invalidate();
    }

    // Geländehöhe interaktiv eingetragen → Böschung sofort berechnen
    private void dgvProfile_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (dgvProfile.Columns[e.ColumnIndex].Name != "H_Gelände") return;
        if (e.RowIndex >= _profile.Count) return;

        if (!TryParse(dgvProfile.Rows[e.RowIndex].Cells["H_Gelände"].Value, out double hGel)) return;

        AbsteckungRechner.AktualisiereProfilPunkt(
            _profile[e.RowIndex], hGel, _planumHalbbreite, _boeschNeigung);

        var row = dgvProfile.Rows[e.RowIndex];
        var p   = _profile[e.RowIndex];
        row.Cells["DeltaH"].Value = p.DeltaH_m.HasValue
            ? p.DeltaH_m.Value.ToString("+0.000;-0.000;0.000", IC) : "";
        row.Cells["Boesch"].Value = p.BoeschLinks_m.HasValue
            ? p.BoeschLinks_m.Value.ToString("F2", IC) : "";
        if (p.DeltaH_m.HasValue)
        {
            row.Cells["DeltaH"].Style.BackColor = p.DeltaH_m.Value > 0
                ? Color.FromArgb(200, 230, 200)
                : Color.FromArgb(255, 210, 200);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        mapLageplan.SpeichereZoom();
        base.OnFormClosing(e);
        if (_profile.Count > 0)
            ProfilabsteckungProtokoll.Schreiben(_station, _profile,
                txtR_A.Text, txtH_A.Text, txtR_E.Text, txtH_E.Text,
                txtIntervall.Text, txtHalbbreite.Text, txtBoesch.Text);
    }

    private void btnSchliessen_Click(object? sender, EventArgs e) => Close();

    private static bool TryParse(object? val, out double r) =>
        double.TryParse(val?.ToString()?.Replace(',', '.'), NumberStyles.Any, IC, out r);
    private static bool TryParse(string? s, out double r) =>
        double.TryParse(s?.Replace(',', '.'), NumberStyles.Any, IC, out r);
}
