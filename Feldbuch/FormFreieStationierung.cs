namespace Feldbuch;

using System.Globalization;

public partial class FormFreieStationierung : Form
{
    private const int MAX_ROWS = 40;
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    // ── Auto-Match-Zustand ────────────────────────────────────────────────────
    private readonly PunktFinderKonfig _pfKonfig = new();

    // Overlay für Bestätigungs-Dialog
    private List<PunktFinderTreffer>? _pendingTreffer;
    private TachymeterMessung?         _pendingMessung;
    private Panel?                     _bestaetigungsPanel;

    public FormFreieStationierung()
    {
        InitializeComponent();
        InitGrid();
        LadeAnschlusspunkte();   // automatisch aus DXF-Viewer-Picks
        AktualisiereNeuerStandpunktButton();
        AktualisiereAutoMatchPanel();

        // Standpunktnummer laden: aktive Station → letzter Name → neues STP{N}
        if (!string.IsNullOrEmpty(StationsdatenManager.StandpunktNr))
            txtStandpunkt.Text = StationsdatenManager.StandpunktNr;
        else if (!string.IsNullOrEmpty(ProjektManager.LetzteStandpunktNr))
            txtStandpunkt.Text = ProjektManager.LetzteStandpunktNr;
        else
            txtStandpunkt.Text = $"STP{ProjektManager.StationierungZaehler}";

        // Instrumentenhöhe laden
        double iHLaden = StationsdatenManager.AktiveStation.InstrumentenHoehe > 0
            ? StationsdatenManager.InstrumentenHoehe
            : ProjektManager.InstrumentenHoehe;
        if (iHLaden > 0)
            nudInstHoehe.Value = (decimal)Math.Clamp(iHLaden, 0.0, 10.0);

        // Anschlusspunkte neu laden wenn DXF-Viewer etwas gespeichert hat
        FormDxfViewer.AnschlusspunktGeschrieben += OnAnschlusspunktGeschrieben;

        LiveWinkelSperre.Binden(this);     // keine Winkelabfragen des DXF-Viewers während der Messung hier
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        FormDxfViewer.AnschlusspunktGeschrieben -= OnAnschlusspunktGeschrieben;
        TachymeterMessungsCache.NeueVollmessung  -= OnNeueVollmessung;

        // Letzte Einstellungen für Neuaufruf merken
        string sp = txtStandpunkt.Text.Trim();
        if (!string.IsNullOrEmpty(sp))
            ProjektManager.LetzteStandpunktNr = sp;
        ProjektManager.InstrumentenHoehe = (double)nudInstHoehe.Value;
        ProjektManager.SpeichereOptionen();

        base.OnFormClosed(e);
    }

    private void OnAnschlusspunktGeschrieben()
    {
        if (InvokeRequired) { BeginInvoke(LadeAnschlusspunkte); return; }
        LadeAnschlusspunkte();
        AktualisiereAutoMatchPanel();
    }

    // ── Button "Neuer Standpunkt" – Sichtbarkeit und Aktion ──────────────────
    private void AktualisiereNeuerStandpunktButton()
    {
        // Sichtbar, wenn Anschlusspunkte.csv existiert
        btnNeuerStandpunkt.Visible = File.Exists(FormDxfViewer.AnschlusspunktePfad);
    }

    private void btnNeuerStandpunkt_Click(object? sender, EventArgs e)
    {
        string pfad = FormDxfViewer.AnschlusspunktePfad;

        // Bestehende Datei umbenennen (Datum + Uhrzeit im Dateinamen)
        if (File.Exists(pfad))
        {
            string zeitstempel = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string dir         = Path.GetDirectoryName(pfad)!;
            string archivPfad  = Path.Combine(dir, $"Anschlusspunkte_{zeitstempel}.csv");
            File.Move(pfad, archivPfad);
        }

        // Alle Grid-Zellen leeren
        foreach (DataGridViewRow row in dgvPunkte.Rows)
            foreach (DataGridViewCell cell in row.Cells)
                cell.Value = null;

        AktualisiereNeuerStandpunktButton();
    }

    // ── Anschlusspunkte aus DXF-Viewer automatisch laden ─────────────────────
    private void LadeAnschlusspunkte()
    {
        string pfad = FormDxfViewer.AnschlusspunktePfad;
        if (!File.Exists(pfad)) return;

        var lines = File.ReadAllLines(pfad)
                        .Skip(1)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();
        if (lines.Count == 0) return;

        // Grid leeren
        foreach (DataGridViewRow row in dgvPunkte.Rows)
            foreach (DataGridViewCell cell in row.Cells)
                cell.Value = null;

        for (int i = 0; i < Math.Min(lines.Count, MAX_ROWS); i++)
        {
            var parts = lines[i].Split(',');
            if (parts.Length < 8) continue;
            for (int j = 0; j < 8; j++)
                dgvPunkte.Rows[i].Cells[j].Value = parts[j].Trim();
        }
    }

    private void InitGrid()
    {
        string[] headers = { "PunktNr", "R", "H", "Höhe", "HZ [gon]", "V [gon]", "Strecke [m]", "Zielhöhe [m]" };
        foreach (string h in headers)
            dgvPunkte.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = h, Name = h });

        dgvPunkte.Rows.Add(MAX_ROWS);
        dgvPunkte.CellMouseClick += DgvPunkte_CellMouseClick;

        // Gültigkeit verfällt, sobald die Punktdaten geändert werden
        btnSpeichern.Enabled = false;
        dgvPunkte.CellValueChanged += (_, __) => btnSpeichern.Enabled = false;
    }

    private void DgvPunkte_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;
        string? pnr = dgvPunkte.Rows[e.RowIndex].Cells[0].Value?.ToString();
        if (string.IsNullOrWhiteSpace(pnr)) return;

        dgvPunkte.CurrentCell = dgvPunkte.Rows[e.RowIndex].Cells[e.ColumnIndex];
        var menu = new ContextMenuStrip();
        menu.Items.Add($"Zeile '{pnr}' entfernen", null, (_, __) => ZeileLeeren(e.RowIndex));
        menu.Show(dgvPunkte, dgvPunkte.PointToClient(Cursor.Position));
    }

    private void ZeileLeeren(int rowIdx)
    {
        // Zeile entfernen, Lücke schließen (leere Zeile hinten wieder anfügen)
        dgvPunkte.Rows.RemoveAt(rowIdx);
        while (dgvPunkte.Rows.Count < MAX_ROWS)
            dgvPunkte.Rows.Add();

        // Anschlusspunkte.csv (Quelle der Liste) und vorhandene Standpunkt-CSV neu schreiben
        try
        {
            var zeilen = GridZeilen();
            File.WriteAllLines(FormDxfViewer.AnschlusspunktePfad, zeilen, System.Text.Encoding.UTF8);

            string spPfad = StandpunktCsvPfad();
            if (File.Exists(spPfad))
                File.WriteAllLines(spPfad, zeilen, System.Text.Encoding.UTF8);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Fehler beim Schreiben der CSV-Dateien: " + ex.Message,
                "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string StandpunktCsvPfad()
    {
        string standpunktNr = txtStandpunkt.Text.Trim();
        string sicher = string.IsNullOrEmpty(standpunktNr)
            ? "Unbekannt"
            : string.Join("_", standpunktNr.Split(Path.GetInvalidFileNameChars()));
        return ProjektManager.IstGeladen
            ? ProjektManager.GetPfad($"Standpunkt-{sicher}.csv")
            : Path.Combine(AppPfade.Basis, $"Standpunkt-{sicher}.csv");
    }

    // Header + alle belegten Gridzeilen als CSV-Zeilen
    private List<string> GridZeilen()
    {
        var zeilen = new List<string> { "PunktNr,R,H,Hoehe,HZ,V,Strecke,Zielhoehe" };
        foreach (DataGridViewRow row in dgvPunkte.Rows)
        {
            string? pnr = row.Cells[0].Value?.ToString();
            if (string.IsNullOrWhiteSpace(pnr)) continue;

            var werte = new string[8];
            for (int j = 0; j < 8; j++)
                werte[j] = row.Cells[j].Value?.ToString()?.Trim() ?? "";
            zeilen.Add(string.Join(",", werte));
        }
        return zeilen;
    }

    private void btnTestdatenLaden_Click(object? sender, EventArgs e)
    {
        string pfad = AppPfade.Get("Muster.csv");
        if (!File.Exists(pfad))
        {
            MessageBox.Show("Muster.csv nicht gefunden.\nErwartet in: " + pfad,
                "Datei nicht gefunden", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Alle Zellen leeren
        foreach (DataGridViewRow row in dgvPunkte.Rows)
            foreach (DataGridViewCell cell in row.Cells)
                cell.Value = null;

        var lines = File.ReadAllLines(pfad).Skip(1).Take(MAX_ROWS).ToList();
        for (int i = 0; i < lines.Count; i++)
        {
            var parts = lines[i].Split(',');
            if (parts.Length < 8) continue;
            for (int j = 0; j < 8; j++)
                dgvPunkte.Rows[i].Cells[j].Value = parts[j].Trim();
        }
    }

    private void btnSpeichern_Click(object? sender, EventArgs e)
    {
        string pfad   = StandpunktCsvPfad();
        var    zeilen = GridZeilen();

        try
        {
            File.WriteAllLines(pfad, zeilen, System.Text.Encoding.UTF8);
            MessageBox.Show($"Gespeichert: {zeilen.Count - 1} Punkte\n{pfad}",
                "Gespeichert", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Fehler beim Speichern: " + ex.Message,
                "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnRechenparameter_Click(object? sender, EventArgs e)
    {
        using var form = new FormRechenparameter();
        if (form.ShowDialog(this) != DialogResult.OK) return;

        // Anzeigen mit ggf. geändertem Sigma-Faktor aktualisieren
        var erg = StationsdatenManager.AktuellesErgebnis;
        if (erg != null)
            RechenparameterManager.SchreibeSigmaProjektdaten(erg.s0_mm);
        AktualisiereAutoMatchPanel();
        FormDxfViewer.AktiveInstanz?.AktualisiereSignallampe();
    }

    private void btnBerechnen_Click(object? sender, EventArgs e)
    {
        var punkte = new List<StationierungsPunkt>();

        foreach (DataGridViewRow row in dgvPunkte.Rows)
        {
            string? pnr = row.Cells[0].Value?.ToString();
            if (string.IsNullOrWhiteSpace(pnr)) continue;

            if (!TryParseZeile(row, out var pkt, out string fehler))
            {
                MessageBox.Show($"Fehler in Zeile {row.Index + 1}: {fehler}",
                    "Eingabefehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            punkte.Add(pkt);
        }

        if (punkte.Count < 2)
        {
            MessageBox.Show("Mindestens 2 vollständige Zeilen erforderlich.",
                "Zu wenig Punkte", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            double iH         = (double)nudInstHoehe.Value;
            var    par        = RechenparameterManager.Params;
            var ergebnis      = FreieStationierungRechner.Berechnen(punkte, iH,
                                    freierMassstab:       par.FreierMassstab,
                                    berechnung3D:         par.Berechnung3D,
                                    fehlergrenzeMM_Hoehe: par.FehlergrenzeMM_Hoehe);

            // Ergebnis in Projektdaten protokollieren
            string standpunkt = txtStandpunkt.Text;
            ProjektdatenManager.SetValue("Freie Stationierung", "Standpunkt",         standpunkt);
            ProjektdatenManager.SetValue("Freie Stationierung", "R [m]",              ergebnis.R.ToString("F3", IC));
            ProjektdatenManager.SetValue("Freie Stationierung", "H [m]",              ergebnis.H.ToString("F3", IC));
            ProjektdatenManager.SetValue("Freie Stationierung", "Hoehe [m]",          ergebnis.Hoehe.ToString("F3", IC));
            ProjektdatenManager.SetValue("Freie Stationierung", "Orientierung [gon]", ergebnis.Orientierung_gon.ToString("F4", IC));
            ProjektdatenManager.SetValue("Freie Stationierung", "s0 [mm]",            ergebnis.s0_mm.ToString("F2", IC));
            RechenparameterManager.SchreibeSigmaProjektdaten(ergebnis.s0_mm);

            // Standpunkt in Feldbuchpunkte.json speichern
            FeldbuchpunkteManager.AddOrUpdate(new FeldbuchPunkt
            {
                PunktNr          = standpunkt,
                Typ              = "Standpunkt",
                R                = ergebnis.R,
                H                = ergebnis.H,
                Hoehe            = ergebnis.Hoehe,
                Orientierung_gon = ergebnis.Orientierung_gon,
                IstBerechnung3D  = ergebnis.Berechnung3D,
                Datum            = DateTime.Now.ToString("yyyy-MM-dd"),
                Quelle           = "Freie Stationierung"
            });

            // Ergebnis in StationsdatenManager eintragen (Auto-Match-Panel liest von dort)
            StationsdatenManager.SetzeErgebnisManuell(standpunkt, iH, punkte, ergebnis);

            using var formErg = new FormErgebnis(standpunkt, iH, ergebnis, punkte);
            formErg.ShowDialog(this);

            // Panel nach Berechnungsdialog aktualisieren (FormErgebnis kann s0 verfeinert haben)
            AktualisiereAutoMatchPanel();

            // Speichern erst bei gültiger Berechnung (≥ 3 grüne Quer- und ≥ 3 grüne Längsabweichungen)
            btnSpeichern.Enabled = FormErgebnis.IstGueltigeBerechnung(StationsdatenManager.AktuellesErgebnis);

            // Fehlerellipsen im DXF-Viewer aktualisieren (FormErgebnis kann Residuen geändert haben)
            var finalErg = StationsdatenManager.AktuellesErgebnis;
            if (finalErg != null)
                FormDxfViewer.AktiveInstanz?.BaueResidualOverlay(finalErg);

            // Standpunkt-Zähler hochsetzen wenn STP{N}-Muster, Feld auf nächste Nr. setzen
            if (standpunkt.StartsWith("STP", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(standpunkt.Substring(3), out int stpNr))
            {
                ProjektManager.StationierungZaehler = stpNr + 1;
                ProjektManager.SpeichereOptionen();
                txtStandpunkt.Text = $"STP{ProjektManager.StationierungZaehler}";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Berechnungsfehler: " + ex.Message,
                "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static bool TryParseZeile(DataGridViewRow row, out StationierungsPunkt pkt, out string fehler)
    {
        pkt = new StationierungsPunkt { PunktNr = row.Cells[0].Value!.ToString()! };
        fehler = "";
        double r, h, hoe, hz, v, s, zh;

        if (!TryParse(row.Cells[1].Value, out r))  { fehler = "R ungültig";         return false; }
        if (!TryParse(row.Cells[2].Value, out h))  { fehler = "H ungültig";         return false; }
        if (!TryParse(row.Cells[3].Value, out hoe)){ fehler = "Höhe ungültig";      return false; }
        if (!TryParse(row.Cells[4].Value, out hz)) { fehler = "HZ ungültig";        return false; }
        if (!TryParse(row.Cells[5].Value, out v))  { fehler = "V ungültig";         return false; }
        if (!TryParse(row.Cells[6].Value, out s))  { fehler = "Strecke ungültig";   return false; }
        if (!TryParse(row.Cells[7].Value, out zh)) { fehler = "Zielhöhe ungültig";  return false; }

        pkt.R = r; pkt.H = h; pkt.Hoehe = hoe;
        pkt.HZ = hz; pkt.V = v; pkt.Strecke = s; pkt.Zielhoehe = zh;
        return true;
    }

    private static bool TryParse(object? val, out double result)
    {
        string? s = val?.ToString()?.Replace(',', '.');
        return double.TryParse(s, NumberStyles.Any, IC, out result);
    }

    // ── Auto-Match-Panel ──────────────────────────────────────────────────────

    private void AktualisiereAutoMatchPanel()
    {
        var ergebnis  = StationsdatenManager.AktuellesErgebnis;
        double grenze = RechenparameterManager.Params.FehlergrenzeMM_s0AutoMatch;
        bool brauchbar = ergebnis != null
            && PunktFinder.IstBrauchbar(ergebnis.s0_mm, ergebnis.Redundanz, grenze);

        chkAutoMatch.Enabled = brauchbar;

        if (!brauchbar && chkAutoMatch.Checked)
            chkAutoMatch.Checked = false;

        if (ergebnis == null)
            lblAutoMatchStatus.Text = "Keine Stationierung vorhanden";
        else if (!brauchbar)
            lblAutoMatchStatus.Text = $"Station zu ungenau (s₀={ergebnis.s0_mm:F1} mm > {grenze:F0} mm) – Auto-Match gesperrt";
        else
            lblAutoMatchStatus.Text = $"Station: {RechenparameterManager.SigmaText(ergebnis.s0_mm)}  r={ergebnis.Redundanz}  " +
                                      $"– Auto-Match {(chkAutoMatch.Checked ? "aktiv" : "bereit")}";
    }

    private void chkAutoMatch_CheckedChanged(object? sender, EventArgs e)
    {
        if (chkAutoMatch.Checked)
            TachymeterMessungsCache.NeueVollmessung += OnNeueVollmessung;
        else
        {
            TachymeterMessungsCache.NeueVollmessung -= OnNeueVollmessung;
            AbbrechenBestaetigungsDialog();
        }
        AktualisiereAutoMatchPanel();
    }

    private void btnProtokoll_Click(object? sender, EventArgs e)
    {
        // Öffnet die zuletzt erzeugte Berechnungsprotokoll-RTF
        string verzeichnis = ProjektManager.IstGeladen
            ? ProjektManager.ProjektVerzeichnis
            : AppPfade.Basis;

        var rtfDateien = Directory.GetFiles(verzeichnis, "FreieStationierung_*.rtf")
                                  .OrderByDescending(f => f)
                                  .ToArray();

        if (rtfDateien.Length == 0)
        {
            MessageBox.Show(
                "Kein Berechnungsprotokoll gefunden.\nEs wird beim Übernehmen eines Ergebnisses im Berechnungsdialog erzeugt.",
                "Kein Protokoll", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = rtfDateien[0],
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show("Protokoll konnte nicht geöffnet werden:\n" + ex.Message,
                "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnAutoMatchLog_Click(object? sender, EventArgs e)
    {
        string standpunktNr = txtStandpunkt.Text.Trim();
        using var form = new FormAutoMatchProtokoll(standpunktNr);
        form.ShowDialog(this);
    }

    // ── Neue Messung empfangen (Auto-Match) ───────────────────────────────────

    private void OnNeueVollmessung(TachymeterMessung messung)
    {
        if (InvokeRequired) { BeginInvoke(() => OnNeueVollmessung(messung)); return; }
        FuehreAutoMatchDurch(messung);
    }

    private void FuehreAutoMatchDurch(TachymeterMessung messung)
    {
        var ergebnis = StationsdatenManager.AktuellesErgebnis;
        if (ergebnis == null) return;
        double grenze = RechenparameterManager.Params.FehlergrenzeMM_s0AutoMatch;
        if (!PunktFinder.IstBrauchbar(ergebnis.s0_mm, ergebnis.Redundanz, grenze)) return;
        if (!messung.IstVollmessung && chkDistanzPflicht.Checked) return;

        // Referenzpunkte aus Import (KOR/CSV) – das ist die primäre Suchbasis
        var importPunkte = ImportPunkteManager.Punkte;
        if (importPunkte.Count == 0)
        {
            lblAutoMatchStatus.Text = "Keine Referenzpunkte geladen (KOR/CSV importieren)";
            return;
        }

        var bereitsGemessen = GetBereitsGemessene();
        var par             = RechenparameterManager.Params;

        string standpunktNr = txtStandpunkt.Text.Trim();
        List<PunktFinderTreffer> treffer;
        double e_pred = 0, n_pred = 0, r_suche = 0;
        double d_m = messung.Schraegstrecke_m ?? 0;

        if (messung.IstVollmessung)
        {
            double d_h = Math.Sin(messung.V_gon!.Value * Math.PI / 200.0) * messung.Schraegstrecke_m!.Value;
            r_suche = PunktFinder.BerechneRadius(ergebnis.s0_mm,
                ergebnis.Redundanz + 2, d_h, _pfKonfig);

            (e_pred, n_pred) = PunktFinder.BerechnePriorPosition(
                ergebnis.R, ergebnis.H, ergebnis.Orientierung_gon,
                messung.Hz_gon!.Value, messung.V_gon.Value, messung.Schraegstrecke_m.Value,
                par.FreierMassstab ? ergebnis.Massstab : 1.0);

            treffer = PunktFinder.SucheNachPosition(
                e_pred, n_pred, r_suche, importPunkte, bereitsGemessen);
        }
        else
        {
            // Winkel-only (nur möglich wenn Distanz-Pflicht deaktiviert)
            double richtung = ((ergebnis.Orientierung_gon + messung.Hz_gon!.Value) % 400.0 + 400.0) % 400.0;
            treffer = PunktFinder.SucheNachRichtung(
                ergebnis.R, ergebnis.H, richtung, importPunkte, bereitsGemessen, _pfKonfig);
        }

        var ereignis = new AutoMatchEreignis(
            DateTime.Now,
            ergebnis.R, ergebnis.H, ergebnis.Hoehe,
            ergebnis.Orientierung_gon, ergebnis.s0_mm,
            ergebnis.Redundanz + 2,
            messung.Hz_gon ?? 0, messung.V_gon ?? 0, d_m,
            e_pred, n_pred, r_suche,
            treffer.Count,
            treffer.Count == 1 ? treffer[0].PunktNr : "",
            treffer.Count == 1 ? treffer[0].Abstand_m : -1,
            treffer.Count == 0 ? AutoMatchErgebnis.KeinTreffer :
            treffer.Count  > 1 ? AutoMatchErgebnis.MehrereTreffer :
            treffer[0].AutoMatch ? AutoMatchErgebnis.AutoMatch : AutoMatchErgebnis.Bestaetigt
        );

        if (treffer.Count == 0)
        {
            lblAutoMatchStatus.Text = "Kein DXF-Punkt gefunden";
            AutoMatchProtokoll.Schreiben(ereignis with { Ergebnis = AutoMatchErgebnis.KeinTreffer }, standpunktNr);
            return;
        }

        // Jeder Treffer – auch eindeutiger Auto-Match – erfordert manuelle Bestätigung
        _pendingTreffer = treffer;
        _pendingMessung = messung;
        AutoMatchProtokoll.Schreiben(ereignis, standpunktNr);

        lblAutoMatchStatus.Text = treffer.Count == 1 && treffer[0].AutoMatch
            ? $"AutoMatch: {treffer[0].PunktNr} – bitte bestätigen"
            : $"{treffer.Count} Kandidat(en) – bitte bestätigen";

        // DXF-Viewer-Overlay
        if (FormDxfViewer.AktiveInstanz != null)
        {
            if (messung.IstVollmessung)
                FormDxfViewer.AktiveInstanz.ZeigePunktFinderOverlay(e_pred, n_pred, r_suche, treffer);
            else
            {
                double richtung2 = ((ergebnis.Orientierung_gon + messung.Hz_gon!.Value) % 400.0 + 400.0) % 400.0;
                FormDxfViewer.AktiveInstanz.ZeigePunktFinderRichtung(
                    ergebnis.R, ergebnis.H, richtung2, _pfKonfig.WinkelToleranz_cc, treffer);
            }
        }

        ZeigeBestaetigungsPanel(treffer, messung, ergebnis, ereignis, standpunktNr);
    }

    private void UebernehmeTreffer(
        PunktFinderTreffer treffer,
        TachymeterMessung messung,
        StationierungsErgebnis ergebnis,
        AutoMatchEreignis ereignis,
        string standpunktNr)
    {
        var punkt = TrefferZuPunkt(treffer, messung);
        FormDxfViewer.SchreibeAnschlusspunkt(punkt);

        var jsonPunkt = new AutoMatchPunkt(
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            standpunktNr, treffer.PunktNr,
            treffer.R, treffer.H, punkt.Hoehe,
            messung.Hz_gon ?? 0, messung.V_gon ?? 0, messung.Schraegstrecke_m ?? 0,
            ereignis.E_pred, ereignis.N_pred, ereignis.Radius_m,
            treffer.Abstand_m,
            treffer.AutoMatch ? "AutoMatch" : "Bestaetigt",
            "DXF");
        AutoMatchPunkte.PunktHinzufuegen(jsonPunkt, standpunktNr);

        var protokollEreignis = ereignis with
        {
            GewaehlterPunkt  = treffer.PunktNr,
            AbstandGewählt_m = treffer.Abstand_m,
            Ergebnis         = treffer.AutoMatch ? AutoMatchErgebnis.AutoMatch : AutoMatchErgebnis.Bestaetigt
        };
        AutoMatchProtokoll.Schreiben(protokollEreignis, standpunktNr);

        lblAutoMatchStatus.Text = treffer.AutoMatch
            ? $"✓ AutoMatch: {treffer.PunktNr}  (Abst.={treffer.Abstand_m * 1000:F0} mm)"
            : $"✓ Bestätigt: {treffer.PunktNr}  (Abst.={treffer.Abstand_m * 1000:F0} mm)";

        // Grid kurz grün hervorheben
        LadeAnschlusspunkte();
        MarkiereLetztePunktzeile(Color.FromArgb(180, 240, 190), standpunktNr);
    }

    private static StationierungsPunkt TrefferZuPunkt(
        PunktFinderTreffer t, TachymeterMessung m)
    {
        // Höhe aus Tachymetermessung wenn vorhanden
        double hoehe = 0;
        if (StationsdatenManager.AktuellesErgebnis is { } erg
            && m.IstVollmessung)
        {
            double dh_vertikal = Math.Cos(m.V_gon!.Value * Math.PI / 200.0)
                               * m.Schraegstrecke_m!.Value;
            double iH = StationsdatenManager.InstrumentenHoehe;
            double zH = m.Zielhoehe_m ?? 0;
            hoehe = erg.Hoehe + iH + dh_vertikal - zH;
        }

        return new StationierungsPunkt
        {
            PunktNr   = t.PunktNr,
            R         = t.R,
            H         = t.H,
            Hoehe     = hoehe,
            HZ        = m.Hz_gon  ?? 0,
            V         = m.V_gon   ?? 0,
            Strecke   = m.Schraegstrecke_m ?? 0,
            Zielhoehe = m.Zielhoehe_m ?? 0
        };
    }

    // ── Bestätigungs-Panel ────────────────────────────────────────────────────

    private void ZeigeBestaetigungsPanel(
        List<PunktFinderTreffer> treffer,
        TachymeterMessung messung,
        StationierungsErgebnis ergebnis,
        AutoMatchEreignis ereignis,
        string standpunktNr)
    {
        AbbrechenBestaetigungsDialog();

        bool istAutoMatch = treffer.Count == 1 && treffer[0].AutoMatch;

        var p = new Panel
        {
            Location    = new Point(20, 556),
            Size        = new Size(940, 52),
            BackColor   = istAutoMatch ? Color.FromArgb(210, 245, 215) : Color.FromArgb(255, 248, 200),
            BorderStyle = BorderStyle.FixedSingle
        };

        string kandidatenText = istAutoMatch
            ? $"AutoMatch: \"{treffer[0].PunktNr}\"  R={treffer[0].R:F3}  H={treffer[0].H:F3}" +
              $"  Abstand={treffer[0].Abstand_m * 1000:F0} mm"
            : treffer.Count == 1
                ? $"Vorschlag: \"{treffer[0].PunktNr}\"  R={treffer[0].R:F3}  H={treffer[0].H:F3}" +
                  $"  Abstand={treffer[0].Abstand_m * 1000:F0} mm"
                : $"{treffer.Count} Kandidaten – bitte auswählen";

        var lbl = new Label
        {
            Text     = kandidatenText,
            Location = new Point(8, 6),
            Size     = new Size(600, 20),
            Font     = new Font("Segoe UI", 9F)
        };

        var btnOK = new Button
        {
            Text      = "Übernehmen ✓",
            Location  = new Point(610, 8),
            Size      = new Size(120, 32),
            Font      = new Font("Segoe UI", 9F, FontStyle.Bold),
            BackColor = Color.FromArgb(30, 140, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnOK.Click += (_, _) =>
        {
            var gewählt = treffer[0];   // bei mehreren: immer nächsten nehmen
            UebernehmeTreffer(gewählt, messung, ergebnis,
                ereignis with { Ergebnis = AutoMatchErgebnis.Bestaetigt }, standpunktNr);
            AbbrechenBestaetigungsDialog();
            FormDxfViewer.AktiveInstanz?.EntfernePunktFinderOverlay();
        };

        var btnAbl = new Button
        {
            Text      = "Ablehnen ✗",
            Location  = new Point(738, 8),
            Size      = new Size(100, 32),
            Font      = new Font("Segoe UI", 9F),
            BackColor = Color.FromArgb(180, 40, 30),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnAbl.Click += (_, _) =>
        {
            AutoMatchProtokoll.Schreiben(
                ereignis with { Ergebnis = AutoMatchErgebnis.Abgelehnt }, standpunktNr);
            lblAutoMatchStatus.Text = $"Abgelehnt: {treffer[0].PunktNr}";
            AbbrechenBestaetigungsDialog();
            FormDxfViewer.AktiveInstanz?.EntfernePunktFinderOverlay();
        };

        p.Controls.AddRange(new Control[] { lbl, btnOK, btnAbl });
        Controls.Add(p);
        p.BringToFront();
        _bestaetigungsPanel = p;

        // pnlAutoMatch hinter Bestätigungspanel verstecken
        pnlAutoMatch.Visible = false;
    }

    private void AbbrechenBestaetigungsDialog()
    {
        if (_bestaetigungsPanel != null)
        {
            Controls.Remove(_bestaetigungsPanel);
            _bestaetigungsPanel.Dispose();
            _bestaetigungsPanel = null;
        }
        pnlAutoMatch.Visible  = true;
        _pendingTreffer        = null;
        _pendingMessung        = null;
    }

    // ── Hilfsmethoden ─────────────────────────────────────────────────────────

    private HashSet<string> GetBereitsGemessene()
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (DataGridViewRow row in dgvPunkte.Rows)
        {
            string? pnr = row.Cells[0].Value?.ToString();
            if (!string.IsNullOrWhiteSpace(pnr))
                result.Add(pnr);
        }
        // Auch Auto-Match-JSON berücksichtigen
        foreach (var pnr in AutoMatchPunkte.GetBereitsGematchte())
            result.Add(pnr);
        return result;
    }

    private void MarkiereLetztePunktzeile(Color farbe, string standpunktNr)
    {
        // Letzte nicht-leere Zeile grün hervorheben, dann nach 1,5 s zurücksetzen
        for (int i = dgvPunkte.Rows.Count - 1; i >= 0; i--)
        {
            if (dgvPunkte.Rows[i].Cells[0].Value?.ToString() is { Length: > 0 } pnr)
            {
                dgvPunkte.Rows[i].DefaultCellStyle.BackColor = farbe;
                var timer = new System.Windows.Forms.Timer { Interval = 1500 };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    if (i < dgvPunkte.Rows.Count)
                        dgvPunkte.Rows[i].DefaultCellStyle.BackColor = Color.Empty;
                    timer.Dispose();
                };
                timer.Start();
                break;
            }
        }
    }
}
