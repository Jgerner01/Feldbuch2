// Zweck:    Koordiniert die Dauerabfrage des Horizontalwinkels (Zielrichtung im DXF-Viewer) mit
//           Fenstern, die selbst am Tachymeter messen. Solange ein solches Fenster geöffnet ist,
//           sendet der DXF-Viewer keine Winkelabfragen; eine noch ausstehende Antwort wird
//           abgewartet, bevor das Fenster eine Messung auslöst. So wird dessen Antwortzuordnung
//           nicht durch fremde Winkelantworten gestört.
// Hardware: Leica-Tachymeter (GeoCOM, GSI Online)
// Autor:    Johann Gerner

namespace Feldbuch;

public static class LiveWinkelSperre
{
    /// <summary>Nach dieser Zeit gilt eine unbeantwortete Winkelabfrage als verloren [ms].</summary>
    public const int ANTWORT_TIMEOUT_MS = 1500;

    private static int _sperren;

    /// <summary>Zeitpunkt der letzten gesendeten Winkelabfrage (UTC); MinValue = keine offen.</summary>
    public static DateTime LetzteAnfrage { get; set; } = DateTime.MinValue;

    /// <summary>True, solange mindestens ein messendes Fenster die Winkelabfrage sperrt.</summary>
    public static bool Gesperrt => _sperren > 0;

    /// <summary>True, wenn die Antwort auf eine gesendete Winkelabfrage noch aussteht.</summary>
    public static bool AntwortOffen
        => (DateTime.UtcNow - LetzteAnfrage).TotalMilliseconds < ANTWORT_TIMEOUT_MS;

    public static void Sperren()   => Interlocked.Increment(ref _sperren);

    public static void Freigeben()
    {
        if (Interlocked.Decrement(ref _sperren) < 0) Interlocked.Exchange(ref _sperren, 0);
    }

    /// <summary>Sperrt die Winkelabfrage, solange das Fenster geöffnet ist (Load bis FormClosed).</summary>
    public static void Binden(Form fenster)
    {
        bool gesperrt = false;
        fenster.Load += (_, _) =>
        {
            if (gesperrt) return;
            gesperrt = true;
            Sperren();
        };
        fenster.FormClosed += (_, _) =>
        {
            if (!gesperrt) return;
            gesperrt = false;
            Freigeben();
        };
    }
}
