namespace Feldbuch;

using System.Globalization;

// ──────────────────────────────────────────────────────────────────────────────
// Flächenteilung (Abspaltung) – Hauptformular
// ──────────────────────────────────────────────────────────────────────────────
public partial class FormFlächenteilung : Form
{
    private readonly CultureInfo _ic = CultureInfo.InvariantCulture;

    private List<(double R, double H)> _polygon = new();
    private FlächenteilungErgebnis?    _ergebnis;

    // Auswahlzustand
    private int _selectedVertex = -1; // für V2 (fester Punkt) und V4 (Dreieck)
    private int _selectedEdge   = 0;  // für V1, V5, V6 (Grundkante)

    public FormFlächenteilung()
    {
        InitializeComponent();
        cmbVerfahren.SelectedIndex = 0;
        UpdateMethodParams();
        mapPreview.Canvas.MouseClick += MapPreview_MouseClick;
        mapPreview.SetHeaderText("Flächenteilung – Vorschau");
    }

    // ── Verfahren-Auswahl ─────────────────────────────────────────────────────
    private void cmbVerfahren_SelectedIndexChanged(object? sender, EventArgs e)
        => UpdateMethodParams();

    private void UpdateMethodParams()
    {
        int v = cmbVerfahren.SelectedIndex + 1; // 1..6

        // Sichtbarkeit je nach Verfahren
        lblEdge.Visible         = (v == 1 || v == 5 || v == 6);
        cmbEdge.Visible         = (v == 1 || v == 5 || v == 6);
        lblVertex.Visible       = (v == 4);
        cmbVertex.Visible       = (v == 4);
        lblFixR.Visible         = (v == 2);
        txtFixR.Visible         = (v == 2);
        lblFixH.Visible         = (v == 2);
        txtFixH.Visible         = (v == 2);
        lblRichtung.Visible     = (v == 3);
        txtRichtung.Visible     = (v == 3);
        lblBreite.Visible       = (v == 6);
        txtBreite.Visible       = (v == 6);
        lblRatio.Visible        = (v == 5);
        pnlRatio.Visible        = (v == 5);
        lblASoll.Visible        = (v != 6);
        txtASoll.Visible        = (v != 6);

        string hint = v switch
        {
            1 => "Grundkante wählen, dann Soll-Fläche eingeben.",
            2 => "Festen Punkt eingeben oder in Vorschau klicken.",
            3 => "Trennrichtung [gon] und Soll-Fläche eingeben.",
            4 => "Eckpunkt wählen, dann Soll-Fläche eingeben.",
            5 => "Verhältnis a:b, Basisverfahren und Grundkante wählen.",
            6 => "Grundkante und Streifenbreite [m] eingeben.",
            _ => ""
        };
        lblHint.Text = hint;

        if (v == 2 && _selectedVertex >= 0 && _selectedVertex < _polygon.Count)
        {
            txtFixR.Text = _polygon[_selectedVertex].R.ToString("F3", _ic);
            txtFixH.Text = _polygon[_selectedVertex].H.ToString("F3", _ic);
        }

        RebuildEdgeCombo();
        RebuildVertexCombo();
    }

    private void RebuildEdgeCombo()
    {
        cmbEdge.Items.Clear();
        for (int i = 0; i < _polygon.Count; i++)
        {
            int j = (i + 1) % _polygon.Count;
            cmbEdge.Items.Add($"Kante {i + 1}–{j + 1}");
        }
        if (cmbEdge.Items.Count > 0)
            cmbEdge.SelectedIndex = Math.Min(_selectedEdge, cmbEdge.Items.Count - 1);
    }

    private void RebuildVertexCombo()
    {
        cmbVertex.Items.Clear();
        for (int i = 0; i < _polygon.Count; i++)
            cmbVertex.Items.Add($"Eckpunkt {i + 1}  ({_polygon[i].R:F3} / {_polygon[i].H:F3})");
        if (cmbVertex.Items.Count > 0)
            cmbVertex.SelectedIndex = Math.Max(0, Math.Min(_selectedVertex, cmbVertex.Items.Count - 1));
    }

    // ── Polygon-DGV ──────────────────────────────────────────────────────────
    private void dgvPolygon_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        => RefreshFromGrid();

    private void dgvPolygon_UserDeletedRow(object? sender, DataGridViewRowEventArgs e)
        => RefreshFromGrid();

    private void RefreshFromGrid()
    {
        _polygon.Clear();
        foreach (DataGridViewRow row in dgvPolygon.Rows)
        {
            if (row.IsNewRow) continue;
            if (TryParseCell(row.Cells[0]) is double r &&
                TryParseCell(row.Cells[1]) is double h)
                _polygon.Add((r, h));
        }
        lblAGesamt.Text = _polygon.Count >= 3
            ? $"A_ges = {FlächenteilungRechner.BerechneFläche(_polygon):F2} m²"
            : "A_ges = –";
        RebuildEdgeCombo();
        RebuildVertexCombo();
        _ergebnis = null;
        ClearResults();
        RefreshMapPreview();
    }

    double? TryParseCell(DataGridViewCell cell) =>
        double.TryParse(cell.Value?.ToString(), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out double v) ||
        double.TryParse(cell.Value?.ToString(), NumberStyles.Float,
                        CultureInfo.CurrentCulture, out v) ? v : null;

    // ── DXF laden ─────────────────────────────────────────────────────────────
    private void btnLaden_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title  = "DXF-Punkte laden (CSV)",
            Filter = "CSV-Dateien (*.csv)|*.csv|Alle Dateien (*.*)|*.*"
        };
        if (ProjektManager.IstGeladen)
            dlg.InitialDirectory = ProjektManager.ProjektVerzeichnis;
        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            var lines = File.ReadAllLines(dlg.FileName);
            dgvPolygon.Rows.Clear();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split(new[]{';',','}, StringSplitOptions.TrimEntries);
                if (parts.Length < 3) continue;
                // Format: PunktNr, R, H[, Hoehe]
                if (double.TryParse(parts[1], NumberStyles.Float, _ic, out double r) &&
                    double.TryParse(parts[2], NumberStyles.Float, _ic, out double h))
                    dgvPolygon.Rows.Add(r.ToString("F3", _ic), h.ToString("F3", _ic));
            }
            RefreshFromGrid();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Laden:\n{ex.Message}", "Fehler",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // ── Berechnen ─────────────────────────────────────────────────────────────
    private void btnBerechnen_Click(object? sender, EventArgs e)
    {
        if (_polygon.Count < 3)
        {
            MessageBox.Show("Mindestens 3 Eckpunkte eingeben.", "Hinweis",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            int v = cmbVerfahren.SelectedIndex + 1;
            _ergebnis = v switch
            {
                1 => Verfahren1(),
                2 => Verfahren2(),
                3 => Verfahren3(),
                4 => Verfahren4(),
                5 => Verfahren5(),
                6 => Verfahren6(),
                _ => throw new InvalidOperationException("Unbekanntes Verfahren")
            };
            ShowResults();
            RefreshMapPreview();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Berechnungsfehler:\n{ex.Message}", "Fehler",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    FlächenteilungErgebnis Verfahren1()
    {
        double a1 = ParseRequired(txtASoll, "Soll-Fläche");
        int edge = Math.Max(0, cmbEdge.SelectedIndex);
        return FlächenteilungRechner.Verfahren1_Parallele(_polygon, edge, a1);
    }

    FlächenteilungErgebnis Verfahren2()
    {
        double a1 = ParseRequired(txtASoll, "Soll-Fläche");
        double rFix = ParseRequired(txtFixR, "R (fester Punkt)");
        double hFix = ParseRequired(txtFixH, "H (fester Punkt)");
        return FlächenteilungRechner.Verfahren2_FesterPunkt(_polygon, (rFix, hFix), a1);
    }

    FlächenteilungErgebnis Verfahren3()
    {
        double a1  = ParseRequired(txtASoll, "Soll-Fläche");
        double ric = ParseRequired(txtRichtung, "Richtung [gon]");
        return FlächenteilungRechner.Verfahren3_GegebeneRichtung(_polygon, ric, a1);
    }

    FlächenteilungErgebnis Verfahren4()
    {
        double a1 = ParseRequired(txtASoll, "Soll-Fläche");
        int vIdx = Math.Max(0, cmbVertex.SelectedIndex);
        return FlächenteilungRechner.Verfahren4_Dreieck(_polygon, vIdx, a1);
    }

    FlächenteilungErgebnis Verfahren5()
    {
        double a = ParseRequired(txtRatioA, "Verhältnis a");
        double b = ParseRequired(txtRatioB, "Verhältnis b");
        int basis = cmbRatioBasis.SelectedIndex + 1; // 1, 2, or 3
        int edge  = Math.Max(0, cmbEdge.SelectedIndex);
        double ric = 0;
        (double R, double H)? fixPt = null;
        if (basis == 3)
            ric = ParseRequired(txtRichtung, "Richtung [gon]");
        if (basis == 2 && double.TryParse(txtFixR.Text, NumberStyles.Float, _ic, out double rFix) &&
            double.TryParse(txtFixH.Text, NumberStyles.Float, _ic, out double hFix))
            fixPt = (rFix, hFix);
        return FlächenteilungRechner.Verfahren5_Verhältnis(_polygon, a, b, basis, edge, ric, fixPt);
    }

    FlächenteilungErgebnis Verfahren6()
    {
        double breite = ParseRequired(txtBreite, "Streifenbreite [m]");
        int edge = Math.Max(0, cmbEdge.SelectedIndex);
        return FlächenteilungRechner.Verfahren6_Streifen(_polygon, edge, breite);
    }

    double ParseRequired(TextBox txt, string name)
    {
        string s = txt.Text.Replace(',', '.');
        if (!double.TryParse(s, NumberStyles.Float, _ic, out double v))
            throw new FormatException($"Ungültiger Wert für {name}: '{txt.Text}'");
        return v;
    }

    // ── Ergebnisse anzeigen ───────────────────────────────────────────────────
    private void ShowResults()
    {
        if (_ergebnis == null) return;
        lblResA1soll.Text  = $"A1 (Soll): {_ergebnis.A1_soll:F2} m²";
        lblResA1ist.Text   = $"A1 (Ist):  {_ergebnis.A1_ist:F2} m²";
        lblResA2.Text      = $"A2:        {_ergebnis.A2:F2} m²";
        double d = _ergebnis.Differenz;
        lblResDiff.Text    = $"Δ:         {d:+0.0000;-0.0000;0.0000} m²";
        lblResDiff.ForeColor = Math.Abs(d) < 0.01 ? Color.DarkGreen : Color.DarkRed;

        // Grenzpunkte-Tabelle
        dgvGrenzpunkte.Rows.Clear();
        foreach (var (nr, r, h) in _ergebnis.NeueGrenzpunkte)
            dgvGrenzpunkte.Rows.Add(nr, r.ToString("F3", _ic), h.ToString("F3", _ic));

        btnAbsteckung.Enabled  = true;
        btnProtokoll.Enabled   = true;
    }

    private void ClearResults()
    {
        lblResA1soll.Text = "A1 (Soll): –";
        lblResA1ist.Text  = "A1 (Ist):  –";
        lblResA2.Text     = "A2:        –";
        lblResDiff.Text   = "Δ:         –";
        dgvGrenzpunkte.Rows.Clear();
        btnAbsteckung.Enabled = false;
        btnProtokoll.Enabled  = false;
    }

    // ── → Absteckung ──────────────────────────────────────────────────────────
    private void btnAbsteckung_Click(object? sender, EventArgs e)
    {
        if (_ergebnis == null || _ergebnis.NeueGrenzpunkte.Count == 0) return;

        var punkte = _ergebnis.NeueGrenzpunkte.Select(gp => new AbsteckPunkt
        {
            PunktNr = gp.Nr,
            R_soll  = gp.R,
            H_soll  = gp.H,
        }).ToList();

        var st = AbsteckungRechner.LadeStandpunkt();
        if (st != null)
        {
            foreach (var p in punkte)
                AbsteckungRechner.BerechnePolareAbsteckung(st, p);
        }

        using var form = new FormPunktabsteckung(punkte, st);
        form.ShowDialog(this);
    }

    // ── Protokoll ────────────────────────────────────────────────────────────
    private void btnProtokoll_Click(object? sender, EventArgs e)
    {
        if (_ergebnis == null) return;
        FlächenteilungProtokoll.Schreiben(_polygon, _ergebnis);
    }

    // ── Schließen ─────────────────────────────────────────────────────────────
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        mapPreview.SpeichereZoom();
        base.OnFormClosing(e);
    }

    private void btnSchliessen_Click(object? sender, EventArgs e) => Close();

    // ── Karten-Vorschau ────────────────────────────────────────────────────────
    private void RefreshMapPreview()
    {
        mapPreview.Canvas.OverlayEntities =
            AbsteckungGrafik.ErzeugeFlächenteilungOverlay(_polygon, _ergebnis);
        mapPreview.Canvas.Invalidate();
    }

    private void MapPreview_MouseClick(object? sender, MouseEventArgs e)
    {
        if (_polygon.Count == 0) return;
        int v = cmbVerfahren.SelectedIndex + 1;
        var (wr, wh) = mapPreview.Canvas.ToWorld(e.X, e.Y);

        if (v == 2) // fester Punkt
        {
            txtFixR.Text = wr.ToString("F3", _ic);
            txtFixH.Text = wh.ToString("F3", _ic);
            RefreshMapPreview();
            return;
        }

        // Nächsten Eckpunkt finden
        int nearest = 0;
        double minDist = double.MaxValue;
        for (int i = 0; i < _polygon.Count; i++)
        {
            double d = Math.Sqrt(Math.Pow(_polygon[i].R - wr, 2) + Math.Pow(_polygon[i].H - wh, 2));
            if (d < minDist) { minDist = d; nearest = i; }
        }

        if (v == 4) // Dreiecksabtrennung
        {
            _selectedVertex = nearest;
            cmbVertex.SelectedIndex = nearest;
            RefreshMapPreview();
        }
        else if (v is 1 or 5 or 6) // Grundkante
        {
            int nearestEdge = 0;
            double minEdgeDist = double.MaxValue;
            for (int i = 0; i < _polygon.Count; i++)
            {
                int j = (i + 1) % _polygon.Count;
                double d = PointToSegDist(wr, wh, _polygon[i].R, _polygon[i].H,
                                          _polygon[j].R, _polygon[j].H);
                if (d < minEdgeDist) { minEdgeDist = d; nearestEdge = i; }
            }
            _selectedEdge = nearestEdge;
            if (cmbEdge.Items.Count > nearestEdge)
                cmbEdge.SelectedIndex = nearestEdge;
            RefreshMapPreview();
        }
    }

    static double PointToSegDist(double px, double py, double ax, double ay, double bx, double by)
    {
        double dx = bx - ax, dy = by - ay;
        double len2 = dx * dx + dy * dy;
        if (len2 < 1e-12) return Math.Sqrt((px-ax)*(px-ax)+(py-ay)*(py-ay));
        double t = ((px - ax) * dx + (py - ay) * dy) / len2;
        t = Math.Max(0, Math.Min(1, t));
        return Math.Sqrt(Math.Pow(px - ax - t * dx, 2) + Math.Pow(py - ay - t * dy, 2));
    }

    // ── cmbEdge / cmbVertex SelectionChange ───────────────────────────────────
    private void cmbEdge_SelectedIndexChanged(object? sender, EventArgs e)
    {
        _selectedEdge = cmbEdge.SelectedIndex;
        RefreshMapPreview();
    }

    private void cmbVertex_SelectedIndexChanged(object? sender, EventArgs e)
    {
        _selectedVertex = cmbVertex.SelectedIndex;
        RefreshMapPreview();
    }
}
