// Zweck:    Tests der gemeinsamen Messkonfiguration (Prisma / Reflektorlos, Prismen-
//           konstante) und der Abweichungsprüfung nach einer Messung.
//           Der Abgleich selbst braucht ein angeschlossenes Gerät und ist hier nicht
//           abgedeckt.
// Hardware: keine
// Autor:    Johann Gerner

using Feldbuch;
using Xunit;

namespace Feldbuch.Tests;

[Collection("Konfiguration")]
public class TachymeterKonfigurationTests
{
    private const decimal PK_MINI_MM = -17.5m;
    private const int     EDM_PRISMA = 3;
    private const int     EDM_RL_KURZ = 5;
    private const int     EDM_RL_LANG = 11;

    public TachymeterKonfigurationTests()
    {
        TachymeterKonfiguration.SetzeModus(MessModus.Prisma);
        TachymeterKonfiguration.SetzePrisma("GPR1 Standard", 0m);
        TachymeterVerbindung.Modell = TachymeterModell.LeicaTS06;
    }

    [Fact]
    public void Weiterschalten_ZweiStufig_WechseltZwischenPrismaUndReflektorlos()
    {
        Assert.Equal(MessModus.ReflektorlosKurz, TachymeterKonfiguration.Weiterschalten(false));

        TachymeterKonfiguration.SetzeModus(MessModus.ReflektorlosKurz);
        Assert.Equal(MessModus.Prisma, TachymeterKonfiguration.Weiterschalten(false));
    }

    [Fact]
    public void Weiterschalten_DreiStufig_DurchlaeuftAlleModi()
    {
        var m = TachymeterKonfiguration.Modus;
        var gesehen = new List<MessModus> { m };
        for (int i = 0; i < 3; i++)
        {
            TachymeterKonfiguration.SetzeModus(TachymeterKonfiguration.Weiterschalten(true));
            gesehen.Add(TachymeterKonfiguration.Modus);
        }

        Assert.Equal(
            [MessModus.Prisma, MessModus.ReflektorlosKurz, MessModus.ReflektorlosLang, MessModus.Prisma],
            gesehen);
    }

    [Fact]
    public void SetzeModus_LoestGeaendertAusUndSpeichertInProjektManager()
    {
        int ausgeloest = 0;
        void Handler() => ausgeloest++;
        TachymeterKonfiguration.Geaendert += Handler;
        try
        {
            TachymeterKonfiguration.SetzeModus(MessModus.ReflektorlosLang);
            TachymeterKonfiguration.SetzeModus(MessModus.ReflektorlosLang);   // unverändert: kein Ereignis
        }
        finally { TachymeterKonfiguration.Geaendert -= Handler; }

        Assert.Equal(1, ausgeloest);
        Assert.Equal("ReflektorlosLang", ProjektManager.MessModusName);
        Assert.True(TachymeterKonfiguration.IstReflektorlos);
    }

    [Fact]
    public void SetzePrisma_UebernimmtNameUndKonstante()
    {
        TachymeterKonfiguration.SetzePrisma("GMP111", PK_MINI_MM);

        Assert.Equal("GMP111", TachymeterKonfiguration.PrismaName);
        Assert.Equal(PK_MINI_MM, TachymeterKonfiguration.PrismenkonstanteMm);
    }

    [Theory]
    [InlineData(MessModus.Prisma,           EDM_PRISMA)]
    [InlineData(MessModus.ReflektorlosKurz, EDM_RL_KURZ)]
    [InlineData(MessModus.ReflektorlosLang, EDM_RL_LANG)]
    public void EdmProgramm_PasstZumModus(MessModus modus, int erwartet)
    {
        Assert.Equal(erwartet, TachymeterKonfiguration.EdmProgramm(modus));
    }

    [Fact]
    public void PruefeMessung_StimmtUeberein_KeineWarnung()
    {
        TachymeterKonfiguration.SetzePrisma("GMP111", PK_MINI_MM);
        var messung = new TachymeterMessung
        {
            EdmModusRoh = EDM_PRISMA,
            Prismenkonstante_mm = (double)PK_MINI_MM
        };

        Assert.Null(TachymeterKonfiguration.PruefeMessung(messung));
    }

    [Fact]
    public void PruefeMessung_GeraetStehtAufReflektorlos_WarntVorEdmModus()
    {
        var messung = new TachymeterMessung { EdmModusRoh = EDM_RL_KURZ, Prismenkonstante_mm = 0 };

        var warnung = TachymeterKonfiguration.PruefeMessung(messung);

        Assert.NotNull(warnung);
        Assert.Contains("EDM-Modus", warnung);
        Assert.False(TachymeterKonfiguration.IstAbgeglichen);
    }

    [Fact]
    public void PruefeMessung_AbweichendePrismenkonstante_Warnt()
    {
        TachymeterKonfiguration.SetzePrisma("GMP111", PK_MINI_MM);
        var messung = new TachymeterMessung { EdmModusRoh = EDM_PRISMA, Prismenkonstante_mm = 34.4 };

        var warnung = TachymeterKonfiguration.PruefeMessung(messung);

        Assert.NotNull(warnung);
        Assert.Contains("Prismenkonstante", warnung);
    }

    [Fact]
    public void PruefeMessung_Reflektorlos_IgnoriertPrismenkonstante()
    {
        TachymeterKonfiguration.SetzeModus(MessModus.ReflektorlosKurz);
        var messung = new TachymeterMessung { EdmModusRoh = EDM_RL_KURZ, Prismenkonstante_mm = 34.4 };

        Assert.Null(TachymeterKonfiguration.PruefeMessung(messung));
    }

    [Fact]
    public void PruefeMessung_OhneGeoCOM_PruefungEntfaellt()
    {
        TachymeterVerbindung.Modell = TachymeterModell.SokkiaSDR;
        var messung = new TachymeterMessung { EdmModusRoh = EDM_RL_KURZ };

        Assert.Null(TachymeterKonfiguration.PruefeMessung(messung));
    }

    [Fact]
    public async Task Abgleichen_OhneVerbindung_MeldetFehlerOhneZuSenden()
    {
        var ergebnis = await TachymeterKonfiguration.AbgleichenAsync();

        Assert.False(ergebnis.Ok);
        Assert.False(TachymeterKonfiguration.Beschaeftigt);
    }
}
