using System.IO.Ports;

namespace Feldbuch;

public enum TachymeterModell
{
    LeicaTCR307,
    LeicaTS06,
    LeicaTS11,
    TrimbleS3,
    TopconGPT3000,
    SokkiaSET,
    LeicaGeoCOM,
    LeicaGeoCOMStandard,
    Manuell
}

public static class TachymeterVerbindung
{
    // ── Einstellungen ─────────────────────────────────────────────────────────
    public static TachymeterModell Modell   { get; set; } = TachymeterModell.LeicaTCR307;
    public static string           Port     { get; set; } = "";
    public static int              BaudRate { get; set; } = 9600;
    public static int              DataBits { get; set; } = 8;
    public static Parity           Parität  { get; set; } = Parity.None;
    public static StopBits         StopBits { get; set; } = StopBits.One;

    // ── Verbindung ────────────────────────────────────────────────────────────
    private static SerialPort? _port;
    public  static bool IstVerbunden => _port?.IsOpen ?? false;

    // ── Ereignis: Rohdaten empfangen (für Testmessungen-Fenster) ─────────────
    public static event EventHandler<string>? DatenEmpfangen;

    public static void Verbinden()
    {
        if (IstVerbunden) return;
        if (string.IsNullOrEmpty(Port))
            throw new InvalidOperationException("Kein COM-Port ausgewählt.");

        var neu = new SerialPort(Port, BaudRate, Parität, DataBits, StopBits)
        {
            ReadTimeout  = 2000,
            WriteTimeout = 2000
        };
        neu.DataReceived += (s, e) =>
        {
            try
            {
                if (s is SerialPort sp && sp.IsOpen)
                {
                    var daten = sp.ReadExisting();
                    if (!string.IsNullOrEmpty(daten))
                        DatenEmpfangen?.Invoke(null, daten);
                }
            }
            catch { }
        };
        try
        {
            neu.Open();
        }
        catch
        {
            neu.Dispose();
            throw;
        }
        _port = neu;
    }

    public static void Trennen()
    {
        _port?.Close();
        _port?.Dispose();
        _port = null;
    }

    // ── Einstellungen laden / speichern ───────────────────────────────────────
    public static void LadeEinstellungen()
    {
        Modell   = ProjektManager.TachymeterModell;
        Port     = ProjektManager.TachymeterPort;
        BaudRate = ProjektManager.TachymeterBaudRate;
        DataBits = ProjektManager.TachymeterDataBits;
        Parität  = Enum.TryParse<Parity>(ProjektManager.TachymeterParitaet, out var p)
                   ? p : Parity.None;
        StopBits = ProjektManager.TachymeterStopBits switch
        {
            "1.5" => StopBits.OnePointFive,
            "2"   => StopBits.Two,
            _     => StopBits.One
        };
    }

    public static void SpeichereEinstellungen()
    {
        ProjektManager.TachymeterModell   = Modell;
        ProjektManager.TachymeterPort     = Port;
        ProjektManager.TachymeterBaudRate = BaudRate;
        ProjektManager.TachymeterDataBits = DataBits;
        ProjektManager.TachymeterParitaet = Parität.ToString();
        ProjektManager.TachymeterStopBits = StopBits switch
        {
            StopBits.OnePointFive => "1.5",
            StopBits.Two          => "2",
            _                     => "1"
        };
        ProjektManager.SpeichereOptionen();
    }

    // ── GeoCOM: Befehl senden ─────────────────────────────────────────────────
    // Protokollformat: %R1Q,{RPC},0:{params}\r\n  →  Antwort: %R1P,0,0:{RC},{daten}\r\n
    // Winkel in Bogenmass (rad), Strecken in Metern.
    public static void GeoCOM_Senden(string befehl)
    {
        if (!IstVerbunden)
            throw new InvalidOperationException("Kein COM-Port verbunden.");
        // Zeilenende: CR+LF laut GeoCOM-Protokoll
        _port!.Write(befehl + "\r\n");
    }

    // ── Voreinstellungen je Modell ────────────────────────────────────────────
    public static (int Baud, int Bits, Parity Par, StopBits Stop) GetPreset(TachymeterModell m) =>
        m switch
        {
            TachymeterModell.LeicaTCR307   => (9600,  8, Parity.None, StopBits.One),
            TachymeterModell.LeicaTS06     => (9600,  8, Parity.None, StopBits.One),
            TachymeterModell.LeicaTS11     => (9600,  8, Parity.None, StopBits.One),
            TachymeterModell.TrimbleS3     => (9600,  8, Parity.None, StopBits.One),
            TachymeterModell.TopconGPT3000 => (9600,  8, Parity.None, StopBits.One),
            TachymeterModell.SokkiaSET     => (9600,  8, Parity.None, StopBits.One),
            // GeoCOM 38400 Baud (COM_SetSWBaudrate Code 5) – schnelle Kommunikation
            TachymeterModell.LeicaGeoCOM         => (38400, 8, Parity.None, StopBits.One),
            // GeoCOM 9600 Baud (COM_SetSWBaudrate Code 3) – sicherer Fallback
            TachymeterModell.LeicaGeoCOMStandard => (9600,  8, Parity.None, StopBits.One),
            _                                    => (9600,  8, Parity.None, StopBits.One)
        };

    // ── Anzeigetexte ──────────────────────────────────────────────────────────
    public static string ModellAnzeige(TachymeterModell m) => m switch
    {
        TachymeterModell.LeicaTCR307   => "Leica TCR307",
        TachymeterModell.LeicaTS06     => "Leica TS06",
        TachymeterModell.LeicaTS11     => "Leica TS11",
        TachymeterModell.TrimbleS3     => "Trimble S3",
        TachymeterModell.TopconGPT3000 => "Topcon GPT-3000",
        TachymeterModell.SokkiaSET     => "Sokkia SET",
        TachymeterModell.LeicaGeoCOM         => "GeoCOM (Leica TPS1200)  –  38400 Baud",
        TachymeterModell.LeicaGeoCOMStandard => "GeoCOM Standard (Leica TPS1200)  –  9600 Baud",
        TachymeterModell.Manuell             => "Manuell",
        _                              => m.ToString()
    };

    public static TachymeterModell[] AlleModelle =>
    [
        TachymeterModell.LeicaTCR307,
        TachymeterModell.LeicaTS06,
        TachymeterModell.LeicaTS11,
        TachymeterModell.TrimbleS3,
        TachymeterModell.TopconGPT3000,
        TachymeterModell.SokkiaSET,
        TachymeterModell.LeicaGeoCOM,
        TachymeterModell.LeicaGeoCOMStandard,
        TachymeterModell.Manuell,
    ];
}
