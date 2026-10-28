namespace Feldbuch;

public partial class FormDxfExport : Form
{
    /// <summary>Fehlerellipsen werden exportiert wenn gesetzt und Residuen aktiv.</summary>
    [System.ComponentModel.DesignerSerializationVisibility(
        System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<OverlayFehlerEllipse>? FehlerEllipsen { get; set; }

    public FormDxfExport()
    {
        InitializeComponent();
    }

    private void btnExportieren_Click(object? sender, EventArgs e)
    {
        var feldbuchPunkte  = FeldbuchpunkteManager.Punkte;
        var importPunkte    = ImportPunkteManager.Punkte;
        var gemessenePunkte = NeupunkteManager.Koordinaten;

        int gesamt = feldbuchPunkte.Count + importPunkte.Count + gemessenePunkte.Count;
        if (gesamt == 0)
        {
            MessageBox.Show(
                "Keine Punkte vorhanden.\n" +
                "Bitte zuerst Koordinaten importieren, Punkte messen\n" +
                "oder eine Freie Stationierung berechnen.",
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
                feldbuchPunkte,
                importPunkte,
                gemessenePunkte,
                dlg.FileName,
                massstab:       (double)nudMassstab.Value,
                symbolSizeMm:   (double)nudSymbolMm.Value,
                textSizeMm:     (double)nudTextMm.Value,
                fehlerEllipsen: FehlerEllipsen);

            int nEllipsen = FehlerEllipsen?.Count ?? 0;
            MessageBox.Show(
                $"Export erfolgreich:\n" +
                $"  {feldbuchPunkte.Count} berechnete Punkt(e)\n" +
                $"  {importPunkte.Count} eingelesene Koordinate(n)\n" +
                $"  {gemessenePunkte.Count} gemessene Punkt(e)\n" +
                (nEllipsen > 0 ? $"  {nEllipsen} Fehlerellipse(n)\n" : "") +
                $"\n{dlg.FileName}",
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
