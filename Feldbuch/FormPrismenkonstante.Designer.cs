namespace Feldbuch;

partial class FormPrismenkonstante
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        btnPrisma1      = new Button();
        btnPrisma2      = new Button();
        btnPrisma3      = new Button();
        btnReflektorlos = new Button();
        lblEingabe      = new Label();
        nudKonstante    = new NumericUpDown();
        lblStatus       = new Label();
        btnÜbernehmen   = new Button();

        SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)nudKonstante).BeginInit();

        // Fenster
        ClientSize = new Size(480, 380);
        Text = "Prismenkonstante";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        AutoScaleMode = AutoScaleMode.Font;

        var iconFont = new Font("Segoe UI", 10F, FontStyle.Bold);
        var iconSize = new Size(100, 90);

        // Button Prisma 1
        btnPrisma1.Size = iconSize;
        btnPrisma1.Location = new Point(30, 30);
        btnPrisma1.Font = iconFont;
        btnPrisma1.TextAlign = ContentAlignment.MiddleCenter;
        btnPrisma1.Click += PrismaButton_Click;

        // Button Prisma 2
        btnPrisma2.Size = iconSize;
        btnPrisma2.Location = new Point(150, 30);
        btnPrisma2.Font = iconFont;
        btnPrisma2.TextAlign = ContentAlignment.MiddleCenter;
        btnPrisma2.Click += PrismaButton_Click;

        // Button Prisma 3
        btnPrisma3.Size = iconSize;
        btnPrisma3.Location = new Point(270, 30);
        btnPrisma3.Font = iconFont;
        btnPrisma3.TextAlign = ContentAlignment.MiddleCenter;
        btnPrisma3.Click += PrismaButton_Click;

        // Button Reflektorlos
        btnReflektorlos.Size = iconSize;
        btnReflektorlos.Location = new Point(350, 30);
        btnReflektorlos.Font = iconFont;
        btnReflektorlos.Text = "Reflektor-\nlos";
        btnReflektorlos.TextAlign = ContentAlignment.MiddleCenter;
        btnReflektorlos.BackColor = Color.LightSteelBlue;
        btnReflektorlos.Click += btnReflektorlos_Click;

        // Neuanordnung: 2x2 Raster
        btnPrisma1.Location      = new Point(30,  30);
        btnPrisma2.Location      = new Point(150, 30);
        btnPrisma3.Location      = new Point(270, 30);
        btnReflektorlos.Location = new Point(350, 30);

        // Übersichtlicheres 2x2-Layout
        btnPrisma1.Location      = new Point(40,  30);
        btnPrisma2.Location      = new Point(170, 30);
        btnPrisma3.Location      = new Point(40,  145);
        btnReflektorlos.Location = new Point(170, 145);

        // Label Eingabe
        lblEingabe.Text = "Prismenkonstante (mm):";
        lblEingabe.Location = new Point(40, 270);
        lblEingabe.Size = new Size(200, 24);
        lblEingabe.Font = new Font("Segoe UI", 10F);

        // NumericUpDown
        nudKonstante.Minimum = -50;
        nudKonstante.Maximum = 50;
        nudKonstante.DecimalPlaces = 1;
        nudKonstante.Increment = 0.5m;
        nudKonstante.Value = 0;
        nudKonstante.Location = new Point(250, 268);
        nudKonstante.Size = new Size(80, 28);
        nudKonstante.Font = new Font("Segoe UI", 10F);
        nudKonstante.ValueChanged += nudKonstante_ValueChanged;

        // Status-Label
        lblStatus.Text = "Gewählt: 0.0 mm";
        lblStatus.Location = new Point(40, 310);
        lblStatus.Size = new Size(300, 24);
        lblStatus.Font = new Font("Segoe UI", 9F);
        lblStatus.ForeColor = Color.DimGray;

        // Button Übernehmen
        btnÜbernehmen.Text = "Übernehmen";
        btnÜbernehmen.Size = new Size(120, 36);
        btnÜbernehmen.Location = new Point(340, 305);
        btnÜbernehmen.Font = new Font("Segoe UI", 10F);
        btnÜbernehmen.Click += btnÜbernehmen_Click;

        Controls.AddRange(new Control[]
        {
            btnPrisma1, btnPrisma2, btnPrisma3, btnReflektorlos,
            lblEingabe, nudKonstante, lblStatus, btnÜbernehmen
        });

        ((System.ComponentModel.ISupportInitialize)nudKonstante).EndInit();
        ResumeLayout(false);
    }

    private Button btnPrisma1;
    private Button btnPrisma2;
    private Button btnPrisma3;
    private Button btnReflektorlos;
    private Label lblEingabe;
    private NumericUpDown nudKonstante;
    private Label lblStatus;
    private Button btnÜbernehmen;
}
