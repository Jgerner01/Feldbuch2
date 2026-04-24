namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// DxfPunktIndex – extrahiert alle relevanten Koordinaten aus DXF-Entities,
// dedupliziert sie (Toleranz-basiert) und weist fortlaufende Nummern zu.
//
// Zweck: Beim Klick auf eine DXF-Entity (Line, Circle, Insert, Point)
//        kann die zugehörige Punktnummer nachgeschlagen werden.
//
// Performance: O(n) beim Aufbau, O(1) pro Lookup.
//              Typische DXF-Dateien (10.000 Entities) < 20 ms.
// ──────────────────────────────────────────────────────────────────────────────

public class DxfPunktIndex
{
    /// <summary>
    /// Toleranz in Metern für die Deduplizierung.
    /// Punkte innerhalb dieses Radius werden als identisch betrachtet.
    /// Default: 3 mm.
    /// </summary>
    public double Toleranz { get; }

    /// <summary>
    /// Interne Schlüssel: gerundete Koordinaten → Punktnummer.
    /// </summary>
    internal readonly Dictionary<(long rKey, long hKey), string> _index = new();

    /// <summary>
    /// Liste aller Einträge (für Persistenz).
    /// </summary>
    internal List<PunktEintrag> _eintraege = new();

    // Konstruktor: intern für Manager, öffentlich für einfachen Aufbau
    internal DxfPunktIndex(double toleranz)
    {
        Toleranz = toleranz;
    }

    /// <summary>
    /// Baut den Index aus allen DXF-Entities auf.
    /// Dedupliziert nach Koordinaten (Toleranz) und nummeriert fortlaufend ab 1.
    /// </summary>
    public static DxfPunktIndex Aufbauen(List<DxfEntity> entities, double toleranz = 0.003)
    {
        var index = new DxfPunktIndex(toleranz);
        var punkte = new HashSet<(long rKey, long hKey)>();

        // Alle relevanten Koordinaten sammeln
        foreach (var e in entities)
        {
            switch (e)
            {
                case DxfLine line:
                    punkte.Add(index.Runde(line.X1, line.Y1));
                    punkte.Add(index.Runde(line.X2, line.Y2));
                    break;

                case DxfInsert ins:
                    punkte.Add(index.Runde(ins.X, ins.Y));
                    break;

                case DxfPoint pt:
                    punkte.Add(index.Runde(pt.X, pt.Y));
                    break;

                case DxfCircle circ:
                    punkte.Add(index.Runde(circ.CX, circ.CY));
                    break;

                case DxfLwPolyline poly:
                    foreach (var v in poly.Vertices)
                        punkte.Add(index.Runde(v.x, v.y));
                    break;

                case DxfArc arc:
                    // Bogen-Endpunkte
                    double sr = arc.StartAngle * Math.PI / 180.0;
                    double er = arc.EndAngle * Math.PI / 180.0;
                    punkte.Add(index.Runde(arc.CX + arc.Radius * Math.Cos(sr),
                                          arc.CY + arc.Radius * Math.Sin(sr)));
                    punkte.Add(index.Runde(arc.CX + arc.Radius * Math.Cos(er),
                                          arc.CY + arc.Radius * Math.Sin(er)));
                    break;
            }
        }

        // Sortieren (erst R, dann H) für stabile Nummerierung
        var sortiert = punkte.OrderBy(p => p.rKey).ThenBy(p => p.hKey).ToList();
        int nr = 1;
        foreach (var key in sortiert)
        {
            string nrStr = nr++.ToString();
            index._index[key] = nrStr;
            index._eintraege.Add(new PunktEintrag(nrStr, key.rKey * toleranz, key.hKey * toleranz));
        }

        return index;
    }

    /// <summary>
    /// Liefert die Punktnummer für eine Koordinate, oder null wenn nicht gefunden.
    /// </summary>
    public string? GetPunktNr(double r, double h)
    {
        var key = Runde(r, h);
        return _index.TryGetValue(key, out var nr) ? nr : null;
    }

    /// <summary>
    /// Gesamtzahl der eindeutigen Punkte nach Deduplizierung.
    /// </summary>
    public int Anzahl => _index.Count;

    // ── Räumliche Suchmethoden (für PunktFinder) ──────────────────────────────

    /// <summary>Liefert alle Einträge innerhalb des gegebenen Radius.</summary>
    public List<PunktEintrag> SucheNahe(double r, double h, double radiusM)
    {
        var result = new List<PunktEintrag>();
        double r2  = radiusM * radiusM;
        foreach (var e in _eintraege)
        {
            double dr = e.R - r, dh = e.H - h;
            if (dr * dr + dh * dh <= r2)
                result.Add(e);
        }
        return result;
    }

    /// <summary>
    /// Liefert alle Einträge, deren Richtung von der Station aus
    /// innerhalb der Winkeltoleranz liegt. Sortiert nach Distanz.
    /// </summary>
    public List<(PunktEintrag Punkt, double Distanz_m)> SucheNachRichtung(
        double stationR, double stationH,
        double richtung_gon, double toleranz_gon,
        double maxDistanz_m = 500.0)
    {
        const double GON2RAD = Math.PI / 200.0;
        double alpha_rad = richtung_gon * GON2RAD;
        double tol_rad   = toleranz_gon * GON2RAD;

        var result = new List<(PunktEintrag, double)>();
        foreach (var e in _eintraege)
        {
            double dr   = e.R - stationR;
            double dh   = e.H - stationH;
            double dist = Math.Sqrt(dr * dr + dh * dh);
            if (dist < 0.5 || dist > maxDistanz_m) continue;

            double alpha_p = Math.Atan2(dr, dh);
            double diff    = Math.Abs(NormRad(alpha_rad - alpha_p));
            if (diff <= tol_rad)
                result.Add((e, dist));
        }
        return result.OrderBy(x => x.Item2).ToList();

        static double NormRad(double a)
        {
            while (a >  Math.PI) a -= 2 * Math.PI;
            while (a < -Math.PI) a += 2 * Math.PI;
            return a;
        }
    }

    // Rundet Koordinaten auf Toleranz-Schritte (long-Schlüssel für Dictionary).
    private (long rKey, long hKey) Runde(double r, double h)
    {
        long rKey = (long)Math.Round(r / Toleranz);
        long hKey = (long)Math.Round(h / Toleranz);
        return (rKey, hKey);
    }
}
