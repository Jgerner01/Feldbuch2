namespace Feldbuch;

partial class FormProjektdaten
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        dgv       = new DataGridView();
        lblTitle  = new Label();
        ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
        SuspendLayout();

        // ── Fenster ───────────────────────────────────────────────────────────
        ClientSize    = new Size(900, 560);
        Text          = "Projektdaten";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        BackColor     = Color.FromArgb(240, 240, 240);
        MinimumSize   = new Size(700, 400);

        // ── Titel ─────────────────────────────────────────────────────────────
        lblTitle.Text      = "Projektdaten – editierbare Tabelle (wird beim Schließen automatisch gespeichert)";
        lblTitle.Location  = new Point(8, 8);
        lblTitle.Size      = new Size(880, 22);
        lblTitle.Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblTitle.Font      = new Font("Segoe UI", 9F, FontStyle.Italic);
        lblTitle.ForeColor = Color.FromArgb(80, 80, 80);

        // ── DataGridView ──────────────────────────────────────────────────────
        dgv.Location              = new Point(8, 36);
        dgv.Size                  = new Size(884, 512);
        dgv.Anchor                = AnchorStyles.Top | AnchorStyles.Bottom |
                                    AnchorStyles.Left | AnchorStyles.Right;
        dgv.AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill;
        dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgv.RowHeadersWidth       = 40;
        dgv.AllowUserToAddRows    = true;
        dgv.AllowUserToDeleteRows = true;
        dgv.EditMode              = DataGridViewEditMode.EditOnKeystrokeOrF2;
        dgv.BackgroundColor       = Color.White;
        dgv.GridColor             = Color.FromArgb(200, 200, 200);
        dgv.BorderStyle           = BorderStyle.Fixed3D;
        dgv.Font                  = new Font("Segoe UI", 9F);
        dgv.DefaultValuesNeeded  += dgv_DefaultValuesNeeded;

        // Spalten definieren
        var colDatum = new DataGridViewTextBoxColumn
        {
            Name = "colDatum", HeaderText = "Datum",
            FillWeight = 12, MinimumWidth = 90
        };
        var colUhrzeit = new DataGridViewTextBoxColumn
        {
            Name = "colUhrzeit", HeaderText = "Uhrzeit",
            FillWeight = 10, MinimumWidth = 75
        };
        var colBearbeiter = new DataGridViewTextBoxColumn
        {
            Name = "colBearbeiter", HeaderText = "Bearbeiter",
            FillWeight = 15, MinimumWidth = 100
        };
        var colKategorie = new DataGridViewTextBoxColumn
        {
            Name = "colKategorie", HeaderText = "Kategorie",
            FillWeight = 15, MinimumWidth = 100
        };
        var colParameter = new DataGridViewTextBoxColumn
        {
            Name = "colParameter", HeaderText = "Parameter",
            FillWeight = 18, MinimumWidth = 110
        };
        var colWert = new DataGridViewTextBoxColumn
        {
            Name = "colWert", HeaderText = "Wert",
            FillWeight = 30, MinimumWidth = 120
        };

        // Kopfzeile stylen
        dgv.ColumnHeadersDefaultCellStyle.BackColor  = Color.FromArgb(50, 80, 130);
        dgv.ColumnHeadersDefaultCellStyle.ForeColor  = Color.White;
        dgv.ColumnHeadersDefaultCellStyle.Font       = new Font("Segoe UI", 9F, FontStyle.Bold);
        dgv.EnableHeadersVisualStyles                = false;

        // Zeilenwechselfarbe
        dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 245, 255);

        dgv.Columns.AddRange(colDatum, colUhrzeit, colBearbeiter,
                             colKategorie, colParameter, colWert);

        Controls.AddRange(new Control[] { lblTitle, dgv });

        ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
        ResumeLayout(false);
    }

    private DataGridView dgv      = null!;
    private Label        lblTitle = null!;
}
