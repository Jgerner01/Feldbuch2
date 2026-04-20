namespace Feldbuch;

using System.Globalization;

public partial class FormSchnurgeruest : Form
{
    private const int MAX_POLYGON = 20;
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    private StandpunktInfo? _station;
    private readonly List<(string? Nr, double R, double H)> _polygonPunkte = new();
    private bool _fixiert = false;
    private int  _selectedAxis = -1;
    private (double R, double H)? _gemessenerPunkt = null;
    private readonly List<SchnurgeruestMessungEintrag> _messungen = new();
    private (string? Nr, double R, double H) _letzterPunktPicked;

    private record SchnurgeruestMessungEintrag(
        double AchseR1, double AchseH1, double AchseR2, double AchseH2,
        double SgAbstand, double GemR, double GemH, double PerpDist);

    public FormSchnurgeruest()
    {
        InitializeComponent();
        LadeStation();
        InitGrid();
    }

    // ── Standpunkt ────────────────────────────────────────────────────────────
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

    // ── Grid initialisieren ───────────────────────────────────────────────────
    private void InitGrid()
    {
        dgvPolygon.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PunktNr", Name = "PunktNr", FillWeight = 25 });
        dgvPolygon.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "R [m]",   Name = "R",       FillWeight = 37 });
        dgvPolygon.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "H [m]",   Name = "H",       FillWeight = 38 });
        dgvPolygon.Rows.Add(MAX_POLYGON);

        dgvPolygon.CellEndEdit    += (_, __) => LesePunkteAusGrid();
        dgvPolygon.CellMouseClick += DgvPolygon_CellMouseClick;

        mapLageplan.Canvas.PointPicked      += OnCanvasKlick;
        mapLageplan.Canvas.MouseDoubleClick += OnCanvasDoppelKlick;
        mapLageplan.MessungEingetroffen     += OnMessungEingetroffen;

        txtAbstand.TextChanged += (_, __) => RefreshGrafik();
    }

    // ── Grid → _polygonPunkte ─────────────────────────────────────────────────
    private void LesePunkteAusGrid()
    {
        _polygonPunkte.Clear();
        foreach (DataGridViewRow row in dgvPolygon.Rows)
        {
            if (!TryParse(row.Cells["R"].Value, out double r) ||
                !TryParse(row.Cells["H"].Value, out double h)) continue;
            string? nr = row.Cells["PunktNr"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(nr)) nr = null;
            _polygonPunkte.Add((nr, r, h));
        }
        RefreshGrafik();
    }

    // ── Doppelklick auf Canvas → Polygon-Punkt hinzufügen ────────────────────
    private void OnCanvasDoppelKlick(object? s, MouseEventArgs e)
    {
        if (_fixiert) return;
        var (nr, r, h) = _letzterPunktPicked;
        AddPolygonPunkt(r, h, nr);
    }

    // ── Einfachklick auf Canvas → Achse wählen (wenn fixiert) ────────────────
    private void OnCanvasKlick(double x, double y, DxfEntity? entity, bool isClick)
    {
        // Cache für Doppelklick (DoubleClick feuert nach dem ersten MouseUp)
        _letzterPunktPicked = (mapLageplan.Canvas.PunktIndex?.GetPunktNr(x, y), x, y);

        if (!isClick || !_fixiert) return;

        if (entity is AbsteckSGKanteEntity kante)
        {
            _selectedAxis = kante.KanteIdx;
            AktualisiereAchsInfo();
            RefreshGrafik();
        }
    }

    // ── Polygon-Punkt einfügen (erste leere Zeile) ───────────────────────────
    private void AddPolygonPunkt(double r, double h, string? nr)
    {
        for (int i = 0; i < dgvPolygon.Rows.Count; i++)
        {
            var row = dgvPolygon.Rows[i];
            if (string.IsNullOrWhiteSpace(row.Cells["R"].Value?.ToString()) &&
                string.IsNullOrWhiteSpace(row.Cells["H"].Value?.ToString()))
            {
                row.Cells["PunktNr"].Value = nr ?? "";
                row.Cells["R"].Value       = r.ToString("F3", IC);
                row.Cells["H"].Value       = h.ToString("F3", IC);
                dgvPolygon.CurrentCell     = row.Cells["PunktNr"];
                LesePunkteAusGrid();
                return;
            }
        }
        MessageBox.Show("Keine freie Zeile mehr.", "Polygon",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ── Rechtsklick → Zeile löschen ──────────────────────────────────────────
    private void DgvPolygon_CellMouseClick(object? s, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right) return;
        if (e.RowIndex < 0 || e.ColumnIndex != 0) return;
        if (string.IsNullOrWhiteSpace(dgvPolygon.Rows[e.RowIndex].Cells["R"].Value?.ToString()) &&
            string.IsNullOrWhiteSpace(dgvPolygon.Rows[e.RowIndex].Cells["H"].Value?.ToString())) return;

        dgvPolygon.CurrentCell = dgvPolygon.Rows[e.RowIndex].Cells["PunktNr"];
        var menu = new ContextMenuStrip();
        menu.Items.Add($"Zeile {e.RowIndex + 1} löschen", null, (_, __) => ZeileLöschen(e.RowIndex));
        menu.Show(dgvPolygon, dgvPolygon.PointToClient(Cursor.Position));
    }

    private void ZeileLöschen(int rowIdx)
    {
        for (int i = rowIdx; i < dgvPolygon.Rows.Count - 1; i++)
        {
            var src = dgvPolygon.Rows[i + 1];
            var dst = dgvPolygon.Rows[i];
            foreach (DataGridViewColumn col in dgvPolygon.Columns)
                dst.Cells[col.Name].Value = src.Cells[col.Name].Value;
        }
        var last = dgvPolygon.Rows[dgvPolygon.Rows.Count - 1];
        foreach (DataGridViewColumn col in dgvPolygon.Columns)
            last.Cells[col.Name].Value = null;

        LesePunkteAusGrid();
    }

    // ── Fixieren / Lösen ──────────────────────────────────────────────────────
    private void btnFixieren_Click(object? s, EventArgs e)
    {
        LesePunkteAusGrid();

        if (!_fixiert)
        {
            if (_polygonPunkte.Count < 2)
            {
                MessageBox.Show("Mindestens 2 Polygon-Punkte erforderlich.", "Schnurgerüst",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_polygonPunkte.Count >= 3) SortiereUhrzeigersinn();

            _fixiert           = true;
            dgvPolygon.ReadOnly = true;
            btnFixieren.Text      = "Lösen";
            btnFixieren.BackColor = Color.FromArgb(140, 50, 50);
            FuelleGridFromList();
        }
        else
        {
            _fixiert              = false;
            _selectedAxis         = -1;
            _gemessenerPunkt      = null;
            dgvPolygon.ReadOnly   = false;
            btnFixieren.Text      = "Fixieren";
            btnFixieren.BackColor = Color.FromArgb(60, 120, 60);
            AktualisiereAchsInfo();
        }

        RefreshGrafik();
    }

    private void SortiereUhrzeigersinn()
    {
        int    n    = _polygonPunkte.Count;
        double area = 0;
        for (int i = 0; i < n; i++)
        {
            var (_, r1, h1) = _polygonPunkte[i];
            var (_, r2, h2) = _polygonPunkte[(i + 1) % n];
            area += r1 * h2 - r2 * h1;
        }
        if (area > 0) _polygonPunkte.Reverse(); // CCW → drehe um → CW
    }

    private void FuelleGridFromList()
    {
        foreach (DataGridViewRow row in dgvPolygon.Rows)
            foreach (DataGridViewColumn col in dgvPolygon.Columns)
                row.Cells[col.Name].Value = null;

        for (int i = 0; i < Math.Min(_polygonPunkte.Count, MAX_POLYGON); i++)
        {
            var (nr, r, h) = _polygonPunkte[i];
            dgvPolygon.Rows[i].Cells["PunktNr"].Value = nr ?? "";
            dgvPolygon.Rows[i].Cells["R"].Value       = r.ToString("F3", IC);
            dgvPolygon.Rows[i].Cells["H"].Value       = h.ToString("F3", IC);
        }
    }

    // ── AP aus DXF laden ──────────────────────────────────────────────────────
    private void btnLaden_Click(object? s, EventArgs e)
    {
        string pfad = FormDxfViewer.AnschlusspunktePfad;
        if (!File.Exists(pfad))
        {
            MessageBox.Show("Keine Punkte verfügbar.", "Hinweis",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var lines = File.ReadAllLines(pfad).Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

        foreach (DataGridViewRow row in dgvPolygon.Rows)
            foreach (DataGridViewCell c in row.Cells) c.Value = null;

        for (int i = 0; i < Math.Min(lines.Count, MAX_POLYGON); i++)
        {
            var parts = lines[i].Split(',');
            if (parts.Length < 3) continue;
            dgvPolygon.Rows[i].Cells["PunktNr"].Value = parts[0].Trim();
            dgvPolygon.Rows[i].Cells["R"].Value       = parts[1].Trim();
            dgvPolygon.Rows[i].Cells["H"].Value       = parts[2].Trim();
        }
        LesePunkteAusGrid();
    }

    // ── Tachymeter-Messung ────────────────────────────────────────────────────
    private void OnMessungEingetroffen(TachymeterMessung m)
    {
        if (_station == null) return;
        double? hz = m.Hz_gon;
        double? s  = m.Horizontalstrecke_m;
        if (!hz.HasValue || !s.HasValue) return;

        double richtung_rad = (_station.Orientierung_gon + hz.Value) * Math.PI / 200.0;
        double gemR = _station.R + s.Value * Math.Sin(richtung_rad);
        double gemH = _station.H + s.Value * Math.Cos(richtung_rad);

        _gemessenerPunkt = (gemR, gemH);
        AktualisiereAchsInfo();
        RefreshGrafik();
    }

    // ── Messung bestätigen ────────────────────────────────────────────────────
    private void btnMessungBestaetigen_Click(object? s, EventArgs e)
    {
        if (!_gemessenerPunkt.HasValue)
        {
            MessageBox.Show("Kein gemessener Punkt vorhanden.", "Hinweis",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (_selectedAxis < 0 || _polygonPunkte.Count < 2)
        {
            MessageBox.Show("Bitte zuerst eine Gebäudeseite als Achse auswählen.", "Hinweis",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int j         = (_selectedAxis + 1) % _polygonPunkte.Count;
        var (_, r1, h1) = _polygonPunkte[_selectedAxis];
        var (_, r2, h2) = _polygonPunkte[j];
        TryParse(txtAbstand.Text, out double abstand);
        double perp = PerpDistOutward(_gemessenerPunkt.Value.R, _gemessenerPunkt.Value.H,
            r1, h1, r2, h2);

        _messungen.Add(new SchnurgeruestMessungEintrag(
            r1, h1, r2, h2, abstand,
            _gemessenerPunkt.Value.R, _gemessenerPunkt.Value.H, perp));

        MessageBox.Show(
            $"Messung #{_messungen.Count} gespeichert.\n" +
            $"Abstand zur Gebäudeseite: {perp:F3} m  (Soll: {abstand:F3} m)",
            "Messung bestätigt", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ── Achsinfo-Label aktualisieren ──────────────────────────────────────────
    private void AktualisiereAchsInfo()
    {
        if (!_fixiert || _selectedAxis < 0 || _polygonPunkte.Count < 2)
        {
            lblAchsInfo.Text = "Achse: – (fixieren, dann Gebäudeseite anklicken)";
            return;
        }

        int j           = (_selectedAxis + 1) % _polygonPunkte.Count;
        var (nr1, r1, h1) = _polygonPunkte[_selectedAxis];
        var (nr2, r2, h2) = _polygonPunkte[j];

        string a1 = nr1 ?? $"({r1:F2}/{h1:F2})";
        string a2 = nr2 ?? $"({r2:F2}/{h2:F2})";

        TryParse(txtAbstand.Text, out double abstand);

        string distTxt = "–";
        if (_gemessenerPunkt.HasValue)
        {
            double d    = PerpDistOutward(_gemessenerPunkt.Value.R, _gemessenerPunkt.Value.H,
                r1, h1, r2, h2);
            double abw  = d - abstand;
            string seite = d >= 0 ? "außen" : "innen";
            distTxt = $"Ist: {d:F3} m ({seite})   Soll: {abstand:F3} m   Abw.: {abw:+0.000;-0.000;0.000} m";
        }

        lblAchsInfo.Text = $"Achse: {a1} → {a2}    |    {distTxt}";
    }

    // ── Senkrechter Abstand (positiv = außen für UZS-Polygon) ────────────────
    private static double PerpDistOutward(double pR, double pH,
        double aR, double aH, double bR, double bH)
    {
        double dR  = bR - aR, dH = bH - aH;
        double len = Math.Sqrt(dR * dR + dH * dH);
        if (len < 1e-10) return 0;
        // Vorzeichen-Abstand (links von A→B = positiv)
        double d = (dR * (pH - aH) - dH * (pR - aR)) / len;
        return -d; // Außenseite (rechts für UZS) = positiv
    }

    // ── Grafik ────────────────────────────────────────────────────────────────
    private void RefreshGrafik()
    {
        mapLageplan.Canvas.OverlayEntities =
            AbsteckungGrafik.ErzeugeSchnurgeruestV2Overlay(
                _station,
                _polygonPunkte,
                ParseAbstand(),
                _fixiert,
                _selectedAxis,
                _gemessenerPunkt);
        mapLageplan.Canvas.Invalidate();
    }

    private double ParseAbstand()
    {
        TryParse(txtAbstand.Text, out double a);
        return a;
    }

    // ── Protokoll ─────────────────────────────────────────────────────────────
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        mapLageplan.SpeichereZoom();
        base.OnFormClosing(e);
        if (_polygonPunkte.Count >= 3)
        {
            var polygon  = _polygonPunkte.Select(p => (p.R, p.H)).ToList();
            TryParse(txtAbstand.Text, out double abstand);
            var sgPunkte = AbsteckungRechner.BerechneSchnurgeruest(polygon, abstand, _station);
            SchnurgeruestProtokoll.Schreiben(_station, polygon, sgPunkte, txtAbstand.Text);
        }
    }

    private void btnSchliessen_Click(object? s, EventArgs e) => Close();

    private static bool TryParse(object? val, out double r) =>
        double.TryParse(val?.ToString()?.Replace(',', '.'), NumberStyles.Any, IC, out r);
    private static bool TryParse(string? s, out double r) =>
        double.TryParse(s?.Replace(',', '.'), NumberStyles.Any, IC, out r);
}
