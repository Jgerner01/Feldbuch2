namespace Feldbuch;

public partial class FormAbsteckungAuswahl : Form
{
    public FormAbsteckungAuswahl()
    {
        InitializeComponent();
    }

    private void btnPunktabsteckung_Click(object? sender, EventArgs e)
    {
        using var form = new FormPunktabsteckung();
        form.ShowDialog(this);
    }

    private void btnAchsabsteckung_Click(object? sender, EventArgs e)
    {
        using var form = new FormAchsabsteckung();
        form.ShowDialog(this);
    }

    private void btnSchnurgeruest_Click(object? sender, EventArgs e)
    {
        using var form = new FormSchnurgeruest();
        form.ShowDialog(this);
    }

    private void btnRasterabsteckung_Click(object? sender, EventArgs e)
    {
        using var form = new FormRasterabsteckung();
        form.ShowDialog(this);
    }

    private void btnProfilabsteckung_Click(object? sender, EventArgs e)
    {
        using var form = new FormProfilabsteckung();
        form.ShowDialog(this);
    }

    private void btnFlächenteilung_Click(object? sender, EventArgs e)
    {
        using var form = new FormFlächenteilung();
        form.ShowDialog(this);
    }

    private void btnSchliessen_Click(object? sender, EventArgs e) => Close();
}
