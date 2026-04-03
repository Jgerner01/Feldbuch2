namespace Feldbuch;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();

        // Letztes Projekt aus Einstellungen.xml laden
        ProjektManager.Laden();

        // ProjektdatenManager im Projektverzeichnis initialisieren
        ProjektdatenManager.Initialize(
            ProjektManager.GetPfad("Projektdaten.csv"));

        // Feldbuchpunkte (Standpunkte, Neupunkte) laden
        FeldbuchpunkteManager.Initialize(
            ProjektManager.GetPfad("Feldbuchpunkte.json"));

        // Rechenparameter bleiben global (Geräteeinstellungen)
        RechenparameterManager.Initialize(
            Path.Combine(AppContext.BaseDirectory, "Freie-Stationierung.xml"));

        AktualisiereAnzeige();
    }

    // ── Projekt ───────────────────────────────────────────────────────────────
    private void btnProjekt_Click(object? sender, EventArgs e)
    {
        using var form = new FormProjekt();
        if (form.ShowDialog(this) != DialogResult.OK) return;

        // Manager im neuen Verzeichnis neu initialisieren
        ProjektdatenManager.Initialize(
            ProjektManager.GetPfad("Projektdaten.csv"));
        FeldbuchpunkteManager.Initialize(
            ProjektManager.GetPfad("Feldbuchpunkte.json"));

        AktualisiereAnzeige();
    }

    private void AktualisiereAnzeige()
    {
        if (ProjektManager.IstGeladen)
        {
            lblProjektInfo.Text = $"Projekt:  {ProjektManager.ProjektName}" +
                                  $"   \u2022   {ProjektManager.ProjektVerzeichnis}";
            Text = $"Feldbuch  –  {ProjektManager.ProjektName}";
        }
        else
        {
            lblProjektInfo.Text = "Kein Projekt gewählt  –  bitte Projekt auswählen";
            Text = "Feldbuch";
        }
    }

    // ── Module ────────────────────────────────────────────────────────────────
    private void btnFreieStationierung_Click(object? sender, EventArgs e)
    {
        using var form = new FormFreieStationierung();
        form.ShowDialog(this);
    }

    private void btnMessen_Click(object? sender, EventArgs e)
    {
        MessageBox.Show("Messen", "Feldbuch");
    }

    private void btnPrismenkonstante_Click(object? sender, EventArgs e)
    {
        using var form = new FormPrismenkonstante();
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            MessageBox.Show(
                $"Prismenkonstante gesetzt: {form.GewähltePrismenkonstante:+0.0;-0.0;0.0} mm",
                "Feldbuch");
        }
    }

    private void btnDxfViewer_Click(object? sender, EventArgs e)
    {
        using var form = new FormDxfViewer();
        form.ShowDialog(this);
    }

    private void btnProjektdaten_Click(object? sender, EventArgs e)
    {
        using var form = new FormProjektdaten();
        form.ShowDialog(this);
    }
}
