namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// AppPfade – liefert das Verzeichnis der laufenden EXE.
//
// Bei PublishSingleFile zeigt AppContext.BaseDirectory auf ein temporäres
// Entpackverzeichnis, NICHT auf den Ordner der EXE.  Dateien die neben der EXE
// liegen (XML-Vorlagen, CSV, Einstellungen) werden deshalb nicht gefunden.
//
// AppPfade.Basis verwendet Environment.ProcessPath und zeigt immer auf den
// echten EXE-Ordner – sowohl bei Single-File-Builds als auch beim Debug-Start.
// ──────────────────────────────────────────────────────────────────────────────
public static class AppPfade
{
    /// <summary>Verzeichnis der laufenden EXE (funktioniert auch bei PublishSingleFile).</summary>
    public static readonly string Basis =
        Path.GetDirectoryName(Environment.ProcessPath)
        ?? AppContext.BaseDirectory;

    /// <summary>Gibt den Pfad zu einer Datei neben der EXE zurück.</summary>
    public static string Get(string dateiname) => Path.Combine(Basis, dateiname);

    /// <summary>Beschreibbare Nutzerdaten; bei Bibliotheksnutzung zunächst das bisherige Verzeichnis.</summary>
    public static string BenutzerBasis { get; private set; } = Basis;

    public static string BenutzerGet(string dateiname) => Path.Combine(BenutzerBasis, dateiname);

    /// <summary>Vor dem Start der Manager aufrufen. Bestehende Einstellungen einmalig übernehmen.</summary>
    public static void InitialisiereBenutzerdaten()
    {
        string ziel = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Feldbuch");
        Directory.CreateDirectory(ziel);
        foreach (string name in new[] { "Einstellungen.xml", "Freie-Stationierung.xml" })
        {
            string quelle = Get(name), neu = Path.Combine(ziel, name);
            if (!File.Exists(neu) && File.Exists(quelle)) File.Copy(quelle, neu, overwrite: false);
        }
        BenutzerBasis = ziel;
    }
}
