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
        nudWinkel.Value      = (decimal)p.FehlergrenzCC_Winkel;
        nudStrecke.Value     = (decimal)p.FehlergrenzeMM_Strecke;
        nudHoehe.Value       = (decimal)p.FehlergrenzeMM_Hoehe;
        nudS0AutoMatch.Value = (decimal)p.FehlergrenzeMM_s0AutoMatch;
        chkFreierMassstab.Checked = p.FreierMassstab;
        chkBerechnung3D.Checked   = p.Berechnung3D;
        cmbSigma.SelectedIndex    = Math.Clamp(p.SigmaFaktor, 1, 3) - 1;
    }

    private void btnOK_Click(object? sender, EventArgs e)
    {
        var p = RechenparameterManager.Params;
        p.FehlergrenzCC_Winkel        = (double)nudWinkel.Value;
        p.FehlergrenzeMM_Strecke      = (double)nudStrecke.Value;
        p.FehlergrenzeMM_Hoehe        = (double)nudHoehe.Value;
        p.FehlergrenzeMM_s0AutoMatch  = (double)nudS0AutoMatch.Value;
        p.FreierMassstab              = chkFreierMassstab.Checked;
        p.Berechnung3D                = chkBerechnung3D.Checked;
        p.SigmaFaktor                 = cmbSigma.SelectedIndex + 1;
        RechenparameterManager.Save();
        DialogResult = DialogResult.OK;
    }

    private void btnAbbrechen_Click(object? sender, EventArgs e)
        => DialogResult = DialogResult.Cancel;
}
