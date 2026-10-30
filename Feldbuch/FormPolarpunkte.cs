// Zweck:    Polarpunktberechnung: Neupunkte aus den Messungen der {Projekt}-dat.csv.
//           Orientierung aus gespeicherter Station oder neu aus den Anschlusspunkten (Ampel, einzelne
//           Beobachtungen schaltbar, Gewichtung bei nahen Zielen), Längs-/Querexzentrizität je Punkt,
//           Maßstab nach Rechenparameter, Speichern nach Neupunkte.json und als KOR-CSV.
// Hardware: keine
// Autor:    Johann Gerner

using System.Globalization;

namespace Feldbuch;

public partial class FormPolarpunkte : Form
{
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    private const string STANDPUNKT_STANDARD = "SP01";
    private const string STRICH              = "–";
    private const int    PROZENT              = 100;
    private const string QUELLE_POLAR         = NeupunkteManager.QUELLE_POLAR;
    private const string STATUS_NEU           = "neu";
    private const string STATUS_ERSETZT       = "ersetzt";

    private static readonly Color FarbeGrau     = Color.LightGray;
    private static readonly Color FarbeExzGross = Color.FromArgb(255, 230, 100);
    private static readonly Color FarbeFehler   = Color.FromArgb(170, 30, 30);
    private static readonly Color FarbeNormal   = Color.Black;

    private PolarMessdaten?              _daten;
    private List<StationierungsPunkt>    _apPunkte        = new();
    private List<PolarMessung>           _np              = new();
    private List<(double L, double Q)>   _exzOriginal     = new();
    private NeupunktErgebnis?[]          _ergebnisse      = [];
    private PunktResiduum?[]             _residuen        = [];
    private StationierungsErgebnis?      _station;
    private MassstabErgebnis             _massstab        = new(1.0, "");
    private readonly List<string>        _ladeHinweise    = new();
    private List<string>                 _hinweise        = new();
    private string                       _stationFehler   = "";
    private bool                         _lokal;
    private bool                         _init;

    public FormPolarpunkte()
    {
        InitializeComponent();
        RueckfrageUeberschreiben = FragePerDialog;

        pnlUnten.SizeChanged += (s, e) => PositioniereButtons();
        PositioniereButtons();

        // Teilung erst nach dem ersten Layout setzen und in den gültigen Bereich zwingen
        Shown  += (s, e) => SetzeTeilung();
        Resize += (s, e) => { if (IsHandleCreated && Visible) SetzeTeilung(); };

        cmbStandpunkt.SelectedIndexChanged += (s, e) => StandpunktGewaehlt();
        rbQuelleStation.CheckedChanged     += (s, e) => { if (!_init && rbQuelleStation.Checked) FuelleTabellen(); };
        rbQuelleDat.CheckedChanged         += (s, e) => { if (!_init && rbQuelleDat.Checked)     FuelleTabellen(); };

        btnRechenparameter.Click += btnRechenparameter_Click;
        btnNeuLaden.Click        += (s, e) => LadeDatei();
        btnNeuBerechnen.Click    += (s, e) => Neuberechnen();
        btnSpeichern.Click       += btnSpeichern_Click;
        btnKorCsv.Click          += btnKorCsv_Click;
        btnProtokoll.Click       += btnProtokoll_Click;
        btnSchliessen.Click      += (s, e) => Close();

        txtInstrHoehe.Leave   += (s, e) => Neuberechnen();
        txtInstrHoehe.KeyDown += (s, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            Neuberechnen();
        };

        dgvAnschluss.CurrentCellDirtyStateChanged += (s, e) => CheckboxSofortUebernehmen(dgvAnschluss);
        dgvAnschluss.CellValueChanged             += (s, e) =>
        {
            if (!_init && e.RowIndex >= 0 && dgvAnschluss.Columns[e.ColumnIndex] is DataGridViewCheckBoxColumn)
                Neuberechnen();
        };

        dgvPolar.CurrentCellDirtyStateChanged += (s, e) => CheckboxSofortUebernehmen(dgvPolar);
        dgvPolar.CellValueChanged             += dgvPolar_CellValueChanged;

        LadeDatei();
    }

    /// <summary>Rückfrage vor dem Überschreiben stark abweichender Punkte (true = fortfahren); Standard: Meldungsfenster.</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Func<string, bool> RueckfrageUeberschreiben { get; set; } = null!;

    private bool FragePerDialog(string text)
        => MessageBox.Show(this, text, "Polarpunkte überschreiben",
               MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes;

    private const int BUTTON_RAND    = 14;
    private const int BUTTON_ABSTAND = 8;
    private const int BUTTON_OBEN    = 9;

    /// <summary>Ordnet die Buttons von rechts her im unteren Panel an (unabhängig von der Panelbreite beim Aufbau).</summary>
    private void PositioniereButtons()
    {
        int x = pnlUnten.ClientSize.Width - BUTTON_RAND;
        foreach (var b in new[] { btnSchliessen, btnKorCsv, btnProtokoll, btnSpeichern, btnNeuBerechnen })
        {
            b.Location = new Point(x - b.Width, BUTTON_OBEN);
            x -= b.Width + BUTTON_ABSTAND;
        }
    }

    private int _letzteSplitHoehe;

    /// <summary>Teilt den Hauptbereich je zur Hälfte; ungültige Bereiche (zu kleines Fenster) werden übersprungen.</summary>
    private void SetzeTeilung()
    {
        int hoehe = splitMain.Height;
        if (hoehe == _letzteSplitHoehe) return;           // Größe unverändert: Teilung des Nutzers behalten
        int min = splitMain.Panel1MinSize;
        int max = hoehe - splitMain.Panel2MinSize - splitMain.SplitterWidth;
        if (max < min) return;
        _letzteSplitHoehe = hoehe;
        splitMain.SplitterDistance = Math.Clamp(hoehe / 2, min, max);
    }

    // ── Laden ─────────────────────────────────────────────────────────────────

    private string SpName => cmbStandpunkt.SelectedItem as string ?? "";

    private void LadeDatei()
    {
        _init = true;
        cmbStandpunkt.Items.Clear();
        dgvAnschluss.Rows.Clear();
        dgvPolar.Rows.Clear();
        _daten = null;
        _station = null;
        _apPunkte = new();
        _np = new();
        _ergebnisse = [];
        _init = false;

        if (!ProjektManager.IstGeladen)
        {
            ZeigeLeer("Kein Projekt geladen.");
            return;
        }

        string pfad = MessdatenCSV.Pfad;
        if (!File.Exists(pfad))
        {
            ZeigeLeer($"Keine Messdaten-Datei vorhanden ({Path.GetFileName(pfad)}).");
            return;
        }

        try
        {
            _daten = PolarMessdatenLeser.Lese(pfad, nr => AnschlusspunktSuche.Finde(nr) != null);
        }
        catch (Exception ex)
        {
            ErrorLogger.Log("FormPolarpunkte.LadeDatei", ex);
            ZeigeLeer($"Messdaten konnten nicht gelesen werden: {ex.Message}");
            return;
        }

        foreach (var m in _daten.Messungen.Where(m => string.IsNullOrEmpty(m.StandpunktNr)))
            m.StandpunktNr = STANDPUNKT_STANDARD;

        foreach (string sp in _daten.Standpunkte) cmbStandpunkt.Items.Add(sp);

        int index = cmbStandpunkt.Items.IndexOf(StationsdatenManager.StandpunktNr);
        if (index < 0) index = cmbStandpunkt.Items.Count - 1;

        if (index >= 0) cmbStandpunkt.SelectedIndex = index;      // löst StandpunktGewaehlt aus
        else            ZeigeLeer("Die Messdaten-Datei enthält keine Messungen.");
    }

    private void ZeigeLeer(string meldung)
    {
        lblErgebnis.ForeColor = FarbeNormal;
        lblErgebnis.Text      = "";
        lblStatus.Text        = meldung;
        ZeigeHinweise(new List<string>());
    }

    private void StandpunktGewaehlt()
    {
        string sp = SpName;
        if (sp.Length == 0 || _daten == null) return;

        _init = true;
        double ih = StationsdatenManager.AktiveStation.StandpunktNr == sp && StationsdatenManager.InstrumentenHoehe > 0
            ? StationsdatenManager.InstrumentenHoehe
            : _daten.KopfInstrHoehe > 0 ? _daten.KopfInstrHoehe : ProjektManager.InstrumentenHoehe;
        txtInstrHoehe.Text = ih.ToString("F3", IC);

        var datei = StationsdatenManager.LeseDatei(sp);
        bool hatStation = datei != null && datei.Messungen.Count > 0;
        rbQuelleStation.Checked = hatStation;
        rbQuelleDat.Checked     = !hatStation;
        _init = false;

        FuelleTabellen();
    }

    private void FuelleTabellen()
    {
        string sp = SpName;
        _init = true;
        dgvAnschluss.Rows.Clear();
        dgvPolar.Rows.Clear();
        _ladeHinweise.Clear();
        _stationFehler = "";
        _apPunkte      = new();
        _np            = new();

        if (_daten != null && sp.Length > 0)
        {
            if (rbQuelleStation.Checked) LadeAnschlusspunkteAusStation(sp);
            else                         LadeAnschlusspunkteAusDat(sp);

            foreach (var p in _apPunkte)
                dgvAnschluss.Rows.Add(p.PunktNr, "", "", "", true, "", true, "", p.Hoehe != 0.0, "");

            _np = _daten.Messungen.Where(m => !m.IstAnschlusspunkt && m.StandpunktNr == sp).ToList();
            _exzOriginal = _np.Select(m => (m.ExzLaengs_m, m.ExzQuer_m)).ToList();
            foreach (var m in _np)
                dgvPolar.Rows.Add(true, m.PunktNr,
                    m.Hz_gon.ToString("F4", IC), m.V_gon.ToString("F4", IC),
                    m.Strecke_m.ToString("F3", IC), m.Zielhoehe_m.ToString("F3", IC),
                    m.ExzLaengs_m.ToString("F3", IC), m.ExzQuer_m.ToString("F3", IC),
                    "", "", "", m.Code, m.Zeitstempel);
        }

        _init = false;
        Neuberechnen();
    }

    private void LadeAnschlusspunkteAusStation(string sp)
    {
        var datei = StationsdatenManager.LeseDatei(sp);
        if (datei == null)
        {
            _stationFehler = $"Keine gespeicherte Stationierung für {sp}. Quelle „neu aus dat.csv“ wählen.";
            return;
        }
        _apPunkte = datei.Messungen.ToList();
    }

    private void LadeAnschlusspunkteAusDat(string sp)
    {
        foreach (var m in _daten!.Messungen.Where(m => m.IstAnschlusspunkt && m.StandpunktNr == sp))
        {
            var k = AnschlusspunktSuche.Finde(m.PunktNr);
            if (k == null)
            {
                _ladeHinweise.Add($"Anschlusspunkt {m.PunktNr}: keine Koordinaten gefunden – nicht verwendet.");
                continue;
            }
            _apPunkte.Add(new StationierungsPunkt
            {
                PunktNr = m.PunktNr, R = k.Value.R, H = k.Value.H, Hoehe = k.Value.Hoehe,
                HZ = m.Hz_gon, V = m.V_gon, Strecke = m.Strecke_m, Zielhoehe = m.Zielhoehe_m
            });
        }
    }

    // ── Berechnung ────────────────────────────────────────────────────────────

    private double LeseInstrHoehe()
        => double.TryParse(txtInstrHoehe.Text.Trim().Replace(',', '.'), NumberStyles.Float, IC, out double ih) ? ih : 0.0;

    private static bool IstAktiv(DataGridView dgv, int zeile, string spalte)
        => dgv.Rows[zeile].Cells[spalte].Value is true;

    private void Neuberechnen()
    {
        if (_init) return;

        var par      = RechenparameterManager.Params;
        string sp    = SpName;
        double ih    = LeseInstrHoehe();
        int n        = _apPunkte.Count;
        var hinweise = new List<string>(_ladeHinweise);

        bool[] aktHz  = new bool[n];
        bool[] aktStr = new bool[n];
        bool[] aktHoe = new bool[n];
        for (int i = 0; i < n; i++)
        {
            aktHz[i]  = IstAktiv(dgvAnschluss, i, COL_AKT_HZ);
            aktStr[i] = IstAktiv(dgvAnschluss, i, COL_AKT_STR);
            aktHoe[i] = IstAktiv(dgvAnschluss, i, COL_AKT_HOE);
        }

        _station = null;
        _lokal   = false;
        string fehler = "";
        var residuenJeZeile = new PunktResiduum?[n];

        if (sp.Length == 0 || _daten == null)
        {
            fehler = "";
        }
        else if (n == 0 && _stationFehler.Length > 0)
        {
            fehler = _stationFehler;
        }
        else if (n == 0)
        {
            _station = LokaleStation();
            _lokal   = true;
        }
        else if (_apPunkte.Select(p => p.PunktNr).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1)
        {
            BerechneEinZiel(sp, ih, par, aktHz, aktStr, aktHoe, residuenJeZeile, ref fehler);
        }
        else
        {
            try
            {
                var erg = FreieStationierungRechner.Berechnen(
                    _apPunkte, ih,
                    freierMassstab:       par.FreierMassstab,
                    aktivRichtung:        aktHz,
                    aktivStrecke:         aktStr,
                    aktivHoehe:           aktHoe,
                    berechnung3D:         par.Berechnung3D,
                    fehlergrenzeMM_Hoehe: par.FehlergrenzeMM_Hoehe,
                    zentrierfehlerZiel_mm:       par.GewichtungNaheZiele ? par.ZentrierfehlerZiel_mm : 0.0,
                    zentrierfehlerStandpunkt_mm: par.GewichtungNaheZiele ? par.ZentrierfehlerStandpunkt_mm : 0.0);
                _station = erg;
                for (int i = 0; i < n && i < erg.Residuen.Count; i++) residuenJeZeile[i] = erg.Residuen[i];
            }
            catch (Exception ex)
            {
                fehler = ex.Message;
            }
        }

        _residuen = residuenJeZeile;
        ZeigeResiduen(residuenJeZeile, par);

        // ── Informationen ─────────────────────────────────────────────────────
        int anschluss = Enumerable.Range(0, n)
            .Where(i => aktHz[i] || aktStr[i])
            .Select(i => _apPunkte[i].PunktNr)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        _massstab = PolarpunktRechner.ErmittleMassstab(par, _station);

        if (_station != null && !_lokal)
        {
            if (anschluss < PolarpunktRechner.MIN_ANSCHLUSSPUNKTE_INFO)
                hinweise.Add($"Nur {anschluss} Anschlusspunkt(e): Orientierung nicht überbestimmt, " +
                             $"Qualität kann nicht beurteilt werden (empfohlen: mindestens {PolarpunktRechner.MIN_ANSCHLUSSPUNKTE_INFO}).");

            int nichtGruen = ZaehleNichtGruen(residuenJeZeile, par);
            if (nichtGruen > 0)
                hinweise.Add($"{nichtGruen} Abweichung(en) (Quer/Längs) nicht im grünen Bereich.");

            if (par.Berechnung3D && _station.Hoehe == 0.0)
                hinweise.Add("Station ohne Höhe – Polarpunkte nur in der Lage (2D).");
        }
        if (_massstab.Hinweis.Length > 0) hinweise.Add(_massstab.Hinweis);

        // ── Polarpunkte ───────────────────────────────────────────────────────
        int berechnet = BerechnePolarpunkte(ih, hinweise);

        _hinweise = hinweise;
        ZeigeHinweise(hinweise);
        ZeigeErgebnis(par, fehler, anschluss, berechnet);
    }

    private static StationierungsErgebnis LokaleStation() => new()
    {
        R = 0, H = 0, Hoehe = 0, Orientierung_gon = 0, Massstab = 1,
        Konvergiert = true, Berechnung3D = true, IstLokalesSystem = true
    };

    // Ein Anschlussziel bei bekanntem Standpunkt: Orientierung aus der einen Richtung
    private void BerechneEinZiel(
        string sp, double ih, Rechenparameter par,
        bool[] aktHz, bool[] aktStr, bool[] aktHoe,
        PunktResiduum?[] residuenJeZeile, ref string fehler)
    {
        var koord = AnschlusspunktSuche.Finde(sp);
        if (koord == null)
        {
            fehler = $"Nur ein Anschlussziel: Der Standpunkt {sp} hat keine bekannten Koordinaten.";
            return;
        }

        int idx = Array.FindIndex(aktHz, a => a);
        if (idx < 0)
        {
            fehler = "Keine aktive Richtung zum Anschlussziel.";
            return;
        }

        var erg = PolarpunktRechner.OrientierungAusEinemZiel(
            koord.Value.R, koord.Value.H, koord.Value.Hoehe,
            _apPunkte[idx], ih, aktHz[idx], aktStr[idx], aktHoe[idx], par.Berechnung3D);

        if (erg == null)
        {
            fehler = "Orientierung aus dem Anschlussziel nicht berechenbar (Standpunkt und Ziel identisch?).";
            return;
        }
        _station = erg;
        residuenJeZeile[idx] = erg.Residuen[0];
    }

    private int BerechnePolarpunkte(double ih, List<string> hinweise)
    {
        _ergebnisse = new NeupunktErgebnis?[_np.Count];
        int berechnet = 0;
        var grosseExz = new List<string>();
        var festAbweichung = new List<string>();

        for (int i = 0; i < _np.Count; i++)
        {
            var m    = _np[i];
            var zeile = dgvPolar.Rows[i];
            bool aktiv = IstAktiv(dgvPolar, i, COL_P_BERECHNEN);

            bool groessL = Math.Abs(m.ExzLaengs_m) > PolarpunktRechner.EXZ_INFO_GRENZE_M;
            bool groessQ = Math.Abs(m.ExzQuer_m)   > PolarpunktRechner.EXZ_INFO_GRENZE_M;
            zeile.Cells[COL_P_EXZL].Style.BackColor = groessL ? FarbeExzGross : Color.Empty;
            zeile.Cells[COL_P_EXZQ].Style.BackColor = groessQ ? FarbeExzGross : Color.Empty;
            if (aktiv && (groessL || groessQ)) grosseExz.Add(m.PunktNr);

            NeupunktErgebnis? e = null;
            if (_station != null && aktiv)
                e = NeupunktRechner.Berechnen(BaueRohdaten(m), _station, _massstab.Wert, ih);
            _ergebnisse[i] = e;

            _init = true;
            zeile.Cells[COL_P_R].Value     = e != null ? e.R.ToString("F3", IC) : STRICH;
            zeile.Cells[COL_P_H].Value     = e != null ? e.H.ToString("F3", IC) : STRICH;
            zeile.Cells[COL_P_HOEHE].Value = e != null && e.Ist3D ? e.Hoehe.ToString("F3", IC) : STRICH;
            _init = false;

            zeile.DefaultCellStyle.BackColor = e == null ? FarbeGrau : Color.Empty;
            if (e != null) berechnet++;

            // Vergleich mit einem bereits übernommenen Festpunkt gleicher Nummer
            var fest = e == null ? null : FeldbuchpunkteManager.Punkte.FirstOrDefault(p =>
                p.Typ == FestpunktUebernahme.TYP_NEUPUNKT && p.PunktNr == e.PunktNr);
            if (e != null && fest != null)
            {
                double abw = PolarpunktRechner.Abstand(e.R - fest.R, e.H - fest.H);
                if (abw > PolarpunktRechner.FESTPUNKT_ABWEICHUNG_INFO_M)
                    festAbweichung.Add($"{e.PunktNr} ({abw.ToString("F3", IC)} m)");
            }
        }

        if (grosseExz.Count > 0)
            hinweise.Add($"Exzentrizität größer als {PolarpunktRechner.EXZ_INFO_GRENZE_M.ToString("F0", IC)} m bei Punkt(en): " +
                         string.Join(", ", grosseExz));
        if (festAbweichung.Count > 0)
            hinweise.Add($"Abweichung zum Festpunkt (> {PolarpunktRechner.FESTPUNKT_ABWEICHUNG_INFO_M.ToString("F2", IC)} m): " +
                         string.Join(", ", festAbweichung));
        return berechnet;
    }

    private NeupunktRohdaten BaueRohdaten(PolarMessung m) => new()
    {
        PunktNr      = m.PunktNr,
        Code         = m.Code,
        Hz_gon       = m.Hz_gon,
        V_gon        = m.V_gon,
        Strecke_m    = m.Strecke_m,
        Zielhoehe_m  = m.Zielhoehe_m,
        Zeitstempel  = DateTime.TryParse(m.Zeitstempel, IC, DateTimeStyles.None, out var t) ? t : DateTime.Now,
        StandpunktNr = SpName,
        ExzLaengs_m  = m.ExzLaengs_m,
        ExzQuer_m    = m.ExzQuer_m
    };

    private static int ZaehleNichtGruen(PunktResiduum?[] res, Rechenparameter par)
    {
        int k = 0;
        foreach (var r in res)
        {
            if (r == null) continue;
            if (r.RichtungAktiv && !double.IsNaN(r.vQuer_mm) &&
                !FormErgebnis.IstGruen(Math.Abs(r.vQuer_mm), par.FehlergrenzeMM_Strecke)) k++;
            if (r.StreckeAktiv && !double.IsNaN(r.vStrecke_mm) &&
                !FormErgebnis.IstGruen(Math.Abs(r.vStrecke_mm), par.FehlergrenzeMM_Strecke)) k++;
        }
        return k;
    }

    // ── Anzeige ───────────────────────────────────────────────────────────────

    private void ZeigeResiduen(PunktResiduum?[] res, Rechenparameter par)
    {
        _init = true;
        for (int i = 0; i < res.Length && i < dgvAnschluss.Rows.Count; i++)
        {
            var row = dgvAnschluss.Rows[i];
            var r   = res[i];

            SetzeZelle(row.Cells[COL_STRECKEH], r != null ? r.StreckeH.ToString("F3", IC) : STRICH);
            SetzeWert(row.Cells[COL_VWINKEL], r != null && r.RichtungAktiv ? r.vWinkel_cc : double.NaN, par.FehlergrenzCC_Winkel);
            SetzeWert(row.Cells[COL_VQUER],   r != null && r.RichtungAktiv ? r.vQuer_mm   : double.NaN, par.FehlergrenzeMM_Strecke);
            SetzeWert(row.Cells[COL_VLAENGS], r != null && r.StreckeAktiv  ? r.vStrecke_mm : double.NaN, par.FehlergrenzeMM_Strecke);
            SetzeWert(row.Cells[COL_VHOEHE],  r != null && r.HoeheAktiv    ? r.vHoehe_mm   : double.NaN, par.FehlergrenzeMM_Hoehe);

            double w = r?.GewichtRichtung ?? double.NaN;
            SetzeZelle(row.Cells[COL_GEWICHT],
                double.IsNaN(w) ? STRICH : $"{(w * PROZENT).ToString("F1", IC)} %");
        }
        _init = false;
    }

    private static void SetzeWert(DataGridViewCell zelle, double wert, double grenze)
    {
        if (double.IsNaN(wert))
        {
            zelle.Value = STRICH;
            zelle.Style.BackColor = FarbeGrau;
            return;
        }
        zelle.Value = wert.ToString("+0.0;-0.0;0.0", CultureInfo.CurrentCulture);
        FormErgebnis.AmpelFarbe(zelle, Math.Abs(wert), grenze);
    }

    private static void SetzeZelle(DataGridViewCell zelle, string text)
    {
        zelle.Value = text;
        zelle.Style.BackColor = Color.Empty;
    }

    private void ZeigeHinweise(List<string> hinweise)
    {
        if (hinweise.Count == 0)
        {
            lblHinweise.Visible = false;
            lblHinweise.Height  = 0;
            lblHinweise.Text    = "";
            return;
        }
        const int ZEILENHOEHE = 20;
        const int RAND        = 10;
        lblHinweise.Text    = string.Join(Environment.NewLine, hinweise.Select(h => "⚠ " + h));
        lblHinweise.Height  = hinweise.Count * ZEILENHOEHE + RAND;
        lblHinweise.Visible = true;
    }

    private void ZeigeErgebnis(Rechenparameter par, string fehler, int anschluss, int berechnet)
    {
        if (fehler.Length > 0)
        {
            lblErgebnis.ForeColor = FarbeFehler;
            lblErgebnis.Text      = fehler;
            lblStatus.Text        = "Keine Orientierung berechenbar.";
            return;
        }
        lblErgebnis.ForeColor = FarbeNormal;

        if (_station == null)
        {
            lblErgebnis.Text = "";
            lblStatus.Text   = "";
            return;
        }

        string gew = par.GewichtungNaheZiele
            ? $"ein (Zielzentrierung {par.ZentrierfehlerZiel_mm.ToString("F1", IC)} mm)"
            : "aus";

        if (_lokal)
        {
            lblErgebnis.Text = "Lokales System: Standpunkt 0/0/0, Orientierung 0 gon – keine Anschlusspunkte erforderlich.";
        }
        else
        {
            string hoehe = par.Berechnung3D && _station.Hoehe != 0.0 ? _station.Hoehe.ToString("F3", IC) : STRICH;
            string s0    = _station.Redundanz > 0 ? RechenparameterManager.SigmaText(_station.s0_mm) : "s₀ = –";
            lblErgebnis.Text =
                $"R {_station.R:F3}  H {_station.H:F3}  Höhe {hoehe}  Orientierung {_station.Orientierung_gon:F4} gon  " +
                $"m {_massstab.Wert:F6}  {s0}  r = {_station.Redundanz}" + Environment.NewLine +
                $"Gewichtung nahe Ziele: {gew}";
        }

        bool dreiD = par.Berechnung3D && (_lokal || _station.Hoehe != 0.0);
        lblStatus.Text = $"{(dreiD ? "3D" : "2D")} · m = {_massstab.Wert.ToString("F6", IC)} · " +
                         $"{(_lokal ? "lokales System" : anschluss + " Anschlusspunkt(e)")} · {berechnet} Polarpunkt(e) berechnet";
    }

    // ── Ereignisse ────────────────────────────────────────────────────────────

    private static void CheckboxSofortUebernehmen(DataGridView dgv)
    {
        if (dgv.CurrentCell is DataGridViewCheckBoxCell && dgv.IsCurrentCellDirty)
            dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
    }

    private void dgvPolar_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (_init || e.RowIndex < 0 || e.RowIndex >= _np.Count) return;
        string spalte = dgvPolar.Columns[e.ColumnIndex].Name;

        if (spalte == COL_P_EXZL || spalte == COL_P_EXZQ)
        {
            var zelle = dgvPolar.Rows[e.RowIndex].Cells[e.ColumnIndex];
            var m     = _np[e.RowIndex];
            string text = Convert.ToString(zelle.Value, IC)?.Trim().Replace(',', '.') ?? "";

            double alt = spalte == COL_P_EXZL ? m.ExzLaengs_m : m.ExzQuer_m;
            double neu = double.TryParse(text, NumberStyles.Float, IC, out double w) ? w : alt;

            if (spalte == COL_P_EXZL) m.ExzLaengs_m = neu; else m.ExzQuer_m = neu;

            _init = true;
            zelle.Value = neu.ToString("F3", IC);       // normiert Eingabe (Komma/Punkt)
            _init = false;
        }
        else if (spalte != COL_P_BERECHNEN)
        {
            return;
        }
        Neuberechnen();
    }

    private void btnRechenparameter_Click(object? sender, EventArgs e)
    {
        using var form = new FormRechenparameter();
        if (form.ShowDialog(this) == DialogResult.OK) Neuberechnen();
    }

    // ── Speichern ─────────────────────────────────────────────────────────────

    private void btnSpeichern_Click(object? sender, EventArgs e)
    {
        Neuberechnen();
        int anzahl = _ergebnisse.Count(x => x != null);
        if (_station == null || anzahl == 0)
        {
            MessageBox.Show("Es sind keine berechneten Polarpunkte zum Speichern vorhanden.",
                "Polarpunkte", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_hinweise.Count > 0)
            MessageBox.Show("Hinweise zu dieser Berechnung:" + Environment.NewLine + Environment.NewLine +
                            string.Join(Environment.NewLine, _hinweise.Select(h => "• " + h)),
                "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

        string sp = SpName;

        // Rückfrage, wenn vorhandene Punkte (gleiche Nummer, gleicher Standpunkt) stark abweichen
        var starkAbweichend = new List<string>();
        for (int i = 0; i < _np.Count; i++)
        {
            var neuErg    = _ergebnisse[i];
            var vorhanden = neuErg == null ? null : NeupunkteManager.FindeKoordinate(neuErg.PunktNr, sp);
            if (neuErg == null || vorhanden == null) continue;
            double abw = PolarpunktRechner.Abstand(neuErg.R - vorhanden.R, neuErg.H - vorhanden.H);
            if (abw > PolarpunktRechner.UEBERSCHREIBEN_RUECKFRAGE_M)
                starkAbweichend.Add($"{neuErg.PunktNr}: {abw.ToString("F3", IC)} m");
        }
        if (starkAbweichend.Count > 0)
        {
            string text =
                $"{starkAbweichend.Count} vorhandene(r) Punkt(e) von {sp} weichen um mehr als " +
                $"{PolarpunktRechner.UEBERSCHREIBEN_RUECKFRAGE_M.ToString("F2", IC)} m vom neuen Ergebnis ab:" +
                Environment.NewLine + Environment.NewLine +
                string.Join(Environment.NewLine, starkAbweichend.Take(10)) +
                (starkAbweichend.Count > 10 ? Environment.NewLine + "…" : "") +
                Environment.NewLine + Environment.NewLine + "Trotzdem überschreiben?";
            if (!RueckfrageUeberschreiben(text))
            {
                lblStatus.Text = "Speichern abgebrochen – vorhandene Punkte unverändert.";
                return;
            }
        }

        var protokollPunkte = new List<PolarProtokollPunkt>();
        int neu = 0, ersetzt = 0;
        for (int i = 0; i < _np.Count; i++)
        {
            var erg = _ergebnisse[i];
            if (erg == null) continue;
            erg.StandpunktNr = sp;
            erg.Quelle       = QUELLE_POLAR;

            // Vorhandene Koordinaten gleicher Punktnummer werden überschrieben; Differenz fürs Protokoll merken
            var alt = NeupunkteManager.FindeKoordinate(erg.PunktNr, sp);
            if (alt == null) neu++; else ersetzt++;
            protokollPunkte.Add(new PolarProtokollPunkt(
                erg.PunktNr, _np[i].Hz_gon, _np[i].V_gon, _np[i].Strecke_m, _np[i].Zielhoehe_m,
                erg.ExzLaengs_m, erg.ExzQuer_m, erg.R, erg.H, erg.Ist3D ? erg.Hoehe : null,
                alt == null ? STATUS_NEU : STATUS_ERSETZT,
                alt == null ? double.NaN : erg.R - alt.R,
                alt == null ? double.NaN : erg.H - alt.H));

            NeupunkteManager.HinzufuegenOderErsetzen(BaueRohdaten(_np[i]), erg);
        }

        SchreibeGeaenderteExzentrizitaeten();
        ProtokolliereSpeichern(sp, anzahl);
        string? protokoll = PolarpunkteProtokoll.Schreiben(BaueProtokollDaten(protokollPunkte));

        // Ein bereits geöffneter DXF-Viewer zeigt die neuen Koordinaten sofort
        FormDxfViewer.AktiveInstanz?.BaueNeupunkteOverlay();

        lblStatus.Text = $"{anzahl} Polarpunkt(e) gespeichert ({neu} neu, {ersetzt} ersetzt) – Neupunkte.json" +
                         (protokoll != null ? $", Protokoll {Path.GetFileName(protokoll)}" : "") + ".";
    }

    // ── Protokoll ─────────────────────────────────────────────────────────────

    private PolarProtokollDaten BaueProtokollDaten(List<PolarProtokollPunkt> punkte)
    {
        var par = RechenparameterManager.Params;
        var st  = _station!;

        var anschluss = new List<PolarProtokollAnschluss>();
        for (int i = 0; i < _apPunkte.Count; i++)
        {
            var r = i < _residuen.Length ? _residuen[i] : null;
            bool inaktiv = !IstAktiv(dgvAnschluss, i, COL_AKT_HZ) ||
                           !IstAktiv(dgvAnschluss, i, COL_AKT_STR) ||
                           !IstAktiv(dgvAnschluss, i, COL_AKT_HOE);
            anschluss.Add(new PolarProtokollAnschluss(
                _apPunkte[i].PunktNr, inaktiv, r?.StreckeH ?? 0.0,
                r != null && r.RichtungAktiv ? r.vWinkel_cc  : double.NaN,
                r != null && r.RichtungAktiv ? r.vQuer_mm    : double.NaN,
                r != null && r.StreckeAktiv  ? r.vStrecke_mm : double.NaN,
                r != null && r.HoeheAktiv    ? r.vHoehe_mm   : double.NaN,
                r?.GewichtRichtung ?? double.NaN));
        }

        string s0Info;
        if (_lokal)
            s0Info = "Lokales System: Standpunkt 0/0/0, Orientierung 0 gon, keine Anschlusspunkte";
        else if (st.Redundanz > 0)
        {
            string bez = RechenparameterManager.SigmaFaktor == 1 ? "s0" : $"{RechenparameterManager.SigmaFaktor}*s0";
            s0Info = $"Standardabw. {bez}:  {RechenparameterManager.SigmaWert(st.s0_mm).ToString("F2", IC)} mm     Redundanz r:  {st.Redundanz}";
        }
        else
            s0Info = "Standardabw. s0:  -  (Redundanz r = 0, eindeutig bestimmt)";

        string gewichtung = par.GewichtungNaheZiele
            ? $"ein (Zielzentrierung {par.ZentrierfehlerZiel_mm.ToString("F1", IC)} mm, Standpunkt {par.ZentrierfehlerStandpunkt_mm.ToString("F1", IC)} mm)"
            : "aus";
        string quelle = _lokal ? "lokales System"
                      : rbQuelleStation.Checked ? "gespeicherter Stationierung" : "Anschlusspunkten der dat.csv";
        bool dreiD = par.Berechnung3D && (_lokal || st.Hoehe != 0.0);

        return new PolarProtokollDaten(
            SpName, LeseInstrHoehe(), quelle, _lokal,
            st.R, st.H, dreiD ? st.Hoehe : null, st.Orientierung_gon, _massstab.Wert,
            s0Info, gewichtung, _hinweise, anschluss, punkte);
    }

    private void btnProtokoll_Click(object? sender, EventArgs e)
    {
        string? pfad = PolarpunkteProtokoll.LetztesProtokoll();
        if (pfad == null)
        {
            MessageBox.Show(
                "Kein Polarpunkte-Protokoll gefunden." + Environment.NewLine +
                "Es wird beim Speichern erzeugt (sofern das Protokoll in den Projektoptionen aktiviert ist).",
                "Kein Protokoll", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = pfad,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ErrorLogger.Log("FormPolarpunkte.btnProtokoll_Click", ex);
            MessageBox.Show($"Protokoll konnte nicht geöffnet werden:{Environment.NewLine}{ex.Message}{Environment.NewLine}{Environment.NewLine}Datei: {pfad}",
                "Protokoll", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void SchreibeGeaenderteExzentrizitaeten()
    {
        if (_daten == null) return;
        var aenderungen = new List<(int, double, double)>();
        for (int i = 0; i < _np.Count; i++)
        {
            var (l, q) = _exzOriginal[i];
            if (Math.Abs(l - _np[i].ExzLaengs_m) > 1e-9 || Math.Abs(q - _np[i].ExzQuer_m) > 1e-9)
                aenderungen.Add((_np[i].ZeilenIndex, _np[i].ExzLaengs_m, _np[i].ExzQuer_m));
        }
        if (aenderungen.Count == 0) return;

        try
        {
            MessdatenCSV.SchreibeExzentrizitaeten(_daten.Pfad, aenderungen);
            _exzOriginal = _np.Select(m => (m.ExzLaengs_m, m.ExzQuer_m)).ToList();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log("FormPolarpunkte.SchreibeGeaenderteExzentrizitaeten", ex);
            MessageBox.Show($"Die geänderten Exzentrizitäten konnten nicht in die dat.csv geschrieben werden:\n{ex.Message}",
                "Polarpunkte", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ProtokolliereSpeichern(string sp, int anzahl)
    {
        var par = RechenparameterManager.Params;
        string gew = par.GewichtungNaheZiele
            ? $"ein (Ziel {par.ZentrierfehlerZiel_mm.ToString("F1", IC)} mm, Standpunkt {par.ZentrierfehlerStandpunkt_mm.ToString("F1", IC)} mm)"
            : "aus";

        ProtokollManager.Log("POLAR",
            $"SP={sp}  IH={LeseInstrHoehe().ToString("F3", IC)} m  Orientierung={_station!.Orientierung_gon.ToString("F4", IC)} gon  " +
            $"m={_massstab.Wert.ToString("F6", IC)}  r={_station.Redundanz}  Gewichtung nahe Ziele: {gew}  Punkte={anzahl}");
        foreach (string h in _hinweise) ProtokollManager.Log("POLAR", "Hinweis: " + h);

        for (int i = 0; i < _np.Count; i++)
        {
            var e = _ergebnisse[i];
            if (e == null) continue;
            ProtokollManager.Log("POLAR",
                $"Punkt {e.PunktNr}: R={e.R.ToString("F3", IC)} H={e.H.ToString("F3", IC)}" +
                (e.Ist3D ? $" Höhe={e.Hoehe.ToString("F3", IC)}" : "") +
                $"  eL={e.ExzLaengs_m.ToString("F3", IC)} eQ={e.ExzQuer_m.ToString("F3", IC)}");
        }
    }

    // ── KOR-CSV ───────────────────────────────────────────────────────────────

    private void btnKorCsv_Click(object? sender, EventArgs e)
    {
        Neuberechnen();
        var punkte = new List<KorPunkt>();
        for (int i = 0; i < _np.Count; i++)
        {
            var erg = _ergebnisse[i];
            if (erg == null) continue;
            punkte.Add(new KorPunkt(erg.PunktNr, erg.R, erg.H, erg.Ist3D ? erg.Hoehe : null, erg.Code));
        }
        if (punkte.Count == 0)
        {
            MessageBox.Show("Es sind keine berechneten Polarpunkte vorhanden.",
                "Als KOR-CSV speichern", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Title            = "Polarpunkte als KOR-CSV speichern",
            Filter           = "KOR-CSV (*-kor.csv)|*-kor.csv|Alle Dateien (*.*)|*.*",
            InitialDirectory = ProjektManager.ProjektVerzeichnis,
            FileName         = ProjektManager.ProjektName + "-kor.csv",
            OverwritePrompt  = false
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        bool erhalten = false;
        if (File.Exists(dlg.FileName))
        {
            var antwort = MessageBox.Show(
                "Die Datei ist bereits vorhanden." + Environment.NewLine + Environment.NewLine +
                "Ja:  Punkte ergänzen bzw. ersetzen (andere Punkte bleiben erhalten)" + Environment.NewLine +
                "Nein:  Datei neu anlegen (überschreiben)",
                "Als KOR-CSV speichern", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (antwort == DialogResult.Cancel) return;
            erhalten = antwort == DialogResult.Yes;
        }

        try
        {
            string sensor = ProjektManager.TachymeterModell == TachymeterModell.Manuell
                ? "" : ProjektManager.TachymeterModell.ToString();
            int gesamt = KorCsvSchreiber.Schreibe(dlg.FileName, ProjektManager.ProjektName, sensor, punkte, erhalten);
            ProtokollManager.Log("POLAR", $"KOR-CSV: {punkte.Count} Punkt(e) nach {Path.GetFileName(dlg.FileName)} ({gesamt} gesamt)");
            lblStatus.Text = $"{punkte.Count} Punkt(e) in {Path.GetFileName(dlg.FileName)} gespeichert ({gesamt} gesamt).";
        }
        catch (Exception ex)
        {
            ErrorLogger.Log("FormPolarpunkte.btnKorCsv_Click", ex);
            MessageBox.Show($"Die Datei konnte nicht geschrieben werden:\n{ex.Message}",
                "Als KOR-CSV speichern", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
