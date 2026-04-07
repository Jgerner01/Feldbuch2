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
            AppPfade.Get("Freie-Stationierung.xml"));

        // Checkboxen aus Einstellungen befüllen (nach ProjektManager.Laden())
        LadeOptionen();

        AktualisiereAnzeige();

        ProtokollManager.Log("START", $"Programm gestartet" +
            (ProjektManager.IstGeladen ? $" | Projekt: {ProjektManager.ProjektName}" : ""));
    }

    // ── Optionen laden / Handler ──────────────────────────────────────────────
    private void LadeOptionen()
    {
        // CheckedChanged-Handler kurz deaktivieren, um Cascaden zu vermeiden
        chkProtokoll.CheckedChanged    -= chkProtokoll_CheckedChanged;
        chkAutoBackup.CheckedChanged   -= chkOption_CheckedChanged;
        chkKoordTooltip.CheckedChanged -= chkOption_CheckedChanged;
        chkTon.CheckedChanged          -= chkOption_CheckedChanged;
        chkErwProto.CheckedChanged     -= chkOption_CheckedChanged;

        chkProtokoll.Checked    = ProjektManager.ProtokollAktiv;
        chkAutoBackup.Checked   = ProjektManager.AutoBackup;
        chkKoordTooltip.Checked = ProjektManager.KoordinatenTooltip;
        chkTon.Checked          = ProjektManager.TonBeiBerechnung;
        chkErwProto.Checked     = ProjektManager.ErweiterteProtokollierung;

        chkProtokoll.CheckedChanged    += chkProtokoll_CheckedChanged;
        chkAutoBackup.CheckedChanged   += chkOption_CheckedChanged;
        chkKoordTooltip.CheckedChanged += chkOption_CheckedChanged;
        chkTon.CheckedChanged          += chkOption_CheckedChanged;
        chkErwProto.CheckedChanged     += chkOption_CheckedChanged;
    }

    private void chkProtokoll_CheckedChanged(object? sender, EventArgs e)
    {
        ProjektManager.ProtokollAktiv = chkProtokoll.Checked;
        ProjektManager.SpeichereOptionen();
        if (ProjektManager.ProtokollAktiv)
            ProtokollManager.Log("EINST", "Protokollierung aktiviert");
    }

    private void chkOption_CheckedChanged(object? sender, EventArgs e)
    {
        ProjektManager.AutoBackup                = chkAutoBackup.Checked;
        ProjektManager.KoordinatenTooltip        = chkKoordTooltip.Checked;
        ProjektManager.TonBeiBerechnung          = chkTon.Checked;
        ProjektManager.ErweiterteProtokollierung = chkErwProto.Checked;
        ProjektManager.SpeichereOptionen();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        ProtokollManager.Log("ENDE", "Programm beendet");
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

        ProtokollManager.Log("PROJEKT", $"Projekt gewechselt: {ProjektManager.ProjektName}");
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

    private void btnKonvertierung_Click(object? sender, EventArgs e)
    {
        using var form = new FormKonvertierung();
        form.ShowDialog(this);
    }

    private void btnDatenManager_Click(object? sender, EventArgs e)
    {
        using var form = new FormDatenEditor();
        form.ShowDialog(this);
    }

    private void btnProtokolle_Click(object? sender, EventArgs e)
    {
        string startDir = ProjektManager.IstGeladen
            ? ProjektManager.ProjektVerzeichnis
            : AppPfade.Basis;

        using var dlg = new OpenFileDialog
        {
            Title            = "Protokoll öffnen",
            Filter           = "RTF-Protokolle (*.rtf)|*.rtf|Alle Dateien (*.*)|*.*",
            InitialDirectory = Directory.Exists(startDir) ? startDir : "",
        };

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var info = new System.Diagnostics.ProcessStartInfo(dlg.FileName)
            {
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(info);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Datei konnte nicht geöffnet werden:\n{ex.Message}",
                "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnTachymeterKommunikation_Click(object? sender, EventArgs e)
    {
        using var form = new FormTachymeterKommunikation();
        form.ShowDialog(this);
    }

    private void btnInfo_Click(object? sender, EventArgs e)
    {
        using var form = new FormInfo();
        form.ShowDialog(this);
    }
}
