namespace Feldbuch;

using System.Globalization;

// ══════════════════════════════════════════════════════════════════════════════
// FormAutoMatchProtokoll  –  Anzeige des Auto-Match-Protokolls
// ══════════════════════════════════════════════════════════════════════════════
public partial class FormAutoMatchProtokoll : Form
{
    private readonly string _standpunktNr;
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    public FormAutoMatchProtokoll(string standpunktNr)
    {
        _standpunktNr = standpunktNr;
        InitializeComponent();
        Text = $"Auto-Match-Protokoll – Standpunkt {standpunktNr}";
        LadeEintraege();
    }

    private void LadeEintraege()
    {
        var eintraege = AutoMatchProtokoll.Laden(_standpunktNr);
        dgvProtokoll.Rows.Clear();

        foreach (var e in eintraege)
        {
            string dist = e.AbstandGewählt_m >= 0
                ? e.AbstandGewählt_m.ToString("F3", IC) + " m"
                : "–";

            dgvProtokoll.Rows.Add(
                e.Zeitstempel.ToString("HH:mm:ss"),
                $"E={e.StationE:F1} N={e.StationN:F1}",
                $"Hz={e.Hz_gon:F4} V={e.V_gon:F4}" +
                    (e.D_m > 0 ? $" D={e.D_m:F3}" : ""),
                $"E={e.E_pred:F3} N={e.N_pred:F3}",
                $"{e.Radius_m:F3} m",
                e.AnzahlTreffer.ToString(),
                e.GewaehlterPunkt,
                dist,
                e.Ergebnis.ToString()
            );

            // Hintergrundfarbe nach Ergebnis
            var row = dgvProtokoll.Rows[dgvProtokoll.Rows.Count - 1];
            row.DefaultCellStyle.BackColor = e.Ergebnis switch
            {
                AutoMatchErgebnis.AutoMatch      => Color.FromArgb(210, 245, 215),
                AutoMatchErgebnis.Bestaetigt     => Color.FromArgb(220, 235, 250),
                AutoMatchErgebnis.Abgelehnt      => Color.FromArgb(255, 230, 220),
                AutoMatchErgebnis.KeinTreffer    => Color.FromArgb(250, 240, 210),
                AutoMatchErgebnis.MehrereTreffer => Color.FromArgb(240, 230, 255),
                _ => Color.White
            };
        }

        lblAnzahl.Text = $"{eintraege.Count} Einträge";
    }

    private void btnAktualisieren_Click(object? sender, EventArgs e)
        => LadeEintraege();

    private void btnCsvOeffnen_Click(object? sender, EventArgs e)
    {
        string pfad = AutoMatchProtokoll.GetPfad(_standpunktNr);
        if (File.Exists(pfad))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(pfad) { UseShellExecute = true });
        else
            MessageBox.Show("Protokolldatei nicht gefunden:\n" + pfad,
                "Nicht gefunden", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void btnSchliessen_Click(object? sender, EventArgs e)
        => Close();
}
