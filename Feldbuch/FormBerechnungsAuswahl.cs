namespace Feldbuch;

public partial class FormBerechnungsAuswahl : Form
{
    public FormBerechnungsAuswahl()
    {
        InitializeComponent();
    }

    private void btnKoordTransformation_Click(object? sender, EventArgs e)
    {
        using var form = new FormKoordTransformation();
        form.ShowDialog(this);
    }

    private void btnRueckwaertsschnitt_Click(object? sender, EventArgs e)
    {
        using var form = new FormRueckwaertsschnitt();
        form.ShowDialog(this);
    }

    private void btnVorwaertsschnitt_Click(object? sender, EventArgs e)
    {
        using var form = new FormVorwaertsschnitt();
        form.ShowDialog(this);
    }

    private void btnBogenschnitt_Click(object? sender, EventArgs e)
    {
        using var form = new FormBogenschnitt();
        form.ShowDialog(this);
    }

    private void btnHochpunktherablegung_Click(object? sender, EventArgs e)
    {
        using var form = new FormHochpunktherablegung();
        form.ShowDialog(this);
    }

    private void btnSchliessen_Click(object? sender, EventArgs e) => Close();
}
