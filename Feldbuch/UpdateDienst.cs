using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// UpdateDienst – sucht neue Versionen in den GitHub-Releases und lädt sie herunter.
//
// Ein Release braucht zwei Dateien (siehe tools/Release-Paket.ps1):
//   Feldbuch-v1.2.3-win-x64.zip  und  Feldbuch-v1.2.3-win-x64.zip.sha256
//
// Die Prüfsumme erkennt beschädigte Downloads, ersetzt aber keine Signatur: Wer das
// GitHub-Konto übernimmt, könnte auch die Prüfsumme austauschen.
// ──────────────────────────────────────────────────────────────────────────────
public sealed record UpdateInfo(
    Version Version, string Tag, string Notizen, string ZipUrl, string PruefsummenUrl, long Groesse);

public static class UpdateDienst
{
    private const string ApiUrl = "https://api.github.com/repos/Jgerner01/Feldbuch2/releases/latest";
    private const long MaxGroesse = 500L * 1024 * 1024;

    public static string UpdateOrdner =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Feldbuch", "Update");

    private static HttpClient NeuerClient(HttpMessageHandler? handler, TimeSpan timeout)
    {
        var client = handler == null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
        client.Timeout = timeout;
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"Feldbuch/{ProgrammVersion.Version}");
        return client;
    }

    /// <summary>Liefert das neueste Release, wenn es neuer ist. Netzwerkfehler werden weitergereicht.</summary>
    public static async Task<UpdateInfo?> SucheAsync(
        CancellationToken abbruch = default, HttpMessageHandler? handler = null)
    {
        using var client = NeuerClient(handler, TimeSpan.FromSeconds(15));
        using var antwort = await client.GetAsync(ApiUrl, abbruch);
        if (antwort.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        antwort.EnsureSuccessStatusCode();
        string json = await antwort.Content.ReadAsStringAsync(abbruch);
        return LeseRelease(json, new Version(ProgrammVersion.Version));
    }

    /// <summary>Wertet die Release-Antwort aus. Null, wenn nichts Neueres oder Paket unvollständig.</summary>
    public static UpdateInfo? LeseRelease(string json, Version aktuell)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.TryGetProperty("draft", out var entwurf) && entwurf.GetBoolean()) return null;
        if (root.TryGetProperty("prerelease", out var vorab) && vorab.GetBoolean()) return null;

        string tag = root.GetProperty("tag_name").GetString() ?? "";
        if (!Version.TryParse(tag.TrimStart('v'), out var version) || version <= aktuell) return null;

        string zipName = $"Feldbuch-{tag}-win-x64.zip";
        string? zipUrl = null, pruefUrl = null;
        long groesse = 0;
        foreach (var asset in root.GetProperty("assets").EnumerateArray())
        {
            string name = asset.GetProperty("name").GetString() ?? "";
            string url = asset.GetProperty("browser_download_url").GetString() ?? "";
            if (name == zipName) { zipUrl = url; groesse = asset.GetProperty("size").GetInt64(); }
            else if (name == zipName + ".sha256") pruefUrl = url;
        }
        if (zipUrl == null || pruefUrl == null) return null;
        if (!IstGitHubUrl(zipUrl) || !IstGitHubUrl(pruefUrl) || groesse <= 0 || groesse > MaxGroesse) return null;

        string notizen = root.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "";
        return new UpdateInfo(version, tag, notizen, zipUrl, pruefUrl, groesse);
    }

    private static bool IstGitHubUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps && uri.Host == "github.com";

    /// <summary>Lädt und prüft das Paket und gibt den entpackten Ordner mit der neuen Feldbuch.exe zurück.</summary>
    public static async Task<string> LadeAsync(
        UpdateInfo info, string updateOrdner, IProgress<int>? fortschritt = null,
        CancellationToken abbruch = default, HttpMessageHandler? handler = null)
    {
        // Kein festes Zeitlimit: Große Pakete dürfen im Feld dauern, der Nutzer kann abbrechen.
        using var client = NeuerClient(handler, Timeout.InfiniteTimeSpan);

        string ordner = Path.Combine(updateOrdner, info.Tag);
        if (Directory.Exists(ordner)) Directory.Delete(ordner, recursive: true);
        Directory.CreateDirectory(ordner);
        string zipPfad = Path.Combine(ordner, "paket.zip");

        string pruefText = await client.GetStringAsync(info.PruefsummenUrl, abbruch);
        string erwartet = pruefText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        if (erwartet.Length != 64)
            throw new InvalidDataException("Die Prüfsummendatei des Updates ist ungültig.");

        using (var antwort = await client.GetAsync(info.ZipUrl, HttpCompletionOption.ResponseHeadersRead, abbruch))
        {
            antwort.EnsureSuccessStatusCode();
            await using var quelle = await antwort.Content.ReadAsStreamAsync(abbruch);
            await using var ziel = new FileStream(zipPfad, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            var puffer = new byte[81920];
            long gelesen = 0;
            int anzahl;
            while ((anzahl = await quelle.ReadAsync(puffer, abbruch)) > 0)
            {
                gelesen += anzahl;
                if (gelesen > info.Groesse)
                    throw new InvalidDataException("Das Update ist größer als angekündigt.");
                await ziel.WriteAsync(puffer.AsMemory(0, anzahl), abbruch);
                fortschritt?.Report((int)(gelesen * 100 / info.Groesse));
            }
            if (gelesen != info.Groesse)
                throw new InvalidDataException("Das Update wurde nicht vollständig heruntergeladen.");
        }

        string tatsaechlich;
        await using (var zip = File.OpenRead(zipPfad))
            tatsaechlich = Convert.ToHexString(await SHA256.HashDataAsync(zip, abbruch));
        if (!string.Equals(tatsaechlich, erwartet, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Die Prüfsumme des Updates stimmt nicht.");

        string entpackt = Path.Combine(ordner, "entpackt");
        ZipFile.ExtractToDirectory(zipPfad, entpackt);

        return Directory.GetFiles(entpackt, UpdateInstallation.ExeName, SearchOption.AllDirectories) is [var exe]
            ? Path.GetDirectoryName(exe)!
            : throw new InvalidDataException("Das Update enthält keine eindeutige Programmdatei.");
    }

    /// <summary>Entfernt Reste früherer Updates. Noch gesperrte Dateien bleiben bis zum nächsten Start liegen.</summary>
    public static void RaeumeAuf(string updateOrdner)
    {
        try { if (Directory.Exists(updateOrdner)) Directory.Delete(updateOrdner, recursive: true); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}
