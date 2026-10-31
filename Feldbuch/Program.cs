namespace Feldbuch;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        DateiSpeicher.Ladefehler += nachricht => MessageBox.Show(nachricht,
            "Datei konnte nicht geladen werden", MessageBoxButtons.OK, MessageBoxIcon.Error);
        Application.ThreadException += (_, e) =>
        {
            ErrorLogger.Log("Anwendung", e.Exception);
            MessageBox.Show(e.Exception.Message, "Vorgang fehlgeschlagen",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        try
        {
            AppPfade.InitialisiereBenutzerdaten();
            IkonGenerator.EnsureIcons();
            TachymeterMessungsCache.Initialisieren();
            Application.Run(new Form1());
        }
        catch (Exception ex)
        {
            ErrorLogger.Log("Programmstart", ex);
            MessageBox.Show(ex.Message, "Programmstart fehlgeschlagen", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { TachymeterVerbindung.Trennen(); }
    }    
}
