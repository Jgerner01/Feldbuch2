namespace Feldbuch;

using System.Globalization;

// ─────────────────────────────────────────────────────────────────────────────
// Universeller Absteck-Einweiser: Zielanzeige, Abweichungsberechnung,
// Einweiser-Grafik (Kompassrose). Wiederverwendbar in allen Absteckformen.
// ─────────────────────────────────────────────────────────────────────────────
public class AbsteckungEinweiserPanel : UserControl
{
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    // Feuert wenn der Benutzer „✓ Markiert" klickt
    public event Action<string>? PunktMarkiert;

    // ── interner Zustand ──────────────────────────────────────────────────────
    private string? _punktNr;
    private double  _hzSoll, _sSoll;
    private bool    _hatZiel;
    private double? _dhz, _ds;
    private double  _tol = 10.0;

    // ── Controls ──────────────────────────────────────────────────────────────
    private readonly Panel        _pnlInfo   = new();
    private readonly Label        _lblPunkt  = new();
    private readonly Label        _lblHzSoll = new();
    private readonly Label        _lblsSoll  = new();
    private readonly Panel        _pnlDraw   = new();
    private readonly Panel        _pnlInput  = new();
    private readonly FlowLayoutPanel _flpRow1 = new();
    private readonly Panel        _pnlRow2   = new();
    private readonly Label        _lblHzIst  = new();
    private readonly TextBox      _txtHzIst  = new();
    private readonly Label        _lblsIst   = new();
    private readonly TextBox      _txtsIst   = new();
    private readonly Label        _lblTol    = new();
    private readonly TextBox      _txtTol    = new();
    private readonly Button       _btnAbw    = new();
    private readonly Label        _lblAbw    = new();
    private readonly Button       _btnMark   = new();

    // ── Konstruktor ───────────────────────────────────────────────────────────
    public AbsteckungEinweiserPanel()
    {
        InitLayout();
    }

    // ── Layout ────────────────────────────────────────────────────────────────
    private void InitLayout()
    {
        var lf  = new Font("Segoe UI", 9F);
        var lfB = new Font("Segoe UI", 9.5F, FontStyle.Bold);

        // ── Info-Zeile ────────────────────────────────────────────────────────
        _pnlInfo.Dock      = DockStyle.Top;
        _pnlInfo.Height    = 26;
        _pnlInfo.BackColor = Color.FromArgb(38, 52, 82);

        _lblPunkt.Dock      = DockStyle.Left;
        _lblPunkt.Width     = 240;
        _lblPunkt.Font      = lfB;
        _lblPunkt.ForeColor = Color.FromArgb(200, 220, 255);
        _lblPunkt.TextAlign = ContentAlignment.MiddleLeft;
        _lblPunkt.Padding   = new Padding(6, 0, 0, 0);
        _lblPunkt.Text      = "Punkt: –";

        _lblHzSoll.Dock      = DockStyle.Left;
        _lblHzSoll.Width     = 210;
        _lblHzSoll.Font      = lf;
        _lblHzSoll.ForeColor = Color.FromArgb(190, 210, 240);
        _lblHzSoll.TextAlign = ContentAlignment.MiddleLeft;
        _lblHzSoll.Text      = "Hz soll: –";

        _lblsSoll.Dock      = DockStyle.Fill;
        _lblsSoll.Font      = lf;
        _lblsSoll.ForeColor = Color.FromArgb(190, 210, 240);
        _lblsSoll.TextAlign = ContentAlignment.MiddleLeft;
        _lblsSoll.Text      = "s soll: –";

        _pnlInfo.Controls.Add(_lblsSoll);
        _pnlInfo.Controls.Add(_lblHzSoll);
        _pnlInfo.Controls.Add(_lblPunkt);

        // ── Einweiser-Grafik ──────────────────────────────────────────────────
        _pnlDraw.Dock      = DockStyle.Fill;
        _pnlDraw.BackColor = Color.FromArgb(28, 32, 42);
        _pnlDraw.Paint    += (_, e) => AbsteckungGrafik.DrawEinweiser(
            e.Graphics, _pnlDraw.ClientRectangle,
            _hzSoll, _sSoll, _dhz, _ds, _tol);

        // ── Eingabe-Bereich ───────────────────────────────────────────────────
        _pnlInput.Dock      = DockStyle.Bottom;
        _pnlInput.Height    = 58;
        _pnlInput.BackColor = Color.FromArgb(48, 54, 72);

        // Zeile 1: Messwert-Eingaben + Abweichungsknopf
        _flpRow1.Dock          = DockStyle.Top;
        _flpRow1.Height        = 30;
        _flpRow1.BackColor     = Color.FromArgb(48, 54, 72);
        _flpRow1.FlowDirection = FlowDirection.LeftToRight;
        _flpRow1.WrapContents  = false;
        _flpRow1.Padding       = new Padding(4, 3, 0, 0);

        void Lbl(Label l, string t) {
            l.Text = t; l.AutoSize = true;
            l.Font = lf; l.ForeColor = Color.FromArgb(190, 205, 230);
            l.Margin = new Padding(4, 5, 2, 0);
            _flpRow1.Controls.Add(l);
        }
        void Txt(TextBox tb, int w) {
            tb.Size = new Size(w, 22); tb.Font = lf;
            tb.Margin = new Padding(0, 4, 6, 0);
            _flpRow1.Controls.Add(tb);
        }

        Lbl(_lblHzIst, "Hz_ist [gon]:");  Txt(_txtHzIst, 100);
        Lbl(_lblsIst,  "s_ist [m]:");     Txt(_txtsIst,   90);
        Lbl(_lblTol,   "Tol. [mm]:");     Txt(_txtTol,    55); _txtTol.Text = "10";

        _btnAbw.Text      = "Abweichung";
        _btnAbw.Size      = new Size(108, 22);
        _btnAbw.Font      = lf;
        _btnAbw.FlatStyle = FlatStyle.Flat;
        _btnAbw.BackColor = Color.FromArgb(60, 100, 160);
        _btnAbw.ForeColor = Color.White;
        _btnAbw.FlatAppearance.BorderColor = Color.FromArgb(40, 80, 140);
        _btnAbw.Margin    = new Padding(4, 4, 0, 0);
        _btnAbw.Click    += OnAbweichung;
        _flpRow1.Controls.Add(_btnAbw);

        // Zeile 2: Abweichungsergebnis + Markiert-Knopf
        _pnlRow2.Dock      = DockStyle.Fill;
        _pnlRow2.BackColor = Color.FromArgb(48, 54, 72);

        _btnMark.Text      = "✓ Punkt markiert";
        _btnMark.Dock      = DockStyle.Right;
        _btnMark.Width     = 160;
        _btnMark.Font      = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _btnMark.FlatStyle = FlatStyle.Flat;
        _btnMark.BackColor = Color.FromArgb(34, 120, 60);
        _btnMark.ForeColor = Color.White;
        _btnMark.FlatAppearance.BorderColor = Color.FromArgb(20, 90, 40);
        _btnMark.Click    += OnMarkiert;

        _lblAbw.Dock      = DockStyle.Fill;
        _lblAbw.Font      = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _lblAbw.ForeColor = Color.FromArgb(255, 200, 90);
        _lblAbw.TextAlign = ContentAlignment.MiddleLeft;
        _lblAbw.Padding   = new Padding(8, 0, 0, 0);
        _lblAbw.Text      = "";

        _pnlRow2.Controls.Add(_lblAbw);
        _pnlRow2.Controls.Add(_btnMark);

        _pnlInput.Controls.Add(_pnlRow2);
        _pnlInput.Controls.Add(_flpRow1);

        Controls.Add(_pnlDraw);
        Controls.Add(_pnlInfo);
        Controls.Add(_pnlInput);

        // Toleranz live aktualisieren
        _txtTol.TextChanged += (_, __) =>
        {
            if (double.TryParse(_txtTol.Text.Replace(',', '.'), NumberStyles.Any, IC, out double t))
                _tol = t;
        };
    }

    // ── Öffentliche API ───────────────────────────────────────────────────────

    public void SetzeZielPunkt(string? nr, double? hzSoll, double? sSoll)
    {
        _punktNr = nr;
        _hatZiel = hzSoll.HasValue && sSoll.HasValue;
        _hzSoll  = hzSoll ?? 0;
        _sSoll   = sSoll  ?? 0;
        _dhz = null;
        _ds  = null;

        _lblPunkt.Text  = nr != null ? $"Punkt: {nr}" : "Punkt: –";
        _lblHzSoll.Text = _hatZiel ? $"Hz soll:  {_hzSoll:F4} gon" : "Hz soll: –";
        _lblsSoll.Text  = _hatZiel ? $"s soll:  {_sSoll:F3} m"     : "s soll: –";
        _lblAbw.Text    = "";
        _txtHzIst.Clear();
        _txtsIst.Clear();
        _pnlDraw.Invalidate();
    }

    public void Zuruecksetzen() => SetzeZielPunkt(null, null, null);

    // ── Abweichung berechnen ──────────────────────────────────────────────────
    private void OnAbweichung(object? sender, EventArgs e)
    {
        if (!_hatZiel) return;
        if (!TryParse(_txtHzIst.Text, out double hzIst) ||
            !TryParse(_txtsIst.Text,  out double sIst)) return;

        double dhz = AbsteckungRechner.Norm400(_hzSoll - hzIst);
        if (dhz > 200) dhz -= 400;
        double ds = (_sSoll - sIst) * 1000.0;

        _dhz = dhz; _ds = ds;
        _lblAbw.Text = $"ΔHz = {dhz:+0.0000;-0.0000} gon    Δs = {ds:+0.0;-0.0} mm";
        _pnlDraw.Invalidate();
    }

    // ── Punkt markiert ────────────────────────────────────────────────────────
    private void OnMarkiert(object? sender, EventArgs e)
    {
        if (_punktNr != null) PunktMarkiert?.Invoke(_punktNr);
    }

    private static bool TryParse(string? s, out double r) =>
        double.TryParse(s?.Replace(',', '.'), NumberStyles.Any, IC, out r);
}
