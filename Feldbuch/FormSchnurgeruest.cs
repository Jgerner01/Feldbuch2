namespace Feldbuch;

using System.Globalization;

public partial class FormSchnurgeruest : Form
{
    private const int MAX_POLYGON = 20;
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    private StandpunktInfo? _station;
    private List<(double R, double H)> _polygon = new();
    private List<AbsteckPunkt> _sgPunkte = new();

    public FormSchnurgeruest()
    {
        InitializeComponent();
        LadeStation();
        InitPolygonGrid();
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
            mapLageplan.SetHeaderText("Schnurgerüst – kein Standpunkt geladen");
        }
    }

    private void InitPolygonGrid()
    {
        dgvPolygon.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "R [m]", Name = "R", FillWeight = 50 });
        dgvPolygon.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "H [m]", Name = "H", FillWeight = 50 });
        dgvPolygon.Rows.Add(MAX_POLYGON);
    }

    private void btnLaden_Click(object? sender, EventArgs e)
    {
        string pfad = FormDxfViewer.AnschlusspunktePfad;
        if (!File.Exists(pfad)) return;
        var lines = File.ReadAllLines(pfad).Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        foreach (DataGridViewRow row in dgvPolygon.Rows)
            foreach (DataGridViewCell c in row.Cells) c.Value = null;
        for (int i = 0; i < Math.Min(lines.Count, MAX_POLYGON); i++)
        {
            var parts = lines[i].Split(',');
            if (parts.Length < 3) continue;
            dgvPolygon.Rows[i].Cells["R"].Value = parts[1].Trim();
            dgvPolygon.Rows[i].Cells["H"].Value = parts[2].Trim();
        }
    }

    private void btnBerechnen_Click(object? sender, EventArgs e)
    {
        LadeStation();
        if (!TryParse(txtAbstand.Text, out double abstand) || abstand <= 0)
        {
            MessageBox.Show("Gültigen Schnurgerüst-Abstand [m] eingeben.", "Eingabe",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _polygon.Clear();
        foreach (DataGridViewRow row in dgvPolygon.Rows)
        {
            if (!TryParse(row.Cells["R"].Value, out double r) ||
                !TryParse(row.Cells["H"].Value, out double h)) continue;
            _polygon.Add((r, h));
        }

        if (_polygon.Count < 3)
        {
            MessageBox.Show("Mindestens 3 Polygon-Punkte erforderlich.", "Eingabe",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _sgPunkte = AbsteckungRechner.BerechneSchnurgeruest(_polygon, abstand, _station);
        FuelleErgebnisTabelle();
        RefreshGrafik();
    }

    private void FuelleErgebnisTabelle()
    {
        dgvSG.Rows.Clear();
        foreach (var p in _sgPunkte)
        {
            string hz = _station != null ? p.Hz_soll_gon.ToString("F4", IC) : "-";
            string s  = _station != null ? p.s_soll_m.ToString("F3", IC) : "-";
            dgvSG.Rows.Add(p.PunktNr, p.R_soll.ToString("F3", IC),
                p.H_soll.ToString("F3", IC), hz, s);
        }
    }

    private void RefreshGrafik()
    {
        mapLageplan.Canvas.OverlayEntities =
            AbsteckungGrafik.ErzeugeSchnurgeruestOverlay(_station, _polygon, _sgPunkte);
        mapLageplan.Canvas.Invalidate();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        mapLageplan.SpeichereZoom();
        base.OnFormClosing(e);
        if (_sgPunkte.Count > 0)
            SchnurgeruestProtokoll.Schreiben(_station, _polygon, _sgPunkte, txtAbstand.Text);
    }

    private void btnSchliessen_Click(object? sender, EventArgs e) => Close();

    private static bool TryParse(object? val, out double r) =>
        double.TryParse(val?.ToString()?.Replace(',', '.'), NumberStyles.Any, IC, out r);
    private static bool TryParse(string? s, out double r) =>
        double.TryParse(s?.Replace(',', '.'), NumberStyles.Any, IC, out r);
}
