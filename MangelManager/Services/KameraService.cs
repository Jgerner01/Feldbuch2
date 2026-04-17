using System.Windows;
using MangelManager.Views;

namespace MangelManager.Services;

/// <summary>
/// Öffnet das eigene Kamerafenster (MediaCapture) und gibt den gespeicherten Dateipfad zurück.
/// </summary>
public static class KameraService
{
    public static string? FotoAufnehmen(string speicherOrdner)
    {
        var window = new KameraCaptureWindow(speicherOrdner)
        {
            Owner = Application.Current.MainWindow
        };

        return window.ShowDialog() == true ? window.AufgenommenePfad : null;
    }
}
