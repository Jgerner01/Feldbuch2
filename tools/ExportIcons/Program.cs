// ExportIcons – rendert alle Feldbuch-Icons als PNG-Dateien.
// Ausgabe: ../../Feldbuch/icons/
//
// Ausführen: dotnet run  (im Verzeichnis tools/ExportIcons)

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

string outDir = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Feldbuch", "icons"));
Directory.CreateDirectory(outDir);

// ── Hilfsmethoden ────────────────────────────────────────────────────────────

void SaveTextIcon(string filename, string text, Color bg, Color fg,
    int size = 36, float fontSize = 12f, bool bold = true)
{
    using var bmp = new Bitmap(size, size);
    using var g   = Graphics.FromImage(bmp);
    g.SmoothingMode     = SmoothingMode.AntiAlias;
    g.TextRenderingHint = TextRenderingHint.AntiAlias;
    g.Clear(bg);

    using var font  = new Font("Segoe UI", fontSize, bold ? FontStyle.Bold : FontStyle.Regular);
    using var brush = new SolidBrush(fg);
    var sf = new StringFormat
    {
        Alignment     = StringAlignment.Center,
        LineAlignment = StringAlignment.Center
    };
    g.DrawString(text, font, brush, new RectangleF(0, 0, size, size), sf);
    bmp.Save(Path.Combine(outDir, filename), ImageFormat.Png);
    Console.WriteLine($"  gespeichert: {filename}");
}

// ── Prismenkonstante (GDI+-Zeichnung aus FormDxfViewer.cs) ──────────────────
void SavePrismaIcon(string filename, int size = 36)
{
    using var bmp = new Bitmap(size, size);
    using var g   = Graphics.FromImage(bmp);
    g.SmoothingMode = SmoothingMode.AntiAlias;

    g.Clear(Color.FromArgb(60, 95, 160));

    float cx = size / 2f;
    float cy = size / 2f;
    float r  = size / 2f - 2.5f;

    var colFacet1 = Color.FromArgb(220, 235, 255);
    var colFacet2 = Color.FromArgb(130, 165, 215);
    var colFacet3 = Color.FromArgb(60,   95, 155);
    var colRim    = Color.White;
    var colLine   = Color.FromArgb(200, 220, 255);

    using var penRim  = new Pen(colRim,  1.6f);
    using var penLine = new Pen(colLine, 1.2f);

    PointF Corner(float angleDeg)
    {
        float rad = angleDeg * MathF.PI / 180f;
        return new PointF(cx + r * MathF.Cos(rad), cy + r * MathF.Sin(rad));
    }

    var p0 = Corner(-90f);
    var p1 = Corner( 30f);
    var p2 = Corner(150f);
    var pc = new PointF(cx, cy);

    PointF Mid(PointF a, PointF b) => new((a.X + b.X) / 2f, (a.Y + b.Y) / 2f);
    var m01 = Mid(p0, p1);
    var m12 = Mid(p1, p2);
    var m20 = Mid(p2, p0);

    g.FillPolygon(new SolidBrush(colFacet1), new[] { p0, m01, pc, m20 });
    g.FillPolygon(new SolidBrush(colFacet2), new[] { p1, m12, pc, m01 });
    g.FillPolygon(new SolidBrush(colFacet3), new[] { p2, m20, pc, m12 });
    g.DrawLine(penLine, p0, pc);
    g.DrawLine(penLine, p1, pc);
    g.DrawLine(penLine, p2, pc);
    g.DrawEllipse(penRim, cx - r, cy - r, 2 * r, 2 * r);
    g.FillEllipse(new SolidBrush(Color.White), cx - 2.5f, cy - 2.5f, 5f, 5f);

    bmp.Save(Path.Combine(outDir, filename), ImageFormat.Png);
    Console.WriteLine($"  gespeichert: {filename}");
}

// ── App-Icon (64×64, blauer Header-Look) ─────────────────────────────────────
void SaveAppIcon(string filename, int size = 64)
{
    using var bmp = new Bitmap(size, size);
    using var g   = Graphics.FromImage(bmp);
    g.SmoothingMode     = SmoothingMode.AntiAlias;
    g.TextRenderingHint = TextRenderingHint.AntiAlias;
    g.Clear(Color.FromArgb(42, 72, 130));

    using var font  = new Font("Segoe UI", size * 0.28f, FontStyle.Bold);
    using var brush = new SolidBrush(Color.White);
    var sf = new StringFormat
    {
        Alignment     = StringAlignment.Center,
        LineAlignment = StringAlignment.Center
    };
    g.DrawString("FB", font, brush, new RectangleF(0, 0, size, size), sf);
    bmp.Save(Path.Combine(outDir, filename), ImageFormat.Png);
    Console.WriteLine($"  gespeichert: {filename}");
}

// ── Icons generieren ─────────────────────────────────────────────────────────

Console.WriteLine($"Ausgabe: {outDir}");
Console.WriteLine();

var colBase   = Color.FromArgb(68,  74,  92);
var colActive = Color.FromArgb(52, 110, 190);
var colRed    = Color.FromArgb(150,  38,  38);

// DXF-Viewer Seitenleiste
SaveTextIcon("sidebar_open.png",        "▤",     colActive, Color.White,                    36, 12f);
SaveTextIcon("sidebar_zoom_in.png",     "⊕",     colBase,   Color.FromArgb(210, 215, 230),  36, 12f);
SaveTextIcon("sidebar_zoom_out.png",    "⊖",     colBase,   Color.FromArgb(210, 215, 230),  36, 12f);
SaveTextIcon("sidebar_fit.png",         "⊡",     colBase,   Color.FromArgb(210, 215, 230),  36, 12f);
SaveTextIcon("sidebar_snap.png",        "◎",     colActive, Color.White,                    36, 12f);
SaveTextIcon("sidebar_points.png",      "◉",     colActive, Color.White,                    36, 12f);
SaveTextIcon("sidebar_dxf_toggle.png",  "DXF",   colActive, Color.White,                    36, 7.5f);
SaveTextIcon("sidebar_dxf_export.png",  "↑DXF",  colBase,   Color.FromArgb(200, 205, 225),  36, 7.5f);
SaveTextIcon("sidebar_new.png",         "⊠",     colBase,   Color.FromArgb(200, 205, 225),  36, 12f);
SaveTextIcon("sidebar_import_kor.png",  "↓KOR",  colRed,    Color.White,                    36, 7.5f);
SaveTextIcon("sidebar_import_json.png", "↓JSON", colRed,    Color.White,                    36, 7.5f);

// DXF-Viewer Toolbar
SavePrismaIcon("toolbar_prisma.png", 36);

// Konvertierung
SaveTextIcon("btn_csv.png", "⬇ CSV", Color.FromArgb(38, 110, 72), Color.White, 36, 9f);

// App-Icon
SaveAppIcon("app_icon.png", 64);

Console.WriteLine();
Console.WriteLine("Fertig.");
