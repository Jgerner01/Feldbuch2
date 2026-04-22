using System.IO.Ports;

namespace Feldbuch;

public enum TachymeterModell
{
    // ── Leica GeoCOM-Protokoll ────────────────────────────────────────────────
    LeicaGeoCOM,              // 38400 Baud
    LeicaGeoCOMStandard,      // 9600 Baud (sicherer Fallback)
    LeicaTS06,
    LeicaTS11,
    TrimbleS3,
    // ── Leica GSI Online-Protokoll ────────────────────────────────────────────
    LeicaTPS300,              // TPS300-Reihe, GSI Online-Protokoll, 9600 Baud
    // ── Sokkia SDR-Format ─────────────────────────────────────────────────────
    SokkiaSDR,                // Sokkia SET-Reihe, SDR33-ASCII-Format
    // ── Topcon-Protokoll ──────────────────────────────────────────────────────
    TopconGTS,                // Topcon GTS/GPT-Reihe, CR-Trigger
    // ── GNSS ──────────────────────────────────────────────────────────────────
    GnssNmea,                 // GNSS-Empfänger, NMEA 0183
    // ── Manuell ───────────────────────────────────────────────────────────────
    Manuell
}

public static class TachymeterVerbindung
{
    // ── Einstellungen ─────────────────────────────────────────────────────────
    public static TachymeterModell Modell   { get; set; } = TachymeterModell.LeicaTPS300;
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

    // ── Ereignis: Verbindungsabbruch erkannt ──────────────────────────────────
    public static event EventHandler? VerbindungGetrennt;

    // ── Health-Check-Timer (Überwachung der Bluetooth-Verbindung) ─────────────
    private static System.Timers.Timer? _healthCheckTimer;
    private const int HealthCheckIntervalMs = 5000;

    // ── Timeout-Werte (angepasst für stabiles Bluetooth) ──────────────────────
    private const int ReadTimeoutMs  = 3000;
    private const int WriteTimeoutMs = 3000;
    private const int MaxRetries     = 2;

    public static void Verbinden()
    {
        if (IstVerbunden) return;
        if (string.IsNullOrEmpty(Port))
            throw new InvalidOperationException("Kein COM-Port ausgewählt.");

        var neu = new SerialPort(Port, BaudRate, Parität, DataBits, StopBits)
        {
            ReadTimeout     = ReadTimeoutMs,
            WriteTimeout    = WriteTimeoutMs,
            ReadBufferSize  = 4096,
            WriteBufferSize = 4096
        };

        // ErrorReceived: Bei SerialPort-Fehlern (z.B. Bluetooth-Abbruch)
        neu.ErrorReceived += (s, e) =>
        {
            // Bei FrameError oder OverrunError → Verbindung als getrennt markieren
            if (e.EventType is SerialError.Frame or SerialError.Overrun)
            {
                VerbindungVerloren();
            }
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
            catch (TimeoutException)
            {
                // Timeout ist erwartbar → ignorieren
            }
            catch (InvalidOperationException)
            {
                // Port wurde geschlossen → ignorieren
            }
            catch
            {
                // Unerwarteter Fehler → Verbindung trennen
                VerbindungVerloren();
            }
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
        StoppeVerbindungsueberwachung();
        _port?.Close();
        _port?.Dispose();
        _port = null;
    }

    /// <summary>
    /// Sendet einen GeoCOM-Befehl mit Retry-Logik bei Bluetooth-Timeouts.
    /// Trennt die Verbindung erst nach mehreren fehlgeschlagenen Versuchen.
    /// </summary>
    public static void GeoCOM_Senden(string befehl)
    {
        if (!IstVerbunden)
            throw new InvalidOperationException("Kein COM-Port verbunden.");

        var daten = befehl + "\r\n";
        Exception? letzterFehler = null;

        for (int versuch = 1; versuch <= MaxRetries; versuch++)
        {
            try
            {
                // SerialPort.Write() verwendet den WriteTimeout korrekt
                _port!.Write(daten);
                return; // Erfolgreich gesendet
            }
            catch (TimeoutException)
            {
                letzterFehler = new TimeoutException(
                    $"Schreibvorgang fehlgeschlagen (Versuch {versuch}/{MaxRetries}) – Bluetooth-Timeout.");
                System.Threading.Thread.Sleep(300);
            }
            catch (IOException)
            {
                letzterFehler = new IOException(
                    $"Schreibvorgang fehlgeschlagen (Versuch {versuch}/{MaxRetries}) – Bluetooth-Verbindung unterbrochen.");
                VerbindungVerloren();
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                // Port wurde geschlossen (z.B. durch Bluetooth-Abbruch)
                letzterFehler = new IOException("Bluetooth-Verbindung wurde getrennt.");
                VerbindungVerloren();
                throw;
            }
        }

        // Alle Versuche fehlgeschlagen → Verbindung trennen
        VerbindungVerloren();
        throw letzterFehler!;
    }

    /// <summary>
    /// Sendet einen GeoCOM-Befehl und wartet asynchron auf eine Antwort mit Timeout.
    /// Verhindert Blockierung der UI bei instabiler Bluetooth-Verbindung.
    /// Versucht bei Timeout bis zu 2 Wiederholungen bevor die Verbindung getrennt wird.
    /// </summary>
    /// <param name="befehl">GeoCOM-Befehl (z.B. "%R1Q,2107,0:0")</param>
    /// <param name="timeoutMs">Timeout pro Versuch in Millisekunden (Standard: 5000)</param>
    /// <param name="antwortFilter">Optionaler Filter: Nur Antworten die diesen String enthalten werden akzeptiert (Standard: "%R1P")</param>
    /// <returns>Die vollständige Antwortzeile</returns>
    public static async Task<string> GeoCOM_SendenUndAntwortAsync(
        string befehl,
        int timeoutMs = 5000,
        string? antwortFilter = null)
    {
        if (!IstVerbunden)
            throw new InvalidOperationException("Kein COM-Port verbunden.");

        var filter = antwortFilter ?? "%R1P";
        Exception? letzterFehler = null;

        for (int versuch = 1; versuch <= MaxRetries; versuch++)
        {
            var cts = new CancellationTokenSource(timeoutMs);
            var empfangen = new TaskCompletionSource<string>();
            var daten = befehl + "\r\n";

            // Temporärer Handler für Antwort
            void OnAntwort(object? s, SerialDataReceivedEventArgs e)
            {
                try
                {
                    if (s is SerialPort sp && sp.IsOpen)
                    {
                        var antwort = sp.ReadExisting();
                        if (!string.IsNullOrEmpty(antwort) && antwort.Contains(filter))
                        {
                            empfangen.TrySetResult(antwort);
                        }
                    }
                }
                catch (TimeoutException) { }
                catch (InvalidOperationException) { }
                catch (Exception ex)
                {
                    empfangen.TrySetException(ex);
                }
            }

            _port!.DataReceived += OnAntwort;

            try
            {
                // Write in einem Task mit Timeout – da SerialPort kein WriteAsync hat
                var bytes = System.Text.Encoding.ASCII.GetBytes(daten);
                var writeTask = Task.Run(() => _port.Write(bytes, 0, bytes.Length), cts.Token);
                await writeTask;

                // Auf Antwort warten (mit Timeout)
                using var reg = cts.Token.Register(() =>
                    empfangen.TrySetException(new TimeoutException(
                        $"Keine Antwort vom Tachymeter innerhalb von {timeoutMs} ms.")));

                return await empfangen.Task;
            }
            catch (TimeoutException)
            {
                letzterFehler = new TimeoutException(
                    $"Keine Antwort vom Tachymeter innerhalb von {timeoutMs} ms (Versuch {versuch}/{MaxRetries}).");
                await Task.Delay(300);
            }
            catch (OperationCanceledException)
            {
                letzterFehler = new TimeoutException(
                    $"Schreibvorgang abgebrochen (Versuch {versuch}/{MaxRetries}).");
                await Task.Delay(300);
            }
            catch (IOException)
            {
                VerbindungVerloren();
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                VerbindungVerloren();
                throw;
            }
            finally
            {
                _port.DataReceived -= OnAntwort;
            }
        }

        VerbindungVerloren();
        throw letzterFehler!;
    }

    // ── Verbindungsüberwachung (Health Check) ─────────────────────────────────

    /// <summary>
    /// Startet eine regelmäßige Überprüfung der Verbindung mittels GeoCOM-Ping.
    /// Wird kein gültiger Ping-Response empfangen, wird die Verbindung als getrennt markiert.
    /// </summary>
    /// <param name="intervalMs">Prüfintervall in Millisekunden (Standard: 5000)</param>
    public static void StarteVerbindungsueberwachung(int intervalMs = HealthCheckIntervalMs)
    {
        StoppeVerbindungsueberwachung();

        _healthCheckTimer = new System.Timers.Timer(intervalMs)
        {
            AutoReset = true,
            Enabled = true
        };

        _healthCheckTimer.Elapsed += async (_, _) => await HealthCheckTickAsync();
        _healthCheckTimer.Start();
    }

    /// <summary>
    /// Stoppt die Verbindungsüberwachung.
    /// </summary>
    public static void StoppeVerbindungsueberwachung()
    {
        _healthCheckTimer?.Stop();
        _healthCheckTimer?.Dispose();
        _healthCheckTimer = null;
    }

    private static async Task HealthCheckTickAsync()
    {
        if (!IstVerbunden) return;

        try
        {
            if (Modell == TachymeterModell.LeicaTPS300)
            {
                // GSI Online Ping: GET/I/WI21 – Momentan-Winkelabfrage ohne EDM
                // Antwort enthält "21." wenn das Gerät antwortet
                await GeoCOM_SendenUndAntwortAsync(
                    "GET/I/WI21", timeoutMs: 3000, antwortFilter: "21.");
            }
            else
            {
                // GeoCOM Ping: RPC_COM_NullProc (RPC 0) – minimale Anfrage
                var antwort = await GeoCOM_SendenUndAntwortAsync("%R1Q,0,0:", timeoutMs: 3000);

                // GRC_OK (0) oder informative RCs prüfen
                bool gueltig = antwort.Contains(":0,")
                            || antwort.Contains(":1283")
                            || antwort.Contains(":1284")
                            || antwort.Contains(":1285");

                if (!gueltig)
                    VerbindungVerloren();
            }
        }
        catch
        {
            VerbindungVerloren();
        }
    }

    private static void VerbindungVerloren()
    {
        if (!IstVerbunden) return; // Bereits getrennt

        Trennen();
        VerbindungGetrennt?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// Versucht die Verbindung wiederherzustellen ohne die Einstellungen zu ändern.
    /// Gibt true zurück wenn die Verbindung erfolgreich wiederhergestellt wurde.
    /// </summary>
    public static bool Wiederherstellen()
    {
        try
        {
            Trennen();
            Verbinden();
            return true;
        }
        catch
        {
            return false;
        }
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

    // ── Voreinstellungen je Modell ────────────────────────────────────────────
    public static (int Baud, int Bits, Parity Par, StopBits Stop) GetPreset(TachymeterModell m) =>
        m switch
        {
            // GeoCOM 38400 Baud (COM_SetSWBaudrate Code 5) – schnelle Kommunikation
            TachymeterModell.LeicaGeoCOM         => (38400, 8, Parity.None, StopBits.One),
            // GeoCOM 9600 Baud (COM_SetSWBaudrate Code 3) – sicherer Fallback
            TachymeterModell.LeicaGeoCOMStandard => (9600,  8, Parity.None, StopBits.One),
            TachymeterModell.LeicaTS06           => (9600,  8, Parity.None, StopBits.One),
            TachymeterModell.LeicaTS11           => (9600,  8, Parity.None, StopBits.One),
            TachymeterModell.TrimbleS3           => (9600,  8, Parity.None, StopBits.One),
            TachymeterModell.LeicaTPS300         => (9600,  8, Parity.None, StopBits.One),
            TachymeterModell.SokkiaSDR           => (9600,  8, Parity.None, StopBits.One),
            TachymeterModell.TopconGTS           => (9600,  8, Parity.None, StopBits.One),
            TachymeterModell.GnssNmea            => (4800,  8, Parity.None, StopBits.One),
            _                                    => (9600,  8, Parity.None, StopBits.One)
        };

    // ── Anzeigetexte ──────────────────────────────────────────────────────────
    public static string ModellAnzeige(TachymeterModell m) => m switch
    {
        TachymeterModell.LeicaGeoCOM         => "Leica GeoCOM (TPS1200)  –  38400 Baud",
        TachymeterModell.LeicaGeoCOMStandard => "Leica GeoCOM Standard (TPS1200)  –  9600 Baud",
        TachymeterModell.LeicaTS06           => "Leica TS06",
        TachymeterModell.LeicaTS11           => "Leica TS11",
        TachymeterModell.TrimbleS3           => "Trimble S3",
        TachymeterModell.LeicaTPS300         => "Leica TPS300  (GSI Online)  –  9600 Baud",
        TachymeterModell.SokkiaSDR           => "Sokkia SDR  (SET-Reihe)",
        TachymeterModell.TopconGTS           => "Topcon GTS/GPT",
        TachymeterModell.GnssNmea            => "GNSS  (NMEA 0183)",
        TachymeterModell.Manuell             => "Manuell",
        _                                    => m.ToString()
    };

    public static TachymeterModell[] AlleModelle =>
    [
        TachymeterModell.LeicaGeoCOM,
        TachymeterModell.LeicaGeoCOMStandard,
        TachymeterModell.LeicaTS06,
        TachymeterModell.LeicaTS11,
        TachymeterModell.TrimbleS3,
        TachymeterModell.LeicaTPS300,
        TachymeterModell.SokkiaSDR,
        TachymeterModell.TopconGTS,
        TachymeterModell.GnssNmea,
        TachymeterModell.Manuell,
    ];
}
