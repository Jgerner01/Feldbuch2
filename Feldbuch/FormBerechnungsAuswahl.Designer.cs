namespace Feldbuch;

partial class FormBerechnungsAuswahl
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        lblTitel                  = new Label();
        btnKoordTransformation    = new Button();
        btnRueckwaertsschnitt     = new Button();
        btnVorwaertsschnitt       = new Button();
        btnBogenschnitt           = new Button();
        btnHochpunktherablegung   = new Button();
        btnSchliessen             = new Button();

        SuspendLayout();

        ClientSize    = new Size(400, 440);
        Text          = "Berechnungen";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var lf    = new Font("Segoe UI", 11F);
        var lfB   = new Font("Segoe UI", 11F, FontStyle.Bold);
        var lfTit = new Font("Segoe UI", 12F, FontStyle.Bold);

        lblTitel.Text      = "Berechnungsverfahren wählen:";
        lblTitel.Location  = new Point(20, 24);
        lblTitel.Size      = new Size(360, 28);
        lblTitel.Font      = lfTit;

        btnKoordTransformation.Text      = "Koordinatentransformation";
        btnKoordTransformation.Location  = new Point(40, 72);
        btnKoordTransformation.Size      = new Size(320, 48);
        btnKoordTransformation.Font      = lf;
        btnKoordTransformation.BackColor = Color.FromArgb(220, 230, 245);
        btnKoordTransformation.FlatStyle = FlatStyle.Flat;
        btnKoordTransformation.Click    += btnKoordTransformation_Click;

        btnRueckwaertsschnitt.Text      = "Rückwärtschnitt";
        btnRueckwaertsschnitt.Location  = new Point(40, 132);
        btnRueckwaertsschnitt.Size      = new Size(320, 48);
        btnRueckwaertsschnitt.Font      = lf;
        btnRueckwaertsschnitt.BackColor = Color.FromArgb(220, 230, 245);
        btnRueckwaertsschnitt.FlatStyle = FlatStyle.Flat;
        btnRueckwaertsschnitt.Click    += btnRueckwaertsschnitt_Click;

        btnVorwaertsschnitt.Text      = "Vorwärtschnitt";
        btnVorwaertsschnitt.Location  = new Point(40, 192);
        btnVorwaertsschnitt.Size      = new Size(320, 48);
        btnVorwaertsschnitt.Font      = lf;
        btnVorwaertsschnitt.BackColor = Color.FromArgb(220, 230, 245);
        btnVorwaertsschnitt.FlatStyle = FlatStyle.Flat;
        btnVorwaertsschnitt.Click    += btnVorwaertsschnitt_Click;

        btnBogenschnitt.Text      = "Bogenschnitt";
        btnBogenschnitt.Location  = new Point(40, 252);
        btnBogenschnitt.Size      = new Size(320, 48);
        btnBogenschnitt.Font      = lf;
        btnBogenschnitt.BackColor = Color.FromArgb(220, 230, 245);
        btnBogenschnitt.FlatStyle = FlatStyle.Flat;
        btnBogenschnitt.Click    += btnBogenschnitt_Click;

        btnHochpunktherablegung.Text      = "Hochpunktherablegung";
        btnHochpunktherablegung.Location  = new Point(40, 312);
        btnHochpunktherablegung.Size      = new Size(320, 48);
        btnHochpunktherablegung.Font      = lf;
        btnHochpunktherablegung.BackColor = Color.FromArgb(220, 230, 245);
        btnHochpunktherablegung.FlatStyle = FlatStyle.Flat;
        btnHochpunktherablegung.Click    += btnHochpunktherablegung_Click;

        btnSchliessen.Text      = "Abbrechen";
        btnSchliessen.Location  = new Point(140, 382);
        btnSchliessen.Size      = new Size(120, 36);
        btnSchliessen.Font      = lf;
        btnSchliessen.Click    += btnSchliessen_Click;

        Controls.AddRange(new Control[]
        {
            lblTitel,
            btnKoordTransformation,
            btnRueckwaertsschnitt,
            btnVorwaertsschnitt,
            btnBogenschnitt,
            btnHochpunktherablegung,
            btnSchliessen
        });

        ResumeLayout(false);
    }

    private Label  lblTitel                  = null!;
    private Button btnKoordTransformation    = null!;
    private Button btnRueckwaertsschnitt     = null!;
    private Button btnVorwaertsschnitt       = null!;
    private Button btnBogenschnitt           = null!;
    private Button btnHochpunktherablegung   = null!;
    private Button btnSchliessen             = null!;
}
