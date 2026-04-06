namespace Feldbuch;

using System.Globalization;

public partial class FormDxfViewer : Form
{
    // Zuletzt gepickter Punkt – für spätere Übergabe an Freie Stationierung
    public (double X, double Y)? GepickterPunkt { get; private set; }

    // Aktuelle Eingabewerte aus der Toolbar
    public string AktuellePunktNr  => txtPunktNr.Text.Trim();
    public string AktuellerCode    => txtCode.Text.Trim();

    // Pfad zur CSV-Datei mit den Anschlusspunkten – immer im aktuellen Projektverzeichnis
    public static string AnschlusspunktePfad =>
        ProjektManager.GetPfad("Anschlusspunkte.csv");

    // Pfad der aktuell geladenen DXF-Datei (für Zoom-Persistenz)
    private string _aktuellerDxfPfad = "";

    public FormDxfViewer()
    {
        InitializeComponent();

        // Snap-Zustand aus Projektdaten laden
        string? snapState = ProjektdatenManager.GetValue("Einstellungen", "Snap");
        canvas.SnapActive = snapState != "Inaktiv";   // Standard: aktiv
        UpdateSnapButton();

        // Katasterpunkte-Zustand aus Projektdaten laden
        string? punkteState = ProjektdatenManager.GetValue("Einstellungen", "Katasterpunkte");
        canvas.PunkteVisible = punkteState != "Inaktiv";   // Standard: aktiv
        UpdatePunkteButton();

        canvas.PointPicked += OnPointPicked;
        FormClosed         += OnFormClosed;

        // ImportPunkteManager initialisieren
        ImportPunkteManager.Initialize(
            ProjektManager.GetPfad("ImportPunkte.json"));

        // Feldbuchpunkte-Overlay aufbauen
        BaueOverlay();

        // Import-Overlay aufbauen (persistente Punkte aus letzter Session)
        BaueImportOverlay();

        // DXF automatisch laden, wenn Projektverzeichnis eine passende DXF-Datei enthält
        if (ProjektManager.HatDxfDatei)
            LadeDxfDatei(ProjektManager.DxfPfad);
        else if (canvas.OverlayEntities.Count > 0 || canvas.ImportLayers.Count > 0)
            canvas.FitToView();
    }

    // ── Import-Layer aufbauen ─────────────────────────────────────────────────
    // Legt pro importierter Datei einen eigenen ImportLayer an und
    // synchronisiert die Checkbox-Leiste in flpLayers.
    private void AddImportLayer(string dateiPfad, List<ImportPunkt> punkte)
    {
        string name = Path.GetFileName(dateiPfad);

        // Bereits vorhandenen Layer aktualisieren oder neuen anlegen
        var layer = canvas.ImportLayers.FirstOrDefault(
            l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (layer == null)
        {
            layer = new ImportLayer(name);
            canvas.ImportLayers.Add(layer);
        }
        else
        {
            layer.Entities.Clear();
        }

        foreach (var p in punkte)
            layer.Entities.Add(new OverlayImportPunkt(p.PunktNr, p.R, p.H, p.Hoehe));

        BaueCheckboxen();
        canvas.Invalidate();
    }

    // Checkboxen in der Statusleiste neu aufbauen (eine je Layer)
    private void BaueCheckboxen()
    {
        flpLayers.Controls.Clear();

        foreach (var layer in canvas.ImportLayers)
        {
            var chk = new CheckBox
            {
                Text      = layer.Name,
                Checked   = layer.Visible,
                AutoSize  = true,
                Font      = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(30, 30, 30),
                Margin    = new Padding(6, 3, 0, 0),
                Cursor    = Cursors.Hand
            };
            // Closure über lokale Kopie des Layers
            var l = layer;
            chk.CheckedChanged += (s, e) =>
            {
                l.Visible = chk.Checked;
                canvas.Invalidate();
            };
            // Tooltip mit vollständigem Namen
            var tt = new ToolTip();
            tt.SetToolTip(chk, layer.Name);

            flpLayers.Controls.Add(chk);
        }
    }

    // Initialer Aufbau beim Öffnen (persistente Punkte aus letzter Session)
    public void BaueImportOverlay()
    {
        // Alle gespeicherten Punkte als einen Layer „ImportPunkte.json" laden
        var punkte = ImportPunkteManager.Punkte.ToList();
        if (punkte.Count == 0) return;

        string name = "ImportPunkte.json";
        var layer = canvas.ImportLayers.FirstOrDefault(
            l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? new ImportLayer(name);

        layer.Entities.Clear();
        foreach (var p in punkte)
            layer.Entities.Add(new OverlayImportPunkt(p.PunktNr, p.R, p.H, p.Hoehe));

        if (!canvas.ImportLayers.Contains(layer))
            canvas.ImportLayers.Add(layer);

        BaueCheckboxen();
        canvas.Invalidate();
    }

    // ── KOR / CSV einlesen ────────────────────────────────────────────────────
    private void btnImportKorCsv_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title  = "KOR- oder CSV-Datei importieren",
            Filter = "KOR / CSV|*.kor;*.csv|KOR-Datei|*.kor|CSV-Datei|*.csv|Alle Dateien|*.*"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        List<ImportPunkt> punkte;
        string ext = Path.GetExtension(dlg.FileName).ToLowerInvariant();
        try
        {
            punkte = ext == ".csv"
                ? ImportPunkteManager.LeseCsv(dlg.FileName)
                : ImportPunkteManager.LeseKor(dlg.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Lesen:\n{ex.Message}", "Importfehler",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var (added, dupl) = ImportPunkteManager.AddRange(punkte);
        AddImportLayer(dlg.FileName, punkte);
        canvas.FitToView();

        string msg = $"{added} Punkte importiert aus {Path.GetFileName(dlg.FileName)}";
        if (dupl > 0) msg += $"  ({dupl} Duplikat(e) übersprungen)";
        lblStatus.Text = msg;
    }

    // ── JSON einlesen ─────────────────────────────────────────────────────────
    private void btnImportJson_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title  = "JSON-Datei importieren",
            Filter = "JSON-Dateien|*.json|Alle Dateien|*.*"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        List<ImportPunkt> punkte;
        try
        {
            punkte = ImportPunkteManager.LeseJson(dlg.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Lesen:\n{ex.Message}", "Importfehler",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (punkte.Count == 0)
        {
            MessageBox.Show("Keine Punkte in der JSON-Datei gefunden.\n" +
                "Unterstützt werden ImportPunkte.json und Feldbuchpunkte.json.",
                "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var (added, dupl) = ImportPunkteManager.AddRange(punkte);
        AddImportLayer(dlg.FileName, punkte);
        canvas.FitToView();

        string msg = $"{added} Punkte importiert aus {Path.GetFileName(dlg.FileName)}";
        if (dupl > 0) msg += $"  ({dupl} Duplikat(e) übersprungen)";
        lblStatus.Text = msg;
    }

    // ── Feldbuchpunkte-Overlay aufbauen ───────────────────────────────────────
    public void BaueOverlay()
    {
        canvas.OverlayEntities.Clear();
        foreach (var p in FeldbuchpunkteManager.Punkte)
        {
            canvas.OverlayEntities.Add(p.Typ == "Standpunkt"
                ? (DxfEntity)new OverlayStandpunkt(p)
                : new OverlayNeupunkt(p));
        }
        canvas.Invalidate();
    }

    // ── Toolbar: rechtsbündige Positionierung ────────────────────────────────
    private void PositioniereToolbarFelder()
    {
        int cy  = (pnlTop.Height - 24) / 2;   // vertikale Zentrierung
        int r   = pnlTop.Width - 8;            // rechter Rand

        // Code-Eingabe ganz rechts
        txtCode.Location  = new Point(r - txtCode.Width, cy);
        r -= txtCode.Width + 8;

        // Code-Label
        lblCode.Location  = new Point(r - lblCode.Width, cy);
        r -= lblCode.Width + 16;

        // PunktNr-Eingabe
        txtPunktNr.Location = new Point(r - txtPunktNr.Width, cy);
        r -= txtPunktNr.Width + 8;

        // PunktNr-Label
        lblNr.Location = new Point(r - lblNr.Width, cy);
    }

    // ── Prismenkonstante ──────────────────────────────────────────────────────
    private void btnPrismenkonstante_Click(object? sender, EventArgs e)
    {
        using var form = new FormPrismenkonstante();
        if (form.ShowDialog(this) == DialogResult.OK)
            lblStatus.Text = $"Prismenkonstante: {form.GewähltePrismenkonstante:+0.0;-0.0;0.0} mm";
    }

    // ── Neu: DXF-Inhalt löschen ───────────────────────────────────────────────
    private void btnNeu_Click(object? sender, EventArgs e)
    {
        var res = MessageBox.Show(
            "Die aktuelle DXF-Datei aus dem Viewer entfernen?\n(Die Datei auf der Festplatte bleibt erhalten.)",
            "DXF leeren", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (res != DialogResult.Yes) return;

        canvas.Entities.Clear();
        _aktuellerDxfPfad = "";
        Text           = "DXF-Viewer";
        lblStatus.Text = "DXF-Inhalt geleert.";
        canvas.Invalidate();
    }

    // ── Datei öffnen ──────────────────────────────────────────────────────────
    private void btnOpen_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title            = "DXF-Datei öffnen",
            Filter           = "DXF-Dateien (*.dxf)|*.dxf|Alle Dateien (*.*)|*.*",
            InitialDirectory = ProjektManager.ProjektVerzeichnis
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        LadeDxfDatei(dlg.FileName);
    }

    private void LadeDxfDatei(string pfad)
    {
        try
        {
            Cursor = Cursors.WaitCursor;
            canvas.Entities = DxfReader.Read(pfad);

            // Gespeicherten Zoom wiederherstellen – sonst Fit-to-View
            var zoom = ProjektManager.LadeZoom(pfad);
            if (zoom.HasValue)
            {
                canvas.Scale = zoom.Value.Scale;
                canvas.PanX  = zoom.Value.PanX;
                canvas.PanY  = zoom.Value.PanY;
                canvas.Invalidate();
            }
            else
            {
                canvas.FitToView();
            }

            _aktuellerDxfPfad = pfad;
            string name       = Path.GetFileName(pfad);
            lblStatus.Text    = $"Geladen: {name}  |  {canvas.Entities.Count} Objekte";
            Text              = $"DXF-Viewer – {name}";
        }
        catch (Exception ex)
        {
            MessageBox.Show("Fehler beim Lesen der DXF-Datei:\n" + ex.Message,
                "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { Cursor = Cursors.Default; }
    }

    // ── Zoom beim Schließen speichern ─────────────────────────────────────────
    private void OnFormClosed(object? sender, FormClosedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_aktuellerDxfPfad))
            ProjektManager.SpeichereZoom(
                _aktuellerDxfPfad, canvas.Scale, canvas.PanX, canvas.PanY);
    }

    // ── Zoom-Buttons ──────────────────────────────────────────────────────────
    private void btnZoomIn_Click (object? s, EventArgs e) => canvas.ZoomIn();
    private void btnZoomOut_Click(object? s, EventArgs e) => canvas.ZoomOut();
    private void btnFit_Click    (object? s, EventArgs e) => canvas.FitToView();

    // ── Snap-Button ───────────────────────────────────────────────────────────
    private void btnSnap_Click(object? s, EventArgs e)
    {
        canvas.SnapActive = !canvas.SnapActive;
        UpdateSnapButton();
        // Zustand in Projektdaten speichern
        ProjektdatenManager.SetValue("Einstellungen", "Snap",
            canvas.SnapActive ? "Aktiv" : "Inaktiv");
    }

    // ── DXF ein-/ausblenden ───────────────────────────────────────────────────
    private void btnDxfToggle_Click(object? s, EventArgs e)
    {
        canvas.DxfVisible = !canvas.DxfVisible;
        canvas.Invalidate();
        UpdateDxfToggleButton();
    }

    private void UpdateDxfToggleButton()
    {
        if (canvas.DxfVisible)
        {
            btnDxfToggle.BackColor = Color.FromArgb(60, 100, 160);
            btnDxfToggle.ForeColor = Color.White;
            btnDxfToggle.FlatAppearance.BorderColor = Color.FromArgb(40, 80, 140);
        }
        else
        {
            btnDxfToggle.BackColor = Color.FromArgb(190, 190, 190);
            btnDxfToggle.ForeColor = Color.FromArgb(30, 30, 30);
            btnDxfToggle.FlatAppearance.BorderColor = Color.FromArgb(160, 160, 160);
        }
        string state = canvas.DxfVisible ? "EIN" : "AUS";
        ToolTip tt = new();
        tt.SetToolTip(btnDxfToggle, $"DXF-Darstellung – aktuell {state}");
    }

    // ── DXF-Export ────────────────────────────────────────────────────────────
    private void btnExportDxf_Click(object? s, EventArgs e)
    {
        using var dlg = new FormDxfExport();
        dlg.ShowDialog(this);
    }

    // ── Katasterpunkte-Button ─────────────────────────────────────────────────
    private void btnPunkte_Click(object? s, EventArgs e)
    {
        canvas.PunkteVisible = !canvas.PunkteVisible;
        canvas.Invalidate();
        UpdatePunkteButton();
        ProjektdatenManager.SetValue("Einstellungen", "Katasterpunkte",
            canvas.PunkteVisible ? "Aktiv" : "Inaktiv");
    }

    private void UpdatePunkteButton()
    {
        if (canvas.PunkteVisible)
        {
            btnPunkte.BackColor = Color.FromArgb(200, 120, 30);   // orange = aktiv
            btnPunkte.ForeColor = Color.White;
            btnPunkte.FlatAppearance.BorderColor = Color.FromArgb(220, 140, 50);
        }
        else
        {
            btnPunkte.BackColor = Color.FromArgb(190, 190, 190);   // grau = inaktiv
            btnPunkte.ForeColor = Color.FromArgb(30, 30, 30);
            btnPunkte.FlatAppearance.BorderColor = Color.FromArgb(160, 160, 160);
        }
        string state = canvas.PunkteVisible ? "EIN" : "AUS";
        ToolTip tt = new();
        tt.SetToolTip(btnPunkte, $"Katasterpunkte – aktuell {state}");
    }

    private void UpdateSnapButton()
    {
        if (canvas.SnapActive)
        {
            btnSnap.BackColor = Color.FromArgb(200, 120, 30);   // orange = aktiv
            btnSnap.ForeColor = Color.White;
            btnSnap.FlatAppearance.BorderColor = Color.FromArgb(220, 140, 50);
        }
        else
        {
            btnSnap.BackColor = Color.FromArgb(190, 190, 190);   // grau = inaktiv
            btnSnap.ForeColor = Color.FromArgb(30, 30, 30);
            btnSnap.FlatAppearance.BorderColor = Color.FromArgb(160, 160, 160);
        }
        string state = canvas.SnapActive ? "EIN" : "AUS";
        // Tooltip aktualisieren
        ToolTip tt = new();
        tt.SetToolTip(btnSnap, $"Punktfang (Snap) – aktuell {state}");
    }

    // ── Picking / Koordinatenanzeige ──────────────────────────────────────────
    private void OnPointPicked(double x, double y, DxfEntity? entity)
    {
        string snapHinweis = canvas.SnapActive ? " [SNAP]" : "";

        if (entity != null)
        {
            GepickterPunkt = (x, y);
            lblStatus.Text = $"R: {x:F3}   H: {y:F3}{snapHinweis}   |   {entity.GetInfo()}";

            // Messdatendialog öffnen
            using var dlg = new FormMessdatenEingabe(x, y);
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Ergebnis != null)
            {
                SchreibeAnschlusspunkt(dlg.Ergebnis);
                lblStatus.Text = $"Punkt '{dlg.Ergebnis.PunktNr}' gespeichert  |  " +
                                 $"R: {x:F3}   H: {y:F3}";
            }
        }
        else
        {
            string info = canvas.Entities.Count > 0
                ? $"R: {x:F3}   H: {y:F3}{snapHinweis}   |   Kein Objekt ausgewählt"
                : $"R: {x:F3}   H: {y:F3}";
            lblStatus.Text = info;
        }
    }

    // ── Anschlusspunkt in CSV schreiben ───────────────────────────────────────
    // Vorhandener Eintrag mit gleicher PunktNr wird ersetzt; neuer wird angehängt.
    private static void SchreibeAnschlusspunkt(StationierungsPunkt p)
    {
        var ic     = CultureInfo.InvariantCulture;
        const string header = "PunktNr,R,H,Hoehe,HZ,V,Strecke,Zielhoehe";

        var zeilen = new List<string>();
        if (File.Exists(AnschlusspunktePfad))
        {
            foreach (var line in File.ReadAllLines(AnschlusspunktePfad).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split(',');
                if (parts.Length > 0 &&
                    string.Equals(parts[0].Trim(), p.PunktNr, StringComparison.Ordinal))
                    continue;   // wird durch neue Zeile ersetzt
                zeilen.Add(line);
            }
        }

        zeilen.Add(string.Join(",", new[]
        {
            p.PunktNr,
            p.R        .ToString("F3", ic),
            p.H        .ToString("F3", ic),
            p.Hoehe    .ToString("F3", ic),
            p.HZ       .ToString("F4", ic),
            p.V        .ToString("F4", ic),
            p.Strecke  .ToString("F3", ic),
            p.Zielhoehe.ToString("F3", ic)
        }));

        File.WriteAllLines(AnschlusspunktePfad,
            new[] { header }.Concat(zeilen),
            System.Text.Encoding.UTF8);
    }
}
