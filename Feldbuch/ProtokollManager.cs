namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// ProtokollManager – schreibt Ereignisse in eine tagesaktuelle Protokolldatei.
//
// Dateiname: Protokoll_YYYY-MM-DD.txt  (im Projektverzeichnis oder AppBaseDir)
// Format:    HH:mm:ss  [KATEGORIE]  Nachricht
//
// Logging findet nur statt, wenn ProjektManager.ProtokollAktiv == true.
// ──────────────────────────────────────────────────────────────────────────────
public static class ProtokollManager
{
    // ── Eintrag schreiben ─────────────────────────────────────────────────────
    /// <summary>
    /// Schreibt eine Zeile ins Tagesprotokoll.
    /// <para>Kategorie (max 8 Zeichen): START, ENDE, PROJEKT, FORM, RECHNUNG,
    /// EINST, FEHLER, INFO</para>
    /// </summary>
    public static void Log(string kategorie, string nachricht)
    {
        if (!ProjektManager.ProtokollAktiv) return;

        try
        {
            string verzeichnis = ProjektManager.IstGeladen
                ? ProjektManager.ProjektVerzeichnis
                : AppPfade.Basis;

            string datum = DateTime.Now.ToString("yyyy-MM-dd");
            string pfad  = Path.Combine(verzeichnis, $"Protokoll_{datum}.txt");

            // Kopfzeile anlegen, wenn die Datei neu ist
            bool istNeu = !File.Exists(pfad);
            using var writer = new StreamWriter(pfad, append: true, System.Text.Encoding.UTF8);
            if (istNeu)
            {
                writer.WriteLine($"Feldbuch Protokoll – {DateTime.Now:dd.MM.yyyy}");
                writer.WriteLine(new string('=', 60));
            }

            string zeit  = DateTime.Now.ToString("HH:mm:ss");
            string zeile = $"{zeit}  [{kategorie,-8}]  {nachricht}";
            writer.WriteLine(zeile);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[ProtokollManager] Schreibfehler: {ex.Message}"); }
    }
}
