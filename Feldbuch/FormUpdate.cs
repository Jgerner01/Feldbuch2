namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// FormUpdate – zeigt eine neue Version an und installiert sie nur auf Wunsch.
// Beim Programmstart wird nur gefragt, solange kein anderes Fenster offen ist,
// damit keine laufende Messung unterbrochen wird.
// ──────────────────────────────────────────────────────────────────────────────
public sealed class FormUpdate : Form
{
    private readonly UpdateInfo _info;
    private readonly int _erlaubteFenster;
    private readonly Label _lblStatus = new();
    private readonly ProgressBar _fortschritt = new();
    private readonly Button _btnJetzt = new();
    private readonly Button _btnSpaeter = new();
    private CancellationTokenSource? _abbruch;

    public FormUpdate(UpdateInfo info, int erlaubteFenster)
    {
        _info = info;
        _erlaubteFenster = erlaubteFenster;

        ClientSize      = new Size(500, 400);
        Text            = "Update verfügbar";
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        Font            = new Font("Segoe UI", 9F);

        var lblTitel = new Label
        {
            Text     = $"Version {info.Version} ist verfügbar (installiert: {ProgrammVersion.Version})",
            Location = new Point(16, 14),
            Size     = new Size(468, 24),
            Font     = new Font("Segoe UI", 10F, FontStyle.Bold)
        };
        var txtNotizen = new TextBox
        {
            Text       = info.Notizen.ReplaceLineEndings(Environment.NewLine),
            Location   = new Point(16, 44),
            Size       = new Size(468, 250),
            Multiline  = true,
            ReadOnly   = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor  = SystemColors.Window
        };

        _lblStatus.Text     = $"Download: {info.Groesse / 1024.0 / 1024.0:F0} MB. Das Programm wird dabei neu gestartet.";
        _lblStatus.Location = new Point(16, 302);
        _lblStatus.Size     = new Size(468, 20);

        _fortschritt.Location = new Point(16, 324);
        _fortschritt.Size     = new Size(468, 14);

        _btnJetzt.Text     = "Jetzt aktualisieren";
        _btnJetzt.Location = new Point(244, 350);
        _btnJetzt.Size     = new Size(140, 34);
        _btnJetzt.Click   += async (_, _) => await InstalliereAsync();

        _btnSpaeter.Text     = "Später";
        _btnSpaeter.Location = new Point(394, 350);
        _btnSpaeter.Size     = new Size(90, 34);
        _btnSpaeter.Click   += (_, _) =>
        {
            if (_abbruch != null) _abbruch.Cancel();
            else Close();
        };

        Controls.AddRange([lblTitel, txtNotizen, _lblStatus, _fortschritt, _btnJetzt, _btnSpaeter]);
        CancelButton = _btnSpaeter;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Das Schließen bricht einen laufenden Download nur ab; der Ablauf meldet sich danach selbst.
        if (_abbruch != null) { _abbruch.Cancel(); e.Cancel = true; }
        base.OnFormClosing(e);
    }

    private async Task InstalliereAsync()
    {
        // Dieses Fenster zählt selbst mit.
        if (Application.OpenForms.Count > _erlaubteFenster + 1)
        {
            MessageBox.Show(this, "Bitte zuerst alle anderen Feldbuch-Fenster schließen.", "Update",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _btnJetzt.Enabled = false;
        _btnSpaeter.Text = "Abbrechen";
        _lblStatus.Text = "Update wird heruntergeladen …";
        _abbruch = new CancellationTokenSource();
        try
        {
            var fortschritt = new Progress<int>(p => _fortschritt.Value = Math.Clamp(p, 0, 100));
            string neu = await UpdateDienst.LadeAsync(_info, UpdateDienst.UpdateOrdner, fortschritt, _abbruch.Token);
            _lblStatus.Text = "Update wird installiert …";
            UpdateInstallation.StarteInstallation(neu, AppPfade.Basis);
            _abbruch = null;
            Application.Exit();
        }
        catch (OperationCanceledException)
        {
            _abbruch = null;
            Close();
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException
                                      or InvalidDataException or System.ComponentModel.Win32Exception)
        {
            _abbruch = null;
            ErrorLogger.Log("Update", ex);
            _btnJetzt.Enabled = true;
            _btnSpaeter.Text = "Später";
            _fortschritt.Value = 0;
            _lblStatus.Text = "Das Update ist fehlgeschlagen. Die installierte Version bleibt unverändert.";
            MessageBox.Show(this, ex.Message, "Update fehlgeschlagen", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>Stille Prüfung beim Start: Ohne Internet oder bei Fehlern arbeitet das Programm normal weiter.</summary>
    public static async Task PruefeBeimStartAsync(Form besitzer)
    {
        if (!UpdateInstallation.IstVeroeffentlicht) return;
        UpdateDienst.RaeumeAuf(UpdateDienst.UpdateOrdner);
        try
        {
            var info = await UpdateDienst.SucheAsync();
            if (info == null || besitzer.IsDisposed || Application.OpenForms.Count != 1) return;
            if (!UpdateInstallation.IstOrdnerSchreibbar(AppPfade.Basis)) return;
            using var form = new FormUpdate(info, erlaubteFenster: 1);
            form.ShowDialog(besitzer);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                      or System.Text.Json.JsonException or KeyNotFoundException or InvalidOperationException)
        {
            ErrorLogger.Log("Update-Prüfung", ex);
        }
    }

    /// <summary>Prüfung auf Wunsch aus dem Info-Fenster, mit Rückmeldung in jedem Fall.</summary>
    public static async Task PruefeManuellAsync(Form besitzer)
    {
        const string titel = "Nach Updates suchen";
        if (!UpdateInstallation.IstVeroeffentlicht)
        {
            MessageBox.Show(besitzer, "Updates sind nur in der veröffentlichten Programmversion möglich.", titel,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            var info = await UpdateDienst.SucheAsync();
            if (besitzer.IsDisposed) return;
            if (info == null)
            {
                MessageBox.Show(besitzer, $"Version {ProgrammVersion.Version} ist aktuell.", titel,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!UpdateInstallation.IstOrdnerSchreibbar(AppPfade.Basis))
            {
                MessageBox.Show(besitzer,
                    $"Version {info.Version} ist verfügbar, aber der Programmordner ist nicht beschreibbar:\n{AppPfade.Basis}\n\n"
                    + "Bitte die neue Version von der GitHub-Release-Seite herunterladen.",
                    titel, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // Hauptfenster und Info-Fenster sind beim manuellen Aufruf geöffnet.
            using var form = new FormUpdate(info, erlaubteFenster: 2);
            form.ShowDialog(besitzer);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                      or System.Text.Json.JsonException or KeyNotFoundException or InvalidOperationException)
        {
            ErrorLogger.Log("Update-Prüfung", ex);
            MessageBox.Show(besitzer, "Die Suche nach Updates ist fehlgeschlagen. Bitte die Internetverbindung prüfen.",
                titel, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
