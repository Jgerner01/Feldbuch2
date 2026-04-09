namespace Feldbuch;

using System.Globalization;
using System.Text;

// ──────────────────────────────────────────────────────────────────────────────
// Dialog: Messdaten zu einem per DXF-Viewer gepickten Anschlusspunkt eingeben.
//
// Koordinaten R, H kommen aus dem DXF-Snap (read-only).
// Punktnummer + Inst. Höhe werden aus der geklickten Overlay-Entity vorbelegt.
// Messdaten (HZ, V, Strecke, Zielhöhe, Code) können manuell oder per Tachymeter
// gefüllt werden.  Reflektormodus (Prisma / Reflektorlos) ist umschaltbar und
// bleibt sitzungsübergreifend erhalten.
// ──────────────────────────────────────────────────────────────────────────────
public partial class FormMessdatenEingabe : Form
{
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;
    private readonly double _r;
    private readonly double _h;

    // ── Persistenter Zustand (bleibt zwischen Dialog-Öffnungen erhalten) ─────
    private static string _letzterCode    = "";
    private static bool   _istReflektorlos = false;

    // ── GeoCOM-Zustandsmaschine ───────────────────────────────────────────────
    private enum MessZustand { Bereit, WarteMessen, WarteErgebnis }
    private MessZustand    _messZustand    = MessZustand.Bereit;
    private int            _letzterRpc     = 0;
    private readonly StringBuilder _zeilenPuffer = new();

    private const int EDM_SINGLE_PRISM = 3;
    private const int EDM_SINGLE_RL_K  = 5;

    /// <summary>Ergebnis nach OK – null wenn Abbrechen.</summary>
    public StationierungsPunkt? Ergebnis { get; private set; }

    /// <summary>Zuletzt eingegebener Code (für nachgelagerte Verarbeitung).</summary>
    public string AktuellerCode => txtCode.Text.Trim();

    // ── Konstruktor ───────────────────────────────────────────────────────────
    /// <param name="r">Rechtswert (aus DXF-Snap)</param>
    /// <param name="h">Hochwert  (aus DXF-Snap)</param>
    /// <param name="punktNr">Vorausgefüllte Punktnummer (aus Overlay-Entity)</param>
    /// <param name="hoehe">Vorausgefüllte Höhe des Anschlusspunktes</param>
    public FormMessdatenEingabe(double r, double h,
        string punktNr = "", double hoehe = 0.0)
    {
        InitializeComponent();
        _r = r;
        _h = h;

        lblRVal.Text = $"R (Rechtswert):   {r:F3} m";
        lblHVal.Text = $"H (Hochwert):     {h:F3} m";

        // Vorbelegung
        if (!string.IsNullOrEmpty(punktNr))
            txtPunktNr.Text = punktNr;

        if (hoehe != 0.0)
            nudHoehe.Value = (decimal)Math.Clamp(hoehe, (double)nudHoehe.Minimum, (double)nudHoehe.Maximum);

        // Letzten Code übernehmen
        txtCode.Text = _letzterCode;

        // Tachymeter-Event anschließen
        TachymeterVerbindung.DatenEmpfangen += OnDatenEmpfangen;

        // Button-Zustand initialisieren
        AktualisiereReflModusButton();
        AktualisiereMessButton();
    }

    // ── Reflektormodus umschalten ─────────────────────────────────────────────
    private void btnReflModus_Click(object? sender, EventArgs e)
    {
        _istReflektorlos = !_istReflektorlos;
        AktualisiereReflModusButton();

        if (TachymeterVerbindung.IstVerbunden)
        {
            try
            {
                int zieltyp = _istReflektorlos ? 1 : 0;     // BAP_REFL_LESS / BAP_REFL_USE
                int edmMode = _istReflektorlos ? EDM_SINGLE_RL_K : EDM_SINGLE_PRISM;
                _letzterRpc = GeoCOMParser.RPC_BAP_SetTargetType;
                TachymeterVerbindung.GeoCOM_Senden($"%R1Q,17021,0:{zieltyp}");
                _letzterRpc = GeoCOMParser.RPC_TMC_SetEdmMode;
                TachymeterVerbindung.GeoCOM_Senden($"%R1Q,2020,0:{edmMode}");
            }
            catch { /* ignorieren – Modus wurde lokal gesetzt */ }
        }
    }

    private void AktualisiereReflModusButton()
    {
        if (_istReflektorlos)
        {
            btnReflModus.Text      = "Reflektorlos";
            btnReflModus.BackColor = Color.FromArgb(130, 80, 20);
            btnReflModus.FlatAppearance.BorderColor = Color.FromArgb(100, 55, 10);
        }
        else
        {
            btnReflModus.Text      = "Prisma";
            btnReflModus.BackColor = Color.FromArgb(38, 100, 58);
            btnReflModus.FlatAppearance.BorderColor = Color.FromArgb(26, 76, 40);
        }
    }

    // ── Messung auslösen ──────────────────────────────────────────────────────
    private void btnMessen_Click(object? sender, EventArgs e)
    {
        if (_messZustand != MessZustand.Bereit) return;

        if (!TachymeterVerbindung.IstVerbunden)
        {
            lblMessInfo.Text      = "Kein Tachymeter verbunden.";
            lblMessInfo.ForeColor = Color.FromArgb(200, 80, 40);
            return;
        }

        try
        {
            btnMessen.Enabled = false;
            lblMessInfo.Text      = "Messung läuft …";
            lblMessInfo.ForeColor = Color.FromArgb(200, 160, 40);

            _messZustand = MessZustand.WarteMessen;
            _letzterRpc  = GeoCOMParser.RPC_TMC_DoMeasure;
            TachymeterVerbindung.GeoCOM_Senden("%R1Q,2008,0:1,1");
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
        if (InvokeRequired) BeginInvoke(AktualisiereMessButton);
        else                 AktualisiereMessButton();
    }

    private void AktualisiereMessButton()
    {
        bool verbunden = TachymeterVerbindung.IstVerbunden;
        btnMessen.Enabled   = verbunden && _messZustand == MessZustand.Bereit;
        btnMessen.BackColor = verbunden
            ? Color.FromArgb(160, 60, 20)
            : Color.FromArgb(90, 90, 90);
    }

    // ── GeoCOM-Datenempfang ───────────────────────────────────────────────────
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
        int rpcKontext = _letzterRpc;
        var messung    = GeoCOMParser.ParseAntwort(zeile, rpcKontext);
        if (messung == null) return;

        if (_messZustand == MessZustand.WarteMessen
            && rpcKontext == GeoCOMParser.RPC_TMC_DoMeasure)
        {
            if (messung.IstFehler)
            {
                lblMessInfo.Text      = $"Messfehler (GRC={messung.ReturnCode}): {messung.Bemerkung}";
                lblMessInfo.ForeColor = Color.FromArgb(200, 60, 40);
                MessungAbschliessen();
            }
            else
            {
                _messZustand = MessZustand.WarteErgebnis;
                _letzterRpc  = GeoCOMParser.RPC_TMC_GetSimpleMea;
                TachymeterVerbindung.GeoCOM_Senden("%R1Q,2108,0:5000,1");
            }
        }
        else if (_messZustand == MessZustand.WarteErgebnis
            && rpcKontext == GeoCOMParser.RPC_TMC_GetSimpleMea)
        {
            MessungAbschliessen();

            if (messung.IstFehler)
            {
                lblMessInfo.Text      = $"Kein Ergebnis (GRC={messung.ReturnCode}): {messung.Bemerkung}";
                lblMessInfo.ForeColor = Color.FromArgb(200, 60, 40);
                return;
            }

            if (!messung.IstVollmessung)
            {
                lblMessInfo.Text      = "Unvollständige Messung (Hz/V/D fehlen).";
                lblMessInfo.ForeColor = Color.FromArgb(200, 130, 40);
                return;
            }

            // Messwerte in Felder eintragen
            nudHz.Value      = (decimal)Math.Round(messung.Hz_gon!.Value,   4);
            nudV.Value       = (decimal)Math.Round(messung.V_gon!.Value,    4);
            nudStrecke.Value = (decimal)Math.Round(messung.Schraegstrecke_m!.Value, 3);

            lblMessInfo.Text = string.Format(IC,
                "Hz={0:F4} gon   V={1:F4} gon   D={2:F3} m",
                messung.Hz_gon.Value, messung.V_gon.Value, messung.Schraegstrecke_m.Value);
            lblMessInfo.ForeColor = Color.FromArgb(40, 180, 60);
        }
    }

    // ── Übernehmen ────────────────────────────────────────────────────────────
    private void btnUebernehmen_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtPunktNr.Text))
        {
            MessageBox.Show("Bitte eine Punktnummer eingeben.",
                "Eingabefehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtPunktNr.Focus();
            return;
        }
        if (nudStrecke.Value <= 0)
        {
            MessageBox.Show("Strecke muss größer als 0 sein.\n" +
                            "Bitte Messdaten eingeben oder Messen auslösen.",
                "Eingabefehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            nudStrecke.Focus();
            return;
        }

        // Letzten Code merken
        _letzterCode = txtCode.Text.Trim();

        Ergebnis = new StationierungsPunkt
        {
            PunktNr   = txtPunktNr.Text.Trim(),
            R         = _r,
            H         = _h,
            Hoehe     = (double)nudHoehe.Value,
            HZ        = (double)nudHz.Value,
            V         = (double)nudV.Value,
            Strecke   = (double)nudStrecke.Value,
            Zielhoehe = (double)nudZielhoehe.Value
        };
        DialogResult = DialogResult.OK;
    }

    private void btnAbbrechen_Click(object? sender, EventArgs e)
        => DialogResult = DialogResult.Cancel;

    // ── Aufräumen ─────────────────────────────────────────────────────────────
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        TachymeterVerbindung.DatenEmpfangen -= OnDatenEmpfangen;
        base.OnFormClosing(e);
    }
}
