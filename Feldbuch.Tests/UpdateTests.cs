using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Feldbuch;
using Xunit;

namespace Feldbuch.Tests;

public sealed class UpdateTests : IDisposable
{
    private readonly string ordner = Path.Combine(Path.GetTempPath(), "feldbuch-update-" + Guid.NewGuid().ToString("N"));

    public UpdateTests() => Directory.CreateDirectory(ordner);

    public void Dispose()
    {
        try { Directory.Delete(ordner, recursive: true); } catch (IOException) { }
    }

    private static string Release(string tag = "v2.0.0", bool vorab = false, bool mitPruefsumme = true,
        string host = "github.com", long groesse = 100) =>
        JsonSerializer.Serialize(new
        {
            tag_name = tag,
            draft = false,
            prerelease = vorab,
            body = "Notizen",
            assets = new[]
            {
                new { name = $"Feldbuch-{tag}-win-x64.zip", size = groesse, browser_download_url = $"https://{host}/a.zip" },
                new { name = mitPruefsumme ? $"Feldbuch-{tag}-win-x64.zip.sha256" : "andere.txt", size = 64L, browser_download_url = $"https://{host}/a.sha256" }
            }
        });

    [Fact]
    public void NeueresRelease_WirdErkannt()
    {
        var info = UpdateDienst.LeseRelease(Release(), new Version(1, 10, 1));
        Assert.NotNull(info);
        Assert.Equal(new Version(2, 0, 0), info!.Version);
        Assert.Equal("Notizen", info.Notizen);
    }

    [Theory]
    [InlineData("v1.10.1")]   // gleich
    [InlineData("v1.9.9")]    // älter; numerisch, nicht als Text verglichen
    [InlineData("kaputt")]
    public void NichtNeueresRelease_WirdIgnoriert(string tag)
    {
        Assert.Null(UpdateDienst.LeseRelease(Release(tag), new Version(1, 10, 1)));
    }

    [Fact]
    public void Versionsvergleich_IstNumerisch()
    {
        Assert.NotNull(UpdateDienst.LeseRelease(Release("v1.10.0"), new Version(1, 9, 9)));
    }

    [Fact]
    public void VorabversionOhnePruefsummeOderFremderHost_WirdIgnoriert()
    {
        var aktuell = new Version(1, 0, 0);
        Assert.Null(UpdateDienst.LeseRelease(Release(vorab: true), aktuell));
        Assert.Null(UpdateDienst.LeseRelease(Release(mitPruefsumme: false), aktuell));
        Assert.Null(UpdateDienst.LeseRelease(Release(host: "example.com"), aktuell));
        Assert.Null(UpdateDienst.LeseRelease(Release(groesse: 0), aktuell));
    }

    private sealed class FesteAntworten(byte[] zip, string pruefsumme) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage anfrage, CancellationToken abbruch)
        {
            HttpContent inhalt = anfrage.RequestUri!.AbsolutePath.EndsWith(".sha256")
                ? new StringContent(pruefsumme) : new ByteArrayContent(zip);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = inhalt });
        }
    }

    private static byte[] Paket(params string[] dateien)
    {
        using var puffer = new MemoryStream();
        using (var zip = new ZipArchive(puffer, ZipArchiveMode.Create, leaveOpen: true))
            foreach (string datei in dateien)
            {
                using var schreiber = new StreamWriter(zip.CreateEntry(datei).Open());
                schreiber.Write("neu");
            }
        return puffer.ToArray();
    }

    private static UpdateInfo Info(long groesse) =>
        new(new Version(2, 0, 0), "v2.0.0", "", "https://github.com/a.zip", "https://github.com/a.sha256", groesse);

    [Theory]
    [InlineData('/')]
    [InlineData('\\')]   // Windows PowerShell 5.1 schreibt Backslashes in das ZIP
    public async Task GueltigesPaket_WirdEntpackt(char trenner)
    {
        byte[] zip = Paket($"Feldbuch-v2.0.0{trenner}Feldbuch.exe", $"Feldbuch-v2.0.0{trenner}icons{trenner}a.png");
        string hash = Convert.ToHexString(SHA256.HashData(zip));

        string pfad = await UpdateDienst.LadeAsync(Info(zip.Length), ordner, handler: new FesteAntworten(zip, $"{hash}  a.zip\n"));

        Assert.True(File.Exists(Path.Combine(pfad, "Feldbuch.exe")));
        Assert.True(File.Exists(Path.Combine(pfad, "icons", "a.png")));
    }

    [Fact]
    public async Task FalschePruefsumme_WirdAbgelehntUndNichtEntpackt()
    {
        byte[] zip = Paket("Feldbuch-v2.0.0/Feldbuch.exe");
        var handler = new FesteAntworten(zip, new string('0', 64));

        await Assert.ThrowsAsync<InvalidDataException>(() => UpdateDienst.LadeAsync(Info(zip.Length), ordner, handler: handler));

        Assert.Empty(Directory.GetFiles(ordner, "Feldbuch.exe", SearchOption.AllDirectories));
    }

    [Theory]
    [InlineData(-1)]   // Antwort länger als angekündigt
    [InlineData(1)]    // abgebrochener Download
    public async Task FalscheGroesse_WirdAbgelehnt(int abweichung)
    {
        byte[] zip = Paket("Feldbuch-v2.0.0/Feldbuch.exe");
        string hash = Convert.ToHexString(SHA256.HashData(zip));

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            UpdateDienst.LadeAsync(Info(zip.Length + abweichung), ordner, handler: new FesteAntworten(zip, hash)));
    }

    [Fact]
    public async Task PaketOhneProgrammdatei_WirdAbgelehnt()
    {
        byte[] zip = Paket("Feldbuch-v2.0.0/anderes.exe");
        string hash = Convert.ToHexString(SHA256.HashData(zip));

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            UpdateDienst.LadeAsync(Info(zip.Length), ordner, handler: new FesteAntworten(zip, hash)));
    }

    private (string Quelle, string Ziel) Installationen()
    {
        string quelle = Path.Combine(ordner, "neu"), ziel = Path.Combine(ordner, "programm");
        Directory.CreateDirectory(Path.Combine(quelle, "icons"));
        Directory.CreateDirectory(Path.Combine(ziel, "icons"));
        File.WriteAllText(Path.Combine(quelle, "Feldbuch.exe"), "neu");
        File.WriteAllText(Path.Combine(quelle, "icons", "a.png"), "neu");
        File.WriteAllText(Path.Combine(quelle, "zusatz.xml"), "neu");
        File.WriteAllText(Path.Combine(ziel, "Feldbuch.exe"), "alt");
        File.WriteAllText(Path.Combine(ziel, "icons", "a.png"), "alt");
        File.WriteAllText(Path.Combine(ziel, "Station-1.json"), "messdaten");
        return (quelle, ziel);
    }

    [Fact]
    public void Installation_ErsetztProgrammdateienUndBehaeltNutzerdaten()
    {
        var (quelle, ziel) = Installationen();

        UpdateInstallation.Anwenden(quelle, ziel);

        Assert.Equal("neu", File.ReadAllText(Path.Combine(ziel, "Feldbuch.exe")));
        Assert.Equal("neu", File.ReadAllText(Path.Combine(ziel, "icons", "a.png")));
        Assert.Equal("neu", File.ReadAllText(Path.Combine(ziel, "zusatz.xml")));
        Assert.Equal("messdaten", File.ReadAllText(Path.Combine(ziel, "Station-1.json")));
        Assert.Equal("alt", File.ReadAllText(Path.Combine(ziel, UpdateInstallation.SicherungsOrdner, "Feldbuch.exe")));
    }

    [Fact]
    public void ZweitesUpdate_ErsetztDieAlteSicherung()
    {
        var (quelle, ziel) = Installationen();
        UpdateInstallation.Anwenden(quelle, ziel);
        File.WriteAllText(Path.Combine(quelle, "Feldbuch.exe"), "neuer");

        UpdateInstallation.Anwenden(quelle, ziel);

        Assert.Equal("neuer", File.ReadAllText(Path.Combine(ziel, "Feldbuch.exe")));
        Assert.Equal("neu", File.ReadAllText(Path.Combine(ziel, UpdateInstallation.SicherungsOrdner, "Feldbuch.exe")));
    }

    [Fact]
    public void GesperrteDatei_StelltDenVorherigenStandWiederHer()
    {
        var (quelle, ziel) = Installationen();
        string gesperrt = Path.Combine(ziel, "icons", "a.png");

        using (new FileStream(gesperrt, FileMode.Open, FileAccess.Read, FileShare.None))
            Assert.ThrowsAny<IOException>(() => UpdateInstallation.Anwenden(quelle, ziel, wartezeitMs: 1));

        Assert.Equal("alt", File.ReadAllText(Path.Combine(ziel, "Feldbuch.exe")));
        Assert.Equal("alt", File.ReadAllText(gesperrt));
        Assert.False(File.Exists(Path.Combine(ziel, "zusatz.xml")));
        Assert.Equal("messdaten", File.ReadAllText(Path.Combine(ziel, "Station-1.json")));
    }

    [Fact]
    public void QuelleOhneProgrammdatei_VeraendertNichts()
    {
        var (quelle, ziel) = Installationen();
        File.Delete(Path.Combine(quelle, "Feldbuch.exe"));

        Assert.Throws<InvalidDataException>(() => UpdateInstallation.Anwenden(quelle, ziel));

        Assert.Equal("alt", File.ReadAllText(Path.Combine(ziel, "Feldbuch.exe")));
    }
}
