// Zweck:    Zeichentests der Overlays für das lokale System: Zielrichtung zeigt
//           in die Himmelsrichtung des Hz-Winkels, Standpunktsymbol wird gezeichnet.
// Hardware: keine
// Autor:    Johann Gerner

using System.Drawing;
using Feldbuch;
using Xunit;

namespace Feldbuch.Tests;

public class LokalesSystemOverlayTests
{
    private const int    BILD_PX        = 400;
    private const double MASSSTAB       = 10.0;   // Pixel pro Meter
    private const int    ROT_MIN        = 150;
    private const int    ROT_MAX_ANDERE = 100;

    // Weltkoordinaten -> Bild (Norden oben, Ursprung in der Bildmitte)
    private static PointF Ts(double r, double h) =>
        new((float)(BILD_PX / 2.0 + r * MASSSTAB), (float)(BILD_PX / 2.0 - h * MASSSTAB));

    private static int ZaehleRot(Bitmap bmp, int x0, int x1, int y0, int y1)
    {
        int n = 0;
        for (int x = x0; x < x1; x++)
            for (int y = y0; y < y1; y++)
            {
                var c = bmp.GetPixel(x, y);
                if (c.R >= ROT_MIN && c.G <= ROT_MAX_ANDERE && c.B <= ROT_MAX_ANDERE) n++;
            }
        return n;
    }

    private static Bitmap Zeichne(DxfEntity e)
    {
        var bmp = new Bitmap(BILD_PX, BILD_PX);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.White);
        using var pen = new Pen(Color.Black);
        e.Draw(g, pen, Ts, MASSSTAB);
        return bmp;
    }

    [Theory]
    [InlineData(0.0)]     // Norden
    [InlineData(100.0)]   // Osten
    [InlineData(200.0)]   // Süden
    [InlineData(300.0)]   // Westen
    public void Zielrichtung_ZeigtInRichtungDesHzWinkels(double hz)
    {
        using var bmp = Zeichne(new OverlayZielrichtung { HzGon = hz });
        int mitte = BILD_PX / 2;

        int oben  = ZaehleRot(bmp, mitte - 3, mitte + 3, 0, mitte - 20);
        int rechts = ZaehleRot(bmp, mitte + 20, BILD_PX, mitte - 3, mitte + 3);
        int unten = ZaehleRot(bmp, mitte - 3, mitte + 3, mitte + 20, BILD_PX);
        int links = ZaehleRot(bmp, 0, mitte - 20, mitte - 3, mitte + 3);

        var anzahl = new[] { oben, rechts, unten, links };
        int erwartet = (int)(hz / 100.0);
        Assert.True(anzahl[erwartet] > 0, $"Keine rote Linie in Richtung {hz} gon");
        for (int i = 0; i < anzahl.Length; i++)
            if (i != erwartet)
                Assert.Equal(0, anzahl[i]);
    }

    [Fact]
    public void Zielrichtung_OhneWinkel_ZeichnetNichts()
    {
        using var bmp = Zeichne(new OverlayZielrichtung { HzGon = null });

        Assert.Equal(0, ZaehleRot(bmp, 0, BILD_PX, 0, BILD_PX));
    }

    [Fact]
    public void Zielrichtung_IstNieAuswaehlbar()
    {
        var z = new OverlayZielrichtung { HzGon = 50 };

        Assert.Equal(double.MaxValue, z.DistanceTo(0, 0));
    }

    [Fact]
    public void Standpunktsymbol_WirdImUrsprungGezeichnet()
    {
        using var bmp = Zeichne(new OverlayLokalerStandpunkt("STP1"));
        int mitte = BILD_PX / 2;

        Assert.True(ZaehleRot(bmp, mitte - 15, mitte + 15, mitte - 15, mitte + 15) > 0);
        Assert.Equal(0, ZaehleRot(bmp, 0, 40, 0, 40));
    }
}
