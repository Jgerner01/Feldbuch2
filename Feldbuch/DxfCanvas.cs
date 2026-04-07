namespace Feldbuch;

using System.ComponentModel;
using System.Runtime.InteropServices;

// ──────────────────────────────────────────────────────────────────────────────
// DxfCanvas – DoubleBuffered Panel mit:
//   • Mausrad-Zoom und Drag-Pan
//   • Touch-Zoom (2-Finger Pinch via WM_TOUCH)
//   • Touch-Pan (1 Finger)
//   • Entitäten-Picking (Klick / Tap)
//   • Snap-Modus: Mittelpunkt (Kreis) / Endpunkt (Linie, Bogen, Polyline)
//   • Fit-to-View, ZoomIn, ZoomOut
//   • Weißer Hintergrund
// ──────────────────────────────────────────────────────────────────────────────
public class DxfCanvas : Panel
{
    // ── Anzeigezustand ────────────────────────────────────────────────────────
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<DxfEntity> Entities { get; set; } = new();
    public DxfEntity?       Selected { get; private set; }

    public new double Scale = 1.0;   // Pixel pro Welteinheit
    public double PanX  = 0.0;       // Weltkoordinate im Bildmittelpunkt
    public double PanY  = 0.0;

    // Snap-Modus (standardmäßig aktiv)
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SnapActive { get; set; } = true;

    // Katasterpunkte (INSERT-Entities) ein-/ausblenden
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool PunkteVisible { get; set; } = true;

    // Original-DXF ein-/ausblenden (nur Feldbuch-Overlay bleibt sichtbar)
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool DxfVisible { get; set; } = true;

    // Feldbuchpunkte-Overlay (Standpunkte, Neupunkte)
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<DxfEntity> OverlayEntities { get; set; } = new();

    // Import-Layer (KOR / CSV / JSON – je Datei ein Layer mit Sichtbarkeits-Flag)
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<ImportLayer> ImportLayers { get; set; } = new();

    // Hilfsmethode: alle sichtbaren Import-Entities flach
    IEnumerable<DxfEntity> SichtbareImports()
        => ImportLayers.Where(l => l.Visible).SelectMany(l => l.Entities);

    public event Action<double, double, DxfEntity?>?           PointPicked;
    public event Action<double, double, double, double>?       RectangleSelected;

    // Delete-Modus: Auswahlrechteck statt Pan
    bool _deleteModeActive;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool DeleteModeActive
    {
        get => _deleteModeActive;
        set { _deleteModeActive = value; Cursor = value ? Cursors.Cross : Cursors.Default; Invalidate(); }
    }

    // ── Erscheinungsbild ──────────────────────────────────────────────────────
    static readonly Color BgColor      = Color.White;
    static readonly Color DefaultColor = Color.FromArgb(40, 40, 100);
    static readonly Color SelectColor  = Color.Red;
    static readonly Color CrossColor   = Color.FromArgb(200, 200, 200);
    static readonly Color SnapColor    = Color.FromArgb(220, 100, 0);   // orange

    // ── Layer-basierte Farbzuweisung ─────────────────────────────────────────
    // Rückgabe null → Entitätsfarbe (ACI) oder Standardfarbe verwenden.
    static Color? LayerFarbe(string layer)
    {
        string l = layer.ToLowerInvariant();
        // Flurstücksgrenzen und Grenzlinien → Grün
        if (l.Contains("flurstueck") ||
            (l.Contains("grenze") && !l.Contains("grenzpunkt")))
            return Color.FromArgb(0, 140, 0);
        // Gebäude und Bauwerke → Schwarz
        if (l.Contains("gebaeude") || l.Contains("bauwerk"))
            return Color.Black;
        return null;
    }

    // ── Maus-Status ───────────────────────────────────────────────────────────
    bool   _panning;
    bool   _moved;
    Point  _lastMouse;

    // ── Snap-Zustand ──────────────────────────────────────────────────────────
    (double x, double y)? _snapPoint;   // aktueller Snap-Punkt (null = keiner)

    // ── Lösch-Auswahlrechteck ─────────────────────────────────────────────────
    bool  _selecting;
    Point _selStart;
    Point _selEnd;

    // ── Touch-Status (WM_TOUCH) ───────────────────────────────────────────────
    readonly Dictionary<uint, Point> _touches = new();
    double _lastPinchDist;

    // ── P/Invoke ──────────────────────────────────────────────────────────────
    [DllImport("user32.dll")] static extern bool RegisterTouchWindow(IntPtr h, int flags);
    [DllImport("user32.dll")] static extern bool GetTouchInputInfo(
        IntPtr hTi, int n, [In, Out] TOUCHINPUT[] arr, int sz);
    [DllImport("user32.dll")] static extern void CloseTouchInputHandle(IntPtr h);

    [StructLayout(LayoutKind.Sequential)]
    struct TOUCHINPUT
    {
        public int    x, y;
        public IntPtr hSource;
        public uint   dwID, dwFlags, dwMask, dwTime;
        public UIntPtr dwExtraInfo;
        public uint   cxContact, cyContact;
    }
    const int WM_TOUCH          = 0x0240;
    const uint TOUCHEVENTF_DOWN = 0x0001;
    const uint TOUCHEVENTF_UP   = 0x0004;
    const uint TOUCHEVENTF_MOVE = 0x0002;

    // ── Konstruktor ───────────────────────────────────────────────────────────
    public DxfCanvas()
    {
        DoubleBuffered    = true;
        BackColor         = BgColor;
        ResizeRedraw      = true;
        MouseDown        += OnMouseDown;
        MouseMove        += OnMouseMove;
        MouseUp          += OnMouseUp;
        MouseWheel       += OnMouseWheel;
        MouseEnter       += (_, _) => Focus();
        TabStop           = true;
    }

    protected override CreateParams CreateParams
    {
        get { var cp = base.CreateParams; cp.ExStyle |= 0x02000000; return cp; }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        RegisterTouchWindow(Handle, 0);
    }

    // ── Koordinaten-Transforms ────────────────────────────────────────────────
    public PointF ToScreen(double wx, double wy) => new(
        (float)((wx - PanX) * Scale + Width  / 2.0),
        (float)(-(wy - PanY) * Scale + Height / 2.0));

    public (double x, double y) ToWorld(double sx, double sy) => (
        (sx - Width  / 2.0) / Scale + PanX,
        -(sy - Height / 2.0) / Scale + PanY);

    // ── Zoom-Operationen ──────────────────────────────────────────────────────
    public void ZoomIn()  => ApplyZoom(1.5,  Width / 2, Height / 2);
    public void ZoomOut() => ApplyZoom(1.0/1.5, Width / 2, Height / 2);

    public void ApplyZoom(double factor, int sx, int sy)
    {
        var (wx, wy) = ToWorld(sx, sy);
        Scale = Math.Clamp(Scale * factor, 1e-9, 1e9);
        PanX  = wx - (sx - Width  / 2.0) / Scale;
        PanY  = wy + (sy - Height / 2.0) / Scale;
        Invalidate();
    }

    public void FitToView()
    {
        // Bei ausgeblendetem DXF: nur Overlay; sonst beides
        IEnumerable<DxfEntity> src = DxfVisible
            ? Entities.Concat(OverlayEntities).Concat(SichtbareImports())
            : OverlayEntities.Concat(SichtbareImports());
        if (!src.Any()) return;
        BoundingBox? bb = null;
        foreach (var e in src)
        {
            var b = e.GetBounds();
            bb = bb == null ? b : bb.Expand(b);
        }
        if (bb == null || bb.Width < 1e-10 && bb.Height < 1e-10) return;
        double margin = 0.9;
        double sw = Width  > 0 ? Width  : 800;
        double sh = Height > 0 ? Height : 600;
        double ww = Math.Max(bb.Width,  1e-6);
        double wh = Math.Max(bb.Height, 1e-6);
        Scale = Math.Min(sw / ww, sh / wh) * margin;
        PanX  = bb.CenterX;
        PanY  = bb.CenterY;
        Invalidate();
    }

    // ── Picking ───────────────────────────────────────────────────────────────
    DxfEntity? Pick(int sx, int sy)
    {
        var (wx, wy) = ToWorld(sx, sy);
        double thresh = 14.0 / Scale;
        DxfEntity? best = null;
        double bestD = thresh;

        // Overlay zuerst (Vorrang, immer sichtbar)
        foreach (var e in OverlayEntities)
        {
            double d = e.DistanceTo(wx, wy);
            if (d < bestD) { bestD = d; best = e; }
        }
        foreach (var e in SichtbareImports())
        {
            double d = e.DistanceTo(wx, wy);
            if (d < bestD) { bestD = d; best = e; }
        }

        // Original-Entities (nur wenn sichtbar)
        if (DxfVisible)
        {
            foreach (var e in Entities)
            {
                if (e is DxfInsert && !PunkteVisible) continue;
                double d = e.DistanceTo(wx, wy);
                if (d < bestD) { bestD = d; best = e; }
            }
        }
        return best;
    }

    // Snap-Punkt für Entity ermitteln (oder Kursor-Koordinate ohne Snap)
    (double x, double y) ResolvePoint(DxfEntity? entity, double wx, double wy)
    {
        if (entity == null) return (wx, wy);
        return SnapActive ? entity.GetSnapPoint(wx, wy) : entity.GetNearestPoint(wx, wy);
    }

    // ── Maus-Events ───────────────────────────────────────────────────────────
    void OnMouseDown(object? s, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (DeleteModeActive)
            { _selecting = true; _selStart = _selEnd = e.Location; Capture = true; }
            else
            { _panning = true; _moved = false; _lastMouse = e.Location; Capture = true; }
        }
    }

    void OnMouseMove(object? s, MouseEventArgs e)
    {
        if (_selecting)
        { _selEnd = e.Location; Invalidate(); return; }

        if (_panning && e.Button == MouseButtons.Left)
        {
            int dx = e.X - _lastMouse.X, dy = e.Y - _lastMouse.Y;
            if (Math.Abs(dx) > 2 || Math.Abs(dy) > 2) _moved = true;
            PanX -= dx / Scale;
            PanY += dy / Scale;
            _lastMouse = e.Location;
            Invalidate();
        }

        var (wx, wy) = ToWorld(e.X, e.Y);

        if (SnapActive && Entities.Count > 0)
        {
            var hovered = Pick(e.X, e.Y);
            if (hovered != null)
            {
                _snapPoint = hovered.GetSnapPoint(wx, wy);
                PointPicked?.Invoke(_snapPoint.Value.x, _snapPoint.Value.y, null);
                Invalidate();
                return;
            }
        }
        if (_snapPoint != null) { _snapPoint = null; Invalidate(); }
        PointPicked?.Invoke(wx, wy, null);
    }

    void OnMouseUp(object? s, MouseEventArgs e)
    {
        if (_selecting)
        {
            _selecting = false;
            Capture    = false;
            int x1 = Math.Min(_selStart.X, _selEnd.X);
            int y1 = Math.Min(_selStart.Y, _selEnd.Y);
            int x2 = Math.Max(_selStart.X, _selEnd.X);
            int y2 = Math.Max(_selStart.Y, _selEnd.Y);
            if (x2 - x1 > 4 && y2 - y1 > 4)
            {
                var (wx1, wy1) = ToWorld(x1, y1);
                var (wx2, wy2) = ToWorld(x2, y2);
                RectangleSelected?.Invoke(
                    Math.Min(wx1, wx2), Math.Min(wy1, wy2),
                    Math.Max(wx1, wx2), Math.Max(wy1, wy2));
            }
            Invalidate();
            return;
        }
        if (!_panning) return;
        _panning = false;
        Capture  = false;
        if (!_moved)
        {
            Selected = Pick(e.X, e.Y);
            var (wx, wy) = ToWorld(e.X, e.Y);
            var pt = ResolvePoint(Selected, wx, wy);
            PointPicked?.Invoke(pt.x, pt.y, Selected);
            Invalidate();
        }
    }

    void OnMouseWheel(object? s, MouseEventArgs e)
    {
        double factor = e.Delta > 0 ? 1.25 : 1.0 / 1.25;
        ApplyZoom(factor, e.X, e.Y);
    }

    // ── WM_TOUCH (Touchscreen) ────────────────────────────────────────────────
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_TOUCH)
        {
            int   count  = m.WParam.ToInt32() & 0xFFFF;
            var   inputs = new TOUCHINPUT[count];
            if (GetTouchInputInfo(m.LParam, count, inputs, Marshal.SizeOf<TOUCHINPUT>()))
            {
                foreach (var ti in inputs)
                {
                    var scrPt  = new Point(ti.x / 100, ti.y / 100);
                    var canPt  = PointToClient(scrPt);
                    uint id    = ti.dwID;

                    if ((ti.dwFlags & TOUCHEVENTF_DOWN) != 0)
                    {
                        _touches[id] = canPt;
                        if (_touches.Count == 1) { _lastMouse = canPt; _moved = false; }
                        _lastPinchDist = 0;
                    }
                    else if ((ti.dwFlags & TOUCHEVENTF_MOVE) != 0 && _touches.ContainsKey(id))
                    {
                        var oldPt = _touches[id];
                        _touches[id] = canPt;

                        if (_touches.Count == 1)
                        {
                            int dx = canPt.X - oldPt.X, dy = canPt.Y - oldPt.Y;
                            if (Math.Abs(dx) + Math.Abs(dy) > 2) _moved = true;
                            PanX -= dx / Scale;
                            PanY += dy / Scale;
                            Invalidate();
                        }
                        else if (_touches.Count == 2)
                        {
                            var pts = _touches.Values.ToArray();
                            double newDist = Dist(pts[0], pts[1]);
                            if (_lastPinchDist > 1)
                            {
                                double factor = newDist / _lastPinchDist;
                                var mid = new Point((pts[0].X + pts[1].X) / 2,
                                                    (pts[0].Y + pts[1].Y) / 2);
                                ApplyZoom(factor, mid.X, mid.Y);
                            }
                            _lastPinchDist = newDist;
                        }
                    }
                    else if ((ti.dwFlags & TOUCHEVENTF_UP) != 0)
                    {
                        if (_touches.Count == 1 && !_moved)
                        {
                            Selected = Pick(canPt.X, canPt.Y);
                            var (wx, wy) = ToWorld(canPt.X, canPt.Y);
                            var pt = ResolvePoint(Selected, wx, wy);
                            PointPicked?.Invoke(pt.x, pt.y, Selected);
                            Invalidate();
                        }
                        _touches.Remove(id);
                        if (_touches.Count == 0) _lastPinchDist = 0;
                    }
                }
                CloseTouchInputHandle(m.LParam);
            }
            m.Result = IntPtr.Zero;
            return;
        }
        base.WndProc(ref m);
    }

    static double Dist(Point a, Point b)
    {
        int dx = a.X - b.X, dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    // ── Zeichnen ──────────────────────────────────────────────────────────────
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        bool hatDaten = (DxfVisible && Entities.Count > 0) || OverlayEntities.Count > 0 || ImportLayers.Count > 0;
        if (!hatDaten)
        {
            using var f = new Font("Segoe UI", 14);
            g.DrawString("DXF-Datei öffnen …", f, Brushes.Gray,
                Width / 2f - 90, Height / 2f - 10);
            return;
        }

        using var defPen = new Pen(DefaultColor, 1f);
        using var selPen = new Pen(SelectColor,  2f);

        // Erst alle normalen Entities (Linien, Kreise, etc.) zeichnen …
        if (!DxfVisible) goto DrawOverlay;
        foreach (var entity in Entities)
        {
            if (entity is DxfInsert) continue;   // Punkte kommen zuletzt (Vordergrund)
            bool   isSel      = entity == Selected;
            Color? layerColor = LayerFarbe(entity.Layer);
            Pen    pen;
            bool   ownPen     = false;

            if (isSel)
                pen = selPen;
            else if (layerColor.HasValue)
            { pen = new Pen(layerColor.Value, 1f); ownPen = true; }
            else if (entity.Color != Color.Empty)
            { pen = new Pen(entity.Color, 1f);     ownPen = true; }
            else
                pen = defPen;

            try { entity.Draw(g, pen, ToScreen, Scale); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"DxfCanvas.Draw: {ex.Message}"); }

            if (ownPen) pen.Dispose();
        }

        // … dann Katasterpunkte (INSERT) im Vordergrund
        if (PunkteVisible)
        {
            foreach (var entity in Entities)
            {
                if (entity is not DxfInsert ins) continue;
                bool isSel = entity == Selected;
                Pen  pen   = isSel ? selPen : defPen;
                try { ins.Draw(g, pen, ToScreen, Scale); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"DxfCanvas.DrawInsert: {ex.Message}"); }
            }
        }

        DrawOverlay:
        // Import-Layer (KOR/CSV/JSON – rote Punkte, nur sichtbare Layer)
        foreach (var entity in SichtbareImports())
        {
            bool isSel = entity == Selected;
            Pen  pen   = isSel ? selPen : defPen;
            try { entity.Draw(g, pen, ToScreen, Scale); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"DxfCanvas.DrawImport: {ex.Message}"); }
        }

        // Feldbuch-Overlay (Standpunkte, Neupunkte) – immer ganz vorne
        foreach (var entity in OverlayEntities)
        {
            bool isSel = entity == Selected;
            Pen  pen   = isSel ? selPen : defPen;
            try { entity.Draw(g, pen, ToScreen, Scale); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"DxfCanvas.DrawOverlay: {ex.Message}"); }
        }

        // Fadenkreuz
        using var crossPen = new Pen(CrossColor, 1);
        g.DrawLine(crossPen, Width / 2, 0, Width / 2, Height);
        g.DrawLine(crossPen, 0, Height / 2, Width, Height / 2);

        // Snap-Marker (oranges Quadrat am Snap-Punkt)
        if (_snapPoint.HasValue)
        {
            var sp = ToScreen(_snapPoint.Value.x, _snapPoint.Value.y);
            using var snapPen = new Pen(SnapColor, 2f);
            float s = 9f;
            g.DrawRectangle(snapPen, sp.X - s, sp.Y - s, s * 2, s * 2);
            // diagonale Markierungslinien
            g.DrawLine(snapPen, sp.X - s - 4, sp.Y, sp.X - s, sp.Y);
            g.DrawLine(snapPen, sp.X + s, sp.Y, sp.X + s + 4, sp.Y);
            g.DrawLine(snapPen, sp.X, sp.Y - s - 4, sp.X, sp.Y - s);
            g.DrawLine(snapPen, sp.X, sp.Y + s, sp.X, sp.Y + s + 4);
        }

        // Lösch-Auswahlrechteck (rot transparent)
        if (_selecting)
        {
            int rx = Math.Min(_selStart.X, _selEnd.X);
            int ry = Math.Min(_selStart.Y, _selEnd.Y);
            int rw = Math.Abs(_selEnd.X - _selStart.X);
            int rh = Math.Abs(_selEnd.Y - _selStart.Y);
            if (rw > 0 && rh > 0)
            {
                using var fill   = new SolidBrush(Color.FromArgb(55, 210, 0, 0));
                using var border = new Pen(Color.FromArgb(210, 180, 0, 0), 1.5f);
                g.FillRectangle(fill, rx, ry, rw, rh);
                g.DrawRectangle(border, rx, ry, rw, rh);
            }
        }
    }
}
