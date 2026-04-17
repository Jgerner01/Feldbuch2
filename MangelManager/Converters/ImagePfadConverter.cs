using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace MangelManager.Converters;

/// <summary>
/// Konvertiert einen lokalen Dateipfad in eine BitmapImage-Quelle.
/// Lädt das Bild mit CacheOption.OnLoad, damit der Datei-Handle sofort
/// freigegeben wird und Dateien gelöscht werden können.
/// </summary>
public class ImagePfadConverter : IValueConverter
{
    public static readonly ImagePfadConverter Instance = new();

    public object? Convert(object value, Type targetType,
        object parameter, CultureInfo culture)
    {
        if (value is not string pfad || string.IsNullOrWhiteSpace(pfad)) return null;
        if (!File.Exists(pfad)) return null;

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(pfad, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;   // Handle sofort freigeben
            image.DecodePixelWidth = 160;                   // 2× Anzeigegröße, RAM-effizient
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType,
        object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
