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

    // Rundet Koordinaten auf Toleranz-Schritte (long-Schlüssel für Dictionary).
    private (long rKey, long hKey) Runde(double r, double h)
    {
        long rKey = (long)Math.Round(r / Toleranz);
        long hKey = (long)Math.Round(h / Toleranz);
        return (rKey, hKey);
    }
}
