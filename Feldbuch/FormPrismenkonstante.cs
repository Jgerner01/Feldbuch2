namespace Feldbuch;

public partial class FormPrismenkonstante : Form
{
    public decimal GewähltePrismenkonstante { get; private set; } = 0;

    private record PrismenEintrag(string Name, decimal Wert);

    public FormPrismenkonstante()
    {
        InitializeComponent();
        LadeKonstanten();
    }

    private void LadeKonstanten()
    {
        var einträge = LeseCSV();
        var iconButtons = new[] { btnPrisma1, btnPrisma2, btnPrisma3 };

        for (int i = 0; i < iconButtons.Length; i++)
        {
            if (i < einträge.Count)
            {
                var eintrag = einträge[i];
                iconButtons[i].Text = $"{eintrag.Name}\n{eintrag.Wert:+0.0;-0.0} mm";
                iconButtons[i].Tag = eintrag.Wert;
            }
            else
            {
                iconButtons[i].Visible = false;
            }
        }
    }

    private List<PrismenEintrag> LeseCSV()
    {
        var liste = new List<PrismenEintrag>();
        string pfad = AppPfade.Get("prismenkonstanten.csv");

        if (!File.Exists(pfad))
            return liste;

        foreach (var zeile in File.ReadLines(pfad).Skip(1))
        {
            var teile = zeile.Split(',');
            if (teile.Length == 2 && decimal.TryParse(teile[1], System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal wert))
            {
                liste.Add(new PrismenEintrag(teile[0].Trim(), wert));
            }
        }
        return liste;
    }

    private void PrismaButton_Click(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.Tag is decimal wert)
        {
            nudKonstante.Value = Math.Clamp(wert, -50, 50);
        }
    }

    private void btnReflektorlos_Click(object? sender, EventArgs e)
    {
        nudKonstante.Value = 0;
        lblStatus.Text = "Modus: Reflektorlos (0.0 mm)";
    }

    private void btnÜbernehmen_Click(object? sender, EventArgs e)
    {
        GewähltePrismenkonstante = nudKonstante.Value;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void nudKonstante_ValueChanged(object? sender, EventArgs e)
    {
        lblStatus.Text = $"Gewählt: {nudKonstante.Value:+0.0;-0.0;0.0} mm";
    }
}
