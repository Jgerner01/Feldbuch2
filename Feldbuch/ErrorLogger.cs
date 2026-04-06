namespace Feldbuch;

using System.Diagnostics;

// ──────────────────────────────────────────────────────────────────────────────
// ErrorLogger – protokolliert unerwartete Exceptions.
//
// Schreibt immer in Fehler.log (neben der EXE, unabhängig von ProtokollAktiv)
// und zusätzlich ins Tagesprotokoll, wenn Protokollierung aktiv ist.
// Fällt bei Schreibfehlern auf Debug.WriteLine zurück.
// ──────────────────────────────────────────────────────────────────────────────
internal static class ErrorLogger
{
    private static readonly string FehlerLogPfad =
        Path.Combine(AppPfade.Basis, "Fehler.log");

    /// <summary>
    /// Protokolliert eine unerwartete Exception mit Kontext-Information.
    /// </summary>
    /// <param name="kontext">Kurze Beschreibung wo der Fehler aufgetreten ist
    /// (z. B. "FeldbuchpunkteManager.Load").</param>
    /// <param name="ex">Die aufgetretene Exception.</param>
    public static void Log(string kontext, Exception ex)
    {
        string nachricht = $"{ex.GetType().Name}: {ex.Message}";

        // Ins Tagesprotokoll (wenn Protokollierung aktiv)
        ProtokollManager.Log("FEHLER", $"{kontext} – {nachricht}");

        // Immer in Fehler.log schreiben (unabhängig von ProtokollAktiv)
        try
        {
            string eintrag =
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  [{kontext}]  {nachricht}{Environment.NewLine}";
            File.AppendAllText(FehlerLogPfad, eintrag, System.Text.Encoding.UTF8);
        }
        catch
        {
            Debug.WriteLine($"[Feldbuch FEHLER] {kontext}: {ex}");
        }
    }
}
