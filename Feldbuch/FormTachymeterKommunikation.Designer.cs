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
        grpGeraetTyp       = new GroupBox();
        lblGeraetTyp       = new Label();
        cboGeraetTyp       = new ComboBox();

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
        ClientSize      = new Size(1200, 800);
        Text            = "Messgerät – Kommunikation";
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        AutoScaleMode   = AutoScaleMode.Font;

        var fontNormal = new Font("Segoe UI", 11F);
        var fontLabel  = new Font("Segoe UI", 11F);
        var fontBold   = new Font("Segoe UI", 11F, FontStyle.Bold);

        // ─────────────────────────────────────────────────────────────────────
        // LINKE SPALTE  (x: 12..584)
        // ─────────────────────────────────────────────────────────────────────

        // ═══ GroupBox: Gerätetyp ══════════════════════════════════════════════
        grpGeraetTyp.Text     = "Gerätetyp";
        grpGeraetTyp.Location = new Point(12, 12);
        grpGeraetTyp.Size     = new Size(572, 72);
        grpGeraetTyp.Font     = fontNormal;

        lblGeraetTyp.Text      = "Typ:";
        lblGeraetTyp.Location  = new Point(16, 28);
        lblGeraetTyp.Size      = new Size(50, 30);
        lblGeraetTyp.Font      = fontLabel;
        lblGeraetTyp.TextAlign = ContentAlignment.MiddleLeft;

        cboGeraetTyp.Location      = new Point(72, 25);
        cboGeraetTyp.Size          = new Size(480, 32);
        cboGeraetTyp.DropDownStyle = ComboBoxStyle.DropDownList;
        cboGeraetTyp.Font          = fontNormal;
        cboGeraetTyp.SelectedIndexChanged += cboGeraetTyp_SelectedIndexChanged;

        grpGeraetTyp.Controls.AddRange([lblGeraetTyp, cboGeraetTyp]);

        // ═══ GroupBox: Gerät / Protokoll ══════════════════════════════════════
        grpModell.Text     = "Gerät / Protokoll";
        grpModell.Location = new Point(12, 96);
        grpModell.Size     = new Size(572, 76);
        grpModell.Font     = fontNormal;

        lblModell.Text     = "Modell:";
        lblModell.Location = new Point(16, 28);
        lblModell.Size     = new Size(68, 30);
        lblModell.Font     = fontLabel;
        lblModell.TextAlign = ContentAlignment.MiddleLeft;

        cboModell.Location      = new Point(90, 25);
        cboModell.Size          = new Size(462, 32);
        cboModell.DropDownStyle = ComboBoxStyle.DropDownList;
        cboModell.Font          = fontNormal;
        cboModell.SelectedIndexChanged += cboModell_SelectedIndexChanged;

        grpModell.Controls.AddRange([lblModell, cboModell]);

        // ═══ GroupBox: Bluetooth / Schnittstelle ══════════════════════════════
        grpBluetooth.Text     = "Bluetooth / Schnittstelle";
        grpBluetooth.Location = new Point(12, 184);
        grpBluetooth.Size     = new Size(572, 170);
        grpBluetooth.Font     = fontNormal;

        lblSchnittstelle.Text      = "Schnittstelle:";
        lblSchnittstelle.Location  = new Point(16, 38);
        lblSchnittstelle.Size      = new Size(120, 32);
        lblSchnittstelle.Font      = fontLabel;
        lblSchnittstelle.TextAlign = ContentAlignment.MiddleLeft;

        cboBtPort.Location      = new Point(142, 34);
        cboBtPort.Size          = new Size(280, 32);
        cboBtPort.DropDownStyle = ComboBoxStyle.DropDownList;
        cboBtPort.Font          = fontNormal;

        btnBtAktualisieren.Text      = "↻ Aktualisieren";
        btnBtAktualisieren.Location  = new Point(430, 32);
        btnBtAktualisieren.Size      = new Size(128, 36);
        btnBtAktualisieren.Font      = new Font("Segoe UI", 10F);
        btnBtAktualisieren.FlatStyle = FlatStyle.Flat;
        btnBtAktualisieren.Click    += btnBtAktualisieren_Click;

        lblStatusIndikator.Location    = new Point(20, 96);
        lblStatusIndikator.Size        = new Size(18, 18);
        lblStatusIndikator.BackColor   = Color.FromArgb(200, 50, 50);
        lblStatusIndikator.BorderStyle = BorderStyle.FixedSingle;

        lblStatusText.Text      = "Getrennt";
        lblStatusText.Location  = new Point(46, 90);
        lblStatusText.Size      = new Size(280, 30);
        lblStatusText.Font      = fontBold;
        lblStatusText.ForeColor = Color.FromArgb(160, 40, 40);

        btnVerbinden.Text      = "Verbinden";
        btnVerbinden.Location  = new Point(394, 82);
        btnVerbinden.Size      = new Size(164, 48);
        btnVerbinden.Font      = new Font("Segoe UI", 12F, FontStyle.Bold);
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
        grpBluetoothGeraete.Location = new Point(12, 368);
        grpBluetoothGeraete.Size     = new Size(572, 356);
        grpBluetoothGeraete.Font     = fontNormal;

        lstBluetoothGeraete.Location      = new Point(14, 34);
        lstBluetoothGeraete.Size          = new Size(544, 240);
        lstBluetoothGeraete.Font          = new Font("Segoe UI", 10F);
        lstBluetoothGeraete.BorderStyle   = BorderStyle.FixedSingle;
        lstBluetoothGeraete.SelectionMode = SelectionMode.One;
        lstBluetoothGeraete.SelectedIndexChanged += lstBluetoothGeraete_SelectedIndexChanged;

        btnGeraetKoppeln.Text      = "+ Neues Gerät koppeln";
        btnGeraetKoppeln.Location  = new Point(14, 284);
        btnGeraetKoppeln.Size      = new Size(220, 52);
        btnGeraetKoppeln.Font      = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnGeraetKoppeln.BackColor = Color.FromArgb(50, 120, 70);
        btnGeraetKoppeln.ForeColor = Color.White;
        btnGeraetKoppeln.FlatStyle = FlatStyle.Flat;
        btnGeraetKoppeln.FlatAppearance.BorderColor = Color.FromArgb(35, 95, 52);
        btnGeraetKoppeln.Click    += btnGeraetKoppeln_Click;

        lblKoppelnInfo.Text      = "Öffnet Windows Bluetooth-Einstellungen.";
        lblKoppelnInfo.Location  = new Point(244, 298);
        lblKoppelnInfo.Size      = new Size(314, 26);
        lblKoppelnInfo.Font      = new Font("Segoe UI", 9F, FontStyle.Italic);
        lblKoppelnInfo.ForeColor = Color.FromArgb(100, 100, 100);
        lblKoppelnInfo.TextAlign = ContentAlignment.MiddleLeft;

        grpBluetoothGeraete.Controls.AddRange(
        [
            lstBluetoothGeraete, btnGeraetKoppeln, lblKoppelnInfo
        ]);

        // ─────────────────────────────────────────────────────────────────────
        // RECHTE SPALTE  (x: 600..1188)
        // ─────────────────────────────────────────────────────────────────────

        // ═══ GroupBox: Kommunikationsparameter ════════════════════════════════
        grpParameter.Text     = "Kommunikationsparameter";
        grpParameter.Location = new Point(600, 12);
        grpParameter.Size     = new Size(588, 220);
        grpParameter.Font     = fontNormal;

        var lblInfo = new Label
        {
            Text      = "Voreinstellungen werden automatisch je nach Geräteauswahl gesetzt.",
            Location  = new Point(16, 20),
            Size      = new Size(556, 22),
            Font      = new Font("Segoe UI", 9F, FontStyle.Italic),
            ForeColor = Color.FromArgb(100, 100, 100)
        };

        lblBaudrate.Text      = "Baudrate:";
        lblBaudrate.Location  = new Point(16, 58);
        lblBaudrate.Size      = new Size(100, 32);
        lblBaudrate.Font      = fontLabel;
        lblBaudrate.TextAlign = ContentAlignment.MiddleLeft;

        cboBaudrate.Location      = new Point(120, 56);
        cboBaudrate.Size          = new Size(130, 32);
        cboBaudrate.DropDownStyle = ComboBoxStyle.DropDownList;
        cboBaudrate.Font          = fontNormal;
        cboBaudrate.Items.AddRange(["1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200"]);
        cboBaudrate.SelectedItem  = "9600";

        lblDatenbits.Text      = "Datenbits:";
        lblDatenbits.Location  = new Point(270, 58);
        lblDatenbits.Size      = new Size(100, 32);
        lblDatenbits.Font      = fontLabel;
        lblDatenbits.TextAlign = ContentAlignment.MiddleLeft;

        cboDatenbits.Location      = new Point(374, 56);
        cboDatenbits.Size          = new Size(80, 32);
        cboDatenbits.DropDownStyle = ComboBoxStyle.DropDownList;
        cboDatenbits.Font          = fontNormal;
        cboDatenbits.Items.AddRange(["7", "8"]);
        cboDatenbits.SelectedItem  = "8";

        lblParitaet.Text      = "Parität:";
        lblParitaet.Location  = new Point(16, 110);
        lblParitaet.Size      = new Size(100, 32);
        lblParitaet.Font      = fontLabel;
        lblParitaet.TextAlign = ContentAlignment.MiddleLeft;

        cboParitaet.Location      = new Point(120, 108);
        cboParitaet.Size          = new Size(120, 32);
        cboParitaet.DropDownStyle = ComboBoxStyle.DropDownList;
        cboParitaet.Font          = fontNormal;
        cboParitaet.Items.AddRange([
            Parity.None.ToString(),
            Parity.Even.ToString(),
            Parity.Odd.ToString()
        ]);
        cboParitaet.SelectedItem = Parity.None.ToString();

        lblStoppbits.Text      = "Stoppbits:";
        lblStoppbits.Location  = new Point(270, 110);
        lblStoppbits.Size      = new Size(100, 32);
        lblStoppbits.Font      = fontLabel;
        lblStoppbits.TextAlign = ContentAlignment.MiddleLeft;

        cboStoppbits.Location      = new Point(374, 108);
        cboStoppbits.Size          = new Size(80, 32);
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
        btnOK.Location  = new Point(922, 744);
        btnOK.Size      = new Size(130, 48);
        btnOK.Font      = new Font("Segoe UI", 12F, FontStyle.Bold);
        btnOK.Click    += btnOK_Click;

        btnAbbrechen.Text      = "Abbrechen";
        btnAbbrechen.Location  = new Point(1062, 744);
        btnAbbrechen.Size      = new Size(130, 48);
        btnAbbrechen.Font      = new Font("Segoe UI", 12F);
        btnAbbrechen.Click    += btnAbbrechen_Click;

        Controls.AddRange(
        [
            grpGeraetTyp, grpModell, grpBluetooth, grpBluetoothGeraete,
            grpParameter,
            btnOK, btnAbbrechen
        ]);

        AcceptButton = btnOK;
        CancelButton = btnAbbrechen;
        ResumeLayout(false);
    }

    private GroupBox grpGeraetTyp;
    private Label    lblGeraetTyp;
    private ComboBox cboGeraetTyp;

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
