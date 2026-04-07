namespace Feldbuch;

partial class FormDatenEditor
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        _split = new SplitContainer();
        ((System.ComponentModel.ISupportInitialize)_split).BeginInit();
        _split.Panel1.SuspendLayout();
        _split.Panel2.SuspendLayout();
        _split.SuspendLayout();
        SuspendLayout();

        // ── SplitContainer ────────────────────────────────────────────────────
        _split.Dock          = DockStyle.Fill;
        _split.Orientation   = Orientation.Vertical;
        _split.SplitterWidth = 6;
        _split.BackColor     = Color.FromArgb(180, 185, 200);

        // ── Fenster ───────────────────────────────────────────────────────────
        ClientSize      = new Size(1260, 760);
        Text            = "Daten-Manager";
        StartPosition   = FormStartPosition.CenterParent;
        AutoScaleMode   = AutoScaleMode.None;
        Font            = new Font("Segoe UI", 9F);
        MinimumSize     = new Size(900, 500);

        Controls.Add(_split);

        ((System.ComponentModel.ISupportInitialize)_split).EndInit();
        _split.Panel1.ResumeLayout(false);
        _split.Panel2.ResumeLayout(false);
        _split.ResumeLayout(false);
        ResumeLayout(false);
    }

    private SplitContainer _split = null!;
}
