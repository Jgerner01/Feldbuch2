using System.Text.Json;

namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// DxfPunktIndexManager – Persistenter Punkt-Index für DXF-Dateien.
//
// Speichert die Zuordnung (PunktNr ↔ Koordinate) als JSON im Projektverzeichnis.
// Beim erneuten Laden einer DXF-Datei (auch überarbeitet) bleiben bestehende
// Punktnummern erhalten. Neue Punkte erhalten fortlaufende Nummern.
//
// Datei: {Projektname}-PunktIndex.json
// ──────────────────────────────────────────────────────────────────────────────

public record PunktEintrag(string PunktNr, double R, double H);

public static class DxfPunktIndexManager
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Lädt den gespeicherten Punkt-Index aus dem Projektverzeichnis.
    /// Gibt null zurück wenn keine Datei existiert.
    /// </summary>
    public static List<PunktEintrag>? Laden(string projektPfad)
    {
        if (!File.Exists(projektPfad)) return null;
        try
        {
            var json = File.ReadAllText(projektPfad);
            return JsonSerializer.Deserialize<List<PunktEintrag>>(json, JsonOpts);
        }
        catch
        {
            return null;  // Bei Fehler: Neu aufbauen
        }
    }

    /// <summary>
    /// Speichert den Punkt-Index als JSON.
    /// </summary>
    public static void Speichern(List<PunktEintrag> eintraege, string projektPfad)
    {
        var json = JsonSerializer.Serialize(eintraege, JsonOpts);
        File.WriteAllText(projektPfad, json, System.Text.Encoding.UTF8);
    }

    /// <summary>
    /// Löscht die gespeicherte Index-Datei.
    /// </summary>
    public static void Loeschen(string projektPfad)
    {
        if (File.Exists(projektPfad)) File.Delete(projektPfad);
    }

    /// <summary>
    /// Baut einen neuen Punkt-Index auf und übernimmt bestehende Nummern
    /// aus dem gespeicherten Index (Merge-Logik).
    ///
    /// Ablauf:
    /// 1. Neue Koordinaten aus DXF-Entities extrahieren und deduplizieren
    /// 2. Für jede Koordinate: Match im bestehenden Index suchen (Toleranz)
    ///    - Gefunden → bestehende Nummer übernehmen
    ///    - Nicht gefunden → neue Nummer (maxBestehend + laufendeNr)
    /// 3. Ergebnis speichern
    /// </summary>
    public static DxfPunktIndex AufbauenMitMerge(
        List<DxfEntity> entities,
        List<PunktEintrag>? bestehend,
        double toleranz = 0.003)
    {
        // Schritt 1: Alle eindeutigen Koordinaten sammeln (wie bisher)
        var neuePunkte = new HashSet<(long rKey, long hKey, double r, double h)>();
        foreach (var e in entities)
        {
            switch (e)
            {
                case DxfLine line:
                    neuePunkte.Add(Runde(line.X1, line.Y1, toleranz));
                    neuePunkte.Add(Runde(line.X2, line.Y2, toleranz));
                    break;
                case DxfInsert ins:
                    neuePunkte.Add(Runde(ins.X, ins.Y, toleranz));
                    break;
                case DxfPoint pt:
                    neuePunkte.Add(Runde(pt.X, pt.Y, toleranz));
                    break;
                case DxfCircle circ:
                    neuePunkte.Add(Runde(circ.CX, circ.CY, toleranz));
                    break;
                case DxfLwPolyline poly:
                    foreach (var v in poly.Vertices)
                        neuePunkte.Add(Runde(v.x, v.y, toleranz));
                    break;
                case DxfArc arc:
                    double sr = arc.StartAngle * Math.PI / 180.0;
                    double er = arc.EndAngle * Math.PI / 180.0;
                    neuePunkte.Add(Runde(
                        arc.CX + arc.Radius * Math.Cos(sr),
                        arc.CY + arc.Radius * Math.Sin(sr), toleranz));
                    neuePunkte.Add(Runde(
                        arc.CX + arc.Radius * Math.Cos(er),
                        arc.CY + arc.Radius * Math.Sin(er), toleranz));
                    break;
            }
        }

        // Nach R sortieren für stabile Nummerierung
        var sortiert = neuePunkte.OrderBy(p => p.rKey).ThenBy(p => p.hKey).ToList();

        // Schritt 2: Bestehende Nummern mergen
        var index = new DxfPunktIndex(toleranz);
        var eintraege = new List<PunktEintrag>();

        // Max bestehende Nummer finden
        int maxNr = 0;
        if (bestehend != null)
        {
            foreach (var b in bestehend)
            {
                if (int.TryParse(b.PunktNr, out int n) && n > maxNr) maxNr = n;
            }
        }

        int neueNr = maxNr;
        foreach (var (rKey, hKey, r, h) in sortiert)
        {
            // Im bestehenden Index suchen (innerhalb Toleranz)
            string? punktnr = null;
            if (bestehend != null)
            {
                foreach (var b in bestehend)
                {
                    long bR = (long)Math.Round(b.R / toleranz);
                    long bH = (long)Math.Round(b.H / toleranz);
                    if (bR == rKey && bH == hKey)
                    {
                        punktnr = b.PunktNr;
                        break;
                    }
                }
            }

            // Nicht gefunden → neue Nummer
            if (punktnr == null)
                punktnr = (++neueNr).ToString();

            index._index[(rKey, hKey)] = punktnr;
            eintraege.Add(new PunktEintrag(punktnr, r, h));
        }

        // Alte Einträge die nicht mehr in der DXF sind: beibehalten (Historie)
        if (bestehend != null)
        {
            foreach (var b in bestehend)
            {
                // Prüfen ob bereits übernommen
                bool vorhanden = eintraege.Any(e => e.PunktNr == b.PunktNr);
                if (!vorhanden)
                    eintraege.Add(b);  // Historie beibehalten
            }
        }

        index._eintraege = eintraege;
        return index;
    }

    // Rundet Koordinaten und gibt Schlüssel + Originalwerte zurück
    private static (long rKey, long hKey, double r, double h) Runde(
        double r, double h, double toleranz)
    {
        long rKey = (long)Math.Round(r / toleranz);
        long hKey = (long)Math.Round(h / toleranz);
        return (rKey, hKey, r, h);
    }
}
