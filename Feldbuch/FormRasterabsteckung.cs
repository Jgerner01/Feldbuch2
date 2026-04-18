namespace Feldbuch;

using System.Globalization;

public partial class FormRasterabsteckung : Form
{
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;
    private StandpunktInfo? _station;
    private List<AbsteckPunkt> _punkte = new();

    public FormRasterabsteckung()
    {
        InitializeComponent();
        LadeStation();
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
            mapLageplan.SetHeaderText("Rasterabsteckung – kein Standpunkt geladen");
        }
    }

    private void btnBerechnen_Click(object? sender, EventArgs e)
    {
        LadeStation();
        if (!TryParse(txtR0.Text,       out double r0)  ||
            !TryParse(txtH0.Text,       out double h0)  ||
            !TryParse(txtRichtung.Text,  out double phi) ||
            !TryParse(txtdS.Text,        out double dS)  ||
            !TryParse(txtdQ.Text,        out double dQ)  ||
            !int.TryParse(txtnRows.Text.Trim(), out int nRows) ||
            !int.TryParse(txtnCols.Text.Trim(), out int nCols) ||
            nRows < 1 || nCols < 1 || dS <= 0 || dQ <= 0)
        {
            MessageBox.Show("Alle Rasterparameter korrekt eingeben.", "Eingabe",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (nRows > 26)
        {
            MessageBox.Show("Maximal 26 Zeilen (A–Z).", "Eingabe",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _punkte = AbsteckungRechner.BerechneRaster(r0, h0, phi, dS, dQ, nRows, nCols, _station);
        FuelleTabelle();
        RefreshGrafik();
    }

    private void FuelleTabelle()
    {
        dgvPunkte.Rows.Clear();
        foreach (var p in _punkte)
        {
            string hz = _station != null ? p.Hz_soll_gon.ToString("F4", IC) : "-";
            string s  = _station != null ? p.s_soll_m.ToString("F3", IC)    : "-";
            dgvPunkte.Rows.Add(p.PunktNr, p.R_soll.ToString("F3", IC),
                p.H_soll.ToString("F3", IC), hz, s, "offen");
        }
    }

    private void RefreshGrafik()
    {
        mapLageplan.Canvas.OverlayEntities =
            AbsteckungGrafik.ErzeugeRasterabsteckungOverlay(_station, _punkte);
        mapLageplan.Canvas.Invalidate();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        mapLageplan.SpeichereZoom();
        base.OnFormClosing(e);
        if (_punkte.Count > 0)
            RasterabsteckungProtokoll.Schreiben(_station, _punkte,
                txtR0.Text, txtH0.Text, txtRichtung.Text,
                txtdS.Text, txtdQ.Text, txtnRows.Text, txtnCols.Text);
    }

    private void btnSchliessen_Click(object? sender, EventArgs e) => Close();

    private static bool TryParse(string? s, out double r) =>
        double.TryParse(s?.Replace(',', '.'), NumberStyles.Any, IC, out r);
}
