// Zweck:    Tests der Zielrichtung (Standpunkt + Orientierung) und der Sperre der Winkelabfrage.
// Hardware: keine
// Autor:    Johann Gerner

using Feldbuch;
using Xunit;

namespace Feldbuch.Tests;

public class ZielrichtungTests
{
    private const int    BILD_PX    = 400;
    private const double MASSSTAB   = 1.0;     // 1 Pixel je Meter
    private const double STAND_R    = 1000.0;
    private const double STAND_H    = 2000.0;
    private const double ORIENT_GON = 100.0;   // Hz 0 zeigt dann nach Osten
    private const int    SUCH_PX    = 3;

    /// <summary>Zeichnet die Linie und liefert die Pixel, die rot genug sind.</summary>
    private static List<(int X, int Y)> Zeichne(OverlayZielrichtung z)
    {
        using var bmp = new Bitmap(BILD_PX, BILD_PX);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            // Weltpunkt (STAND_R, STAND_H) liegt in der Bildmitte, H wächst nach oben
            PointF Ts(double r, double h) => new(
                (float)((r - STAND_R) * MASSSTAB + BILD_PX / 2.0),
                (float)(BILD_PX / 2.0 - (h - STAND_H) * MASSSTAB));
            using var pen = new Pen(Color.Black);
            z.Draw(g, pen, Ts, MASSSTAB);
        }

        var treffer = new List<(int, int)>();
        for (int y = 0; y < BILD_PX; y++)
            for (int x = 0; x < BILD_PX; x++)
            {
                var c = bmp.GetPixel(x, y);
                if (c.R > 150 && c.G < 120 && c.B < 120) treffer.Add((x, y));
            }
        return treffer;
    }

    [Fact]
    public void Linie_GehtVomStandpunktAus_UndFolgtOrientierung()
    {
        var z = new OverlayZielrichtung
        {
            HzGon = 0.0, StandpunktR = STAND_R, StandpunktH = STAND_H, OrientierungGon = ORIENT_GON
        };
        var pixel = Zeichne(z);

        Assert.NotEmpty(pixel);
        // t = 0 + 100 gon = Osten: Linie läuft rechts der Mitte waagerecht
        int mitte = BILD_PX / 2;
        Assert.Contains(pixel, p => p.X > mitte + 20 && Math.Abs(p.Y - mitte) <= SUCH_PX);
        Assert.DoesNotContain(pixel, p => p.X < mitte - 20 && Math.Abs(p.Y - mitte) <= SUCH_PX);
    }

    [Fact]
    public void Linie_OhneOrientierung_ZeigtBeiHzNullNachNorden()
    {
        var z = new OverlayZielrichtung { HzGon = 0.0, StandpunktR = STAND_R, StandpunktH = STAND_H };
        var pixel = Zeichne(z);

        int mitte = BILD_PX / 2;
        Assert.Contains(pixel, p => p.Y < mitte - 20 && Math.Abs(p.X - mitte) <= SUCH_PX);
    }

    [Fact]
    public void Linie_OhneHz_IstNichtSichtbar()
        => Assert.Empty(Zeichne(new OverlayZielrichtung { StandpunktR = STAND_R, StandpunktH = STAND_H }));

    [Fact]
    public void Sperre_ZaehltMehrereFensterUndBleibtNichtNegativ()
    {
        while (LiveWinkelSperre.Gesperrt) LiveWinkelSperre.Freigeben();

        LiveWinkelSperre.Sperren();
        LiveWinkelSperre.Sperren();
        LiveWinkelSperre.Freigeben();
        Assert.True(LiveWinkelSperre.Gesperrt);
        LiveWinkelSperre.Freigeben();
        Assert.False(LiveWinkelSperre.Gesperrt);

        LiveWinkelSperre.Freigeben();           // zu viel freigegeben
        LiveWinkelSperre.Sperren();
        Assert.True(LiveWinkelSperre.Gesperrt);
        LiveWinkelSperre.Freigeben();
        Assert.False(LiveWinkelSperre.Gesperrt);
    }

    [Fact]
    public void AntwortOffen_NurInnerhalbDerFrist()
    {
        LiveWinkelSperre.LetzteAnfrage = DateTime.UtcNow;
        Assert.True(LiveWinkelSperre.AntwortOffen);

        LiveWinkelSperre.LetzteAnfrage = DateTime.UtcNow.AddMilliseconds(-(LiveWinkelSperre.ANTWORT_TIMEOUT_MS + 100));
        Assert.False(LiveWinkelSperre.AntwortOffen);

        LiveWinkelSperre.LetzteAnfrage = DateTime.MinValue;
        Assert.False(LiveWinkelSperre.AntwortOffen);
    }
}
