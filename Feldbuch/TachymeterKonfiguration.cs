namespace Feldbuch;

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

// ──────────────────────────────────────────────────────────────────────────────
// TachymeterKonfiguration – eine gemeinsame Quelle für Zieltyp (Prisma /
// Reflektorlos), EDM-Modus und Prismenkonstante aller Messfenster, plus
// Abgleich dieser Einstellungen mit dem Gerät (nur Leica GeoCOM).
//
// Ablauf des Abgleichs (jeder Schritt wartet auf die Bestätigung des Geräts):
//   1. BAP_SetTargetType (17021)   Prisma / Reflektorlos
//   2. TMC_SetEdmMode    (2020)    EDM-Programm
//   3. TMC_SetPrismCorr  (2024)    Prismenkonstante – nur im Prisma-Modus; die
//                                  Reflektorlos-Einstellung des Geräts wird nie
//                                  mit der Prismenkonstante überschrieben
//   4. Rücklesen: BAP_GetTargetType (17022), TMC_GetEdmMode (2021),
//                 TMC_GetPrismCorr (2023) und Vergleich mit der Konfiguration.
//
// Jede Anfrage trägt eine Transaktionsnummer (TrId, 1..7), die das Gerät in der
// Antwort wiederholt; verspätete Antworten früherer Befehle werden so verworfen.
// Meldet das Gerät keine Nummer zurück, wird auf Nummer 0 umgestellt und die
// erste Antwortzeile akzeptiert.
//
// Die Einstellungen werden in Einstellungen.xml gespeichert (ProjektManager).
// Hardware: Leica-Tachymeter mit GeoCOM   Autor: Johann Gerner
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>Messmodus der Distanzmessung.</summary>
public enum MessModus { Prisma, ReflektorlosKurz, ReflektorlosLang }

/// <summary>Ergebnis eines Abgleichs mit dem Gerät.</summary>
public record AbgleichErgebnis(bool Ok, string Text);

public static class TachymeterKonfiguration
{
    // ── GeoCOM-Konstanten ─────────────────────────────────────────────────────
    private const int BAP_REFL_USE     = 0;    // Zieltyp Reflektor (IR)
    private const int BAP_REFL_LESS    = 1;    // Zieltyp Reflektorlos (RL)
    private const int EDM_SINGLE_PRISM = 3;    // IR schnell
    private const int EDM_SINGLE_RL_K  = 5;    // RL Standard
    private const int EDM_SINGLE_RL_L  = 11;   // RL Mittelwert

    private const int    TRID_MAX            = 7;      // laut GeoCOM-Handbuch normal 1..7
    private const int    ANTWORT_TIMEOUT_MS  = 4000;
    private const double PK_TOLERANZ_MM      = 0.6;    // Vergleich Prismenkonstante [mm]
    private const double MM_PRO_M            = 1000.0;
    private const string PK_FORMAT           = "F4";   // Meter, wie bisher gesendet

    private static readonly Regex AntwortMuster =
        new(@"^%R1P,(-?\d+),(\d+):(-?\d+)(?:,(.*))?$", RegexOptions.Compiled);

    // ── Zustand ───────────────────────────────────────────────────────────────
    private static int  _version;                 // zählt jede Änderung der Konfiguration
    private static int  _abgeglichenVersion  = -1;
    private static int  _abgeglichenVerbindung = -1;
    private static bool _beschaeftigt;
    private static bool _trIdEcho            = true;
    private static int  _trIdZaehler;

    /// <summary>Wird nach jeder Änderung der Konfiguration ausgelöst.</summary>
    public static event Action? Geaendert;

    /// <summary>Wird nach einem Abgleich mit dem Gerät ausgelöst (UI-Thread).</summary>
    public static event Action<AbgleichErgebnis>? Abgeglichen;

    // ── Konfiguration ─────────────────────────────────────────────────────────
    public static MessModus Modus =>
        Enum.TryParse(ProjektManager.MessModusName, out MessModus m) ? m : MessModus.Prisma;

    public static bool    IstReflektorlos    => Modus != MessModus.Prisma;
    public static string  PrismaName         => ProjektManager.PrismaName;
    public static decimal PrismenkonstanteMm => ProjektManager.PrismaKonstanteMm;

    /// <summary>True während ein Abgleich läuft; Messungen sollten dann warten.</summary>
    public static bool Beschaeftigt => _beschaeftigt;

    /// <summary>True wenn das Gerät mit der aktuellen Konfiguration abgeglichen wurde.</summary>
    public static bool IstAbgeglichen =>
        _abgeglichenVersion == _version && _abgeglichenVerbindung == TachymeterVerbindung.VerbindungsNr;

    /// <summary>Nächster Modus im Wechsel Prisma → RL kurz (→ RL lang) → Prisma.</summary>
    public static MessModus Weiterschalten(bool dreiStufig) => Modus switch
    {
        MessModus.Prisma           => MessModus.ReflektorlosKurz,
        MessModus.ReflektorlosKurz => dreiStufig ? MessModus.ReflektorlosLang : MessModus.Prisma,
        _                          => MessModus.Prisma
    };

    public static void SetzeModus(MessModus modus)
    {
        if (modus == Modus) return;
        ProjektManager.MessModusName = modus.ToString();
        Geaendert_();
    }

    public static void SetzePrisma(string name, decimal konstanteMm)
    {
        if (name == ProjektManager.PrismaName && konstanteMm == ProjektManager.PrismaKonstanteMm) return;
        ProjektManager.PrismaName        = name;
        ProjektManager.PrismaKonstanteMm = konstanteMm;
        Geaendert_();
    }

    private static void Geaendert_()
    {
        _version++;
        ProjektManager.SpeichereOptionen();
        Geaendert?.Invoke();
    }

    /// <summary>Kurzbeschreibung für Tooltips und Meldungen.</summary>
    public static string Beschreibung => Modus switch
    {
        MessModus.Prisma           => $"Prisma ({PrismaName}, {PrismenkonstanteMm:+0.0;-0.0;0.0} mm)",
        MessModus.ReflektorlosKurz => "Reflektorlos kurz",
        _                          => "Reflektorlos lang"
    };

    public static int EdmProgramm(MessModus modus) => modus switch
    {
        MessModus.ReflektorlosKurz => EDM_SINGLE_RL_K,
        MessModus.ReflektorlosLang => EDM_SINGLE_RL_L,
        _                          => EDM_SINGLE_PRISM
    };

    /// <summary>Verwirft den Abgleichsstatus (z. B. nach einer Abweichung).</summary>
    public static void Verwerfen() => _abgeglichenVersion = -1;

    // ── Kontrolle nach einer Messung ──────────────────────────────────────────
    /// <summary>
    /// Vergleicht die vom Gerät bei der Messung gemeldeten Werte (EDM-Modus,
    /// Prismenkonstante) mit der Konfiguration. Gibt eine Warnung zurück, wenn sie
    /// abweichen, sonst null. Bei Abweichung gilt das Gerät als nicht abgeglichen.
    /// </summary>
    public static string? PruefeMessung(TachymeterMessung messung)
    {
        if (!TachymeterBefehlsgeberFactory.IstGeoCOM(TachymeterVerbindung.Modell)) return null;

        var probleme = new List<string>();

        if (messung.EdmModusRoh.HasValue && messung.EdmModusRoh.Value != EdmProgramm(Modus))
            probleme.Add($"EDM-Modus am Gerät {messung.EdmModusRoh.Value}, eingestellt {EdmProgramm(Modus)}");

        if (Modus == MessModus.Prisma && messung.Prismenkonstante_mm.HasValue
            && Math.Abs(messung.Prismenkonstante_mm.Value - (double)PrismenkonstanteMm) > PK_TOLERANZ_MM)
            probleme.Add(string.Format(CultureInfo.InvariantCulture,
                "Prismenkonstante am Gerät {0:+0.0;-0.0;0.0} mm, eingestellt {1:+0.0;-0.0;0.0} mm",
                messung.Prismenkonstante_mm.Value, PrismenkonstanteMm));

        if (probleme.Count == 0) return null;
        Verwerfen();
        return "Gerät weicht von der Einstellung ab: " + string.Join("; ", probleme);
    }

    // ── Abgleich ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Überträgt die Konfiguration ans Gerät und liest sie zur Kontrolle zurück.
    /// Ohne Verbindung oder ohne GeoCOM geschieht nichts. Ist das Gerät bereits
    /// abgeglichen, wird (außer bei <paramref name="erzwingen"/>) nichts gesendet.
    /// Muss vom UI-Thread aufgerufen werden und stört keine laufende Messung:
    /// der Aufrufer startet ihn nur im Ruhezustand.
    /// </summary>
    public static async Task<AbgleichErgebnis> AbgleichenAsync(bool erzwingen = false)
    {
        if (!TachymeterVerbindung.IstVerbunden)
            return new(false, "Kein Tachymeter verbunden.");
        if (!TachymeterBefehlsgeberFactory.IstGeoCOM(TachymeterVerbindung.Modell))
            return new(true, "Prisma / Reflektorlos bitte am Gerät einstellen (kein GeoCOM).");
        if (_beschaeftigt)
            return new(false, "Abgleich läuft bereits.");
        if (!erzwingen && IstAbgeglichen)
            return new(true, $"Gerät abgeglichen: {Beschreibung}");

        _beschaeftigt = true;
        int version    = _version;
        int verbindung = TachymeterVerbindung.VerbindungsNr;
        AbgleichErgebnis ergebnis;
        try
        {
            ergebnis = await AbgleichDurchfuehrenAsync();
            if (ergebnis.Ok)
            {
                _abgeglichenVersion    = version;
                _abgeglichenVerbindung = verbindung;
            }
        }
        catch (TimeoutException)
        {
            ergebnis = new(false, "Abgleich: keine Antwort vom Tachymeter.");
        }
        catch (Exception ex)
        {
            ErrorLogger.Log("TachymeterKonfiguration.AbgleichenAsync", ex);
            ergebnis = new(false, $"Abgleich fehlgeschlagen: {ex.Message}");
        }
        finally
        {
            _beschaeftigt = false;
        }

        Abgeglichen?.Invoke(ergebnis);
        return ergebnis;
    }

    private static async Task<AbgleichErgebnis> AbgleichDurchfuehrenAsync()
    {
        var modus    = Modus;
        int zieltyp  = modus == MessModus.Prisma ? BAP_REFL_USE : BAP_REFL_LESS;
        int edm      = EdmProgramm(modus);
        var probleme = new List<string>();

        // Einstellungen senden
        var (rc, _) = await AnfrageAsync(GeoCOMParser.RPC_BAP_SetTargetType, zieltyp.ToString());
        if (rc != 0) probleme.Add($"Zieltyp setzen (Fehler {rc})");

        (rc, _) = await AnfrageAsync(GeoCOMParser.RPC_TMC_SetEdmMode, edm.ToString());
        if (rc != 0) probleme.Add($"EDM-Modus setzen (Fehler {rc})");

        if (modus == MessModus.Prisma)
        {
            double pk_m = (double)PrismenkonstanteMm / MM_PRO_M;
            (rc, _) = await AnfrageAsync(GeoCOMParser.RPC_TMC_SetPrismCorr,
                pk_m.ToString(PK_FORMAT, CultureInfo.InvariantCulture));
            if (rc != 0) probleme.Add($"Prismenkonstante setzen (Fehler {rc})");
        }

        // Zur Kontrolle zurücklesen
        var (rcT, dT) = await AnfrageAsync(GeoCOMParser.RPC_BAP_GetTargetType, "");
        if (rcT != 0 || !int.TryParse(dT.FirstOrDefault(), out int zieltypGelesen))
            probleme.Add("Zieltyp nicht lesbar");
        else if (zieltypGelesen != zieltyp)
            probleme.Add($"Zieltyp am Gerät {zieltypGelesen}, eingestellt {zieltyp}");

        var (rcE, dE) = await AnfrageAsync(GeoCOMParser.RPC_TMC_GetEdmMode, "");
        if (rcE != 0 || !int.TryParse(dE.FirstOrDefault(), out int edmGelesen))
            probleme.Add("EDM-Modus nicht lesbar");
        else if (edmGelesen != edm)
            probleme.Add($"EDM-Modus am Gerät {edmGelesen}, eingestellt {edm}");

        if (modus == MessModus.Prisma)
        {
            var (rcP, dP) = await AnfrageAsync(GeoCOMParser.RPC_TMC_GetPrismCorr, "");
            if (rcP != 0 || !double.TryParse(dP.FirstOrDefault(),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out double pkGelesen_m))
                probleme.Add("Prismenkonstante nicht lesbar");
            else if (Math.Abs(pkGelesen_m * MM_PRO_M - (double)PrismenkonstanteMm) > PK_TOLERANZ_MM)
                probleme.Add(string.Format(CultureInfo.InvariantCulture,
                    "Prismenkonstante am Gerät {0:+0.0;-0.0;0.0} mm, eingestellt {1:+0.0;-0.0;0.0} mm",
                    pkGelesen_m * MM_PRO_M, PrismenkonstanteMm));
        }

        return probleme.Count == 0
            ? new(true,  $"Gerät abgeglichen: {Beschreibung}")
            : new(false, "Abgleich mit Abweichung: " + string.Join("; ", probleme));
    }

    // ── Anfrage mit Transaktionsnummer ────────────────────────────────────────
    /// <summary>Sendet eine Anfrage und wartet auf die zugehörige Antwort.</summary>
    private static async Task<(int Rc, string[] Daten)> AnfrageAsync(int rpc, string parameter)
    {
        try
        {
            return await SendeUndWarteAsync(rpc, parameter, _trIdEcho ? NaechsteTrId() : 0);
        }
        catch (TimeoutException) when (_trIdEcho)
        {
            // Gerät wiederholt die Transaktionsnummer evtl. nicht: ohne Nummer erneut versuchen
            _trIdEcho = false;
            return await SendeUndWarteAsync(rpc, parameter, 0);
        }
    }

    private static int NaechsteTrId() => (_trIdZaehler++ % TRID_MAX) + 1;

    private static async Task<(int Rc, string[] Daten)> SendeUndWarteAsync(int rpc, string parameter, int trId)
    {
        var tcs     = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var puffer  = new StringBuilder();
        var sperre  = new object();

        void OnDaten(object? sender, string daten)
        {
            lock (sperre)
            {
                puffer.Append(daten);
                string text = puffer.ToString();
                int pos;
                while ((pos = text.IndexOf('\n')) >= 0)
                {
                    string zeile = text[..pos].Trim();
                    text = text[(pos + 1)..];
                    var m = AntwortMuster.Match(zeile);
                    if (!m.Success) continue;
                    // Mit Nummer: nur die passende Antwort; ohne Nummer: die erste Antwort
                    if (trId == 0 || int.Parse(m.Groups[2].Value) == trId)
                        tcs.TrySetResult(zeile);
                }
                puffer.Clear();
                puffer.Append(text);
            }
        }

        TachymeterVerbindung.DatenEmpfangen += OnDaten;
        try
        {
            TachymeterVerbindung.GeoCOM_Senden($"%R1Q,{rpc},{trId}:{parameter}");

            using var cts = new CancellationTokenSource(ANTWORT_TIMEOUT_MS);
            using var reg = cts.Token.Register(() => tcs.TrySetException(
                new TimeoutException($"Keine Antwort auf RPC {rpc}.")));

            string antwort = await tcs.Task;
            var m = AntwortMuster.Match(antwort);
            int rc = int.Parse(m.Groups[3].Value);
            string[] daten = m.Groups[4].Success
                ? m.Groups[4].Value.Split(',', StringSplitOptions.TrimEntries)
                : [];
            return (rc, daten);
        }
        finally
        {
            TachymeterVerbindung.DatenEmpfangen -= OnDaten;
        }
    }

    /// <summary>Setzt den Nummern-Modus zurück, z. B. nach einer neuen Verbindung.</summary>
    internal static void ZuruecksetzenNachVerbinden() => _trIdEcho = true;
}
