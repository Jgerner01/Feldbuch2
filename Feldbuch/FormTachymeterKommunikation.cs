using System.IO.Ports;
using System.Management;
using System.Diagnostics;

namespace Feldbuch;

public partial class FormTachymeterKommunikation : Form
{
    public FormTachymeterKommunikation()
    {
        InitializeComponent();
        LadeModelle();
        LadeComPorts();
        LadeBluetoothGeraete();
        LadeAktuelleEinstellungen();
        AktualisiereVerbindungsStatus();
    }

    // ── Initialisierung ───────────────────────────────────────────────────────

    private void LadeModelle()
    {
        cboModell.BeginUpdate();
        foreach (var m in TachymeterVerbindung.AlleModelle)
            cboModell.Items.Add(TachymeterVerbindung.ModellAnzeige(m));
        cboModell.EndUpdate();
    }

    private void LadeComPorts()
    {
        cboBtPort.BeginUpdate();
        cboBtPort.Items.Clear();

        var ports    = SerialPort.GetPortNames().OrderBy(p => p).ToArray();
        var friendly = HoleFriendlyNames();

        if (ports.Length == 0)
        {
            cboBtPort.Items.Add("– Kein COM-Port gefunden –");
        }
        else
        {
            foreach (var p in ports)
            {
                var eintrag = friendly.TryGetValue(p, out var name)
                    ? $"{p}  –  {name}"
                    : p;
                cboBtPort.Items.Add(eintrag);
            }
        }

        // Gespeicherten Port vorauswählen
        var gespeichert = TachymeterVerbindung.Port;
        if (!string.IsNullOrEmpty(gespeichert))
        {
            for (int i = 0; i < cboBtPort.Items.Count; i++)
            {
                if (cboBtPort.Items[i]!.ToString()!.StartsWith(gespeichert,
                        StringComparison.OrdinalIgnoreCase))
                {
                    cboBtPort.SelectedIndex = i;
                    break;
                }
            }
        }
        if (cboBtPort.SelectedIndex < 0 && cboBtPort.Items.Count > 0)
            cboBtPort.SelectedIndex = 0;

        cboBtPort.EndUpdate();
    }

    // ── Bluetooth-Geräteverwaltung ────────────────────────────────────────────

    /// <summary>
    /// Lädt bereits gekoppelte Bluetooth-Geräte und zeigt sie in der Liste an.
    /// Erkennt Geräte über die Bluetooth-Device-Klasse in WMI.
    /// </summary>
    private void LadeBluetoothGeraete()
    {
        lstBluetoothGeraete.BeginUpdate();
        lstBluetoothGeraete.Items.Clear();

        try
        {
            // Gekoppelte Bluetooth-Geräte über Win32_PnPEntity ermitteln
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, DeviceID, ClassGuid FROM Win32_PnPEntity " +
                "WHERE ClassGuid='{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}'");

            var geraete = new List<string>();
            foreach (ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString();
                if (!string.IsNullOrEmpty(name) && !geraete.Contains(name))
                    geraete.Add(name);
            }

            // Zusätzlich: COM-Ports die über Bluetooth bereitgestellt werden
            var btComPorts = new List<string>();
            using var comSearcher = new ManagementObjectSearcher(
                "SELECT Name, DeviceID FROM Win32_PnPEntity WHERE Name LIKE '%(COM%)'");
            foreach (ManagementObject obj in comSearcher.Get())
            {
                var name = obj["Name"]?.ToString();
                if (!string.IsNullOrEmpty(name) && name.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase))
                {
                    // Extrahiere COM-Port aus Name
                    var start = name.LastIndexOf('(');
                    var end = name.LastIndexOf(')');
                    if (start >= 0 && end > start)
                    {
                        var port = name[(start + 1)..end].Trim();
                        var label = name[..start].Trim();
                        btComPorts.Add($"{port} – {label}");
                    }
                }
            }

            if (geraete.Count == 0 && btComPorts.Count == 0)
            {
                lstBluetoothGeraete.Items.Add("Keine Bluetooth-Geräte gefunden");
            }
            else
            {
                foreach (var g in geraete)
                    lstBluetoothGeraete.Items.Add($"📡 {g}");
                foreach (var p in btComPorts)
                    lstBluetoothGeraete.Items.Add($"🔌 {p}");
            }
        }
        catch (Exception ex)
        {
            lstBluetoothGeraete.Items.Add($"Fehler beim Laden: {ex.Message}");
        }

        lstBluetoothGeraete.EndUpdate();
    }

    /// <summary>
    /// Öffnet den Windows Bluetooth-Pairing-Assistenten zum Koppeln neuer Geräte.
    /// </summary>
    private void btnGeraetKoppeln_Click(object? sender, EventArgs e)
    {
        try
        {
            // Windows 10/11: Bluetooth-Einstellungen öffnen
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:bluetooth",
                UseShellExecute = true
            });

            lblKoppelnInfo.Text = "Füge ein Gerät in den Windows-Einstellungen hinzu. Klicke dann auf 'Aktualisieren'.";
        }
        catch (Exception)
        {
            // Fallback: Systemsteuerung Bluetooth
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "control.exe",
                    Arguments = "bthprops.cpl",
                    UseShellExecute = true
                });
                lblKoppelnInfo.Text = "Füge ein Gerät im Bluetooth-Assistenten hinzu. Klicke dann auf 'Aktualisieren'.";
            }
            catch
            {
                MessageBox.Show(
                    "Bluetooth-Einstellungen konnten nicht geöffnet werden.\n\n" +
                    "Bitte öffne manuell:\n" +
                    "  Windows 10/11: Einstellungen → Bluetooth & Geräte\n" +
                    "  Windows 7/8:   Systemsteuerung → Geräte und Drucker → Bluetooth",
                    "Bluetooth", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    /// <summary>
    /// Aktualisiert alle Listen (COM-Ports und Bluetooth-Geräte).
    /// </summary>
    private void btnBtAktualisieren_Click(object? sender, EventArgs e)
    {
        var vorher = cboBtPort.Text;
        LadeComPorts();
        LadeBluetoothGeraete();

        // Port-Auswahl wiederherstellen, falls noch vorhanden
        for (int i = 0; i < cboBtPort.Items.Count; i++)
        {
            if (cboBtPort.Items[i]!.ToString()!.StartsWith(
                    vorher.Split(' ')[0], StringComparison.OrdinalIgnoreCase))
            {
                cboBtPort.SelectedIndex = i;
                break;
            }
        }

        lblKoppelnInfo.Text = "Listen aktualisiert.";
    }

    /// <summary>
    /// Übernimmt den ausgewählten Bluetooth-COM-Port aus der Geräte-Liste.
    /// </summary>
    private void lstBluetoothGeraete_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (lstBluetoothGeraete.SelectedItem is not string auswahl) return;
        if (!auswahl.StartsWith("🔌 ")) return;

        // COM-Port aus Eintrag extrahieren
        var ohneIcon = auswahl.Substring(2);
        var port = ohneIcon.Split(' ', '–', '-')[0].Trim();

        if (port.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
        {
            // Suche passenden Eintrag in cboBtPort
            for (int i = 0; i < cboBtPort.Items.Count; i++)
            {
                if (cboBtPort.Items[i]!.ToString()!.StartsWith(port,
                        StringComparison.OrdinalIgnoreCase))
                {
                    cboBtPort.SelectedIndex = i;
                    lblKoppelnInfo.Text = $"COM-Port {port} ausgewählt.";
                    break;
                }
            }
        }
    }

    /// <summary>Liefert eine Map COM-Port → Anzeigename via WMI.</summary>
    private static Dictionary<string, string> HoleFriendlyNames()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name FROM Win32_PnPEntity WHERE Name LIKE '%(COM%)'");
            foreach (ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString();
                if (string.IsNullOrEmpty(name)) continue;

                // Name enthält z. B. "Standard Serial over Bluetooth link (COM5)"
                var start = name.LastIndexOf('(');
                var end   = name.LastIndexOf(')');
                if (start >= 0 && end > start)
                {
                    var port = name[(start + 1)..end].Trim();
                    var label = name[..start].Trim();
                    result[port] = label;
                }
            }
        }
        catch (Exception ex)
        {
            // WMI nicht verfügbar – Fallback auf nur Port-Namen
            System.Diagnostics.Debug.WriteLine($"FormTachymeterKommunikation: WMI nicht verfügbar: {ex.Message}");
        }
        return result;
    }

    private void LadeAktuelleEinstellungen()
    {
        // Gerätetyp
        rdoGnss.Checked        = ProjektManager.IstGnssGeraet;
        rdoTachymeter.Checked  = !ProjektManager.IstGnssGeraet;
        AktualisiereGeraetTypUI();

        // Modell
        var idx = Array.IndexOf(TachymeterVerbindung.AlleModelle, TachymeterVerbindung.Modell);
        cboModell.SelectedIndex = idx >= 0 ? idx : 0;

        // Baudrate
        cboBaudrate.SelectedItem = TachymeterVerbindung.BaudRate.ToString();
        if (cboBaudrate.SelectedIndex < 0) cboBaudrate.SelectedIndex = 0;

        // Datenbits
        cboDatenbits.SelectedItem = TachymeterVerbindung.DataBits.ToString();
        if (cboDatenbits.SelectedIndex < 0) cboDatenbits.SelectedIndex = 1; // "8"

        // Parität
        cboParitaet.SelectedItem = TachymeterVerbindung.Parität.ToString();
        if (cboParitaet.SelectedIndex < 0) cboParitaet.SelectedIndex = 0;

        // Stoppbits
        cboStoppbits.SelectedItem = StopBitsAnzeige(TachymeterVerbindung.StopBits);
        if (cboStoppbits.SelectedIndex < 0) cboStoppbits.SelectedIndex = 0;

        // Parameter-Gruppe nur bei Manuell bearbeitbar
        AktualisiereParameterFelder();
    }

    // ── Gerätetyp-Umschalten ──────────────────────────────────────────────────

    private void rdoGeraetTyp_CheckedChanged(object? sender, EventArgs e)
    {
        AktualisiereGeraetTypUI();
    }

    private void AktualisiereGeraetTypUI()
    {
        bool istGnss = rdoGnss.Checked;
        // Tachymeter-spezifische Felder ausgrauen wenn GNSS gewählt
        grpModell.Enabled    = !istGnss;
        grpParameter.Enabled = !istGnss;
        if (istGnss)
        {
            grpModell.Text    = "Gerät / Protokoll  (nicht relevant für GNSS)";
            grpParameter.Text = "Kommunikationsparameter  (9600 Baud, 8N1 für NMEA)";
        }
        else
        {
            grpModell.Text    = "Gerät / Protokoll";
            grpParameter.Text = "Kommunikationsparameter";
        }
    }

    // ── Ereignisse ────────────────────────────────────────────────────────────

    private void cboModell_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (cboModell.SelectedIndex < 0) return;
        var modell = TachymeterVerbindung.AlleModelle[cboModell.SelectedIndex];

        if (modell != TachymeterModell.Manuell)
        {
            var (baud, bits, par, stop) = TachymeterVerbindung.GetPreset(modell);
            cboBaudrate.SelectedItem  = baud.ToString();
            cboDatenbits.SelectedItem = bits.ToString();
            cboParitaet.SelectedItem  = par.ToString();
            cboStoppbits.SelectedItem = StopBitsAnzeige(stop);
        }

        AktualisiereParameterFelder();
    }

    private void AktualisiereParameterFelder()
    {
        var manuell = cboModell.SelectedIndex >= 0 &&
                      TachymeterVerbindung.AlleModelle[cboModell.SelectedIndex]
                          == TachymeterModell.Manuell;
        cboBaudrate.Enabled  = manuell;
        cboDatenbits.Enabled = manuell;
        cboParitaet.Enabled  = manuell;
        cboStoppbits.Enabled = manuell;
    }

    private void btnAktualisieren_Click(object? sender, EventArgs e)
    {
        var vorher = cboBtPort.Text;
        LadeComPorts();
        // Port-Auswahl wiederherstellen, falls noch vorhanden
        for (int i = 0; i < cboBtPort.Items.Count; i++)
        {
            if (cboBtPort.Items[i]!.ToString()!.StartsWith(
                    vorher.Split(' ')[0], StringComparison.OrdinalIgnoreCase))
            {
                cboBtPort.SelectedIndex = i;
                break;
            }
        }
    }

    private void btnVerbinden_Click(object? sender, EventArgs e)
    {
        if (TachymeterVerbindung.IstVerbunden)
        {
            TachymeterVerbindung.Trennen();
        }
        else
        {
            if (!PortAusEintrag(cboBtPort.Text, out var port))
            {
                MessageBox.Show("Bitte einen gültigen COM-Port auswählen.",
                    "Tachymeter", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            TachymeterVerbindung.Port     = port;
            TachymeterVerbindung.BaudRate = int.Parse(cboBaudrate.SelectedItem!.ToString()!);
            TachymeterVerbindung.DataBits = int.Parse(cboDatenbits.SelectedItem!.ToString()!);
            TachymeterVerbindung.Parität  = Enum.Parse<Parity>(cboParitaet.SelectedItem!.ToString()!);
            TachymeterVerbindung.StopBits = StopBitsVonAnzeige(cboStoppbits.SelectedItem!.ToString()!);

            try
            {
                TachymeterVerbindung.Verbinden();
                TachymeterVerbindung.SpeichereEinstellungen();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Verbindung fehlgeschlagen:\n{ex.Message}",
                    "Tachymeter", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        AktualisiereVerbindungsStatus();
    }

    private void AktualisiereVerbindungsStatus()
    {
        if (TachymeterVerbindung.IstVerbunden)
        {
            lblStatusIndikator.BackColor = Color.LimeGreen;
            lblStatusText.Text           = $"Verbunden  ({TachymeterVerbindung.Port})";
            lblStatusText.ForeColor      = Color.DarkGreen;
            btnVerbinden.Text            = "Trennen";
            cboBtPort.Enabled            = false;
        }
        else
        {
            lblStatusIndikator.BackColor = Color.FromArgb(200, 50, 50);
            lblStatusText.Text           = "Getrennt";
            lblStatusText.ForeColor      = Color.FromArgb(160, 40, 40);
            btnVerbinden.Text            = "Verbinden";
            cboBtPort.Enabled            = true;
        }
    }

    private void btnOK_Click(object? sender, EventArgs e)
    {
        // Gerätetyp speichern
        ProjektManager.IstGnssGeraet = rdoGnss.Checked;

        // Einstellungen übernehmen (ohne Verbindungsaufbau)
        if (cboModell.SelectedIndex >= 0)
            TachymeterVerbindung.Modell = TachymeterVerbindung.AlleModelle[cboModell.SelectedIndex];

        if (PortAusEintrag(cboBtPort.Text, out var port))
            TachymeterVerbindung.Port = port;

        if (int.TryParse(cboBaudrate.SelectedItem?.ToString(), out var baud))
            TachymeterVerbindung.BaudRate = baud;

        if (int.TryParse(cboDatenbits.SelectedItem?.ToString(), out var bits))
            TachymeterVerbindung.DataBits = bits;

        if (Enum.TryParse<Parity>(cboParitaet.SelectedItem?.ToString(), out var par))
            TachymeterVerbindung.Parität = par;

        if (cboStoppbits.SelectedItem is string sb)
            TachymeterVerbindung.StopBits = StopBitsVonAnzeige(sb);

        TachymeterVerbindung.SpeichereEinstellungen();

        DialogResult = DialogResult.OK;
        Close();
    }

    private void btnAbbrechen_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    // ── Hilfsmethoden ─────────────────────────────────────────────────────────

    private static bool PortAusEintrag(string eintrag, out string port)
    {
        port = eintrag.Split(' ', '–', '-')[0].Trim();
        return port.StartsWith("COM", StringComparison.OrdinalIgnoreCase) && port.Length > 3;
    }

    private static string StopBitsAnzeige(StopBits s) => s switch
    {
        StopBits.One          => "1",
        StopBits.OnePointFive => "1.5",
        StopBits.Two          => "2",
        _                     => "1"
    };

    private static StopBits StopBitsVonAnzeige(string s) => s switch
    {
        "1.5" => StopBits.OnePointFive,
        "2"   => StopBits.Two,
        _     => StopBits.One
    };
}
