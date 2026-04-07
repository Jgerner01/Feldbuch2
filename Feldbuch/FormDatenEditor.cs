namespace Feldbuch;

public partial class FormDatenEditor : Form
{
    private readonly DatenPanelControl _panel1 = new() { Dock = DockStyle.Fill };
    private readonly DatenPanelControl _panel2 = new() { Dock = DockStyle.Fill };

    public FormDatenEditor()
    {
        InitializeComponent();
        _split.Panel1.Controls.Add(_panel1);
        _split.Panel2.Controls.Add(_panel2);
        Shown += (_, _) =>
        {
            _split.Panel1MinSize    = 380;
            _split.Panel2MinSize    = 380;
            _split.SplitterDistance = (_split.Width - _split.SplitterWidth) / 2;
        };
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        _panel1.Speichern();
        _panel2.Speichern();
    }
}
