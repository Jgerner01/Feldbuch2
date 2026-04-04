namespace Feldbuch;

public partial class FormErgebnis : Form
{
    private readonly string                    _standpunkt;
    private readonly double                    _iH;
    private readonly List<StationierungsPunkt> _allePunkte;
    private bool                               _initializingGrid;
    private StationierungsErgebnis?            _letzteErgebnis;
    private bool                               _protokollGeschrieben;

    public FormErgebnis(string standpunkt, double iH,
                        StationierungsErgebnis erg,
                        List<StationierungsPunkt> allePunkte)
    {
        InitializeComponent();
        _standpunkt = standpunkt;
        _iH         = iH;
        _allePunkte = allePunkte;

        // Reihenfolge der Spalten:
        // PunktNr | StreckeH | vWinkel | vQuer | [Hz] | vLängs | [Str] | vHoehe | [Hoe]
        _initializingGrid = true;
        foreach (var p in _allePunkte)
        {
            bool hoeAktiv = p.Hoehe != 0.0;   // Hoehe == 0 → automatisch deaktiviert
            dgvResiduen.Rows.Add(p.PunktNr, "", "", "", true, "", true, "", hoeAktiv);
        }
        _initializingGrid = false;

        AktualisiereAnzeige(erg);
        _letzteErgebnis = erg;

        // Falls Höhenpunkte automatisch deaktiviert wurden: Neuberechnung mit korrekter Aktivierung
        if (RechenparameterManager.Params.Berechnung3D && _allePunkte.Any(p => p.Hoehe == 0.0))
            Neuberechnen();
    }

    // ── Protokoll schreiben ───────────────────────────────────────────────────
    private void SchreibeProtokoll()
    {
        if (_letzteErgebnis == null || _protokollGeschrieben) return;
        _protokollGeschrieben = true;
        var (aktHz, aktStr, aktHoe) = LesAktivierung();
        FreieStationierungProtokoll.Schreiben(
            _letzteErgebnis, _allePunkte,
            aktHz, aktStr, aktHoe,
            _standpunkt, _iH);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        SchreibeProtokoll();
    }

    private (bool[] aktHz, bool[] aktStr, bool[] aktHoe) LesAktivierung()
    {
        int    n      = _allePunkte.Count;
        bool[] aktHz  = new bool[n];
        bool[] aktStr = new bool[n];
        bool[] aktHoe = new bool[n];
        for (int i = 0; i < n; i++)
        {
            aktHz[i]  = (bool)(dgvResiduen.Rows[i].Cells["colAktivHz"].Value    ?? true);
            aktStr[i] = (bool)(dgvResiduen.Rows[i].Cells["colAktivStr"].Value   ?? true);
            aktHoe[i] = (bool)(dgvResiduen.Rows[i].Cells["colAktivHoehe"].Value ?? true);
        }
        return (aktHz, aktStr, aktHoe);
    }

    // ── Ergebnisanzeige aktualisieren ─────────────────────────────────────────
    private void AktualisiereAnzeige(StationierungsErgebnis erg)
    {
        // ── Kopfdaten ──────────────────────────────────────────────────────────
        lblStandpunkt.Text = $"Standpunktnummer:        {_standpunkt}";
        lblInstHoehe.Text  = $"Instrumentenhöhe:        {_iH:F3} m";
        lblR.Text          = $"R  (Rechtswert):         {erg.R:F3} m";
        lblH.Text          = $"H  (Hochwert):           {erg.H:F3} m";
        lblHoehe.Text      = erg.Berechnung3D
            ? $"Höhe:                    {erg.Hoehe:F3} m"
            : $"Höhe:                    –  (keine aktiven Höhenmessungen)";
        lblOrient.Text     = $"Orientierungsunbekannte: {erg.Orientierung_gon:F4} gon";
        lblMassstab.Text   = $"Maßstab (Helmert):       {erg.Massstab:F6}" +
                             (RechenparameterManager.Params.FreierMassstab
                                ? "" : "  [fixiert]");
        lblS0.Text   = erg.Redundanz > 0
            ? $"Standardabw. s\u2080:        {erg.s0_mm:F2} mm   (Redundanz r = {erg.Redundanz})"
            : $"Standardabw. s\u2080:        –   (Redundanz r = 0, eindeutig bestimmt)";
        lblIter.Text = _allePunkte.Count == 2
            ? "Methode:                 Ähnlichkeitstransformation  (2 Punkte, Direktlösung)"
            : $"Iterationen:             {erg.Iterationen}" +
              (erg.Konvergiert ? "  (konvergiert)" : "  !! NICHT konvergiert !!");

        // ── Residuen in Tabelle schreiben ─────────────────────────────────────
        var resDict = erg.Residuen.ToDictionary(r => r.PunktNr);
        var p       = RechenparameterManager.Params;

        for (int i = 0; i < dgvResiduen.Rows.Count; i++)
        {
            var    row = dgvResiduen.Rows[i];
            string pNr = _allePunkte[i].PunktNr;

            if (resDict.TryGetValue(pNr, out var res))
            {
                row.Cells["StreckeH"].Value = $"{res.StreckeH:F3}";

                // v Winkel [cc] – Querabweichung als Winkelmaß
                if (res.RichtungAktiv && !double.IsNaN(res.vWinkel_cc))
                {
                    row.Cells["vWinkel"].Value = $"{res.vWinkel_cc:+0.0;-0.0;0.0}";
                    AmpelFarbe(row.Cells["vWinkel"], Math.Abs(res.vWinkel_cc), p.FehlergrenzCC_Winkel);
                }
                else
                {
                    row.Cells["vWinkel"].Value = "–";
                    row.Cells["vWinkel"].Style.BackColor = Color.LightGray;
                }

                // v Quer [mm] – Querabweichung in Millimeter
                if (res.RichtungAktiv && !double.IsNaN(res.vQuer_mm))
                {
                    row.Cells["vQuer"].Value = $"{res.vQuer_mm:+0.0;-0.0;0.0}";
                    AmpelFarbe(row.Cells["vQuer"], Math.Abs(res.vQuer_mm), p.FehlergrenzeMM_Strecke);
                }
                else
                {
                    row.Cells["vQuer"].Value = "–";
                    row.Cells["vQuer"].Style.BackColor = Color.LightGray;
                }

                // v Längs [mm] – Längsabweichung (Streckenresiduum)
                if (res.StreckeAktiv && !double.IsNaN(res.vStrecke_mm))
                {
                    row.Cells["vLängs"].Value = $"{res.vStrecke_mm:+0.0;-0.0;0.0}";
                    AmpelFarbe(row.Cells["vLängs"], Math.Abs(res.vStrecke_mm), p.FehlergrenzeMM_Strecke);
                }
                else
                {
                    row.Cells["vLängs"].Value = "–";
                    row.Cells["vLängs"].Style.BackColor = Color.LightGray;
                }

                // v Höhe [mm]
                if (res.HoeheAktiv && !double.IsNaN(res.vHoehe_mm))
                {
                    row.Cells["vHoehe"].Value = $"{res.vHoehe_mm:+0.0;-0.0;0.0}";
                    AmpelFarbe(row.Cells["vHoehe"], Math.Abs(res.vHoehe_mm), p.FehlergrenzeMM_Hoehe);
                }
                else
                {
                    row.Cells["vHoehe"].Value = "–";
                    row.Cells["vHoehe"].Style.BackColor = Color.LightGray;
                }
            }
            else
            {
                row.Cells["StreckeH"].Value = "–";
                row.Cells["vWinkel"].Value  = "–";
                row.Cells["vQuer"].Value    = "–";
                row.Cells["vLängs"].Value   = "–";
                row.Cells["vHoehe"].Value   = "–";
                row.Cells["vWinkel"].Style.BackColor = Color.LightGray;
                row.Cells["vQuer"].Style.BackColor   = Color.LightGray;
                row.Cells["vLängs"].Style.BackColor  = Color.LightGray;
                row.Cells["vHoehe"].Style.BackColor  = Color.LightGray;
            }
        }
    }

    // ── Neuberechnung (manuell oder bei Aktivierungsänderung) ─────────────────
    private void OnAktivierungGeaendert()
    {
        if (_initializingGrid) return;
        Neuberechnen();
    }

    private void Neuberechnen()
    {
        int    n      = _allePunkte.Count;
        bool[] aktHz  = new bool[n];
        bool[] aktStr = new bool[n];
        bool[] aktHoe = new bool[n];

        for (int i = 0; i < n; i++)
        {
            aktHz[i]  = dgvResiduen.Rows[i].Cells["colAktivHz"].Value    is true;
            aktStr[i] = dgvResiduen.Rows[i].Cells["colAktivStr"].Value   is true;
            aktHoe[i] = dgvResiduen.Rows[i].Cells["colAktivHoehe"].Value is true;
        }

        try
        {
            var par = RechenparameterManager.Params;
            var erg = FreieStationierungRechner.Berechnen(
                _allePunkte, _iH,
                freierMassstab:       par.FreierMassstab,
                aktivRichtung:        aktHz,
                aktivStrecke:         aktStr,
                aktivHoehe:           aktHoe,
                berechnung3D:         par.Berechnung3D,
                fehlergrenzeMM_Hoehe: par.FehlergrenzeMM_Hoehe);

            FeldbuchpunkteManager.AddOrUpdate(new FeldbuchPunkt
            {
                PunktNr          = _standpunkt,
                Typ              = "Standpunkt",
                R                = erg.R,
                H                = erg.H,
                Hoehe            = erg.Hoehe,
                Orientierung_gon = erg.Orientierung_gon,
                IstBerechnung3D  = erg.Berechnung3D,
                Datum            = DateTime.Now.ToString("yyyy-MM-dd"),
                Quelle           = "Freie Stationierung"
            });

            AktualisiereAnzeige(erg);
            _letzteErgebnis = erg;   // für Protokoll beim Schließen

            var ic = System.Globalization.CultureInfo.InvariantCulture;
            ProjektdatenManager.SetValue("Freie Stationierung", "Standpunkt",         _standpunkt);
            ProjektdatenManager.SetValue("Freie Stationierung", "R [m]",              erg.R.ToString("F3", ic));
            ProjektdatenManager.SetValue("Freie Stationierung", "H [m]",              erg.H.ToString("F3", ic));
            ProjektdatenManager.SetValue("Freie Stationierung", "Hoehe [m]",          erg.Hoehe.ToString("F3", ic));
            ProjektdatenManager.SetValue("Freie Stationierung", "Orientierung [gon]", erg.Orientierung_gon.ToString("F4", ic));
            ProjektdatenManager.SetValue("Freie Stationierung", "s0 [mm]",            erg.s0_mm.ToString("F2", ic));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void btnNeuBerechnen_Click(object? sender, EventArgs e) => Neuberechnen();

    // ── Ampel-Färbung ─────────────────────────────────────────────────────────
    // Grün:  |v| ≤ (Fehlergrenze − 1)
    // Gelb:  |v| ∈ (Fehlergrenze − 1 … Fehlergrenze + 5]
    // Rot:   |v| > (Fehlergrenze + 5)
    static void AmpelFarbe(DataGridViewCell cell, double absVal, double limit)
    {
        if (absVal <= limit - 1.0)
            cell.Style.BackColor = Color.FromArgb(180, 230, 160);   // grün
        else if (absVal <= limit + 5.0)
            cell.Style.BackColor = Color.FromArgb(255, 230, 100);   // gelb
        else
            cell.Style.BackColor = Color.FromArgb(255, 110, 100);   // rot
    }

    private void btnSchliessen_Click(object? sender, EventArgs e) => Close();
}
