using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Feldbuch;

/// <summary>Atomare Dateiersetzung und Schutz vor Überschreiben nach einem Ladefehler.</summary>
public static class DateiSpeicher
{
    private static readonly object Sperre = new();
    private static readonly HashSet<string> GesperrtePfade = new(StringComparer.OrdinalIgnoreCase);

    // Nicht bestimmte Residuen werden als "NaN" gespeichert und beim Laden wiederhergestellt.
    public static JsonSerializerOptions JsonOptionen => new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    public static event Action<string>? Ladefehler;

    public static void MeldeLadefehler(string pfad, Exception fehler)
    {
        lock (Sperre) GesperrtePfade.Add(Path.GetFullPath(pfad));
        ErrorLogger.Log("Laden: " + pfad, fehler);
        Ladefehler?.Invoke($"Die Datei konnte nicht geladen werden:\n{pfad}\n\n{fehler.Message}\n\n"
            + "Die Datei bleibt erhalten und ist für Schreibzugriffe gesperrt. "
            + "Bitte eine intakte Datei wiederherstellen und das Projekt erneut öffnen.");
    }

    public static void LadenErfolgreich(string pfad)
    {
        lock (Sperre) GesperrtePfade.Remove(Path.GetFullPath(pfad));
    }

    public static void PruefeSchreibzugriff(string pfad)
    {
        if (string.IsNullOrEmpty(pfad)) return;
        lock (Sperre)
            if (GesperrtePfade.Contains(Path.GetFullPath(pfad)))
                throw new IOException($"Schreiben gesperrt nach einem Ladefehler: {pfad}. "
                    + "Bitte die Datei wiederherstellen und das Projekt erneut öffnen.");
    }

    public static void SchreibeText(string pfad, string text, Encoding? encoding = null)
    {
        lock (Sperre)
        {
            PruefeSchreibzugriff(pfad);
            string ziel = Path.GetFullPath(pfad);
            string verzeichnis = Path.GetDirectoryName(ziel)!;
            string temp = Path.Combine(verzeichnis, $".{Path.GetFileName(ziel)}.{Guid.NewGuid():N}.tmp");
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    using (var writer = new StreamWriter(stream, encoding ?? Encoding.UTF8, leaveOpen: true))
                        writer.Write(text);
                    stream.Flush(flushToDisk: true);
                }

                if (File.Exists(ziel))
                {
                    string? backup = null;
                    if (ProjektManager.AutoBackup)
                    {
                        string backupDir = Path.Combine(verzeichnis, "Backup");
                        Directory.CreateDirectory(backupDir);
                        backup = Path.Combine(backupDir,
                            $"{Path.GetFileName(ziel)}.{DateTime.Now:yyyyMMdd-HHmmss-fffffff}.{Guid.NewGuid():N}.bak");
                    }
                    File.Replace(temp, ziel, backup);
                }
                else File.Move(temp, ziel);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                ErrorLogger.Log("Speichern: " + ziel, ex);
                throw new IOException($"Die Datei wurde nicht gespeichert: {ziel}. {ex.Message}", ex);
            }
            finally
            {
                // Ausschließlich die eigene temporäre Datei entfernen, niemals das Original.
                try { if (File.Exists(temp)) File.Delete(temp); }
                catch (Exception ex) { ErrorLogger.Log("Temporäre Datei: " + temp, ex); }
            }
        }
    }

    public static void SchreibeZeilen(string pfad, IEnumerable<string> zeilen, Encoding? encoding = null)
        => SchreibeText(pfad, string.Concat(zeilen.Select(z => z + Environment.NewLine)), encoding);
}
