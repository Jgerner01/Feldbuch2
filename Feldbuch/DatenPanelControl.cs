namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// DatenPanelControl – eine Seite im Daten-Manager.
// Enthält Toolbar (Öffnen/Neu), Metadaten-Felder und editierbare Datentabelle.
// ──────────────────────────────────────────────────────────────────────────────
public class DatenPanelControl : UserControl
{
    // ── Modell ────────────────────────────────────────────────────────────────
    CsvDatenDatei? _datei;
    bool           _geaendert;
    readonly ToolTip _tt = new();

    public bool HatDatei     => _datei != null;
    public bool IstGeaendert => _geaendert;

    // ── Controls ──────────────────────────────────────────────────────────────
    Label        _lblDatei    = null!;
    TextBox      _txtProjekt  = null!, _txtSensor  = null!,
                 _txtBearb    = null!, _txtDatum   = null!;
    RadioButton  _rdoKor      = null!, _rdoDat     = null!;
    DataGridView _grid        = null!;
    Panel        _pnlStp      = null!;
    TextBox      _txtStpNr    = null!, _txtStpH = null!, _txtStpCode = null!;

    // ── Konstruktor ───────────────────────────────────────────────────────────
    public DatenPanelControl() => BaueUI();

    // ═════════════════════════════════════════════════════════════════════════
    // UI-Aufbau
    // ═════════════════════════════════════════════════════════════════════════
    void BaueUI()
    {
        SuspendLayout();
        Padding = new Padding(6);

        var fntLbl  = new Font("Segoe UI", 9F);
        var fntTxt  = new Font("Segoe UI", 9F);
        var fntGrp  = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        var fntBtn  = new Font("Segoe UI", 8.5F);
        var colGrp  = Color.FromArgb(40, 60, 110);

        // ── Toolbar ───────────────────────────────────────────────────────────
        var pnlBar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 42,
            BackColor = Color.FromArgb(228, 231, 242),
            Padding   = new Padding(4, 4, 4, 4)
        };

        var btnOeffnen = BarBtn("Öffnen",  3,   fntBtn, Color.FromArgb(52, 88, 155));
        var btnNeuKor  = BarBtn("Neu KOR", 89,  fntBtn, Color.FromArgb(36, 108, 68));
        var btnNeuDat  = BarBtn("Neu DAT", 175, fntBtn, Color.FromArgb(130, 72, 20));

        btnOeffnen.Click += (_, _) => OeffnenDialog();
        btnNeuKor .Click += (_, _) => NeuDatei("Koordinaten");
        btnNeuDat .Click += (_, _) => NeuDatei("Messdaten");

        _lblDatei = new Label
        {
            Text        = "(keine Datei geöffnet)",
            Location    = new Point(266, 13),
            Size        = new Size(500, 18),
            Font        = new Font("Consolas", 8F),
            ForeColor   = Color.FromArgb(80, 85, 110),
            AutoEllipsis = true
        };

        pnlBar.Controls.AddRange([btnOeffnen, btnNeuKor, btnNeuDat, _lblDatei]);

        // ── Metadaten GroupBox ─────────────────────────────────────────────────
        var grpMeta = new GroupBox
        {
            Text      = "Metadaten",
            Dock      = DockStyle.Top,
            Height    = 192,
            Font      = fntGrp,
            ForeColor = colGrp,
            Padding   = new Padding(8, 4, 8, 6)
        };

        var tbl = new TableLayoutPanel
        {
            Dock            = DockStyle.Fill,
            ColumnCount     = 2,
            RowCount        = 5,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            Padding         = new Padding(0, 2, 0, 0)
        };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 4; i++)
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _txtProjekt = MetaTxt(fntTxt);
        _txtSensor  = MetaTxt(fntTxt);
        _txtBearb   = MetaTxt(fntTxt);
        _txtDatum   = MetaTxt(fntTxt);

        tbl.Controls.Add(MetaLbl("Projekt:",    fntLbl), 0, 0); tbl.Controls.Add(_txtProjekt, 1, 0);
        tbl.Controls.Add(MetaLbl("Sensor:",     fntLbl), 0, 1); tbl.Controls.Add(_txtSensor,  1, 1);
        tbl.Controls.Add(MetaLbl("Bearbeiter:", fntLbl), 0, 2); tbl.Controls.Add(_txtBearb,   1, 2);
        tbl.Controls.Add(MetaLbl("Datum:",      fntLbl), 0, 3); tbl.Controls.Add(_txtDatum,   1, 3);

        _rdoKor = new RadioButton { Text = "Koordinaten", Location = new Point(2, 6),  Size = new Size(115, 20), Font = fntLbl, Checked = true };
        _rdoDat = new RadioButton { Text = "Messdaten",   Location = new Point(122, 6), Size = new Size(100, 20), Font = fntLbl };
        var rdoPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
        rdoPanel.Controls.AddRange([_rdoKor, _rdoDat]);

        tbl.Controls.Add(MetaLbl("Datentyp:", fntLbl), 0, 4);
        tbl.Controls.Add(rdoPanel, 1, 4);

        grpMeta.Controls.Add(tbl);

        // Sichtbarkeit Standpunkt-Panel steuern
        _rdoKor.CheckedChanged += (_, _) => AktualisiereStpSichtbarkeit();
        _rdoDat.CheckedChanged += (_, _) => AktualisiereStpSichtbarkeit();

        // ── Standpunkt-Panel (nur für Messdaten sichtbar) ─────────────────────
        _pnlStp = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 40,
            BackColor = Color.FromArgb(238, 240, 252),
            Padding   = new Padding(8, 6, 8, 4),
            Visible   = false
        };

        _txtStpNr   = StpTxt(fntTxt, 130);
        _txtStpH    = StpTxt(fntTxt, 80);
        _txtStpCode = StpTxt(fntTxt, 60);

        var stpLayout = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false,
            Padding       = new Padding(0)
        };

        stpLayout.Controls.Add(StpLbl("Standpunkt-Nr:", fntLbl));
        stpLayout.Controls.Add(_txtStpNr);
        stpLayout.Controls.Add(StpLbl("  Instr.-Höhe:", fntLbl));
        stpLayout.Controls.Add(_txtStpH);
        stpLayout.Controls.Add(StpLbl("  Code:", fntLbl));
        stpLayout.Controls.Add(_txtStpCode);

        _pnlStp.Controls.Add(stpLayout);

        // ── Daten GroupBox ─────────────────────────────────────────────────────
        var grpDaten = new GroupBox
        {
            Text      = "Daten",
            Dock      = DockStyle.Fill,
            Font      = fntGrp,
            ForeColor = colGrp,
            Padding   = new Padding(8, 4, 8, 8)
        };

        // Button-Leiste unter der Tabelle
        var pnlBtnRow = new Panel { Dock = DockStyle.Bottom, Height = 34 };

        var btnSpalten = AktBtn("Spalten…",  0,   70, fntBtn, Color.FromArgb(200, 205, 225), Color.FromArgb(30, 40, 70));
        var btnHinzu   = AktBtn("+ Zeile",   76,  70, fntBtn, Color.FromArgb(36, 108, 68),   Color.White);
        var btnEntf    = AktBtn("− Zeile",   152, 70, fntBtn, Color.FromArgb(150, 38, 38),   Color.White);
        var btnImport  = AktBtn("↓ Import",  234, 76, fntBtn, Color.FromArgb(52, 88, 155),   Color.White);
        var btnExport  = AktBtn("↑ Export",  316, 76, fntBtn, Color.FromArgb(70, 70, 90),    Color.FromArgb(210, 215, 235));

        btnSpalten.Click += (_, _) => SpaltendBearbeiten();
        btnHinzu  .Click += (_, _) => _grid.Rows.Add();
        btnEntf   .Click += (_, _) => EntferneMarkierteZeilen();
        btnImport .Click += (_, _) => Importieren();
        btnExport .Click += (_, _) => Exportieren();

        pnlBtnRow.Controls.AddRange([btnSpalten, btnHinzu, btnEntf, btnImport, btnExport]);

        // DataGridView
        _grid = new DataGridView
        {
            Dock                          = DockStyle.Fill,
            AllowUserToAddRows            = true,
            AllowUserToDeleteRows         = true,
            AutoSizeColumnsMode           = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible             = false,
            BorderStyle                   = BorderStyle.None,
            BackgroundColor               = Color.White,
            GridColor                     = Color.FromArgb(210, 215, 228),
            SelectionMode                 = DataGridViewSelectionMode.FullRowSelect,
            EditMode                      = DataGridViewEditMode.EditOnKeystrokeOrF2,
            ColumnHeadersHeightSizeMode   = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight           = 28,
            Font                          = new Font("Consolas", 9F),
            ClipboardCopyMode             = DataGridViewClipboardCopyMode.EnableWithoutHeaderText
        };
        _grid.RowTemplate.Height = 24;
        _grid.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(215, 222, 242);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(25, 45, 100);
        _grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _grid.DefaultCellStyle.SelectionBackColor     = Color.FromArgb(185, 208, 245);
        _grid.DefaultCellStyle.SelectionForeColor     = Color.FromArgb(15, 30, 70);
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(246, 248, 255);

        _grid.CellValueChanged += (_, _) => _geaendert = true;
        _grid.RowsAdded        += (_, _) => _geaendert = true;
        _grid.RowsRemoved      += (_, _) => _geaendert = true;

        grpDaten.Controls.Add(_grid);
        grpDaten.Controls.Add(pnlBtnRow);

        // ── Zusammenbau (Reihenfolge für DockStyle!) ──────────────────────────
        Controls.Add(grpDaten);   // Fill  (zuerst)
        Controls.Add(_pnlStp);    // Top   (Standpunkt, unter Meta)
        Controls.Add(grpMeta);    // Top
        Controls.Add(pnlBar);     // Top   (zuletzt = ganz oben)

        ResumeLayout(false);
    }

    // ── UI-Hilfsmethoden ──────────────────────────────────────────────────────
    static Button BarBtn(string text, int x, Font font, Color back) => new()
    {
        Text      = text, Location = new Point(x, 4), Size = new Size(82, 30),
        Font      = font, FlatStyle = FlatStyle.Flat,
        BackColor = back, ForeColor = Color.White, Cursor = Cursors.Hand
    };

    static Button AktBtn(string text, int x, int w, Font font, Color back, Color fore) => new()
    {
        Text      = text, Location = new Point(x, 4), Size = new Size(w, 26),
        Font      = font, FlatStyle = FlatStyle.Flat,
        BackColor = back, ForeColor = fore, Cursor = Cursors.Hand
    };

    void AktualisiereStpSichtbarkeit() =>
        _pnlStp.Visible = _rdoDat.Checked;

    static Label StpLbl(string text, Font font) => new()
    {
        Text      = text,
        AutoSize  = false,
        Width     = text.Length > 12 ? 106 : 54,
        Height    = 24,
        TextAlign = ContentAlignment.MiddleRight,
        Font      = font,
        ForeColor = Color.FromArgb(55, 65, 90),
        Margin    = new Padding(0, 2, 2, 0)
    };

    static TextBox StpTxt(Font font, int width) => new()
    {
        Width       = width,
        Height      = 24,
        Font        = font,
        BorderStyle = BorderStyle.FixedSingle,
        Margin      = new Padding(0, 2, 4, 0)
    };

    static Label MetaLbl(string text, Font font) => new()
    {
        Text      = text, Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight,
        Font      = font, ForeColor = Color.FromArgb(55, 65, 90),
        Padding   = new Padding(0, 0, 6, 0)
    };

    static TextBox MetaTxt(Font font) => new()
    {
        Dock        = DockStyle.Fill,
        Font        = font,
        Margin      = new Padding(0, 6, 4, 2),
        BorderStyle = BorderStyle.FixedSingle
    };

    // ═════════════════════════════════════════════════════════════════════════
    // Datei-Operationen
    // ═════════════════════════════════════════════════════════════════════════

    void OeffnenDialog()
    {
        string startDir = ProjektManager.IstGeladen
            ? ProjektManager.ProjektVerzeichnis : AppPfade.Basis;

        using var dlg = new OpenFileDialog
        {
            Title            = "CSV-Datendatei öffnen",
            Filter           = "CSV-Datendateien|*-kor.csv;*-dat.csv|Alle CSV|*.csv|Alle Dateien|*.*",
            InitialDirectory = Directory.Exists(startDir) ? startDir : ""
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        Laden(dlg.FileName);
    }

    public void Laden(string pfad)
    {
        try
        {
            _datei    = CsvDatenDatei.Load(pfad);
            _geaendert = false;
            LadeInUI();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Laden:\n{ex.Message}", "Fehler",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    void NeuDatei(string datentyp)
    {
        string startDir = ProjektManager.IstGeladen
            ? ProjektManager.ProjektVerzeichnis : AppPfade.Basis;
        string vorschlag = datentyp == "Messdaten" ? "messdaten-dat.csv" : "koordinaten-kor.csv";

        using var dlg = new SaveFileDialog
        {
            Title            = $"Neue {datentyp}-Datei anlegen",
            Filter           = "CSV-Datei|*.csv",
            InitialDirectory = Directory.Exists(startDir) ? startDir : "",
            FileName         = vorschlag
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        _datei          = CsvDatenDatei.Neu(datentyp);
        _datei.Pfad     = CsvDatenDatei.NormalisierePfad(dlg.FileName, datentyp);
        _datei.Datum    = DateTime.Today.ToString("yyyy-MM-dd");
        if (ProjektManager.IstGeladen) _datei.Projekt = ProjektManager.ProjektName;

        _geaendert = true;
        LadeInUI();
    }

    // ── UI ↔ Modell ───────────────────────────────────────────────────────────
    void LadeInUI()
    {
        if (_datei == null) return;

        _txtProjekt.Text = _datei.Projekt;
        _txtSensor .Text = _datei.Sensor;
        _txtBearb  .Text = _datei.Bearbeiter;
        _txtDatum  .Text = _datei.Datum;
        _rdoKor.Checked  = _datei.Datentyp != "Messdaten";
        _rdoDat.Checked  = _datei.Datentyp == "Messdaten";

        _txtStpNr  .Text = _datei.StandpunktNr;
        _txtStpH   .Text = _datei.InstrumentenHoehe;
        _txtStpCode.Text = _datei.StandpunktCode;
        AktualisiereStpSichtbarkeit();

        string anzeige = Path.GetFileName(_datei.Pfad);
        _lblDatei.Text  = anzeige;
        _tt.SetToolTip(_lblDatei, _datei.Pfad);

        // Grid
        _grid.Columns.Clear();
        _grid.Rows.Clear();
        foreach (var col in _datei.Spalten)
            _grid.Columns.Add(new DataGridViewTextBoxColumn
                { HeaderText = col, Name = col, SortMode = DataGridViewColumnSortMode.NotSortable });

        foreach (var row in _datei.Zeilen)
        {
            int idx = _grid.Rows.Add();
            for (int i = 0; i < row.Count && i < _grid.Columns.Count; i++)
                _grid.Rows[idx].Cells[i].Value = row[i];
        }

        _geaendert = false;
    }

    void LeseAusUI()
    {
        if (_datei == null) return;
        _datei.Projekt           = _txtProjekt.Text.Trim();
        _datei.Sensor            = _txtSensor .Text.Trim();
        _datei.Bearbeiter        = _txtBearb  .Text.Trim();
        _datei.Datum             = _txtDatum  .Text.Trim();
        _datei.Datentyp          = _rdoDat.Checked ? "Messdaten" : "Koordinaten";
        _datei.StandpunktNr      = _txtStpNr  .Text.Trim();
        _datei.InstrumentenHoehe = _txtStpH   .Text.Trim();
        _datei.StandpunktCode    = _txtStpCode.Text.Trim();

        if (!string.IsNullOrEmpty(_datei.Pfad))
            _datei.Pfad = CsvDatenDatei.NormalisierePfad(_datei.Pfad, _datei.Datentyp);

        _datei.Spalten.Clear();
        foreach (DataGridViewColumn col in _grid.Columns)
            _datei.Spalten.Add(col.HeaderText);

        _datei.Zeilen.Clear();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.IsNewRow) continue;
            var cells = row.Cells.Cast<DataGridViewCell>()
                .Select(c => c.Value?.ToString()?.Trim() ?? "").ToList();
            if (cells.All(string.IsNullOrEmpty)) continue;
            _datei.Zeilen.Add(cells);
        }
    }

    public void Speichern()
    {
        if (_datei == null) return;
        LeseAusUI();

        if (string.IsNullOrEmpty(_datei.Pfad))
        {
            using var dlg = new SaveFileDialog
            {
                Title = "Datei speichern", Filter = "CSV-Datei|*.csv"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            _datei.Pfad = CsvDatenDatei.NormalisierePfad(dlg.FileName, _datei.Datentyp);
        }

        try
        {
            _datei.Save();
            _lblDatei.Text = Path.GetFileName(_datei.Pfad);
            _tt.SetToolTip(_lblDatei, _datei.Pfad);
            _geaendert = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Speichern:\n{ex.Message}", "Fehler",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── Zeilen entfernen ──────────────────────────────────────────────────────
    void EntferneMarkierteZeilen()
    {
        foreach (var row in _grid.SelectedRows.Cast<DataGridViewRow>()
                     .Where(r => !r.IsNewRow)
                     .OrderByDescending(r => r.Index))
            _grid.Rows.Remove(row);
    }

    // ── Import (KOR / DAT) ────────────────────────────────────────────────────
    void Importieren()
    {
        bool istKor = _rdoKor.Checked;
        string startDir = ProjektManager.IstGeladen
            ? ProjektManager.ProjektVerzeichnis : AppPfade.Basis;

        using var dlg = new OpenFileDialog
        {
            Title            = istKor ? "KOR-Datei importieren" : "DAT-Datei importieren",
            Filter           = istKor ? "KOR-Dateien|*.kor|Alle Dateien|*.*"
                                      : "DAT-Dateien|*.dat|Alle Dateien|*.*",
            InitialDirectory = Directory.Exists(startDir) ? startDir : ""
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            _datei     = istKor ? CsvDatenDatei.ImportiereKor(dlg.FileName)
                                : CsvDatenDatei.ImportiereDat(dlg.FileName);
            _geaendert = true;
            LadeInUI();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Import:\n{ex.Message}", "Importfehler",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── Export (KOR / DAT) ────────────────────────────────────────────────────
    void Exportieren()
    {
        if (_datei == null)
        {
            MessageBox.Show("Keine Datei geöffnet.", "Export",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        LeseAusUI();

        bool istKor = _datei.Datentyp != "Messdaten";
        string startDir = ProjektManager.IstGeladen
            ? ProjektManager.ProjektVerzeichnis : AppPfade.Basis;

        string basis = Path.GetFileNameWithoutExtension(_datei.Pfad);
        if (basis.EndsWith("-kor", StringComparison.OrdinalIgnoreCase) ||
            basis.EndsWith("-dat", StringComparison.OrdinalIgnoreCase))
            basis = basis[..^4];

        using var dlg = new SaveFileDialog
        {
            Title            = istKor ? "Als KOR exportieren" : "Als DAT exportieren",
            Filter           = istKor ? "KOR-Dateien|*.kor|Alle Dateien|*.*"
                                      : "DAT-Dateien|*.dat|Alle Dateien|*.*",
            InitialDirectory = Directory.Exists(startDir) ? startDir : "",
            FileName         = basis + (istKor ? ".kor" : ".dat")
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            if (istKor) _datei.ExportiereKor(dlg.FileName);
            else        _datei.ExportiereDat(dlg.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Export:\n{ex.Message}", "Exportfehler",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── Spalten bearbeiten ────────────────────────────────────────────────────
    void SpaltendBearbeiten()
    {
        string aktuell = string.Join("; ",
            _grid.Columns.Cast<DataGridViewColumn>().Select(c => c.HeaderText));

        using var dlg = new Form
        {
            Text = "Spalten bearbeiten", Size = new Size(460, 160),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false
        };
        var lbl = new Label
        {
            Text = "Spaltenbezeichnungen (Semikolon-getrennt):",
            Location = new Point(12, 14), AutoSize = true, Font = new Font("Segoe UI", 9F)
        };
        var txt = new TextBox
        {
            Text = aktuell, Location = new Point(12, 38),
            Size = new Size(424, 24), Font = new Font("Consolas", 9F)
        };
        var btnOk  = new Button { Text = "OK",         DialogResult = DialogResult.OK,
            Location = new Point(248, 76), Size = new Size(88, 28), Font = new Font("Segoe UI", 9F) };
        var btnAbb = new Button { Text = "Abbrechen",  DialogResult = DialogResult.Cancel,
            Location = new Point(348, 76), Size = new Size(88, 28), Font = new Font("Segoe UI", 9F) };
        dlg.Controls.AddRange([lbl, txt, btnOk, btnAbb]);
        dlg.AcceptButton = btnOk; dlg.CancelButton = btnAbb;

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var neuSpalten = txt.Text.Split(';')
            .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
        if (neuSpalten.Count == 0) return;

        // Daten retten
        var alteZeilen = _grid.Rows.Cast<DataGridViewRow>()
            .Where(r => !r.IsNewRow)
            .Select(r => r.Cells.Cast<DataGridViewCell>()
                .Select(c => c.Value?.ToString() ?? "").ToList())
            .ToList();

        _grid.Columns.Clear();
        foreach (var s in neuSpalten)
            _grid.Columns.Add(new DataGridViewTextBoxColumn
                { HeaderText = s, Name = s, SortMode = DataGridViewColumnSortMode.NotSortable });

        foreach (var z in alteZeilen)
        {
            int idx = _grid.Rows.Add();
            for (int i = 0; i < z.Count && i < _grid.Columns.Count; i++)
                _grid.Rows[idx].Cells[i].Value = z[i];
        }
        _geaendert = true;
    }
}
