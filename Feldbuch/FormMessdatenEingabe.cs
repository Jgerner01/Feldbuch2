namespace Feldbuch;

using System.Globalization;

// ──────────────────────────────────────────────────────────────────────────────
// Dialog: Messdaten zu einem per DXF-Viewer gepickten Anschlusspunkt eingeben.
//
// Koordinaten R, H kommen aus dem DXF-Snap (read-only).
// Höhe, PunktNr und Messdaten (HZ, V, Strecke, Zielhöhe) werden eingetragen.
// "Simulieren" füllt plausible Testwerte ein (Platzhalter für TCR307-Anbindung).
// ──────────────────────────────────────────────────────────────────────────────
public partial class FormMessdatenEingabe : Form
{
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;
    private readonly double _r;
    private readonly double _h;

    /// <summary>Ergebnis nach OK – null wenn Abbrechen.</summary>
    public StationierungsPunkt? Ergebnis { get; private set; }

    public FormMessdatenEingabe(double r, double h)
    {
        InitializeComponent();
        _r = r;
        _h = h;
        lblRVal.Text = $"R (Rechtswert):   {r:F3} m";
        lblHVal.Text = $"H (Hochwert):     {h:F3} m";
    }

    // ── Simulieren ────────────────────────────────────────────────────────────
    // Füllt plausible Messwerte ein, wie sie ein Tachymeter liefern würde.
    // Später wird dieser Block durch die TCR307-Kommunikation ersetzt.
    private void btnSimulieren_Click(object? sender, EventArgs e)
    {
        var rng = new Random();
        // Horizontalrichtung: zufällig im Gesamtkreis
        nudHz.Value      = (decimal)Math.Round(rng.NextDouble() * 400.0,      4);
        // Zenitwinkel: leicht nach unten (95–108 gon typisch im Nahbereich)
        nudV.Value       = (decimal)Math.Round(95.0 + rng.NextDouble() * 13.0, 4);
        // Schrägstrecke: 30–150 m
        nudStrecke.Value = (decimal)Math.Round(30.0 + rng.NextDouble() * 120.0, 3);
        // Standard-Prismenstabshöhe
        nudZielhoehe.Value = 1.700m;
    }

    // ── Übernehmen ────────────────────────────────────────────────────────────
    private void btnUebernehmen_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtPunktNr.Text))
        {
            MessageBox.Show("Bitte eine Punktnummer eingeben.",
                "Eingabefehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtPunktNr.Focus();
            return;
        }
        if (nudStrecke.Value <= 0)
        {
            MessageBox.Show("Strecke muss größer als 0 sein.\n" +
                            "Bitte Messdaten eingeben oder 'Simulieren' verwenden.",
                "Eingabefehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            nudStrecke.Focus();
            return;
        }

        Ergebnis = new StationierungsPunkt
        {
            PunktNr   = txtPunktNr.Text.Trim(),
            R         = _r,
            H         = _h,
            Hoehe     = (double)nudHoehe.Value,
            HZ        = (double)nudHz.Value,
            V         = (double)nudV.Value,
            Strecke   = (double)nudStrecke.Value,
            Zielhoehe = (double)nudZielhoehe.Value
        };
        DialogResult = DialogResult.OK;
    }

    private void btnAbbrechen_Click(object? sender, EventArgs e)
        => DialogResult = DialogResult.Cancel;
}
