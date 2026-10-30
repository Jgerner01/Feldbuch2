// Zweck:    Dialog zur Auswahl gemessener Neupunkte, die als Festpunkte nach
//           Feldbuchpunkte.json übernommen werden (Übernahme von Hand).
// Hardware: keine
// Autor:    Johann Gerner

using System.Globalization;

namespace Feldbuch;

public class FormFestpunkte : Form
{
    private const int    FORM_BREITE      = 520;
    private const int    FORM_HOEHE       = 460;
    private const int    RAND             = 12;
    private const int    BUTTON_BREITE    = 120;
    private const int    BUTTON_HOEHE     = 30;
    private const string MARKE_VORHANDEN  = "   [bereits Festpunkt]";

    private readonly CheckedListBox       _liste     = new();
    private readonly Label                _info      = new();
    private readonly List<NeupunktErgebnis> _kandidaten;

    /// <summary>Anzahl der übernommenen Punkte nach dem Schließen mit OK.</summary>
    public FestpunktUebernahme.UebernahmeErgebnis? Ergebnis { get; private set; }

    public FormFestpunkte()
    {
        _kandidaten = FestpunktUebernahme.Kandidaten().ToList();

        Text            = "Neupunkte als Festpunkte übernehmen";
        ClientSize      = new Size(FORM_BREITE, FORM_HOEHE);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;

        _info.Text     = "Die gewählten Koordinaten werden als feste Anschlusspunkte nach Feldbuchpunkte.json " +
                         "kopiert (Quelle „Lokal“) und ändern sich danach nicht mehr automatisch.";
        _info.Location = new Point(RAND, RAND);
        _info.Size     = new Size(FORM_BREITE - 2 * RAND, 44);

        var btnAlle   = NeuerButton("Alle wählen",  RAND, FORM_HOEHE - RAND - BUTTON_HOEHE);
        var btnKeine  = NeuerButton("Keine",        RAND + BUTTON_BREITE + 8, FORM_HOEHE - RAND - BUTTON_HOEHE);
        var btnOk     = NeuerButton("Übernehmen",   FORM_BREITE - 2 * BUTTON_BREITE - RAND - 8, FORM_HOEHE - RAND - BUTTON_HOEHE);
        var btnAbbruch = NeuerButton("Abbrechen",   FORM_BREITE - BUTTON_BREITE - RAND, FORM_HOEHE - RAND - BUTTON_HOEHE);

        _liste.Location     = new Point(RAND, _info.Bottom + 8);
        _liste.Size         = new Size(FORM_BREITE - 2 * RAND, btnAlle.Top - _info.Bottom - 16);
        _liste.Font         = new Font("Consolas", 9.5F);
        _liste.CheckOnClick = true;
        _liste.HorizontalScrollbar = true;

        foreach (var e in _kandidaten)
        {
            bool vorhanden = FestpunktUebernahme.Existiert(e.PunktNr);
            string hoehe   = e.Ist3D ? e.Hoehe.ToString("F3", CultureInfo.InvariantCulture) : "-";
            string zeile   = $"{e.PunktNr,-8} {e.StandpunktNr,-6} R {e.R,12:F3}  H {e.H,12:F3}  Hoehe {hoehe,9}"
                                 .Replace(',', '.');
            // Vorhandene Festpunkte sind nicht vorgewählt, damit nichts unbeabsichtigt überschrieben wird
            _liste.Items.Add(zeile + (vorhanden ? MARKE_VORHANDEN : ""), !vorhanden);
        }

        btnAlle.Click    += (s, e) => SetzeAlle(true);
        btnKeine.Click   += (s, e) => SetzeAlle(false);
        btnOk.Click      += (s, e) => Uebernehmen();
        btnAbbruch.Click += (s, e) => DialogResult = DialogResult.Cancel;

        if (_kandidaten.Count == 0)
        {
            _info.Text = "Es sind keine gemessenen Neupunkte mit Koordinaten vorhanden.";
            btnOk.Enabled = btnAlle.Enabled = btnKeine.Enabled = false;
        }

        Controls.AddRange(new Control[] { _info, _liste, btnAlle, btnKeine, btnOk, btnAbbruch });
        AcceptButton = btnOk;
        CancelButton = btnAbbruch;
    }

    private static Button NeuerButton(string text, int x, int y) => new()
    {
        Text     = text,
        Location = new Point(x, y),
        Size     = new Size(BUTTON_BREITE, BUTTON_HOEHE)
    };

    private void SetzeAlle(bool wert)
    {
        for (int i = 0; i < _liste.Items.Count; i++) _liste.SetItemChecked(i, wert);
    }

    private void Uebernehmen()
    {
        var gewaehlt = _liste.CheckedIndices.Cast<int>().Select(i => _kandidaten[i]).ToList();
        if (gewaehlt.Count == 0) { DialogResult = DialogResult.Cancel; return; }

        // Festpunkte sind nur über die Nummer eindeutig: dieselbe Nummer von zwei Standpunkten geht nicht
        var doppelt = gewaehlt.GroupBy(e => e.PunktNr).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (doppelt.Count > 0)
        {
            MessageBox.Show(this,
                "Folgende Punktnummern sind mehrfach (von verschiedenen Standpunkten) gewählt:" + Environment.NewLine +
                string.Join(", ", doppelt) + Environment.NewLine + Environment.NewLine +
                "Bitte je Nummer nur einen Punkt wählen.",
                "Festpunkte übernehmen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var ersetzen = gewaehlt.Where(e => FestpunktUebernahme.Existiert(e.PunktNr))
                               .Select(e => e.PunktNr).ToList();
        if (ersetzen.Count > 0)
        {
            string nummern = string.Join(", ", ersetzen.Take(10)) + (ersetzen.Count > 10 ? ", …" : "");
            var antwort = MessageBox.Show(this,
                $"{ersetzen.Count} Festpunkt(e) sind bereits vorhanden und werden mit den aktuellen " +
                $"Koordinaten überschrieben:\n{nummern}\n\nFortfahren?",
                "Festpunkte überschreiben", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (antwort != DialogResult.Yes) return;
        }

        Ergebnis     = FestpunktUebernahme.Uebernehme(gewaehlt);
        DialogResult = DialogResult.OK;
    }
}
