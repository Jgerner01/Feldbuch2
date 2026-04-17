using System.IO.Ports;
using System.Management;
using System.Diagnostics;

namespace Feldbuch;

public partial class FormTachymeterKommunikation : Form
{
    public FormTachymeterKommunikation()
    {
        InitializeComponent();
        LadeGeraetTypen();
        LadeModelle();
        LadeComPorts();
        LadeBluetoothGeraete();
        LadeAktuelleEinstellungen();
        AktualisiereVerbindungsStatus();
    }

    // ── Initialisierung ───────────────────────────────────────────────────────

    private void LadeGeraetTypen()
    {
        cboGeraetTyp.BeginUpdate();
        cboGeraetTyp.Items.Clear();
        foreach (var m in TachymeterVerbindung.AlleModelle)
            cboGeraetTyp.Items.Add(TachymeterVerbindung.ModellAnzeige(m));
        cboGeraetTyp.EndUpdate();
    }

    private void LadeModelle()
    {
        // cboModell ist der bisherige Präzisions-Sub-Selektor (z.B. für GeoCOM 38400/9600)
        // – wird aktuell durch cboGeraetTyp ersetzt; bleibt als Alias für AlleModelle
        cboModell.BeginUpdate();
        cboModell.Items.Clear();
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

    private void LadeBluetoothGeraete()
    {
        lstBluetoothGeraete.BeginUpdate();
        lstBluetoothGeraete.Items.Clear();

        try
        {
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

            var btComPorts = new List<string>();
            using var comSearcher = new ManagementObjectSearcher(
                "SELECT Name, DeviceID FROM Win32_PnPEntity WHERE Name LIKE '%(COM%)'");
            foreach (ManagementObject obj in comSearcher.Get())
            {
                var name = obj["Name"]?.ToString();
                if (!string.IsNullOrEmpty(name) && name.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase))
                {
                    var start = name.LastIndexOf('(');
                    var end = name.LastIndexOf(')');
                    if (start >= 0 && end > start)
                    {
                        var port  = name[(start + 1)..end].Trim();
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

    private void btnGeraetKoppeln_Click(object? sender, EventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:bluetooth",
                UseShellExecute = true
            });
            lblKoppelnInfo.Text = "Füge ein Gerät in den Windows-Einstellungen hinzu. Klicke dann auf 'Aktualisieren'.";
        }
        catch
        {
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

    private void btnBtAktualisieren_Click(object? sender, EventArgs e)
    {
        var vorher = cboBtPort.Text;
        LadeComPorts();
        LadeBluetoothGeraete();
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

    private void lstBluetoothGeraete_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (lstBluetoothGeraete.SelectedItem is not string auswahl) return;
        if (!auswahl.StartsWith("🔌 ")) return;

        var ohneIcon = auswahl[2..];
        var port = ohneIcon.Split(' ', '–', '-')[0].Trim();

        if (port.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
        {
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
                var start = name.LastIndexOf('(');
                var end   = name.LastIndexOf(')');
                if (start >= 0 && end > start)
                {
                    var port  = name[(start + 1)..end].Trim();
                    var label = name[..start].Trim();
                    result[port] = label;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FormTachymeterKommunikation: WMI: {ex.Message}");
        }
        return result;
    }

    private void LadeAktuelleEinstellungen()
    {
        // Gerätetyp-ComboBox auswählen
        var idx = Array.IndexOf(TachymeterVerbindung.AlleModelle, TachymeterVerbindung.Modell);
        cboGeraetTyp.SelectedIndex = idx >= 0 ? idx : 0;

        // Modell-ComboBox synchron halten
        cboModell.SelectedIndex = idx >= 0 ? idx : 0;

        // Baudrate
        cboBaudrate.SelectedItem = TachymeterVerbindung.BaudRate.ToString();
        if (cboBaudrate.SelectedIndex < 0) cboBaudrate.SelectedIndex = 0;

        // Datenbits
        cboDatenbits.SelectedItem = TachymeterVerbindung.DataBits.ToString();
        if (cboDatenbits.SelectedIndex < 0) cboDatenbits.SelectedIndex = 1;

        // Parität
        cboParitaet.SelectedItem = TachymeterVerbindung.Parität.ToString();
        if (cboParitaet.SelectedIndex < 0) cboParitaet.SelectedIndex = 0;

        // Stoppbits
        cboStoppbits.SelectedItem = StopBitsAnzeige(TachymeterVerbindung.StopBits);
        if (cboStoppbits.SelectedIndex < 0) cboStoppbits.SelectedIndex = 0;

        AktualisiereParameterFelder();
    }

    // ── Gerätetyp-Auswahl ─────────────────────────────────────────────────────

    private void cboGeraetTyp_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (cboGeraetTyp.SelectedIndex < 0) return;
        var modell = TachymeterVerbindung.AlleModelle[cboGeraetTyp.SelectedIndex];

        // Modell-Combobox synchron halten (gleicher Index, da identische Listen)
        cboModell.SelectedIndex = cboGeraetTyp.SelectedIndex;

        // Kommunikationsparameter auto-befüllen (außer Manuell)
        if (modell != TachymeterModell.Manuell)
        {
            var (baud, bits, par, stop) = TachymeterVerbindung.GetPreset(modell);
            cboBaudrate.SelectedItem  = baud.ToString();
            cboDatenbits.SelectedItem = bits.ToString();
            cboParitaet.SelectedItem  = par.ToString();
            cboStoppbits.SelectedItem = StopBitsAnzeige(stop);
        }

        AktualisiereParameterFelder();
        AktualisiereGeraetTypUI();
    }

    private void cboModell_SelectedIndexChanged(object? sender, EventArgs e)
    {
        // Wenn cboModell separat bedient wird, cboGeraetTyp synchronisieren
        if (cboModell.SelectedIndex == cboGeraetTyp.SelectedIndex) return;
        cboGeraetTyp.SelectedIndex = cboModell.SelectedIndex;
    }

    private void AktualisiereGeraetTypUI()
    {
        if (cboGeraetTyp.SelectedIndex < 0) return;
        var modell = TachymeterVerbindung.AlleModelle[cboGeraetTyp.SelectedIndex];
        bool istGnss = modell == TachymeterModell.GnssNmea;
        bool istManuell = modell == TachymeterModell.Manuell;

        grpModell.Enabled    = !istGnss;
        grpParameter.Enabled = !istGnss || istManuell;

        if (istGnss)
        {
            grpModell.Text    = "Gerät / Protokoll  (nicht relevant für GNSS)";
            grpParameter.Text = "Kommunikationsparameter  (typisch: 4800 Baud, 8N1 für NMEA)";
        }
        else
        {
            grpModell.Text    = "Gerät / Protokoll";
            grpParameter.Text = "Kommunikationsparameter";
        }
    }

    private void AktualisiereParameterFelder()
    {
        var manuell = cboGeraetTyp.SelectedIndex >= 0 &&
                      TachymeterVerbindung.AlleModelle[cboGeraetTyp.SelectedIndex]
                          == TachymeterModell.Manuell;
        cboBaudrate.Enabled  = manuell;
        cboDatenbits.Enabled = manuell;
        cboParitaet.Enabled  = manuell;
        cboStoppbits.Enabled = manuell;
    }

    // ── Verbinden / Trennen ───────────────────────────────────────────────────

    private void btnAktualisieren_Click(object? sender, EventArgs e)
    {
        var vorher = cboBtPort.Text;
        LadeComPorts();
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
        // Gerätetyp (= Modell) speichern
        if (cboGeraetTyp.SelectedIndex >= 0)
            TachymeterVerbindung.Modell = TachymeterVerbindung.AlleModelle[cboGeraetTyp.SelectedIndex];

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
