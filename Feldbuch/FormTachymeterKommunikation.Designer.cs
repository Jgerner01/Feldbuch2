using System.IO.Ports;

namespace Feldbuch;

partial class FormTachymeterKommunikation
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        grpModell          = new GroupBox();
        lblModell          = new Label();
        cboModell          = new ComboBox();

        grpBluetooth       = new GroupBox();
        lblSchnittstelle   = new Label();
        cboBtPort          = new ComboBox();
        btnBtAktualisieren = new Button();
        lblStatusIndikator = new Label();
        lblStatusText      = new Label();
        btnVerbinden       = new Button();

        grpBluetoothGeraete = new GroupBox();
        lstBluetoothGeraete = new ListBox();
        btnGeraetKoppeln    = new Button();
        lblKoppelnInfo      = new Label();

        grpParameter       = new GroupBox();
        lblBaudrate        = new Label();
        cboBaudrate        = new ComboBox();
        lblDatenbits       = new Label();
        cboDatenbits       = new ComboBox();
        lblParitaet        = new Label();
        cboParitaet        = new ComboBox();
        lblStoppbits       = new Label();
        cboStoppbits       = new ComboBox();

        btnOK              = new Button();
        btnAbbrechen       = new Button();

        SuspendLayout();

        // ── Fenster ───────────────────────────────────────────────────────────
        ClientSize      = new Size(520, 600);  // Vergrößert für Bluetooth-Liste
        Text            = "Tachymeter Kommunikation";
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        AutoScaleMode   = AutoScaleMode.Font;

        var fontNormal = new Font("Segoe UI", 9.5F);
        var fontLabel  = new Font("Segoe UI", 9.5F);

        // ═══ GroupBox: Tachymeter-Gerät ═══════════════════════════════════════
        grpModell.Text     = "Tachymeter-Gerät";
        grpModell.Location = new Point(12, 12);
        grpModell.Size     = new Size(494, 64);
        grpModell.Font     = fontNormal;

        lblModell.Text     = "Gerät:";
        lblModell.Location = new Point(14, 28);
        lblModell.Size     = new Size(60, 24);
        lblModell.Font     = fontLabel;
        lblModell.TextAlign = ContentAlignment.MiddleLeft;

        cboModell.Location     = new Point(80, 25);
        cboModell.Size         = new Size(396, 26);
        cboModell.DropDownStyle = ComboBoxStyle.DropDownList;
        cboModell.Font         = fontNormal;
        cboModell.SelectedIndexChanged += cboModell_SelectedIndexChanged;

        grpModell.Controls.AddRange([lblModell, cboModell]);

        // ═══ GroupBox: Bluetooth / Schnittstelle ══════════════════════════════
        grpBluetooth.Text     = "Bluetooth / Schnittstelle";
        grpBluetooth.Location = new Point(12, 88);
        grpBluetooth.Size     = new Size(494, 140);
        grpBluetooth.Font     = fontNormal;

        lblSchnittstelle.Text      = "Schnittstelle:";
        lblSchnittstelle.Location  = new Point(14, 32);
        lblSchnittstelle.Size      = new Size(100, 24);
        lblSchnittstelle.Font      = fontLabel;
        lblSchnittstelle.TextAlign = ContentAlignment.MiddleLeft;

        cboBtPort.Location      = new Point(120, 29);
        cboBtPort.Size          = new Size(280, 26);
        cboBtPort.DropDownStyle = ComboBoxStyle.DropDownList;
        cboBtPort.Font          = fontNormal;

        btnBtAktualisieren.Text      = "↻ Aktualisieren";
        btnBtAktualisieren.Location  = new Point(368, 28);
        btnBtAktualisieren.Size      = new Size(108, 28);
        btnBtAktualisieren.Font      = new Font("Segoe UI", 9F);
        btnBtAktualisieren.FlatStyle = FlatStyle.Flat;
        btnBtAktualisieren.Click    += btnBtAktualisieren_Click;

        // Status-Indikator (kleine farbige Kreisfläche)
        lblStatusIndikator.Location  = new Point(20, 78);
        lblStatusIndikator.Size      = new Size(16, 16);
        lblStatusIndikator.BackColor = Color.FromArgb(200, 50, 50);
        lblStatusIndikator.BorderStyle = BorderStyle.FixedSingle;

        lblStatusText.Text      = "Getrennt";
        lblStatusText.Location  = new Point(44, 74);
        lblStatusText.Size      = new Size(220, 24);
        lblStatusText.Font      = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblStatusText.ForeColor = Color.FromArgb(160, 40, 40);

        btnVerbinden.Text      = "Verbinden";
        btnVerbinden.Location  = new Point(358, 70);
        btnVerbinden.Size      = new Size(118, 34);
        btnVerbinden.Font      = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnVerbinden.BackColor = Color.FromArgb(60, 100, 160);
        btnVerbinden.ForeColor = Color.White;
        btnVerbinden.FlatStyle = FlatStyle.Flat;
        btnVerbinden.FlatAppearance.BorderColor = Color.FromArgb(40, 80, 140);
        btnVerbinden.Click    += btnVerbinden_Click;

        grpBluetooth.Controls.AddRange(
        [
            lblSchnittstelle, cboBtPort, btnBtAktualisieren,
            lblStatusIndikator, lblStatusText, btnVerbinden
        ]);

        // ═══ GroupBox: Bluetooth-Geräte ═══════════════════════════════════════
        grpBluetoothGeraete.Text     = "Bluetooth-Geräte (gekoppelt)";
        grpBluetoothGeraete.Location = new Point(12, 238);
        grpBluetoothGeraete.Size     = new Size(494, 140);
        grpBluetoothGeraete.Font     = fontNormal;

        lstBluetoothGeraete.Location    = new Point(14, 28);
        lstBluetoothGeraete.Size        = new Size(462, 70);
        lstBluetoothGeraete.Font        = new Font("Segoe UI", 9F);
        lstBluetoothGeraete.BorderStyle = BorderStyle.FixedSingle;
        lstBluetoothGeraete.SelectionMode = SelectionMode.One;
        lstBluetoothGeraete.SelectedIndexChanged += lstBluetoothGeraete_SelectedIndexChanged;

        btnGeraetKoppeln.Text      = "+ Neues Gerät koppeln";
        btnGeraetKoppeln.Location  = new Point(14, 104);
        btnGeraetKoppeln.Size      = new Size(180, 28);
        btnGeraetKoppeln.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnGeraetKoppeln.BackColor = Color.FromArgb(50, 120, 70);
        btnGeraetKoppeln.ForeColor = Color.White;
        btnGeraetKoppeln.FlatStyle = FlatStyle.Flat;
        btnGeraetKoppeln.FlatAppearance.BorderColor = Color.FromArgb(35, 95, 52);
        btnGeraetKoppeln.Click    += btnGeraetKoppeln_Click;

        lblKoppelnInfo.Text      = "Klicke auf 'Neues Gerät koppeln' um den Windows Bluetooth-Assistenten zu öffnen.";
        lblKoppelnInfo.Location  = new Point(210, 108);
        lblKoppelnInfo.Size      = new Size(266, 24);
        lblKoppelnInfo.Font      = new Font("Segoe UI", 8F, FontStyle.Italic);
        lblKoppelnInfo.ForeColor = Color.FromArgb(100, 100, 100);
        lblKoppelnInfo.TextAlign = ContentAlignment.MiddleLeft;

        grpBluetoothGeraete.Controls.AddRange(
        [
            lstBluetoothGeraete, btnGeraetKoppeln, lblKoppelnInfo
        ]);

        // ═══ GroupBox: Kommunikationsparameter ════════════════════════════════
        grpParameter.Text     = "Kommunikationsparameter";
        grpParameter.Location = new Point(12, 388);
        grpParameter.Size     = new Size(494, 130);
        grpParameter.Font     = fontNormal;

        var lblInfo = new Label
        {
            Text      = "Voreinstellungen werden automatisch je nach Geräteauswahl gesetzt.",
            Location  = new Point(14, 16),
            Size      = new Size(462, 18),
            Font      = new Font("Segoe UI", 8F, FontStyle.Italic),
            ForeColor = Color.FromArgb(100, 100, 100)
        };

        lblBaudrate.Text      = "Baudrate:";
        lblBaudrate.Location  = new Point(14, 44);
        lblBaudrate.Size      = new Size(80, 24);
        lblBaudrate.Font      = fontLabel;
        lblBaudrate.TextAlign = ContentAlignment.MiddleLeft;

        cboBaudrate.Location      = new Point(100, 42);
        cboBaudrate.Size          = new Size(100, 26);
        cboBaudrate.DropDownStyle = ComboBoxStyle.DropDownList;
        cboBaudrate.Font          = fontNormal;
        cboBaudrate.Items.AddRange(["1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200"]);
        cboBaudrate.SelectedItem  = "9600";

        lblDatenbits.Text      = "Datenbits:";
        lblDatenbits.Location  = new Point(220, 44);
        lblDatenbits.Size      = new Size(80, 24);
        lblDatenbits.Font      = fontLabel;
        lblDatenbits.TextAlign = ContentAlignment.MiddleLeft;

        cboDatenbits.Location      = new Point(300, 42);
        cboDatenbits.Size          = new Size(70, 26);
        cboDatenbits.DropDownStyle = ComboBoxStyle.DropDownList;
        cboDatenbits.Font          = fontNormal;
        cboDatenbits.Items.AddRange(["7", "8"]);
        cboDatenbits.SelectedItem  = "8";

        lblParitaet.Text      = "Parität:";
        lblParitaet.Location  = new Point(14, 78);
        lblParitaet.Size      = new Size(80, 24);
        lblParitaet.Font      = fontLabel;
        lblParitaet.TextAlign = ContentAlignment.MiddleLeft;

        cboParitaet.Location      = new Point(100, 76);
        cboParitaet.Size          = new Size(90, 26);
        cboParitaet.DropDownStyle = ComboBoxStyle.DropDownList;
        cboParitaet.Font          = fontNormal;
        cboParitaet.Items.AddRange([
            Parity.None.ToString(),
            Parity.Even.ToString(),
            Parity.Odd.ToString()
        ]);
        cboParitaet.SelectedItem = Parity.None.ToString();

        lblStoppbits.Text      = "Stoppbits:";
        lblStoppbits.Location  = new Point(220, 78);
        lblStoppbits.Size      = new Size(80, 24);
        lblStoppbits.Font      = fontLabel;
        lblStoppbits.TextAlign = ContentAlignment.MiddleLeft;

        cboStoppbits.Location      = new Point(300, 76);
        cboStoppbits.Size          = new Size(70, 26);
        cboStoppbits.DropDownStyle = ComboBoxStyle.DropDownList;
        cboStoppbits.Font          = fontNormal;
        cboStoppbits.Items.AddRange(["1", "1.5", "2"]);
        cboStoppbits.SelectedItem  = "1";

        grpParameter.Controls.AddRange(
        [
            lblInfo,
            lblBaudrate, cboBaudrate,
            lblDatenbits, cboDatenbits,
            lblParitaet, cboParitaet,
            lblStoppbits, cboStoppbits
        ]);

        // ═══ OK / Abbrechen ═══════════════════════════════════════════════════
        btnOK.Text      = "OK";
        btnOK.Location  = new Point(310, 528);
        btnOK.Size      = new Size(90, 34);
        btnOK.Font      = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnOK.Click    += btnOK_Click;

        btnAbbrechen.Text      = "Abbrechen";
        btnAbbrechen.Location  = new Point(410, 528);
        btnAbbrechen.Size      = new Size(94, 34);
        btnAbbrechen.Font      = new Font("Segoe UI", 10F);
        btnAbbrechen.Click    += btnAbbrechen_Click;

        Controls.AddRange(
        [
            grpModell, grpBluetooth, grpBluetoothGeraete, grpParameter,
            btnOK, btnAbbrechen
        ]);

        AcceptButton = btnOK;
        CancelButton = btnAbbrechen;
        ResumeLayout(false);
    }

    private GroupBox grpModell;
    private Label    lblModell;
    private ComboBox cboModell;

    private GroupBox grpBluetooth;
    private Label    lblSchnittstelle;
    private ComboBox cboBtPort;
    private Button   btnBtAktualisieren;
    private Label    lblStatusIndikator;
    private Label    lblStatusText;
    private Button   btnVerbinden;

    private GroupBox grpBluetoothGeraete;
    private ListBox  lstBluetoothGeraete;
    private Button   btnGeraetKoppeln;
    private Label    lblKoppelnInfo;

    private GroupBox grpParameter;
    private Label    lblBaudrate;
    private ComboBox cboBaudrate;
    private Label    lblDatenbits;
    private ComboBox cboDatenbits;
    private Label    lblParitaet;
    private ComboBox cboParitaet;
    private Label    lblStoppbits;
    private ComboBox cboStoppbits;

    private Button btnOK;
    private Button btnAbbrechen;
}
