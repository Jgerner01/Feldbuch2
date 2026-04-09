namespace Feldbuch;

using System.Text;
using System.Text.Json;

// ──────────────────────────────────────────────────────────────────────────────
// NeupunkteManager – verwaltet gemessene Neupunkte.
//
// Datei: {ProjektName}-Neupunkte.json im Projektverzeichnis.
//
// Struktur:
//   • Rohdaten  (NeupunktRohdaten[]) – unveränderliche Polarmessungen
//   • Koordinaten (NeupunktErgebnis[]) – berechnete XYZ-Koordinaten
//
// Bei Stationierungsänderung werden nur die Koordinaten neu berechnet,
// die Rohdaten bleiben erhalten.  Jeder Neupunkt ist seinem Standpunkt
// über StandpunktNr zugeordnet; nach Neuberechnung werden nur die
// Koordinaten des betreffenden Standpunktes aktualisiert.
// ──────────────────────────────────────────────────────────────────────────────

public class NeupunkteDatei
{
    public List<NeupunktRohdaten>  Rohdaten    { get; set; } = new();
    public List<NeupunktErgebnis>  Koordinaten { get; set; } = new();
}

public static class NeupunkteManager
{
    private static NeupunkteDatei _daten = new();
    private static string         _pfad  = "";

    public static IReadOnlyList<NeupunktRohdaten>  Rohdaten    => _daten.Rohdaten;
    public static IReadOnlyList<NeupunktErgebnis>  Koordinaten => _daten.Koordinaten;

    // ── Initialisierung ───────────────────────────────────────────────────────

    public static void Initialize(string pfad)
    {
        _pfad = pfad;
        if (File.Exists(pfad)) Laden();
        else _daten = new NeupunkteDatei();
    }

    private static void Laden()
    {
        try
        {
            string json = File.ReadAllText(_pfad, Encoding.UTF8);
            var data = JsonSerializer.Deserialize<NeupunkteDatei>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            _daten = data ?? new NeupunkteDatei();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log("NeupunkteManager.Laden", ex);
            _daten = new NeupunkteDatei();
        }
    }

    // ── Punkt hinzufügen ─────────────────────────────────────────────────────

    /// <summary>
    /// Fügt einen gemessenen Neupunkt hinzu (Rohdaten + berechnete Koordinaten).
    /// Vorhandener Punkt mit gleicher PunktNr wird ersetzt.
    /// </summary>
    public static void HinzufuegenOderErsetzen(
        NeupunktRohdaten  rohdaten,
        NeupunktErgebnis  ergebnis)
    {
        int ri = _daten.Rohdaten.FindIndex(r => r.PunktNr == rohdaten.PunktNr);
        if (ri >= 0) _daten.Rohdaten[ri] = rohdaten;
        else          _daten.Rohdaten.Add(rohdaten);

        int ei = _daten.Koordinaten.FindIndex(e => e.PunktNr == ergebnis.PunktNr);
        if (ei >= 0) _daten.Koordinaten[ei] = ergebnis;
        else          _daten.Koordinaten.Add(ergebnis);

        Speichern();
    }

    // ── Neuberechnung nach Stationierungsänderung ─────────────────────────────

    /// <summary>
    /// Berechnet alle Neupunkte des angegebenen Standpunktes mit den neuen
    /// Stationierungsdaten neu.  Alle anderen Standpunkte bleiben unverändert.
    /// </summary>
    public static void BerechneNeu(
        StationierungsErgebnis station,
        string                 standpunktNr)
    {
        var neueKoords = NeupunktRechner.BerechneAlle(
            _daten.Rohdaten, station, standpunktNr);

        // Alte Koordinaten dieses Standpunktes entfernen und neue einfügen
        _daten.Koordinaten.RemoveAll(e => e.StandpunktNr == standpunktNr);
        _daten.Koordinaten.AddRange(neueKoords);

        Speichern();
    }

    // ── Löschen ───────────────────────────────────────────────────────────────

    /// <summary>Löscht alle Punkte die das Prädikat erfüllen.  Gibt Anzahl zurück.</summary>
    public static int RemoveWhere(Func<NeupunktRohdaten, bool> predicate)
    {
        var nrs = _daten.Rohdaten
            .Where(predicate)
            .Select(r => r.PunktNr)
            .ToHashSet(StringComparer.Ordinal);

        int removed = _daten.Rohdaten.RemoveAll(r => nrs.Contains(r.PunktNr));
        _daten.Koordinaten.RemoveAll(e => nrs.Contains(e.PunktNr));

        if (removed > 0) Speichern();
        return removed;
    }

    // ── Persistenz ────────────────────────────────────────────────────────────

    public static void Speichern()
    {
        if (string.IsNullOrEmpty(_pfad)) return;
        try
        {
            var opts = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_daten, opts);
            File.WriteAllText(_pfad, json, Encoding.UTF8);
        }
        catch (Exception ex) { ErrorLogger.Log("NeupunkteManager.Speichern", ex); }
    }
}
