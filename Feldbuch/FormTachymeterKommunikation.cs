using System.IO.Ports;
using System.Management;

namespace Feldbuch;

public partial class FormTachymeterKommunikation : Form
{
    public FormTachymeterKommunikation()
    {
        InitializeComponent();
        LadeModelle();
        LadeComPorts();
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
