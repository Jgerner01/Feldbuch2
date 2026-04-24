namespace Feldbuch;

partial class FormAutoMatchProtokoll
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        dgvProtokoll    = new DataGridView();
        lblAnzahl       = new Label();
        btnAktualisieren = new Button();
        btnCsvOeffnen   = new Button();
        btnSchliessen   = new Button();

        SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvProtokoll).BeginInit();

        // ── Fenster ───────────────────────────────────────────────────────────
        ClientSize    = new Size(1100, 540);
        Text          = "Auto-Match-Protokoll";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;

        // ── Grid ─────────────────────────────────────────────────────────────
        dgvProtokoll.Location                    = new Point(10, 10);
        dgvProtokoll.Size                        = new Size(1080, 470);
        dgvProtokoll.ReadOnly                    = true;
        dgvProtokoll.AllowUserToAddRows          = false;
        dgvProtokoll.AllowUserToDeleteRows       = false;
        dgvProtokoll.SelectionMode               = DataGridViewSelectionMode.FullRowSelect;
        dgvProtokoll.Font                        = new Font("Segoe UI", 8.5F);
        dgvProtokoll.AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill;
        dgvProtokoll.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvProtokoll.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;

        dgvProtokoll.Columns.AddRange(
            new DataGridViewTextBoxColumn { HeaderText = "Zeit",         Name = "Zeit"   },
            new DataGridViewTextBoxColumn { HeaderText = "Station",      Name = "Stat"   },
            new DataGridViewTextBoxColumn { HeaderText = "Messung",      Name = "Mess"   },
            new DataGridViewTextBoxColumn { HeaderText = "Vorhergesagt", Name = "Pred"   },
            new DataGridViewTextBoxColumn { HeaderText = "Radius",       Name = "Rad",   FillWeight = 50 },
            new DataGridViewTextBoxColumn { HeaderText = "Treffer",      Name = "Treff", FillWeight = 40 },
            new DataGridViewTextBoxColumn { HeaderText = "Gewählt",      Name = "Gew",   FillWeight = 60 },
            new DataGridViewTextBoxColumn { HeaderText = "Abstand",      Name = "Abst",  FillWeight = 60 },
            new DataGridViewTextBoxColumn { HeaderText = "Ergebnis",     Name = "Erg",   FillWeight = 80 }
        );

        // ── Statuszeile ───────────────────────────────────────────────────────
        lblAnzahl.Location  = new Point(10, 490);
        lblAnzahl.Size      = new Size(200, 24);
        lblAnzahl.Font      = new Font("Segoe UI", 9F);
        lblAnzahl.ForeColor = Color.FromArgb(60, 60, 60);

        // ── Buttons ───────────────────────────────────────────────────────────
        btnAktualisieren.Text      = "Aktualisieren";
        btnAktualisieren.Location  = new Point(560, 487);
        btnAktualisieren.Size      = new Size(150, 32);
        btnAktualisieren.Font      = new Font("Segoe UI", 9F);
        btnAktualisieren.Click    += btnAktualisieren_Click;

        btnCsvOeffnen.Text      = "CSV öffnen";
        btnCsvOeffnen.Location  = new Point(720, 487);
        btnCsvOeffnen.Size      = new Size(150, 32);
        btnCsvOeffnen.Font      = new Font("Segoe UI", 9F);
        btnCsvOeffnen.Click    += btnCsvOeffnen_Click;

        btnSchliessen.Text      = "Schließen";
        btnSchliessen.Location  = new Point(940, 487);
        btnSchliessen.Size      = new Size(150, 32);
        btnSchliessen.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnSchliessen.Click    += btnSchliessen_Click;

        Controls.AddRange(new Control[]
        {
            dgvProtokoll, lblAnzahl,
            btnAktualisieren, btnCsvOeffnen, btnSchliessen
        });

        ((System.ComponentModel.ISupportInitialize)dgvProtokoll).EndInit();
        ResumeLayout(false);
    }

    private DataGridView dgvProtokoll   = null!;
    private Label        lblAnzahl      = null!;
    private Button       btnAktualisieren = null!;
    private Button       btnCsvOeffnen  = null!;
    private Button       btnSchliessen  = null!;
}
