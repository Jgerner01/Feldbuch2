namespace Feldbuch;

using System.Globalization;

// ─────────────────────────────────────────────────────────────────────────────
// FormKoordTransformation
//
// Links: Ausgangssystem (Quelle)   Rechts: Zielsystem (Soll)
// Spalten beider Grids: PunktNr | X (R) | Y (H) | Z (Höhe)
// Zuordnung: Punkte müssen gleiche PunktNr haben ODER gleiche Zeilenreihenfolge.
// ─────────────────────────────────────────────────────────────────────────────
public partial class FormKoordTransformation : Form
{
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;
    private static readonly string[]    COLS = { "PunktNr", "X (R) [m]", "Y (H) [m]", "Z (Höhe) [m]" };

    private TransformationsErgebnis? _letzteBerechnung;
    private List<TransformPunkt>?    _quelleZuletzt;
    private List<TransformPunkt>?    _zielZuletzt;
    private bool                     _initializingResiduen = false;

    // Identische Punkte (nach Berechnung/Transformieren bekannt)
    private HashSet<string> _identischePunktNr = new(StringComparer.OrdinalIgnoreCase);
    // Vollständige Export-Liste (nach "Transformieren →" gesetzt)
    private List<TransformPunkt>? _transformierteVollstaendig = null;
    // Alle Quellpunkte (für auto-Transformation nach Berechnung)
    private List<TransformPunkt>? _quelleAlle = null;
    // True, wenn dgvZiel durch TransformierAlle überschrieben wurde (kein frisches Input mehr)
    private bool _zielIstResultat = false;

    // Ampel-Schwellwerte [cm]
    private const double AMPEL_GRUEN   = 1.0;
    private const double AMPEL_GELB    = 3.0;
    private const double AMPEL_TIEFROT = 9.0;   // 3 × AMPEL_GELB

    public FormKoordTransformation()
    {
        InitializeComponent();
        InitGrids();

        // Gespeicherte Einstellungen laden
        chkFesterMassstab.Checked = ProjektManager.TransformFesterMassstab;
        cmbTyp.SelectedIndex = Math.Clamp(ProjektManager.TransformTypIndex, 0, cmbTyp.Items.Count - 1);

        // Einstellungen bei Änderung sofort sichern
        chkFesterMassstab.CheckedChanged += (s, e) =>
        {
            ProjektManager.TransformFesterMassstab = chkFesterMassstab.Checked;
            ProjektManager.SpeichereOptionen();
            if (_letzteBerechnung != null) Neuberechnen();
        };
        cmbTyp.SelectedIndexChanged += (s, e) =>
        {
            ProjektManager.TransformTypIndex = cmbTyp.SelectedIndex;
            ProjektManager.SpeichereOptionen();
            // Automatisch neuberechnen, wenn dgvZiel bereits mit Ergebnissen gefüllt ist
            // (gespeicherte Passpunkte werden verwendet, nicht das überschriebene dgvZiel)
            if (_zielIstResultat && _letzteBerechnung != null) Neuberechnen();
        };

        // Wenn der Nutzer dgvZiel manuell bearbeitet, gilt es wieder als frisches Input
        dgvZiel.CellEndEdit += (s, e) => _zielIstResultat = false;

        // Checkbox-Änderung sofort committen und Neuberechnung auslösen
        dgvResiduen.CurrentCellDirtyStateChanged += (s, e) =>
        {
            if (dgvResiduen.IsCurrentCellDirty &&
                dgvResiduen.CurrentCell?.OwningColumn?.Name is "colAktiv" or "colAktivHoehe")
                dgvResiduen.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        dgvResiduen.CellValueChanged += (s, e) =>
        {
            if (!_initializingResiduen && e.ColumnIndex >= 0 &&
                dgvResiduen.Columns[e.ColumnIndex].Name is "colAktiv" or "colAktivHoehe")
                OnAktivierungGeaendert();
        };
    }

    // ── Grid-Initialisierung ──────────────────────────────────────────────────
    private void InitGrids()
    {
        foreach (string c in COLS)
        {
            dgvQuelle.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = c, Name = c });
            dgvZiel  .Columns.Add(new DataGridViewTextBoxColumn { HeaderText = c, Name = c });
        }
        // Residuen-Grid wird nach Berechnung aufgebaut
    }

    // ── Import KOR ────────────────────────────────────────────────────────────
    private void ImportKor(bool isQuelle)
    {
        using var dlg = new OpenFileDialog
        {
            Title  = "KOR-Datei importieren",
            Filter = "KOR / KOR-CSV|*.kor;*-kor.csv|Alle Dateien|*.*"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            string ext = Path.GetExtension(dlg.FileName).ToLowerInvariant();
            bool   isCsv = ext == ".csv" ||
                           dlg.FileName.EndsWith("-kor.csv", StringComparison.OrdinalIgnoreCase);
            var punkte = isCsv
                ? ImportPunkteManager.LeseCsv(dlg.FileName)
                : ImportPunkteManager.LeseKor(dlg.FileName);
            FuellGrid(isQuelle ? dgvQuelle : dgvZiel, punkte);
            if (!isQuelle) _zielIstResultat = false;   // Zielsystem neu geladen → kein Resultat mehr
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler:\n{ex.Message}", "Import", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── Import CSV ────────────────────────────────────────────────────────────
    private void ImportCsv(bool isQuelle)
    {
        using var dlg = new OpenFileDialog
        {
            Title  = "CSV-Datei importieren",
            Filter = "CSV-Datei|*.csv|Alle Dateien|*.*"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var punkte = ImportPunkteManager.LeseCsv(dlg.FileName);
            FuellGrid(isQuelle ? dgvQuelle : dgvZiel, punkte);
            if (!isQuelle) _zielIstResultat = false;   // Zielsystem neu geladen → kein Resultat mehr
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler:\n{ex.Message}", "Import", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── Grid füllen ───────────────────────────────────────────────────────────
    private static void FuellGrid(DataGridView dgv, List<ImportPunkt> punkte)
    {
        dgv.Rows.Clear();
        foreach (var p in punkte)
        {
            int r = dgv.Rows.Add();
            dgv.Rows[r].Cells[0].Value = p.PunktNr;
            dgv.Rows[r].Cells[1].Value = p.R.ToString("F3", IC);
            dgv.Rows[r].Cells[2].Value = p.H.ToString("F3", IC);
            dgv.Rows[r].Cells[3].Value = p.Hoehe != 0 ? p.Hoehe.ToString("F3", IC) : "";
        }
    }

    // ── Grid leeren ───────────────────────────────────────────────────────────
    private static void DgvLeeren(DataGridView dgv) => dgv.Rows.Clear();

    // ── Automatische Zuordnung nach PunktNr ───────────────────────────────────
    // Sortiert nur das Zielsystem so, dass die Reihenfolge der Passpunkte mit
    // der Quelle übereinstimmt. Das Ausgangssystem (dgvQuelle) bleibt vollständig
    // erhalten, damit "Berechnen" anschließend ALLE Quellpunkte transformieren kann.
    private void btnPunkteZuordnen_Click(object? sender, EventArgs e)
    {
        var q = LesePunkte(dgvQuelle);
        var z = LesePunkte(dgvZiel);
        if (q.Count == 0 || z.Count == 0)
        {
            MessageBox.Show("Beide Listen müssen Punkte enthalten.",
                "Zuordnung", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Ziel nach PunktNr-Reihenfolge der Quelle sortieren – nur Passpunkte
        var zDict  = z.GroupBy(p => p.PunktNr, StringComparer.OrdinalIgnoreCase)
                      .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var zNeu   = new List<TransformPunkt>();
        var gefunden   = new List<string>();
        var nichtGefunden = new List<string>();

        foreach (var qp in q)
        {
            if (zDict.TryGetValue(qp.PunktNr, out var zp))
            {
                zNeu.Add(zp);
                gefunden.Add(qp.PunktNr);
            }
            else
                nichtGefunden.Add(qp.PunktNr);
        }

        if (zNeu.Count == 0)
        {
            MessageBox.Show("Keine übereinstimmenden Punktnummern gefunden.",
                "Zuordnung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Nur dgvZiel aktualisieren – dgvQuelle bleibt vollständig erhalten!
        PunkteInGrid(dgvZiel, zNeu);
        _zielIstResultat = false;   // Zielsystem wurde manuell neu sortiert → kein Resultat mehr

        string msg = $"{zNeu.Count} Passpunkte im Zielsystem zugeordnet.\n" +
                     $"Ausgangssystem ({q.Count} Punkte) unverändert.";
        if (nichtGefunden.Count > 0)
            msg += $"\nNicht im Zielsystem gefunden: {string.Join(", ", nichtGefunden)}";
        MessageBox.Show(msg, "Zuordnung", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void PunkteInGrid(DataGridView dgv, List<TransformPunkt> punkte)
    {
        dgv.Rows.Clear();
        foreach (var p in punkte)
        {
            int r = dgv.Rows.Add();
            dgv.Rows[r].Cells[0].Value = p.PunktNr;
            dgv.Rows[r].Cells[1].Value = p.X.ToString("F3", IC);
            dgv.Rows[r].Cells[2].Value = p.Y.ToString("F3", IC);
            dgv.Rows[r].Cells[3].Value = p.Z != 0 ? p.Z.ToString("F3", IC) : "";
        }
    }

    // ── Berechnen ─────────────────────────────────────────────────────────────
    private void btnBerechnen_Click(object? sender, EventArgs e)
    {
        // dgvZiel wurde durch TransformierAlle überschrieben → enthält keine
        // Original-Passpunkt-Referenzdaten mehr. Gespeicherte Passpunkte verwenden.
        if (_zielIstResultat && _quelleZuletzt != null && _zielZuletzt != null)
        {
            Neuberechnen();
            return;
        }

        var alleSrc = LesePunkte(dgvQuelle);
        var alleTgt = LesePunkte(dgvZiel);

        if (alleSrc.Count == 0 || alleTgt.Count == 0)
        {
            MessageBox.Show("Bitte Punkte in beiden Listen eingeben.",
                "Berechnen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Auto-Zuordnung nach PunktNr (Passpunkte)
        var tgtDict = alleTgt.GroupBy(p => p.PunktNr, StringComparer.OrdinalIgnoreCase)
                             .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var quelle  = alleSrc.Where(p => tgtDict.ContainsKey(p.PunktNr)).ToList();
        var ziel    = quelle .Select(p => tgtDict[p.PunktNr]).ToList();

        if (quelle.Count == 0)
        {
            MessageBox.Show(
                "Keine übereinstimmenden Punktnummern gefunden.\n" +
                "Bitte sicherstellen, dass Passpunkte in beiden Listen gleiche Nummern haben.",
                "Berechnen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var  typ            = (TransformationsTyp)cmbTyp.SelectedIndex;
        bool festerMassstab = chkFesterMassstab.Checked;
        TransformationsErgebnis erg;
        try
        {
            erg = KoordTransformationsRechner.Berechnen(typ, quelle, ziel, festerMassstab);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler bei der Berechnung:\n{ex.Message}",
                "Berechnen", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!string.IsNullOrEmpty(erg.FehlerMeldung))
        {
            MessageBox.Show(erg.FehlerMeldung, "Berechnen",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _letzteBerechnung = erg;
        _quelleZuletzt    = quelle;
        _zielZuletzt      = ziel;
        _quelleAlle       = alleSrc;
        _identischePunktNr = new HashSet<string>(
            quelle.Select(p => p.PunktNr), StringComparer.OrdinalIgnoreCase);

        ZeigeErgebnis(erg, typ);
        btnProtokoll.Enabled      = true;
        btnTransformieren.Enabled = true;

        // Automatisch alle Quellpunkte transformieren
        TransformierAlle(alleSrc);
    }

    // ── Residuen-Grid Spalten aufbauen ────────────────────────────────────────
    private void BaueResiduenSpalten(bool ist3D)
    {
        dgvResiduen.Columns.Clear();
        dgvResiduen.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name="colAktiv", HeaderText="Aktiv",
            ToolTipText="Punkt in Ausgleichung aktiv",
            Width=44, AutoSizeMode=DataGridViewAutoSizeColumnMode.None,
            TrueValue=true, FalseValue=false, ReadOnly=false
        });
        if (ist3D)
            dgvResiduen.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name="colAktivHoehe", HeaderText="Z",
                ToolTipText="Höhe in Ausgleichung berücksichtigen",
                Width=34, AutoSizeMode=DataGridViewAutoSizeColumnMode.None,
                TrueValue=true, FalseValue=false, ReadOnly=false
            });
        string[] heads = ist3D
            ? new[] { "PunktNr", "vX [cm]", "vY [cm]", "vZ [cm]", "vGes. [cm]" }
            : new[] { "PunktNr", "vX [cm]", "vY [cm]", "vGes. [cm]" };
        string[] names = ist3D
            ? new[] { "colPunktNr", "colVX", "colVY", "colVZ", "colVGes" }
            : new[] { "colPunktNr", "colVX", "colVY", "colVGes" };
        for (int i = 0; i < heads.Length; i++)
            dgvResiduen.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = heads[i], Name = names[i], ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = heads[i].StartsWith("P")
                        ? DataGridViewContentAlignment.MiddleLeft
                        : DataGridViewContentAlignment.MiddleRight
                }
            });
    }

    // ── Ergebnis anzeigen ─────────────────────────────────────────────────────
    private void ZeigeErgebnis(TransformationsErgebnis erg, TransformationsTyp typ)
    {
        _initializingResiduen = true;
        try
        {
            lblErgebnisInfo.Text = BaueInfoText(erg, typ);

            bool ist3D = typ != TransformationsTyp.Helmert2D;
            BaueResiduenSpalten(ist3D);

            dgvResiduen.Rows.Clear();
            foreach (var r in erg.Residuen)
            {
                int ri = dgvResiduen.Rows.Add();
                var row = dgvResiduen.Rows[ri];
                row.Cells["colAktiv"]  .Value = true;
                if (ist3D) row.Cells["colAktivHoehe"].Value = r.HoeheAktiv;
                row.Cells["colPunktNr"].Value = r.PunktNr;
                row.Cells["colVX"]     .Value = $"{r.vX_mm/10.0:+0.0;-0.0}";
                row.Cells["colVY"]     .Value = $"{r.vY_mm/10.0:+0.0;-0.0}";
                if (ist3D)
                {
                    row.Cells["colVZ"]  .Value = r.HoeheAktiv
                        ? $"{r.vZ_mm/10.0:+0.0;-0.0}" : "–";
                    row.Cells["colVGes"].Value = $"{r.vGesamt_mm/10.0:F1}";
                }
                else
                    row.Cells["colVGes"].Value = $"{r.vGesamt_mm/10.0:F1}";

                SetResiduenAmpel(row, r, ist3D);
            }

            if (pnlErgebnis.Height == 0)
                pnlErgebnis.Height = 240;
        }
        finally
        {
            _initializingResiduen = false;
        }
    }

    // ── Aktivierungsänderung ──────────────────────────────────────────────────
    private void OnAktivierungGeaendert() => Neuberechnen();

    private void Neuberechnen()
    {
        if (_quelleZuletzt == null || _zielZuletzt == null) return;

        var  typ   = (TransformationsTyp)cmbTyp.SelectedIndex;
        bool ist3D = typ != TransformationsTyp.Helmert2D;

        // ── Aktuelle Aktivierungszustände aus Grid sichern (Index = Zeilenindex) ──
        bool hatAktivHoehe = dgvResiduen.Columns.Contains("colAktivHoehe");
        bool gridIst3D     = dgvResiduen.Columns.Contains("colVZ");
        bool strukturPasst = (ist3D == gridIst3D);

        var aktivBits = new List<bool>();
        var hoeheBits = new List<bool>();

        for (int i = 0; i < _quelleZuletzt.Count; i++)
        {
            if (i < dgvResiduen.Rows.Count)
            {
                aktivBits.Add(dgvResiduen.Rows[i].Cells["colAktiv"].Value is true);
                hoeheBits.Add(!hatAktivHoehe ||
                    dgvResiduen.Rows[i].Cells["colAktivHoehe"].Value is not false);
            }
            else
            {
                aktivBits.Add(true);   // neue Punkte standardmäßig aktiv
                hoeheBits.Add(true);
            }
        }

        // ── Aktive Punkte für Berechnung zusammenstellen ──────────────────────
        var aktiveQuelle = new List<TransformPunkt>();
        var aktiveZiel   = new List<TransformPunkt>();

        for (int i = 0; i < _quelleZuletzt.Count; i++)
        {
            if (!aktivBits[i]) continue;
            bool useZ = ist3D && hoeheBits[i];
            var qp = _quelleZuletzt[i];
            aktiveQuelle.Add(new TransformPunkt
                { PunktNr=qp.PunktNr, X=qp.X, Y=qp.Y, Z=qp.Z, UseZ=useZ });
            aktiveZiel.Add(_zielZuletzt[i]);
        }

        if (aktiveQuelle.Count == 0)
        {
            lblErgebnisInfo.Text = "Keine aktiven Punkte für Berechnung.";
            return;
        }

        TransformationsErgebnis erg;
        bool festerMassstab = chkFesterMassstab.Checked;
        try { erg = KoordTransformationsRechner.Berechnen(typ, aktiveQuelle, aktiveZiel, festerMassstab); }
        catch (Exception ex) { lblErgebnisInfo.Text = $"Fehler: {ex.Message}"; return; }

        if (!string.IsNullOrEmpty(erg.FehlerMeldung))
        {
            lblErgebnisInfo.Text = erg.FehlerMeldung;
            return;
        }

        _letzteBerechnung = erg;
        lblErgebnisInfo.Text = BaueInfoText(erg, typ);

        _initializingResiduen = true;
        try
        {
            // ── Grid-Struktur bei 2D ↔ 3D Wechsel komplett neu aufbauen ──────────
            if (!strukturPasst)
            {
                BaueResiduenSpalten(ist3D);
                dgvResiduen.Rows.Clear();
            }

            // ── Zeilen befüllen / aktualisieren ──────────────────────────────────
            int ergIdx = 0;
            for (int i = 0; i < _quelleZuletzt.Count; i++)
            {
                DataGridViewRow row;
                if (!strukturPasst)
                {
                    int ri = dgvResiduen.Rows.Add();
                    row = dgvResiduen.Rows[ri];
                    row.Cells["colPunktNr"].Value = _quelleZuletzt[i].PunktNr;
                }
                else
                {
                    if (i >= dgvResiduen.Rows.Count) break;
                    row = dgvResiduen.Rows[i];
                }

                // Checkbox-Zustände setzen (wiederherstellen bei Strukturwechsel)
                row.Cells["colAktiv"].Value = aktivBits[i];
                if (ist3D && dgvResiduen.Columns.Contains("colAktivHoehe"))
                    row.Cells["colAktivHoehe"].Value = hoeheBits[i];

                if (aktivBits[i] && ergIdx < erg.Residuen.Count)
                {
                    var r = erg.Residuen[ergIdx++];
                    row.Cells["colVX"].Value = $"{r.vX_mm/10.0:+0.0;-0.0}";
                    row.Cells["colVY"].Value = $"{r.vY_mm/10.0:+0.0;-0.0}";
                    if (ist3D)
                    {
                        row.Cells["colVZ"]  .Value = r.HoeheAktiv
                            ? $"{r.vZ_mm/10.0:+0.0;-0.0}" : "–";
                        row.Cells["colVGes"].Value = $"{r.vGesamt_mm/10.0:F1}";
                    }
                    else
                        row.Cells["colVGes"].Value = $"{r.vGesamt_mm/10.0:F1}";
                    SetResiduenAmpel(row, r, ist3D);
                }
                else
                {
                    row.Cells["colVX"].Value = "–";
                    row.Cells["colVY"].Value = "–";
                    if (ist3D) { row.Cells["colVZ"].Value = "–"; row.Cells["colVGes"].Value = "–"; }
                    else       { row.Cells["colVGes"].Value = "–"; }
                    SetZeilenFarbe(row, Color.FromArgb(228, 228, 228), Color.Gray);
                }
            }
        }
        finally { _initializingResiduen = false; }

        btnProtokoll.Enabled = true;
        btnExportKor.Enabled = erg.TransformiertePunkte.Count > 0;

        if (_quelleAlle != null)
            TransformierAlle(_quelleAlle);
    }

    // ── Info-Text ─────────────────────────────────────────────────────────────
    private static string BaueInfoText(TransformationsErgebnis erg, TransformationsTyp typ)
    {
        var p = erg.Parameter;
        string info;
        if (typ == TransformationsTyp.Helmert2D)
            info = $"  dx = {p.Dx:+0.000;-0.000} m    dy = {p.Dy:+0.000;-0.000} m" +
                   $"    α = {p.Alpha_gon:F5} gon    m = {p.Massstab2D:F8}";
        else if (typ == TransformationsTyp.Parameter9)
            info = $"  dx = {p.Dx:+0.000;-0.000} m    dy = {p.Dy:+0.000;-0.000} m" +
                   $"    dz = {p.Dz:+0.000;-0.000} m\n" +
                   $"  mx = {p.Mx*1e6:+0.00;-0.00} ppm    my = {p.My*1e6:+0.00;-0.00} ppm" +
                   $"    mz = {p.Mz*1e6:+0.00;-0.00} ppm";
        else
            info = $"  dx = {p.Dx:+0.000;-0.000} m    dy = {p.Dy:+0.000;-0.000} m" +
                   $"    dz = {p.Dz:+0.000;-0.000} m\n" +
                   $"  m  = {p.M*1e6:+0.00;-0.00} ppm";
        string s0Str = erg.Redundanz > 0
            ? $"   |   s₀ = {erg.S0_mm:F2} mm   (r = {erg.Redundanz})"
            : "   |   eindeutig bestimmt (r = 0)";
        return info + s0Str;
    }

    // ── Protokoll ─────────────────────────────────────────────────────────────
    private void btnProtokoll_Click(object? sender, EventArgs e)
    {
        if (_letzteBerechnung == null || _quelleZuletzt == null || _zielZuletzt == null)
            return;
        KoordTransformationsProtokoll.Schreiben(
            _letzteBerechnung,
            _quelleZuletzt,
            _zielZuletzt,
            _transformierteVollstaendig ?? _letzteBerechnung.TransformiertePunkte);
    }

    // ── Transformieren → (alle Quellpunkte ins Zielsystem übertragen) ────────
    private void btnTransformieren_Click(object? sender, EventArgs e)
    {
        if (_letzteBerechnung == null)
        {
            MessageBox.Show("Bitte zuerst berechnen.", "Transformieren",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        TransformierAlle(_quelleAlle ?? LesePunkte(dgvQuelle));
    }

    private void TransformierAlle(List<TransformPunkt> alleSrc)
    {
        if (_letzteBerechnung == null || alleSrc.Count == 0) return;

        var typ = _letzteBerechnung.Typ;
        var par = _letzteBerechnung.Parameter;

        // ── Ausgangssystem einfärben ──────────────────────────────────────────
        foreach (DataGridViewRow row in dgvQuelle.Rows)
        {
            if (row.IsNewRow) continue;
            string? nr = row.Cells[0].Value?.ToString()?.Trim();
            if (string.IsNullOrEmpty(nr)) continue;
            SetZeilenFarbe(row, _identischePunktNr.Contains(nr)
                ? Color.FromArgb(255, 210, 210)    // rot  = Passpunkt
                : Color.FromArgb(200, 240, 200));   // grün = Neupunkt
        }

        // ── Zielsystem komplett ersetzen ──────────────────────────────────────
        dgvZiel.Rows.Clear();
        var exportListe   = new List<TransformPunkt>();
        int anzIdentisch  = 0, anzNeu = 0;

        foreach (var qp in alleSrc)
        {
            var tp = KoordTransformationsRechner.Transformiere(typ, par, qp);
            exportListe.Add(tp);

            int ri  = dgvZiel.Rows.Add();
            var row = dgvZiel.Rows[ri];
            row.Cells[0].Value = tp.PunktNr;
            row.Cells[1].Value = tp.X.ToString("F3", IC);
            row.Cells[2].Value = tp.Y.ToString("F3", IC);
            row.Cells[3].Value = tp.Z != 0 ? tp.Z.ToString("F3", IC) : "";

            bool istIdentisch = _identischePunktNr.Contains(qp.PunktNr);
            SetZeilenFarbe(row, istIdentisch
                ? Color.FromArgb(255, 210, 210)    // rot  = Passpunkt
                : Color.FromArgb(200, 240, 200));   // grün = transformierter Neupunkt
            if (istIdentisch) anzIdentisch++; else anzNeu++;
        }

        _transformierteVollstaendig = exportListe;
        btnExportKor.Enabled  = true;
        _zielIstResultat      = true;   // dgvZiel enthält jetzt Transformationsergebnisse

        ProtokollManager.Log("TRANSF",
            $"{KoordTransformationsRechner.TypName(typ)}: " +
            $"{anzIdentisch} Passpunkte (rot), {anzNeu} Neupunkte transformiert (grün)");
    }

    // ── Export KOR ────────────────────────────────────────────────────────────
    private void btnExportKor_Click(object? sender, EventArgs e)
    {
        if (_letzteBerechnung == null) return;

        // Nach "Transformieren →" vollständige Liste verwenden, sonst nur Rechenergebnis
        var exportPunkte = _transformierteVollstaendig
            ?? _letzteBerechnung.TransformiertePunkte;
        if (exportPunkte.Count == 0) return;

        using var dlg = new SaveFileDialog
        {
            Title      = "Transformierte Punkte speichern",
            Filter     = "KOR-Datei|*.kor|Alle Dateien|*.*",
            DefaultExt = "kor",
            InitialDirectory = ProjektManager.IstGeladen
                ? ProjektManager.ProjektVerzeichnis : "",
            FileName   = "Transformiert"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"% Koordinatentransformation  {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"% Typ: {KoordTransformationsRechner.TypName(_letzteBerechnung.Typ)}");
            foreach (var p in exportPunkte)
            {
                // Identische Passpunkte erhalten Code "T"
                string code = _identischePunktNr.Contains(p.PunktNr) ? "T" : "";
                string x = p.X.ToString("F3", IC);
                string y = p.Y.ToString("F3", IC);
                string z = p.Z.ToString("F3", IC);
                sb.AppendLine($"{p.PunktNr,-8}{x,16}{y,16}{z,10}    {code}");
            }
            File.WriteAllText(dlg.FileName, sb.ToString(), System.Text.Encoding.UTF8);

            // Begleit-CSV
            string csvPfad = Path.Combine(
                Path.GetDirectoryName(dlg.FileName)!,
                Path.GetFileNameWithoutExtension(dlg.FileName) + "-kor.csv");
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("# METADATA");
            csv.AppendLine($"Projekt: {ProjektManager.ProjektName}");
            csv.AppendLine($"Sensor: {KoordTransformationsRechner.TypName(_letzteBerechnung.Typ)}");
            csv.AppendLine("Bearbeiter: ");
            csv.AppendLine($"Datum: {DateTime.Now:yyyy-MM-dd}");
            csv.AppendLine("---");
            csv.AppendLine("# DATENTYP");
            csv.AppendLine("Koordinaten");
            csv.AppendLine("# DATEN");
            csv.AppendLine("PunktNr; X; Y; Z; Code");
            foreach (var p in exportPunkte)
            {
                string code = _identischePunktNr.Contains(p.PunktNr) ? "T" : "";
                csv.AppendLine($"{p.PunktNr}; {p.X.ToString("F3",IC)}; {p.Y.ToString("F3",IC)}; {p.Z.ToString("F3",IC)}; {code}");
            }
            File.WriteAllText(csvPfad, csv.ToString(), System.Text.Encoding.UTF8);

            MessageBox.Show($"Exportiert:\n{dlg.FileName}\n{csvPfad}",
                "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Export:\n{ex.Message}",
                "Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── Ampel-Farbe (4 Stufen) ────────────────────────────────────────────────
    // Gibt (Hintergrund, Vordergrund) für einen absoluten cm-Betrag zurück.
    private static (Color bg, Color fg) AmpelFarbe(double absCm)
    {
        if (absCm <= AMPEL_GRUEN)   return (Color.FromArgb(200, 240, 200), Color.Empty);  // grün
        if (absCm <= AMPEL_GELB)    return (Color.FromArgb(255, 245, 165), Color.Empty);  // gelb
        if (absCm <= AMPEL_TIEFROT) return (Color.FromArgb(255, 185, 185), Color.Empty);  // rot
        return (Color.FromArgb(180, 20, 20), Color.White);                                // tief-rot
    }

    // Färbt einzelne Wert-Zellen einer Residuen-Zeile per-Spalte Ampel.
    // Checkbox- und PunktNr-Zellen erhalten neutrale Farbe.
    private static void SetResiduenAmpel(DataGridViewRow row,
        TransformationsResiduum r, bool ist3D)
    {
        // Zeilenebene neutral (sonst überschreibt AlternatingStyle)
        row.DefaultCellStyle.BackColor = Color.Empty;
        row.DefaultCellStyle.ForeColor = Color.Empty;
        foreach (DataGridViewCell c in row.Cells)
        { c.Style.BackColor = Color.Empty; c.Style.ForeColor = Color.Empty; }

        // Wert-Zellen nach eigenem Betrag färben
        var (bgX, fgX) = AmpelFarbe(Math.Abs(r.vX_mm / 10.0));
        var (bgY, fgY) = AmpelFarbe(Math.Abs(r.vY_mm / 10.0));
        var (bgG, fgG) = AmpelFarbe(r.vGesamt_mm / 10.0);
        SetCellFarbe(row.Cells["colVX"],   bgX, fgX);
        SetCellFarbe(row.Cells["colVY"],   bgY, fgY);
        SetCellFarbe(row.Cells["colVGes"], bgG, fgG);
        if (ist3D && row.DataGridView?.Columns.Contains("colVZ") == true)
        {
            var (bgZ, fgZ) = r.HoeheAktiv
                ? AmpelFarbe(Math.Abs(r.vZ_mm / 10.0))
                : (Color.FromArgb(228, 228, 228), Color.Gray);
            SetCellFarbe(row.Cells["colVZ"], bgZ, fgZ);
        }
    }

    private static void SetCellFarbe(DataGridViewCell cell, Color bg, Color fg)
    {
        cell.Style.BackColor = bg;
        cell.Style.ForeColor = fg;
    }

    // Setzt Hintergrund- und Vordergrundfarbe auf Zellebene (höchste Priorität,
    // überschreibt AlternatingRowsDefaultCellStyle zuverlässig).
    private static void SetZeilenFarbe(DataGridViewRow row, Color bg,
                                       Color fg = default)
    {
        row.DefaultCellStyle.BackColor = bg;
        row.DefaultCellStyle.ForeColor = fg;
        foreach (DataGridViewCell c in row.Cells)
        {
            c.Style.BackColor = bg;
            c.Style.ForeColor = fg;
        }
    }

    // ── Punkte aus Grid lesen ─────────────────────────────────────────────────
    private static List<TransformPunkt> LesePunkte(DataGridView dgv)
    {
        var result = new List<TransformPunkt>();
        foreach (DataGridViewRow row in dgv.Rows)
        {
            if (row.IsNewRow) continue;
            string? nr = row.Cells[0].Value?.ToString()?.Trim();
            if (string.IsNullOrEmpty(nr)) continue;

            if (!double.TryParse(row.Cells[1].Value?.ToString()?.Trim().Replace(',','.'),
                    NumberStyles.Any, IC, out double x)) continue;
            if (!double.TryParse(row.Cells[2].Value?.ToString()?.Trim().Replace(',','.'),
                    NumberStyles.Any, IC, out double y)) continue;
            double.TryParse(row.Cells[3].Value?.ToString()?.Trim().Replace(',','.'),
                    NumberStyles.Any, IC, out double z);

            result.Add(new TransformPunkt { PunktNr = nr, X = x, Y = y, Z = z });
        }
        return result;
    }
}
