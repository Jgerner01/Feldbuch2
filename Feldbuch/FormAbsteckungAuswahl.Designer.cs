namespace Feldbuch;

partial class FormAbsteckungAuswahl
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        lblTitel              = new Label();
        btnPunktabsteckung    = new Button();
        btnAchsabsteckung     = new Button();
        btnSchnurgeruest      = new Button();
        btnRasterabsteckung   = new Button();
        btnProfilabsteckung   = new Button();
        btnFlächenteilung     = new Button();
        btnSchliessen         = new Button();

        SuspendLayout();

        ClientSize      = new Size(420, 560);
        Text            = "Absteckungsverfahren";
        StartPosition   = FormStartPosition.CenterParent;
        AutoScaleMode   = AutoScaleMode.Font;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;

        var lf    = new Font("Segoe UI", 11F);
        var lfTit = new Font("Segoe UI", 12F, FontStyle.Bold);

        lblTitel.Text      = "Absteckungsverfahren wählen:";
        lblTitel.Location  = new Point(20, 24);
        lblTitel.Size      = new Size(380, 28);
        lblTitel.Font      = lfTit;

        void MakeBtn(Button b, string text, int y, EventHandler handler)
        {
            b.Text      = text;
            b.Location  = new Point(40, y);
            b.Size      = new Size(340, 48);
            b.Font      = lf;
            b.BackColor = Color.FromArgb(215, 228, 248);
            b.FlatStyle = FlatStyle.Flat;
            b.Click    += handler;
        }

        MakeBtn(btnPunktabsteckung,  "Punktabsteckung",   72,  btnPunktabsteckung_Click);
        MakeBtn(btnAchsabsteckung,   "Achsabsteckung",    132, btnAchsabsteckung_Click);
        MakeBtn(btnSchnurgeruest,    "Schnurgerüst",      192, btnSchnurgeruest_Click);
        MakeBtn(btnRasterabsteckung, "Rasterabsteckung",  252, btnRasterabsteckung_Click);
        MakeBtn(btnProfilabsteckung, "Profilabsteckung",    312, btnProfilabsteckung_Click);
        MakeBtn(btnFlächenteilung,  "Flächenteilung …",   372, btnFlächenteilung_Click);

        btnSchliessen.Text     = "Abbrechen";
        btnSchliessen.Location = new Point(150, 490);
        btnSchliessen.Size     = new Size(120, 36);
        btnSchliessen.Font     = lf;
        btnSchliessen.Click   += btnSchliessen_Click;

        Controls.AddRange(new Control[]
        {
            lblTitel,
            btnPunktabsteckung, btnAchsabsteckung, btnSchnurgeruest,
            btnRasterabsteckung, btnProfilabsteckung, btnFlächenteilung,
            btnSchliessen
        });

        ResumeLayout(false);
    }

    private Label  lblTitel             = null!;
    private Button btnPunktabsteckung   = null!;
    private Button btnAchsabsteckung    = null!;
    private Button btnSchnurgeruest     = null!;
    private Button btnRasterabsteckung  = null!;
    private Button btnProfilabsteckung  = null!;
    private Button btnFlächenteilung    = null!;
    private Button btnSchliessen        = null!;
}
