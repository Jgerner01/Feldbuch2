namespace Feldbuch;

using System.Text;
using System.Text.Json;

// ──────────────────────────────────────────────────────────────────────────────
// StationsdatenManager – verwaltet den lebendigen Stationierungsstack.
//
// Datei: Station-{StandpunktNr}.json im Projektverzeichnis.
//
// Der Stack enthält alle Rohmessungen zu einem Standpunkt.
// Durch HinzufuegenUndBerechnen() wird nach jeder neuen Messung die
// Stationierung vollständig neu berechnet.  Das Ergebnis wird sowohl in der
// JSON-Datei als auch im ProjektdatenManager und FeldbuchpunkteManager
// synchronisiert.
// ──────────────────────────────────────────────────────────────────────────────

public class StationsdatenDatei
{
    public string                    StandpunktNr      { get; set; } = "";
    public double                    InstrumentenHoehe { get; set; }
    public string                    Datum             { get; set; } = "";
    [System.Text.Json.Serialization.JsonRequired]
    public List<StationierungsPunkt> Messungen         { get; set; } = new();
    public StationierungsErgebnis?   Ergebnis          { get; set; }
}

public static class StationsdatenManager
{
    private static StationsdatenDatei _aktiv = new();
    private static string             _pfad  = "";

    public static StationsdatenDatei AktiveStation    => _aktiv;
    public static string             StandpunktNr     => _aktiv.StandpunktNr;
    public static double             InstrumentenHoehe
    {
        get => _aktiv.InstrumentenHoehe;
        set { DateiSpeicher.PruefeSchreibzugriff(_pfad); _aktiv.InstrumentenHoehe = value; Speichern(); }
    }

    /// <summary>
    /// Konvergierte, endliche Stationierung mit mindestens zwei unterschiedlichen Anschlusspunkten.
    /// </summary>
    public static bool HatGueltigeStation =>
        StationierungsQualitaet.IstVerwendbar(_aktiv.Ergebnis)
        && _aktiv.Messungen.Select(p => p.PunktNr).Where(nr => !string.IsNullOrWhiteSpace(nr))
            .Distinct(StringComparer.Ordinal).Count() >= 2;

    public static StationierungsErgebnis? AktuellesErgebnis => _aktiv.Ergebnis;

    // ── Laden ─────────────────────────────────────────────────────────────────

    /// <summary>Lädt Stationsdaten für einen bestimmten Standpunkt (neu anlegen wenn nötig).</summary>
    public static void Laden(string standpunktNr)
    {
        string pfad = ProjektManager.GetPfad($"Station-{standpunktNr}.json");
        _pfad = pfad;
        _aktiv = Neu(standpunktNr);
        if (File.Exists(pfad))
        {
            try
            {
                string json = File.ReadAllText(pfad, Encoding.UTF8);
                var data = JsonSerializer.Deserialize<StationsdatenDatei>(json,
                    DateiSpeicher.JsonOptionen);
                _aktiv = data ?? throw new JsonException("Leere Stationsdatei.");
                if (_aktiv.Messungen == null) throw new JsonException("Fehlende Messungen.");
                if (_aktiv.Messungen.Any(p => p == null)) throw new JsonException("Ungültige Messung.");
                DateiSpeicher.LadenErfolgreich(pfad);
                _aktiv.StandpunktNr = standpunktNr;   // Sicherheit
            }
            catch (Exception ex)
            {
                DateiSpeicher.MeldeLadefehler(pfad, ex);
                _aktiv = Neu(standpunktNr);
            }
        }
        else
        {
            _aktiv = Neu(standpunktNr);
            DateiSpeicher.LadenErfolgreich(pfad);
        }
    }

    /// <summary>
    /// Liest den letzten Standpunkt aus ProjektdatenManager und lädt dessen Stationsdaten.
    /// Wird beim Öffnen des DXF-Viewers aufgerufen.
    /// </summary>
    public static void LadeAktuellenStandpunkt()
    {
        Zuruecksetzen();
        string? nr = ProjektdatenManager.GetValue("Freie Stationierung", "Standpunkt");
        if (!string.IsNullOrEmpty(nr))
        {
            Laden(nr);

            // Instrumentenhöhe aus ProjektdatenManager übernehmen (falls nicht in JSON)
            if (_aktiv.InstrumentenHoehe == 0)
            {
                string? ihStr = ProjektdatenManager.GetValue("Freie Stationierung", "Instrumentenhoehe [m]");
                if (double.TryParse(ihStr,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double ih))
                    _aktiv.InstrumentenHoehe = ih;
            }
        }
    }

    public static void Zuruecksetzen()
    {
        _aktiv = new();
        _pfad = "";
    }

    /// <summary>
    /// Liest die Stationsdatei eines Standpunkts, ohne den aktiven Stand zu verändern
    /// (für das Polarmodul). Gibt null zurück, wenn keine Datei vorhanden oder lesbar ist.
    /// </summary>
    public static StationsdatenDatei? LeseDatei(string standpunktNr)
    {
        if (!ProjektManager.IstGeladen || string.IsNullOrWhiteSpace(standpunktNr)) return null;
        string pfad = ProjektManager.GetPfad($"Station-{standpunktNr}.json");
        if (!File.Exists(pfad)) return null;
        try
        {
            var data = JsonSerializer.Deserialize<StationsdatenDatei>(
                File.ReadAllText(pfad, Encoding.UTF8),
                DateiSpeicher.JsonOptionen);
            if (data == null || data.Messungen == null || data.Messungen.Any(p => p == null))
                throw new JsonException("Ungültige Stationsdatei.");
            data.StandpunktNr = standpunktNr;
            DateiSpeicher.LadenErfolgreich(pfad);
            return data;
        }
        catch (Exception ex)
        {
            DateiSpeicher.MeldeLadefehler(pfad, ex);
            return null;
        }
    }

    // ── Messung hinzufügen und sofort neu berechnen ───────────────────────────

    /// <summary>
    /// Fügt eine Messung zum Anschlusspunkt-Datensatz hinzu und berechnet die Stationierung
    /// sofort neu. Bestehende Messungen gleicher PunktNr bleiben erhalten – ein Punkt kann
    /// mehrfach (z. B. in beiden Fernrohrlagen) in die Berechnung eingehen.
    /// Gibt das neue Ergebnis zurück, oder null wenn noch zu wenige Messungen.
    /// </summary>
    public static StationierungsErgebnis? HinzufuegenUndBerechnen(
        StationierungsPunkt punkt)
    {
        DateiSpeicher.PruefeSchreibzugriff(_pfad);
        _aktiv.Ergebnis = null;
        _aktiv.Messungen.Add(punkt);
        return Neuberechnen();
    }

    private static StationierungsErgebnis? Neuberechnen()
    {
        if (_aktiv.Messungen.Count < 2)
        {
            Speichern();
            return null;
        }

        var par      = RechenparameterManager.Params;
        var ergebnis = FreieStationierungRechner.Berechnen(
            _aktiv.Messungen,
            _aktiv.InstrumentenHoehe,
            freierMassstab:       par.FreierMassstab,
            berechnung3D:         par.Berechnung3D,
            fehlergrenzeMM_Hoehe: par.FehlergrenzeMM_Hoehe);

        _aktiv.Ergebnis = ergebnis;
        Speichern();
        if (!HatGueltigeStation) return ergebnis;

        // ProjektdatenManager synchronisieren
        var ic = System.Globalization.CultureInfo.InvariantCulture;
        ProjektdatenManager.SetValues("Freie Stationierung", new Dictionary<string, string>
        {
            ["Standpunkt"] = _aktiv.StandpunktNr,
            ["R [m]"] = ergebnis.R.ToString("F3", ic),
            ["H [m]"] = ergebnis.H.ToString("F3", ic),
            ["Hoehe [m]"] = ergebnis.Hoehe.ToString("F3", ic),
            ["Orientierung [gon]"] = ergebnis.Orientierung_gon.ToString("F4", ic),
            ["s0 [mm]"] = ergebnis.s0_mm.ToString("F2", ic),
            ["Massstab"] = ergebnis.Massstab.ToString("F6", ic),
            ["Instrumentenhoehe [m]"] = _aktiv.InstrumentenHoehe.ToString("F3", ic)
        });
        RechenparameterManager.SchreibeSigmaProjektdaten(ergebnis.s0_mm);

        // FeldbuchpunkteManager synchronisieren
        FeldbuchpunkteManager.AddOrUpdate(new FeldbuchPunkt
        {
            PunktNr          = _aktiv.StandpunktNr,
            Typ              = "Standpunkt",
            R                = ergebnis.R,
            H                = ergebnis.H,
            Hoehe            = ergebnis.Hoehe,
            Orientierung_gon = ergebnis.Orientierung_gon,
            IstBerechnung3D  = ergebnis.Berechnung3D,
            Datum            = DateTime.Today.ToString("yyyy-MM-dd"),
            Quelle           = "Freie Stationierung (Live)"
        });

        return ergebnis;
    }

    /// <summary>Löscht einen Anschlusspunkt aus dem Stack und berechnet neu.</summary>
    public static StationierungsErgebnis? EntfernenUndBerechnen(string punktNr)
    {
        DateiSpeicher.PruefeSchreibzugriff(_pfad);
        _aktiv.Ergebnis = null;
        _aktiv.Messungen.RemoveAll(p => p.PunktNr == punktNr);
        if (_aktiv.Messungen.Count < 2)
        {
            _aktiv.Ergebnis = null;
            Speichern();
            return null;
        }
        return Neuberechnen();
    }

    // ── Ergebnis aus manueller Berechnung übernehmen ─────────────────────────

    /// <summary>
    /// Übernimmt das Ergebnis einer manuellen Freie-Stationierung-Berechnung
    /// (FormFreieStationierung/FormErgebnis) in den aktiven Stationsdatensatz
    /// und schreibt die JSON-Datei.
    /// </summary>
    public static void SetzeErgebnisManuell(
        string standpunktNr, double instrHoehe,
        List<StationierungsPunkt> punkte,
        StationierungsErgebnis ergebnis)
    {
        string zielPfad = ProjektManager.GetPfad($"Station-{standpunktNr}.json");
        StationierungsQualitaet.Pruefe(ergebnis);
        if (File.Exists(zielPfad) && !string.Equals(_pfad, zielPfad, StringComparison.OrdinalIgnoreCase))
            Laden(standpunktNr);
        DateiSpeicher.PruefeSchreibzugriff(zielPfad);
        _aktiv.StandpunktNr      = standpunktNr;
        _aktiv.InstrumentenHoehe = instrHoehe;
        _aktiv.Messungen         = new List<StationierungsPunkt>(punkte);
        _aktiv.Ergebnis          = ergebnis;
        _aktiv.Datum             = DateTime.Today.ToString("yyyy-MM-dd");
        _pfad = zielPfad;
        Speichern();
    }

    // ── Neuen Standpunkt beginnen ─────────────────────────────────────────────

    /// <summary>
    /// Beginnt einen neuen Standpunkt: leer oder lädt vorhandene Datei.
    /// Setzt Instrumentenhöhe aus ProjektManager.
    /// </summary>
    public static void NeuerStandpunkt(string standpunktNr, double instrHoehe)
    {
        Laden(standpunktNr);
        DateiSpeicher.PruefeSchreibzugriff(_pfad);
        _aktiv.InstrumentenHoehe = instrHoehe;
        _aktiv.Datum             = DateTime.Today.ToString("yyyy-MM-dd");
        Speichern();
    }

    // ── Persistenz ────────────────────────────────────────────────────────────

    public static void Speichern()
    {
        if (string.IsNullOrEmpty(_pfad)) return;
        try
        {
            var opts = DateiSpeicher.JsonOptionen;
            string json = JsonSerializer.Serialize(_aktiv, opts);
            DateiSpeicher.SchreibeText(_pfad, json, Encoding.UTF8);
        }
        catch (Exception ex) { ErrorLogger.Log("StationsdatenManager.Speichern", ex); throw; }
    }

    // ── Hilfsmethoden ─────────────────────────────────────────────────────────

    private static StationsdatenDatei Neu(string nr) => new()
    {
        StandpunktNr = nr,
        Datum        = DateTime.Today.ToString("yyyy-MM-dd")
    };
}
