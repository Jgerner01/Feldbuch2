using System.Diagnostics;
using System.Reflection;

namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// UpdateInstallation – tauscht die Programmdateien aus.
//
// Eine laufende EXE lässt sich nicht überschreiben. Deshalb startet die alte Version
// die neue EXE aus dem Download-Ordner mit dem Argument --update-installieren. Diese
// wartet auf das Ende der alten Version, kopiert sich in den Programmordner und
// startet dort neu. Dieser Aufruf ist die Schnittstelle zwischen zwei Versionen und
// darf sich nicht ändern, sonst können ältere Versionen nicht mehr aktualisieren.
//
// Ersetzt werden nur Dateien aus dem Paket; alles andere im Ordner bleibt unberührt.
// Die vorherige Version bleibt bis zum nächsten Update im Unterordner erhalten.
// ──────────────────────────────────────────────────────────────────────────────
public static class UpdateInstallation
{
    public const string Argument = "--update-installieren";
    public const string ExeName = "Feldbuch.exe";
    public const string SicherungsOrdner = "_vorherige_Version";

    /// <summary>Nur veröffentlichte Einzeldatei-Versionen lassen sich austauschen, keine Entwicklungs-Builds.</summary>
    public static bool IstVeroeffentlicht =>
        string.IsNullOrEmpty(GetAssemblyPfad())
        && string.Equals(Path.GetFileName(Environment.ProcessPath), ExeName, StringComparison.OrdinalIgnoreCase);

    // Bei PublishSingleFile ist der Assembly-Pfad leer.
#pragma warning disable IL3000
    private static string GetAssemblyPfad() => Assembly.GetExecutingAssembly().Location;
#pragma warning restore IL3000

    public static bool IstOrdnerSchreibbar(string ordner)
    {
        string probe = Path.Combine(ordner, $".update-probe-{Guid.NewGuid():N}");
        try
        {
            File.WriteAllText(probe, "");
            File.Delete(probe);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return false; }
    }

    /// <summary>Startet die neue Version; die laufende Version muss sich danach beenden.</summary>
    public static void StarteInstallation(string neuerOrdner, string zielOrdner)
    {
        var start = new ProcessStartInfo(Path.Combine(neuerOrdner, ExeName))
        {
            UseShellExecute = false,
            WorkingDirectory = neuerOrdner
        };
        start.ArgumentList.Add(Argument);
        start.ArgumentList.Add(Environment.ProcessId.ToString());
        start.ArgumentList.Add(neuerOrdner);
        start.ArgumentList.Add(zielOrdner);
        Process.Start(start);
    }

    /// <summary>Einstieg der neuen EXE im Installationsmodus. Startet am Ende immer eine Version im Zielordner.</summary>
    public static void FuehreAus(string[] argumente)
    {
        string zielOrdner = argumente[3];
        try
        {
            WarteAufEnde(int.Parse(argumente[1]));
            Anwenden(argumente[2], zielOrdner);
        }
        catch (Exception ex)
        {
            ErrorLogger.Log("Update", ex);
            MessageBox.Show($"Das Update konnte nicht installiert werden. Die bisherige Version bleibt erhalten.\n\n{ex.Message}",
                "Update fehlgeschlagen", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        Process.Start(new ProcessStartInfo(Path.Combine(zielOrdner, ExeName))
        {
            UseShellExecute = false,
            WorkingDirectory = zielOrdner
        });
    }

    private static void WarteAufEnde(int prozessId)
    {
        try
        {
            using var prozess = Process.GetProcessById(prozessId);
            if (!prozess.WaitForExit(TimeSpan.FromSeconds(60)))
                throw new TimeoutException("Die laufende Feldbuch-Version wurde nicht beendet.");
        }
        catch (ArgumentException) { /* bereits beendet */ }
    }

    /// <summary>Ersetzt die Dateien. Bei einem Fehler wird der vorherige Stand wiederhergestellt.</summary>
    public static void Anwenden(string quelle, string ziel, int wartezeitMs = 500)
    {
        if (!File.Exists(Path.Combine(quelle, ExeName)))
            throw new InvalidDataException("Das Update enthält keine Programmdatei.");

        string sicherung = Path.Combine(ziel, SicherungsOrdner);
        if (Directory.Exists(sicherung)) Directory.Delete(sicherung, recursive: true);

        var gesichert = new List<(string Original, string Kopie)>();
        var neu = new List<string>();
        try
        {
            foreach (string datei in Directory.GetFiles(quelle, "*", SearchOption.AllDirectories))
            {
                string relativ = Path.GetRelativePath(quelle, datei);
                string zielDatei = Path.Combine(ziel, relativ);
                Directory.CreateDirectory(Path.GetDirectoryName(zielDatei)!);

                if (File.Exists(zielDatei))
                {
                    string kopie = Path.Combine(sicherung, relativ);
                    Directory.CreateDirectory(Path.GetDirectoryName(kopie)!);
                    // Virenscanner halten eine gerade beendete EXE oft noch kurz offen.
                    MitWiederholung(() => File.Move(zielDatei, kopie), wartezeitMs);
                    gesichert.Add((zielDatei, kopie));
                }
                neu.Add(zielDatei);
                File.Copy(datei, zielDatei);
            }
        }
        catch
        {
            foreach (string datei in neu)
                if (File.Exists(datei)) File.Delete(datei);
            foreach (var (original, kopie) in gesichert)
                File.Move(kopie, original, overwrite: true);
            throw;
        }
    }

    private static void MitWiederholung(Action aktion, int wartezeitMs)
    {
        for (int versuch = 1; ; versuch++)
        {
            try { aktion(); return; }
            catch (Exception ex) when (versuch < 20 && ex is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(wartezeitMs);
            }
        }
    }
}
