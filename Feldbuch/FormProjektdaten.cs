namespace Feldbuch;

public partial class FormProjektdaten : Form
{
    public FormProjektdaten()
    {
        InitializeComponent();
        LadeTabelle();
    }

    // ── Daten in DataGridView laden ───────────────────────────────────────────
    private void LadeTabelle()
    {
        dgv.Rows.Clear();
        foreach (var e in ProjektdatenManager.GetAll())
            dgv.Rows.Add(e.Datum, e.Uhrzeit, e.Bearbeiter, e.Kategorie, e.Parameter, e.Wert);
    }

    // ── Beim Schließen automatisch speichern ─────────────────────────────────
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        SpeichereTabelle();
        base.OnFormClosing(e);
    }

    private void SpeichereTabelle()
    {
        var liste = new List<ProjektEintrag>();
        foreach (DataGridViewRow row in dgv.Rows)
        {
            if (row.IsNewRow) continue;
            string datum      = row.Cells[0].Value?.ToString() ?? "";
            string uhrzeit    = row.Cells[1].Value?.ToString() ?? "";
            string bearbeiter = row.Cells[2].Value?.ToString() ?? "";
            string kategorie  = row.Cells[3].Value?.ToString() ?? "";
            string parameter  = row.Cells[4].Value?.ToString() ?? "";
            string wert       = row.Cells[5].Value?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(datum) && string.IsNullOrWhiteSpace(parameter)) continue;
            liste.Add(new ProjektEintrag(datum, uhrzeit, bearbeiter, kategorie, parameter, wert));
        }
        ProjektdatenManager.ReplaceAll(liste);
    }

    // ── Neu-Zeile Datum/Uhrzeit vorausfüllen ─────────────────────────────────
    private void dgv_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
    {
        e.Row.Cells["colDatum"].Value      = DateTime.Now.ToString("yyyy-MM-dd");
        e.Row.Cells["colUhrzeit"].Value    = DateTime.Now.ToString("HH:mm:ss");
        e.Row.Cells["colBearbeiter"].Value = ProjektdatenManager.Bearbeiter;
    }
}
