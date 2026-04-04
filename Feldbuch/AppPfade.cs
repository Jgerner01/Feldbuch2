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
}
