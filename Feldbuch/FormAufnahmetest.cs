namespace Feldbuch;

using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

// ══════════════════════════════════════════════════════════════════════════════
// FormAufnahmetest  –  Koordinatenaufnahme und motorisches Anfahren (GeoCOM)
//
// Funktion 1 – Aufnahme:
//   Messung auslösen (TMC_DoMeasure → TMC_GetSimpleMea), lokale Koordinaten
//   berechnen (Instrument = Ursprung 0/0) und in bearbeitbarer Tabelle merken.
//
// Funktion 2 – Anfahren:
//   Ausgewählte Tabellenzeile → Hz/V zurückrechnen → AUT_MakeHzV (RPC 6007).
//   Format: %R1Q,6007,0:{Hz},{V},0,0,0  (PosMode=0=Normal, ATRMode=0=Aus)
//   Watchdog-Timer (30 s) schützt vor hängendem Gerät.
//
// Koordinatensystem lokal:
//   X = Horizontalstrecke × sin(Hz_rad)   [Ost]
//   Y = Horizontalstrecke × cos(Hz_rad)   [Nord]
//   Z = Schrägstrecke     × cos(V_rad)    [Höhe]
// ══════════════════════════════════════════════════════════════════════════════
public partial class FormAufnahmetest : Form
{
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;
    private static readonly JsonSerializerOptions JsonOpt = new() { WriteIndented = true };

    // ── Zustandsmaschinen ─────────────────────────────────────────────────────
    private enum MessZustand   { Bereit, WarteMessen, WarteErgebnis }
    private enum AnfahrZustand
    {
        Bereit,
        WarteAktPos,         // liest Ist-Hz/V vor der Bewegung
        WartePositionierung, // wartet auf RPC 6007 Antwort (Relativbewegung)
        WarteWinkel          // liest Ist-Hz/V zur Verifikation nach der Bewegung
    }

    private MessZustand   _messZustand   = MessZustand.Bereit;
    private AnfahrZustand _anfahrZustand = AnfahrZustand.Bereit;
    private int           _letzterRpc    = 0;

    // Zielposition für die Relativbewegung
    private double _zielHzGon = 0;
    private double _zielVGon  = 0;

    // Watchdog für AUT_MakePositioning (blockiert bis zu 30 s)
    private readonly System.Windows.Forms.Timer _watchdog = new() { Interval = 30_000 };

    // ── Datenpuffer ───────────────────────────────────────────────────────────
    private readonly StringBuilder _zeilenPuffer = new();

    // ── Koordinatentabelle ────────────────────────────────────────────────────
    private readonly DataTable _tabelle = new();

    // ── Dateipfade ────────────────────────────────────────────────────────────
    private static string DateiPfad =>
        ProjektManager.IstGeladen
            ? ProjektManager.GetPfad("aufnahmetest.json")
            : Path.Combine(AppPfade.Basis, "aufnahmetest.json");

    private static string LogPfad =>
        ProjektManager.IstGeladen
            ? ProjektManager.GetPfad("aufnahmetest.log")
            : Path.Combine(AppPfade.Basis, "aufnahmetest.log");

    private static void Logge(string text)
    {
        try
        {
            File.AppendAllText(LogPfad,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {text}{Environment.NewLine}",
                Encoding.UTF8);
        }
        catch { }
    }

    // ── JSON-Hilfsklasse ──────────────────────────────────────────────────────
    private sealed class JsonZeile
    {
        [JsonPropertyName("nr")]     public int    Nr     { get; set; }
        [JsonPropertyName("punktNr")]public string PunktNr{ get; set; } = "";
        [JsonPropertyName("code")]   public string Code   { get; set; } = "";
        [JsonPropertyName("x")]      public double X      { get; set; }
        [JsonPropertyName("y")]      public double Y      { get; set; }
        [JsonPropertyName("z")]      public double Z      { get; set; }
        [JsonPropertyName("d")]      public double D      { get; set; }
        [JsonPropertyName("hz")]     public double Hz     { get; set; }
        [JsonPropertyName("v")]      public double V      { get; set; }
    }

    // ── Konstruktor ───────────────────────────────────────────────────────────
    public FormAufnahmetest()
    {
        InitializeComponent();

        BaueTabelle();
        dgvPunkte.DataSource = _tabelle;
        KonfiguriereSpalten();

        TachymeterVerbindung.DatenEmpfangen += OnDatenEmpfangen;
        _watchdog.Tick += OnWatchdogTick;

        LadeKoordinaten();
        AktualisiereUI();
    }

    // ── Tabelle aufbauen ──────────────────────────────────────────────────────
    private void BaueTabelle()
    {
        _tabelle.Columns.Add("Nr",      typeof(int));
        _tabelle.Columns.Add("PunktNr", typeof(string));
        _tabelle.Columns.Add("Code",    typeof(string));
        _tabelle.Columns.Add("X_m",     typeof(double));
        _tabelle.Columns.Add("Y_m",     typeof(double));
        _tabelle.Columns.Add("Z_m",     typeof(double));
        _tabelle.Columns.Add("D_m",     typeof(double));
        _tabelle.Columns.Add("Hz_gon",  typeof(double));
        _tabelle.Columns.Add("V_gon",   typeof(double));
    }

    private void KonfiguriereSpalten()
    {
        if (dgvPunkte.Columns.Count == 0) return;

        dgvPunkte.Columns["Nr"]!.Width        = 38;
        dgvPunkte.Columns["Nr"]!.ReadOnly     = true;
        dgvPunkte.Columns["PunktNr"]!.Width   = 70;
        dgvPunkte.Columns["Code"]!.Width      = 60;
        dgvPunkte.Columns["X_m"]!.Width       = 85;
        dgvPunkte.Columns["Y_m"]!.Width       = 85;
        dgvPunkte.Columns["Z_m"]!.Width       = 75;
        dgvPunkte.Columns["D_m"]!.Width       = 70;
        dgvPunkte.Columns["D_m"]!.ReadOnly    = true;
        dgvPunkte.Columns["Hz_gon"]!.Width    = 85;
        dgvPunkte.Columns["Hz_gon"]!.ReadOnly = true;
        dgvPunkte.Columns["V_gon"]!.Width     = 75;
        dgvPunkte.Columns["V_gon"]!.ReadOnly  = true;

        dgvPunkte.Columns["X_m"]!.DefaultCellStyle.Format    = "F4";
        dgvPunkte.Columns["Y_m"]!.DefaultCellStyle.Format    = "F4";
        dgvPunkte.Columns["Z_m"]!.DefaultCellStyle.Format    = "F4";
        dgvPunkte.Columns["D_m"]!.DefaultCellStyle.Format    = "F3";
        dgvPunkte.Columns["Hz_gon"]!.DefaultCellStyle.Format = "F4";
        dgvPunkte.Columns["V_gon"]!.DefaultCellStyle.Format  = "F4";
    }

    // ── Speichern / Laden ─────────────────────────────────────────────────────
    private void SpeichereKoordinaten()
    {
        try
        {
            var liste = new List<JsonZeile>(_tabelle.Rows.Count);
            foreach (DataRow r in _tabelle.Rows)
                liste.Add(new JsonZeile
                {
                    Nr      = (int)   r["Nr"],
                    PunktNr = (string)r["PunktNr"],
                    Code    = (string)r["Code"],
                    X       = (double)r["X_m"],
                    Y       = (double)r["Y_m"],
                    Z       = (double)r["Z_m"],
                    D       = (double)r["D_m"],
                    Hz      = (double)r["Hz_gon"],
                    V       = (double)r["V_gon"]
                });
            File.WriteAllText(DateiPfad,
                JsonSerializer.Serialize(liste, JsonOpt), Encoding.UTF8);
        }
        catch { /* Speicherfehler still ignorieren */ }
    }

    private void LadeKoordinaten()
    {
        try
        {
            if (!File.Exists(DateiPfad)) return;
            var json  = File.ReadAllText(DateiPfad, Encoding.UTF8);
            var liste = JsonSerializer.Deserialize<List<JsonZeile>>(json);
            if (liste == null) return;

            _tabelle.Rows.Clear();
            foreach (var z in liste)
                _tabelle.Rows.Add(z.Nr, z.PunktNr, z.Code, z.X, z.Y, z.Z, z.D, z.Hz, z.V);
        }
        catch { /* Defekte Datei → leer starten */ }
    }

    // ── Messung auslösen ──────────────────────────────────────────────────────
    private void btnMessen_Click(object? sender, EventArgs e)
    {
        if (_messZustand != MessZustand.Bereit || _anfahrZustand != AnfahrZustand.Bereit) return;
        if (!TachymeterVerbindung.IstVerbunden)
        {
            lblMessInfo.Text      = "Kein Tachymeter verbunden.";
            lblMessInfo.ForeColor = Color.FromArgb(200, 80, 40);
            return;
        }

        try
        {
            lblMessInfo.Text      = "Messung läuft …";
            lblMessInfo.ForeColor = Color.FromArgb(200, 160, 40);
            _messZustand = MessZustand.WarteMessen;
            _letzterRpc  = GeoCOMParser.RPC_TMC_DoMeasure;
            TachymeterVerbindung.GeoCOM_Senden("%R1Q,2008,0:1,0");
            AktualisiereUI();
        }
        catch (Exception ex)
        {
            lblMessInfo.Text      = $"Fehler: {ex.Message}";
            lblMessInfo.ForeColor = Color.FromArgb(200, 60, 40);
            MessungAbschliessen();
        }
    }

    private void MessungAbschliessen()
    {
        _messZustand = MessZustand.Bereit;
        if (InvokeRequired) BeginInvoke(AktualisiereUI);
        else                 AktualisiereUI();
    }

    // ── Anfahren ──────────────────────────────────────────────────────────────
    private void btnAnfahren_Click(object? sender, EventArgs e)
    {
        if (_anfahrZustand != AnfahrZustand.Bereit || _messZustand != MessZustand.Bereit) return;
        if (!TachymeterVerbindung.IstVerbunden)
        {
            lblAnfahrenInfo.Text      = "Kein Tachymeter verbunden.";
            lblAnfahrenInfo.ForeColor = Color.FromArgb(200, 80, 40);
            return;
        }
        if (dgvPunkte.CurrentRow == null || dgvPunkte.CurrentRow.IsNewRow)
        {
            lblAnfahrenInfo.Text      = "Keine Zeile ausgewählt.";
            lblAnfahrenInfo.ForeColor = Color.FromArgb(200, 80, 40);
            return;
        }

        try
        {
            dgvPunkte.EndEdit();
            var row = ((DataRowView)dgvPunkte.CurrentRow.DataBoundItem!).Row;

            _zielHzGon = Convert.ToDouble(row["Hz_gon"]);
            _zielVGon  = Convert.ToDouble(row["V_gon"]);

            // Schritt 1: Ist-Position lesen, dann Delta berechnen und RPC 6007 senden
            const string aktPosCmd = "%R1Q,2107,0:0";
            Logge($"ZIEL  Hz={_zielHzGon:F4} gon  V={_zielVGon:F4} gon");
            Logge($"SEND AKTPOS  {aktPosCmd}");
            lblAnfahrenInfo.Text      = $"Lese Ist-Position …";
            lblAnfahrenInfo.ForeColor = Color.FromArgb(200, 160, 40);
            _anfahrZustand = AnfahrZustand.WarteAktPos;
            _letzterRpc    = GeoCOMParser.RPC_TMC_GetAngle;
            _watchdog.Start();
            TachymeterVerbindung.GeoCOM_Senden(aktPosCmd);
            AktualisiereUI();
        }
        catch (Exception ex)
        {
            lblAnfahrenInfo.Text      = $"Fehler: {ex.Message}";
            lblAnfahrenInfo.ForeColor = Color.FromArgb(200, 60, 40);
            AnfahrenAbschliessen();
        }
    }

    private void btnStopp_Click(object? sender, EventArgs e)
    {
        _watchdog.Stop();
        lblAnfahrenInfo.Text      = "Abgebrochen (Gerät fährt ggf. noch).";
        lblAnfahrenInfo.ForeColor = Color.FromArgb(200, 130, 40);
        AnfahrenAbschliessen();
    }

    private void AnfahrenAbschliessen()
    {
        _watchdog.Stop();
        _anfahrZustand = AnfahrZustand.Bereit;
        if (InvokeRequired) BeginInvoke(AktualisiereUI);
        else                 AktualisiereUI();
    }

    private void OnWatchdogTick(object? sender, EventArgs e)
    {
        _watchdog.Stop();
        if (InvokeRequired) BeginInvoke(ZeigeWatchdogFehler);
        else                 ZeigeWatchdogFehler();
    }

    private void ZeigeWatchdogFehler()
    {
        lblAnfahrenInfo.Text      = "Timeout (30 s) – keine Antwort vom Gerät.";
        lblAnfahrenInfo.ForeColor = Color.FromArgb(200, 60, 40);
        AnfahrenAbschliessen();
    }

    // ── Tabellenzeilen verwalten ──────────────────────────────────────────────
    private void btnZeileHinzu_Click(object? sender, EventArgs e)
    {
        int nr = _tabelle.Rows.Count + 1;
        _tabelle.Rows.Add(nr, $"P{nr:D3}", "", 0.0, 0.0, 0.0, 0.0, 0.0, (double)nudVGon.Value);
        dgvPunkte.ClearSelection();
        dgvPunkte.Rows[^1].Selected = true;
        SpeichereKoordinaten();
    }

    private void btnZeileLoeschen_Click(object? sender, EventArgs e)
    {
        if (dgvPunkte.CurrentRow == null || dgvPunkte.CurrentRow.IsNewRow) return;
        int idx = dgvPunkte.CurrentRow.Index;
        _tabelle.Rows.RemoveAt(idx);
        for (int i = 0; i < _tabelle.Rows.Count; i++)
            _tabelle.Rows[i]["Nr"] = i + 1;
        SpeichereKoordinaten();
    }

    // ── Datenempfang ──────────────────────────────────────────────────────────
    private void OnDatenEmpfangen(object? sender, string daten)
    {
        if (InvokeRequired) { BeginInvoke(() => OnDatenEmpfangen(sender, daten)); return; }

        _zeilenPuffer.Append(daten);
        string puffer = _zeilenPuffer.ToString();
        int pos;
        while ((pos = puffer.IndexOf('\n')) >= 0)
        {
            string zeile = puffer[..pos].TrimEnd('\r');
            puffer = puffer[(pos + 1)..];
            if (!string.IsNullOrWhiteSpace(zeile))
                VerarbeiteZeile(zeile);
        }
        _zeilenPuffer.Clear();
        _zeilenPuffer.Append(puffer);
    }

    private void VerarbeiteZeile(string zeile)
    {
        int rpc = _letzterRpc;
        var m   = GeoCOMParser.ParseAntwort(zeile, rpc);
        if (m == null) return;

        // ── Schritt 1: DoMeasure-Bestätigung ─────────────────────────────────
        if (_messZustand == MessZustand.WarteMessen
            && rpc == GeoCOMParser.RPC_TMC_DoMeasure)
        {
            if (m.IstFehler)
            {
                lblMessInfo.Text      = $"Messfehler: {m.Bemerkung}";
                lblMessInfo.ForeColor = Color.FromArgb(200, 60, 40);
                MessungAbschliessen();
                return;
            }
            _messZustand = MessZustand.WarteErgebnis;
            _letzterRpc  = GeoCOMParser.RPC_TMC_GetSimpleMea;
            TachymeterVerbindung.GeoCOM_Senden("%R1Q,2108,0:5000,0");
            return;
        }

        // ── Schritt 2: Messergebnis Hz/V/D ───────────────────────────────────
        if (_messZustand == MessZustand.WarteErgebnis
            && rpc == GeoCOMParser.RPC_TMC_GetSimpleMea)
        {
            MessungAbschliessen();

            if (m.IstFehler)
            {
                lblMessInfo.Text      = $"Kein Ergebnis: {m.Bemerkung}";
                lblMessInfo.ForeColor = Color.FromArgb(200, 60, 40);
                return;
            }
            if (!m.IstVollmessung)
            {
                lblMessInfo.Text      = "Unvollständige Messung (Hz/V/D fehlen).";
                lblMessInfo.ForeColor = Color.FromArgb(200, 130, 40);
                return;
            }

            VerarbeiteMessergebnis(m);
            return;
        }

        // ── Ist-Position gelesen → Delta berechnen → RPC 6007 senden ─────────
        if (_anfahrZustand == AnfahrZustand.WarteAktPos
            && rpc == GeoCOMParser.RPC_TMC_GetAngle)
        {
            Logge($"RECV AKTPOS  {zeile}  →  Hz={m.Hz_gon:F4} gon  V={m.V_gon:F4} gon");

            if (m.IstFehler || m.Hz_gon == null)
            {
                AnfahrenAbschliessen();
                lblAnfahrenInfo.Text      = $"Ist-Position nicht lesbar: {m.Bemerkung}";
                lblAnfahrenInfo.ForeColor = Color.FromArgb(200, 60, 40);
                return;
            }

            double aktHz = m.Hz_gon.Value;
            double aktV  = m.V_gon ?? _zielVGon;

            // Kürzester Weg für Hz (−200 … +200 gon)
            double deltaHz = _zielHzGon - aktHz;
            while (deltaHz >  200) deltaHz -= 400;
            while (deltaHz < -200) deltaHz += 400;
            double deltaV  = _zielVGon - aktV;

            double deltaHzRad = deltaHz * (Math.PI / 200.0);
            double deltaVRad  = deltaV  * (Math.PI / 200.0);

            string befehl = string.Format(IC,
                "%R1Q,6007,0:{0:F10},{1:F10},0,0,0", deltaHzRad, deltaVRad);

            Logge($"AKTPOS  Hz={aktHz:F4} gon  V={aktV:F4} gon");
            Logge($"DELTA   ΔHz={deltaHz:F4} gon  ΔV={deltaV:F4} gon");
            Logge($"SEND MOV  {befehl}");
            lblAnfahrenInfo.Text = string.Format(IC,
                "Bewege … Ziel Hz={0:F4} gon  V={1:F4} gon  (ΔHz={2:+0.0000;-0.0000} gon)",
                _zielHzGon, _zielVGon, deltaHz);
            lblAnfahrenInfo.ForeColor = Color.FromArgb(200, 160, 40);
            _anfahrZustand = AnfahrZustand.WartePositionierung;
            _letzterRpc    = GeoCOMParser.RPC_MOT_MoveAngle;
            TachymeterVerbindung.GeoCOM_Senden(befehl);
            return;
        }

        // ── Bewegung abgeschlossen → Ist-Winkel zur Verifikation ─────────────
        if (_anfahrZustand == AnfahrZustand.WartePositionierung
            && rpc == GeoCOMParser.RPC_MOT_MoveAngle)
        {
            Logge($"RECV MOV  {zeile}  →  RC={m.ReturnCode}  {m.Bemerkung}");

            if (m.Typ == MessungsTyp.Fehler)
            {
                AnfahrenAbschliessen();
                lblAnfahrenInfo.Text      = $"Bewegung fehlgeschlagen: {m.Bemerkung}";
                lblAnfahrenInfo.ForeColor = Color.FromArgb(200, 60, 40);
                return;
            }

            _anfahrZustand = AnfahrZustand.WarteWinkel;
            _letzterRpc    = GeoCOMParser.RPC_TMC_GetAngle;
            const string winkelBefehl = "%R1Q,2107,0:0";
            Logge($"SEND WINKEL  {winkelBefehl}");
            lblAnfahrenInfo.Text      = "Bewegung OK – messe Ist-Winkel …";
            lblAnfahrenInfo.ForeColor = Color.FromArgb(140, 200, 140);
            TachymeterVerbindung.GeoCOM_Senden(winkelBefehl);
            return;
        }

        // ── Ist-Winkel nach Anfahren ──────────────────────────────────────────
        if (_anfahrZustand == AnfahrZustand.WarteWinkel
            && rpc == GeoCOMParser.RPC_TMC_GetAngle)
        {
            Logge($"RECV WINKEL  {zeile}  →  RC={m.ReturnCode}  Hz={m.Hz_gon:F4} gon  V={m.V_gon:F4} gon");
            AnfahrenAbschliessen();

            if (m.IstFehler || m.Hz_gon == null)
            {
                lblAnfahrenInfo.Text      = $"Winkelmessung fehlgeschlagen: {m.Bemerkung}";
                lblAnfahrenInfo.ForeColor = Color.FromArgb(200, 130, 40);
                return;
            }

            lblAnfahrenInfo.Text = string.Format(IC,
                "Ist-Winkel:  Hz={0:F4} gon  V={1:F4} gon",
                m.Hz_gon.Value, m.V_gon ?? double.NaN);
            lblAnfahrenInfo.ForeColor = Color.FromArgb(100, 200, 255);
        }
    }

    // ── Messergebnis → lokale Koordinaten → Tabelle ───────────────────────────
    private void VerarbeiteMessergebnis(TachymeterMessung m)
    {
        double hzGon  = m.Hz_gon!.Value;
        double vGon   = m.V_gon!.Value;
        double d      = m.Schraegstrecke_m!.Value;

        double hzRad  = hzGon * (Math.PI / 200.0);
        double vRad   = vGon  * (Math.PI / 200.0);
        double dHoriz = d * Math.Sin(vRad);
        double x      = Math.Round(dHoriz * Math.Sin(hzRad), 4);
        double y      = Math.Round(dHoriz * Math.Cos(hzRad), 4);
        double z      = Math.Round(d * Math.Cos(vRad),       4);

        string punktNr = txtPunktNr.Text.Trim();
        string code    = txtCode.Text.Trim();
        int    nr      = _tabelle.Rows.Count + 1;

        if (string.IsNullOrEmpty(punktNr)) punktNr = $"P{nr:D3}";

        _tabelle.Rows.Add(nr, punktNr, code, x, y, z, Math.Round(d, 3), Math.Round(hzGon, 4), Math.Round(vGon, 4));
        SpeichereKoordinaten();

        if (int.TryParse(punktNr, out int numNr))
            txtPunktNr.Text = (numNr + 1).ToString();

        if (dgvPunkte.Rows.Count > 0)
        {
            dgvPunkte.ClearSelection();
            dgvPunkte.Rows[^1].Selected = true;
            dgvPunkte.FirstDisplayedScrollingRowIndex = dgvPunkte.Rows.Count - 1;
        }

        lblMessInfo.Text = string.Format(IC,
            "Hz={0:F4} gon   V={1:F4} gon   D={2:F3} m   →  X={3:F4}  Y={4:F4}  Z={5:F4}",
            hzGon, vGon, d, x, y, z);
        lblMessInfo.ForeColor = Color.FromArgb(40, 180, 60);
    }

    // ── UI-Zustand ────────────────────────────────────────────────────────────
    private void AktualisiereUI()
    {
        bool verbunden    = TachymeterVerbindung.IstVerbunden;
        bool messbereit   = verbunden && _messZustand   == MessZustand.Bereit
                                      && _anfahrZustand == AnfahrZustand.Bereit;
        bool anfahrbereit = verbunden && _anfahrZustand == AnfahrZustand.Bereit
                                      && _messZustand   == MessZustand.Bereit;
        bool positioniert = _anfahrZustand is AnfahrZustand.WarteAktPos
                                           or AnfahrZustand.WartePositionierung
                                           or AnfahrZustand.WarteWinkel;

        btnMessen.Enabled   = messbereit;
        btnAnfahren.Enabled = anfahrbereit;
        btnStopp.Enabled    = positioniert;

        btnMessen.BackColor   = messbereit   ? Color.FromArgb(160, 60, 20)   : Color.FromArgb(90, 90, 90);
        btnAnfahren.BackColor = anfahrbereit ? Color.FromArgb(30, 100, 160)  : Color.FromArgb(90, 90, 90);
        btnStopp.BackColor    = positioniert ? Color.FromArgb(160, 40, 40)   : Color.FromArgb(90, 90, 90);

        lblVerbindung.Text = verbunden
            ? TachymeterVerbindung.ModellAnzeige(TachymeterVerbindung.Modell)
            : "Nicht verbunden";
        lblVerbindung.ForeColor = verbunden
            ? Color.FromArgb(120, 220, 120)
            : Color.FromArgb(180, 80, 80);
    }

    // ── Anfahren von extern (z. B. DXF-Viewer-Rechtsklick) ───────────────────
    /// <summary>Wählt den Punkt mit der angegebenen PunktNr aus und löst Anfahren aus.
    /// Gibt false zurück wenn der Punkt nicht in der Tabelle vorhanden ist.</summary>
    public bool AnfahrenNach(string punktNr)
    {
        for (int i = 0; i < _tabelle.Rows.Count; i++)
        {
            if (string.Equals(_tabelle.Rows[i]["PunktNr"]?.ToString(), punktNr,
                    StringComparison.Ordinal))
            {
                dgvPunkte.ClearSelection();
                dgvPunkte.CurrentCell = dgvPunkte.Rows[i].Cells[0];
                dgvPunkte.Rows[i].Selected = true;
                btnAnfahren_Click(null, EventArgs.Empty);
                return true;
            }
        }
        return false;
    }

    // ── Export KOR + DAT ─────────────────────────────────────────────────────
    private void btnExportieren_Click(object? sender, EventArgs e)
    {
        if (_tabelle.Rows.Count == 0)
        {
            MessageBox.Show("Keine Messpunkte vorhanden.", "Export",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Title            = "KOR-Datei speichern",
            Filter           = "CSV-Koordinaten (*.csv)|*.csv|Alle Dateien (*.*)|*.*",
            DefaultExt       = "csv",
            FileName         = $"aufnahme-kor.csv",
            InitialDirectory = ProjektManager.IstGeladen
                                   ? ProjektManager.ProjektVerzeichnis
                                   : AppPfade.Basis
        };

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        string korPfad = dlg.FileName;
        string basisOhneExt = Path.Combine(
            Path.GetDirectoryName(korPfad)!,
            Path.GetFileNameWithoutExtension(korPfad));
        // "-kor" am Ende entfernen, falls vorhanden, dann "-dat" anhängen
        string basisName = basisOhneExt.EndsWith("-kor", StringComparison.OrdinalIgnoreCase)
            ? basisOhneExt[..^4]
            : basisOhneExt;
        string datPfad = basisName + "-dat.csv";

        try
        {
            SchreibeKor(korPfad);
            SchreibeDat(datPfad);

            MessageBox.Show(
                $"Export abgeschlossen:\n\n  {Path.GetFileName(korPfad)}\n  {Path.GetFileName(datPfad)}",
                "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Exportfehler:\n{ex.Message}", "Export",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SchreibeKor(string pfad)
    {
        string sensor    = TachymeterVerbindung.ModellAnzeige(TachymeterVerbindung.Modell);
        string projekt   = ProjektManager.ProjektName;
        string bearbeiter = ProjektdatenManager.Bearbeiter;
        string datum     = DateTime.Now.ToString("yyyy-MM-dd");

        var sb = new StringBuilder();
        sb.AppendLine("# METADATA");
        sb.AppendLine($"Projekt: {projekt}");
        sb.AppendLine($"Sensor: {sensor}");
        sb.AppendLine($"Bearbeiter: {bearbeiter}");
        sb.AppendLine($"Datum: {datum}");
        sb.AppendLine("---");
        sb.AppendLine("# DATENTYP");
        sb.AppendLine("Koordinaten");
        sb.AppendLine("# DATEN");
        sb.AppendLine("PunktNr; X; Y; Z; Code");

        foreach (DataRow r in _tabelle.Rows)
        {
            sb.AppendLine(string.Format(IC, "{0}; {1:F4}; {2:F4}; {3:F4}; {4}",
                r["PunktNr"],
                (double)r["X_m"],
                (double)r["Y_m"],
                (double)r["Z_m"],
                r["Code"]));
        }

        File.WriteAllText(pfad, sb.ToString(), Encoding.UTF8);
    }

    private void SchreibeDat(string pfad)
    {
        string sensor    = TachymeterVerbindung.ModellAnzeige(TachymeterVerbindung.Modell);
        string projekt   = ProjektManager.ProjektName;
        string bearbeiter = ProjektdatenManager.Bearbeiter;
        string datum     = DateTime.Now.ToString("yyyy-MM-dd");
        string zeitstempel = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        var sb = new StringBuilder();
        sb.AppendLine("# METADATA");
        sb.AppendLine($"Projekt: {projekt}");
        sb.AppendLine($"Sensor: {sensor}");
        sb.AppendLine($"Bearbeiter: {bearbeiter}");
        sb.AppendLine($"Datum: {datum}");
        sb.AppendLine("StandpunktNr: ");
        sb.AppendLine("InstrumentenHoehe: ");
        sb.AppendLine("StandpunktCode: ");
        sb.AppendLine("---");
        sb.AppendLine("# DATENTYP");
        sb.AppendLine("Messdaten");
        sb.AppendLine("# DATEN");
        sb.AppendLine("PunktNr; HZ_gon; V_gon; Strecke_m; Zielhoehe_m; Code; Zeitstempel");

        foreach (DataRow r in _tabelle.Rows)
        {
            sb.AppendLine(string.Format(IC, "{0}; {1:F3}; {2:F3}; {3:F3}; 0.000; {4}; {5}",
                r["PunktNr"],
                (double)r["Hz_gon"],
                (double)r["V_gon"],
                (double)r["D_m"],
                r["Code"],
                zeitstempel));
        }

        File.WriteAllText(pfad, sb.ToString(), Encoding.UTF8);
    }

    // ── Aufräumen ─────────────────────────────────────────────────────────────
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        SpeichereKoordinaten();
        TachymeterVerbindung.DatenEmpfangen -= OnDatenEmpfangen;
        _watchdog.Stop();
        _watchdog.Dispose();
        base.OnFormClosing(e);
    }
}
