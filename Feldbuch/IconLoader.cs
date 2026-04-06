namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// IconLoader – lädt PNG-Dateien aus dem icons/-Verzeichnis neben der EXE.
//
// Alle Buttons beziehen ihr Bild aus Feldbuch/icons/*.png.
// Fehlt eine Datei, bleibt der Unicode-Fallback-Text erhalten.
// ──────────────────────────────────────────────────────────────────────────────
internal static class IconLoader
{
    public static readonly string IconVerzeichnis =
        Path.Combine(AppPfade.Basis, "icons");

    /// <summary>
    /// Lädt ein Icon als Image. Gibt null zurück wenn die Datei fehlt.
    /// Verwendet MemoryStream um die Datei nicht zu sperren.
    /// </summary>
    public static Image? Load(string dateiname)
    {
        string pfad = Path.Combine(IconVerzeichnis, dateiname);
        if (!File.Exists(pfad)) return null;
        try
        {
            byte[] data = File.ReadAllBytes(pfad);
            return Image.FromStream(new MemoryStream(data));
        }
        catch (Exception ex)
        {
            ErrorLogger.Log($"IconLoader.Load({dateiname})", ex);
            return null;
        }
    }

    /// <summary>
    /// Setzt Button.Image aus der Icons-Datei und löscht den Text.
    /// Wenn die Datei fehlt, bleibt der bestehende Text (Unicode-Fallback) erhalten.
    /// </summary>
    public static void Apply(Button button, string dateiname)
    {
        var img = Load(dateiname);
        if (img == null) return;
        button.Image      = img;
        button.Text       = "";
        button.ImageAlign = ContentAlignment.MiddleCenter;
        button.Padding    = new Padding(0);
    }
}
