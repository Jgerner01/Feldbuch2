namespace Feldbuch;

public partial class FormDxfExport : Form
{
    public FormDxfExport()
    {
        InitializeComponent();
    }

    private void btnExportieren_Click(object? sender, EventArgs e)
    {
        var punkte = FeldbuchpunkteManager.Punkte;
        if (punkte.Count == 0)
        {
            MessageBox.Show("Keine Feldbuchpunkte vorhanden.\n" +
                            "Bitte zuerst eine Freie Stationierung berechnen.",
                "Keine Daten", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Zieldatei wählen
        string vorschlag = ProjektManager.IstGeladen
            ? System.IO.Path.Combine(ProjektManager.ProjektVerzeichnis,
                                     ProjektManager.ProjektName + "_Feldbuch.dxf")
            : "Feldbuch.dxf";

        using var dlg = new SaveFileDialog
        {
            Title            = "Feldbuchpunkte exportieren",
            Filter           = "DXF-Dateien (*.dxf)|*.dxf",
            FileName         = System.IO.Path.GetFileName(vorschlag),
            InitialDirectory = System.IO.Path.GetDirectoryName(vorschlag)
                               ?? ProjektManager.ProjektVerzeichnis
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            DxfExporter.Exportieren(
                punkte,
                dlg.FileName,
                massstab:     (double)nudMassstab.Value,
                symbolSizeMm: (double)nudSymbolMm.Value,
                textSizeMm:   (double)nudTextMm.Value);

            MessageBox.Show(
                $"Exportiert: {punkte.Count} Punkt(e)\n{dlg.FileName}",
                "Export erfolgreich", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Fehler beim Export:\n" + ex.Message,
                "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnAbbrechen_Click(object? sender, EventArgs e)
        => DialogResult = DialogResult.Cancel;
}
