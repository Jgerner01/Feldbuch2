using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Storage;
using Windows.Storage.Streams;
using WinBitmapEncoder = Windows.Graphics.Imaging.BitmapEncoder;

namespace MangelManager.Views;

public partial class KameraCaptureWindow : Window
{
    private readonly string _zielOrdner;
    private MediaCapture? _capture;
    private MediaFrameReader? _frameReader;
    private DeviceInformationCollection? _devices;
    private DateTime _lastPreview = DateTime.MinValue;
    private readonly CancellationTokenSource _cts = new();
    private volatile bool _isCapturing;

    public string? AufgenommenePfad { get; private set; }

    public KameraCaptureWindow(string zielOrdner)
    {
        _zielOrdner = zielOrdner;
        InitializeComponent();
        Loaded += async (_, _) => await InitKamerasAsync();
        Closing += OnWindowClosing;
    }

    // ── Initialisierung ──────────────────────────────────────────────────────

    private async Task InitKamerasAsync()
    {
        SetStatus("Kameras werden gesucht...");

        _devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

        if (_devices.Count == 0)
        {
            SetStatus("Keine Kamera gefunden.");
            NoBildText.Text = "Kein Kameragerät";
            return;
        }

        foreach (var d in _devices)
            KameraAuswahl.Items.Add(d.Name);

        KameraAuswahl.SelectedIndex = 0;
        // Auswahl-Event startet die Kamera
    }

    private async Task StartKameraAsync(string deviceId)
    {
        await StopKameraAsync();
        SetStatus("Kamera wird gestartet...");
        AufnehmenBtn.IsEnabled = false;
        NoBildText.Visibility = Visibility.Visible;
        PreviewImage.Source = null;

        try
        {
            _capture = new MediaCapture();
            await _capture.InitializeAsync(new MediaCaptureInitializationSettings
            {
                VideoDeviceId = deviceId,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu
            });

            // Farb-Framequelle auswählen
            var source = _capture.FrameSources
                .Where(fs => fs.Value.Info.SourceKind == MediaFrameSourceKind.Color)
                .Select(fs => fs.Value)
                .FirstOrDefault();

            if (source == null)
            {
                SetStatus("Kein Videostream gefunden.");
                return;
            }

            _frameReader = await _capture.CreateFrameReaderAsync(source);
            _frameReader.FrameArrived += OnFrameArrived;
            await _frameReader.StartAsync();

            SetStatus("Bereit – klicken Sie auf Aufnehmen");
            AufnehmenBtn.IsEnabled = true;
        }
        catch (UnauthorizedAccessException)
        {
            SetStatus("Kamerazugriff verweigert.");
            NoBildText.Text = "Zugriff verweigert";
            MessageBox.Show(
                "Kamerazugriff wurde verweigert.\n\n" +
                "Bitte aktivieren Sie den Zugriff unter:\n" +
                "Einstellungen → Datenschutz & Sicherheit → Kamera",
                "Kamerazugriff verweigert",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler: {ex.Message}");
            NoBildText.Text = "Fehler beim Starten";
        }
    }

    private async Task StopKameraAsync()
    {
        if (_frameReader != null)
        {
            _frameReader.FrameArrived -= OnFrameArrived;
            await _frameReader.StopAsync();
            _frameReader.Dispose();
            _frameReader = null;
        }
        _capture?.Dispose();
        _capture = null;
    }

    // ── Vorschau ─────────────────────────────────────────────────────────────

    private async void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        if (_cts.IsCancellationRequested || _isCapturing) return;

        // Auf ca. 8 fps drosseln
        var now = DateTime.UtcNow;
        if ((now - _lastPreview).TotalMilliseconds < 125) return;
        _lastPreview = now;

        using var frame = sender.TryAcquireLatestFrame();
        var src = frame?.VideoMediaFrame?.SoftwareBitmap;
        if (src == null) return;

        try
        {
            using var bgra = SoftwareBitmap.Convert(src,
                BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

            var bitmapSource = await ZuBitmapSourceAsync(bgra, 640);

            if (!_cts.IsCancellationRequested)
                await Dispatcher.InvokeAsync(() =>
                {
                    PreviewImage.Source = bitmapSource;
                    NoBildText.Visibility = Visibility.Collapsed;
                });
        }
        catch { /* Frame-Fehler ignorieren */ }
    }

    // Konvertiert SoftwareBitmap → WPF BitmapImage (JPEG im RAM, kein unsafe code)
    private static async Task<BitmapImage> ZuBitmapSourceAsync(SoftwareBitmap bitmap, uint breite)
    {
        using var stream = new InMemoryRandomAccessStream();
        var encoder = await WinBitmapEncoder.CreateAsync(WinBitmapEncoder.JpegEncoderId, stream);
        encoder.SetSoftwareBitmap(bitmap);

        if (bitmap.PixelWidth > (int)breite)
        {
            encoder.BitmapTransform.ScaledWidth = breite;
            encoder.BitmapTransform.ScaledHeight =
                (uint)Math.Round(breite * (double)bitmap.PixelHeight / bitmap.PixelWidth);
            encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
        }

        await encoder.FlushAsync();

        // InMemoryRandomAccessStream → byte[] → MemoryStream (kein AsStream() nötig)
        stream.Seek(0);
        var reader = new DataReader(stream);
        await reader.LoadAsync((uint)stream.Size);
        var bytes = new byte[stream.Size];
        reader.ReadBytes(bytes);

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = new MemoryStream(bytes);
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        bitmapImage.Freeze();
        return bitmapImage;
    }

    // ── Aufnahme ─────────────────────────────────────────────────────────────

    private async void Aufnehmen_Click(object sender, RoutedEventArgs e)
    {
        if (_frameReader == null) return;

        _isCapturing = true;
        AufnehmenBtn.IsEnabled = false;
        SetStatus("Aufnahme wird gespeichert...");

        try
        {
            using var frame = _frameReader.TryAcquireLatestFrame();
            var src = frame?.VideoMediaFrame?.SoftwareBitmap;

            if (src == null)
            {
                MessageBox.Show(
                    "Kein Bild verfügbar – bitte einen Moment warten.",
                    "Kein Bild", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            using var bgra = SoftwareBitmap.Convert(src,
                BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

            Directory.CreateDirectory(_zielOrdner);
            var dateiName = $"Kamera_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";

            // Direkt in Datei schreiben über WinRT StorageFolder
            var folder = await StorageFolder.GetFolderFromPathAsync(_zielOrdner);
            var file = await folder.CreateFileAsync(
                dateiName, CreationCollisionOption.ReplaceExisting);

            using var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite);
            var encoder = await WinBitmapEncoder.CreateAsync(
                WinBitmapEncoder.JpegEncoderId, fileStream);
            encoder.SetSoftwareBitmap(bgra);
            await encoder.FlushAsync();

            AufgenommenePfad = file.Path;
            DialogResult = true;   // schließt das Fenster
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler bei der Aufnahme:\n{ex.Message}",
                "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            SetStatus("Fehler – erneut versuchen");
        }
        finally
        {
            _isCapturing = false;
            if (IsLoaded && IsVisible)
                AufnehmenBtn.IsEnabled = true;
        }
    }

    private void Abbrechen_Click(object sender, RoutedEventArgs e) => Close();

    // ── Kamera wechseln ──────────────────────────────────────────────────────

    private async void KameraAuswahl_SelectionChanged(object sender,
        System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_devices == null || KameraAuswahl.SelectedIndex < 0) return;
        await StartKameraAsync(_devices[KameraAuswahl.SelectedIndex].Id);
    }

    // ── Cleanup ──────────────────────────────────────────────────────────────

    private bool _schliessen;

    private void OnWindowClosing(object? sender,
        System.ComponentModel.CancelEventArgs e)
    {
        if (_schliessen) return;
        _schliessen = true;
        _cts.Cancel();
        e.Cancel = true;

        StopKameraAsync().ContinueWith(_ =>
            Dispatcher.Invoke(() =>
            {
                _schliessen = false;
                Close();
            }));
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────────

    private void SetStatus(string text) =>
        Dispatcher.InvokeAsync(() => StatusText.Text = text);
}
