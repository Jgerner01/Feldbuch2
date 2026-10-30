namespace Feldbuch;

using System.Globalization;

public partial class FormRechenparameter : Form
{
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

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

        // Polarpunkte
        rbMassstabEins.Checked       = p.PolarMassstabModus == MassstabModus.Eins;
        rbMassstabBerechnet.Checked  = p.PolarMassstabModus == MassstabModus.Berechnet;
        rbMassstabVorgegeben.Checked = p.PolarMassstabModus == MassstabModus.Vorgegeben;
        txtMassstabWert.Text         = p.PolarMassstabWert.ToString("F6", IC);
        chkGewichtung.Checked        = p.GewichtungNaheZiele;
        nudZentrZiel.Value           = ZwischenNudGrenzen(nudZentrZiel,  p.ZentrierfehlerZiel_mm);
        nudZentrStand.Value          = ZwischenNudGrenzen(nudZentrStand, p.ZentrierfehlerStandpunkt_mm);
        AktualisiereGewichtungAktiv();
    }

    private static decimal ZwischenNudGrenzen(NumericUpDown nud, double wert)
        => Math.Clamp((decimal)wert, nud.Minimum, nud.Maximum);

    private void AktualisiereGewichtungAktiv()
    {
        nudZentrZiel.Enabled  = chkGewichtung.Checked;
        nudZentrStand.Enabled = chkGewichtung.Checked;
    }

    private void btnOK_Click(object? sender, EventArgs e)
    {
        // Vorgegebenen Maßstab prüfen (Dezimalkomma und -punkt erlaubt)
        double massstab = RechenparameterManager.Params.PolarMassstabWert;
        if (!double.TryParse(txtMassstabWert.Text.Trim().Replace(',', '.'), NumberStyles.Float, IC, out massstab)
            || !PolarpunktRechner.MassstabPlausibel(massstab))
        {
            if (rbMassstabVorgegeben.Checked)
            {
                MessageBox.Show(
                    $"Der vorgegebene Maßstab muss zwischen {PolarpunktRechner.MASSSTAB_MIN.ToString(IC)} und " +
                    $"{PolarpunktRechner.MASSSTAB_MAX.ToString(IC)} liegen (z. B. 0.9996).",
                    "Maßstab", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMassstabWert.Focus();
                return;
            }
            massstab = RechenparameterManager.Params.PolarMassstabWert;   // unbenutzt: alten Wert behalten
        }

        var p = RechenparameterManager.Params;
        p.FehlergrenzCC_Winkel        = (double)nudWinkel.Value;
        p.FehlergrenzeMM_Strecke      = (double)nudStrecke.Value;
        p.FehlergrenzeMM_Hoehe        = (double)nudHoehe.Value;
        p.FehlergrenzeMM_s0AutoMatch  = (double)nudS0AutoMatch.Value;
        p.FreierMassstab              = chkFreierMassstab.Checked;
        p.Berechnung3D                = chkBerechnung3D.Checked;
        p.SigmaFaktor                 = cmbSigma.SelectedIndex + 1;
        p.PolarMassstabModus          = rbMassstabVorgegeben.Checked ? MassstabModus.Vorgegeben
                                      : rbMassstabBerechnet.Checked  ? MassstabModus.Berechnet
                                      : MassstabModus.Eins;
        if (p.PolarMassstabWert != massstab) p.PolarMassstabQuelle = "manuell";
        p.PolarMassstabWert           = massstab;
        p.GewichtungNaheZiele         = chkGewichtung.Checked;
        p.ZentrierfehlerZiel_mm       = (double)nudZentrZiel.Value;
        p.ZentrierfehlerStandpunkt_mm = (double)nudZentrStand.Value;
        RechenparameterManager.Save();
        DialogResult = DialogResult.OK;
    }

    private void btnAbbrechen_Click(object? sender, EventArgs e)
        => DialogResult = DialogResult.Cancel;
}
