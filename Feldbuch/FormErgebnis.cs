namespace Feldbuch;

public partial class FormErgebnis : Form
{
    private readonly string                    _standpunkt;
    private readonly double                    _iH;
    private readonly List<StationierungsPunkt> _allePunkte;

    public FormErgebnis(string standpunkt, double iH,
                        StationierungsErgebnis erg,
                        List<StationierungsPunkt> allePunkte)
    {
        InitializeComponent();
        _standpunkt = standpunkt;
        _iH         = iH;
        _allePunkte = allePunkte;

        // Reihenfolge der Spalten: PunktNr | StreckeH | vWinkel | colAktivHz | vStrecke | colAktivStr | vHoehe
        foreach (var p in _allePunkte)
            dgvResiduen.Rows.Add(p.PunktNr, "", "", true, "", true, "");

        AktualisiereAnzeige(erg);
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
            : $"Höhe:                    (2D – nicht berechnet)";
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
        var p = RechenparameterManager.Params;

        for (int i = 0; i < dgvResiduen.Rows.Count; i++)
        {
            var row     = dgvResiduen.Rows[i];
            string pNr  = _allePunkte[i].PunktNr;

            if (resDict.TryGetValue(pNr, out var res))
            {
                row.Cells["StreckeH"].Value = $"{res.StreckeH:F3}";

                // Richtung
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

                // Strecke
                if (res.StreckeAktiv && !double.IsNaN(res.vStrecke_mm))
                {
                    row.Cells["vStrecke"].Value = $"{res.vStrecke_mm:+0.0;-0.0;0.0}";
                    AmpelFarbe(row.Cells["vStrecke"], Math.Abs(res.vStrecke_mm), p.FehlergrenzeMM_Strecke);
                }
                else
                {
                    row.Cells["vStrecke"].Value = "–";
                    row.Cells["vStrecke"].Style.BackColor = Color.LightGray;
                }

                // Höhe (hängt an Strecke)
                if (res.StreckeAktiv && !double.IsNaN(res.vHoehe_mm))
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
                // Punkt komplett inaktiv
                row.Cells["StreckeH"].Value = "–";
                row.Cells["vWinkel"].Value  = "–";
                row.Cells["vStrecke"].Value = "–";
                row.Cells["vHoehe"].Value   = "–";
                row.Cells["vWinkel"].Style.BackColor  = Color.LightGray;
                row.Cells["vStrecke"].Style.BackColor = Color.LightGray;
                row.Cells["vHoehe"].Style.BackColor   = Color.LightGray;
            }
        }
    }

    // ── "Neu berechnen" ───────────────────────────────────────────────────────
    private void btnNeuBerechnen_Click(object? sender, EventArgs e)
    {
        int n = _allePunkte.Count;
        bool[] aktHz  = new bool[n];
        bool[] aktStr = new bool[n];

        for (int i = 0; i < n; i++)
        {
            aktHz[i]  = dgvResiduen.Rows[i].Cells["colAktivHz"].Value  is true;
            aktStr[i] = dgvResiduen.Rows[i].Cells["colAktivStr"].Value is true;
        }

        try
        {
            var par  = RechenparameterManager.Params;
            var erg = FreieStationierungRechner.Berechnen(
                _allePunkte, _iH,
                freierMassstab:       par.FreierMassstab,
                aktivRichtung:        aktHz,
                aktivStrecke:         aktStr,
                berechnung3D:         par.Berechnung3D,
                fehlergrenzeMM_Hoehe: par.FehlergrenzeMM_Hoehe);

            if (!string.IsNullOrEmpty(erg.WarnungHoehe))
                MessageBox.Show(erg.WarnungHoehe,
                    "Warnung – Höhenresiduen", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            // Standpunkt in Feldbuchpunkte.json aktualisieren
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
                Quelle           = "Freie Stationierung (Neuberechnung)"
            });

            AktualisiereAnzeige(erg);
            btnNeuBerechnen.Enabled = false;

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

    // ── Ampel-Färbung ─────────────────────────────────────────────────────────
    // Grün:  |v| ≤ (Fehlergrenze − 1)    → unauffällig
    // Gelb:  |v| ∈ (Fehlergrenze − 1 … Fehlergrenze + 5]   → Warnung
    // Rot:   |v| > (Fehlergrenze + 5)    → Überschreitung
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
