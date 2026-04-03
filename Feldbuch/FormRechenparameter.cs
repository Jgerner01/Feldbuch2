namespace Feldbuch;

public partial class FormRechenparameter : Form
{
    public FormRechenparameter()
    {
        InitializeComponent();
        LadeParmeter();
    }

    private void LadeParmeter()
    {
        var p = RechenparameterManager.Params;
        nudWinkel.Value   = (decimal)p.FehlergrenzCC_Winkel;
        nudStrecke.Value  = (decimal)p.FehlergrenzeMM_Strecke;
        nudHoehe.Value    = (decimal)p.FehlergrenzeMM_Hoehe;
        chkFreierMassstab.Checked = p.FreierMassstab;
        chkBerechnung3D.Checked   = p.Berechnung3D;
    }

    private void btnOK_Click(object? sender, EventArgs e)
    {
        var p = RechenparameterManager.Params;
        p.FehlergrenzCC_Winkel    = (double)nudWinkel.Value;
        p.FehlergrenzeMM_Strecke  = (double)nudStrecke.Value;
        p.FehlergrenzeMM_Hoehe    = (double)nudHoehe.Value;
        p.FreierMassstab          = chkFreierMassstab.Checked;
        p.Berechnung3D            = chkBerechnung3D.Checked;
        RechenparameterManager.Save();
        DialogResult = DialogResult.OK;
    }

    private void btnAbbrechen_Click(object? sender, EventArgs e)
        => DialogResult = DialogResult.Cancel;
}
