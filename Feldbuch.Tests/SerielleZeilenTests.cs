using Feldbuch;
using Xunit;

namespace Feldbuch.Tests;

public class SerielleZeilenTests
{
    [Fact]
    public void FragmentierteAntworten_WerdenErstAlsVollstaendigeZeileAusgegeben()
    {
        var puffer = new SerielleZeilen();
        Assert.Empty(puffer.Empfange("%R1"));
        Assert.Empty(puffer.Empfange("P,0,0:0,12"));
        Assert.Equal(new[] { "%R1P,0,0:0,12.34", "zweite" }, puffer.Empfange(".34\r\nzweite\nRest"));
        Assert.Equal(new[] { "Rest" }, puffer.Empfange("\r\n"));
        Assert.Empty(puffer.Empfange("\r\n"));
    }

    [Fact]
    public void EndloseAntwort_WirdBegrenztUndPufferDanachWiederVerwendbar()
    {
        var puffer = new SerielleZeilen();
        Assert.Throws<IOException>(() => puffer.Empfange(new string('x', 65537)));
        Assert.Equal(new[] { "OK" }, puffer.Empfange("OK\r\n"));
    }
}
