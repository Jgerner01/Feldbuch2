namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// Flächenteilung (Abspaltung) – alle 6 Verfahren
// ──────────────────────────────────────────────────────────────────────────────

public enum Teilungsverfahren
{
    ParalleleZurGrundseite = 1,
    DurchFestenPunkt       = 2,
    GegebeneRichtung       = 3,
    Dreiecksabtrennung     = 4,
    Verhältnisteilung      = 5,
    Streifenabtrennung     = 6,
}

public class FlächenteilungErgebnis
{
    public double   A_gesamt    { get; set; }
    public double   A1_soll     { get; set; }
    public double   A1_ist      { get; set; }
    public double   A2          { get; set; }
    public double   Differenz   => A1_ist - A1_soll;
    public List<(double R, double H)> Polygon1           { get; set; } = new();
    public List<(double R, double H)> Polygon2           { get; set; } = new();
    public List<(string Nr, double R, double H)> NeueGrenzpunkte { get; set; } = new();
    public int      Iterationen { get; set; }
    public string   Verfahren   { get; set; } = "";
}

public static class FlächenteilungRechner
{
    const double GON2RAD = Math.PI / 200.0;

    // ── Gauss'sche Flächenformel (Shoelace) ──────────────────────────────────
    public static double BerechneFläche(IList<(double R, double H)> poly)
    {
        int n = poly.Count;
        if (n < 3) return 0;
        double sum = 0;
        for (int i = 0; i < n; i++)
        {
            var (r1, h1) = poly[i];
            var (r2, h2) = poly[(i + 1) % n];
            sum += r1 * h2 - r2 * h1;
        }
        return Math.Abs(sum) * 0.5;
    }

    // ── Schwerpunkt eines Polygons ────────────────────────────────────────────
    public static (double R, double H) Schwerpunkt(IList<(double R, double H)> poly)
    {
        if (poly.Count == 0) return (0, 0);
        return (poly.Average(p => p.R), poly.Average(p => p.H));
    }

    // ── Schnittpunkt Linie × Kante ───────────────────────────────────────────
    // Linie: P = lineP + t * lineU
    // Kante: A + s*(B-A), s ∈ [0,1)
    static (double s, (double R, double H) Pt)? LineSegCross(
        (double R, double H) lineP, (double R, double H) lineU,
        (double R, double H) A,     (double R, double H) B)
    {
        double dR = B.R - A.R, dH = B.H - A.H;
        double det = lineU.R * (-dH) - lineU.H * (-dR);
        if (Math.Abs(det) < 1e-12) return null;
        double dx = A.R - lineP.R, dy = A.H - lineP.H;
        double s = (lineU.R * dy - lineU.H * dx) / det;
        if (s < -1e-9 || s >= 1.0 - 1e-9) return null;
        s = Math.Max(0, Math.Min(1 - 1e-12, s));
        return (s, (A.R + s * dR, A.H + s * dH));
    }

    // ── Alle Schnittpunkte Linie × Polygon (sortiert nach t) ─────────────────
    static List<(int EdgeIdx, double s_edge, (double R, double H) Pt)>
        FindIntersections(IList<(double R, double H)> poly,
                          (double R, double H) lineP, (double R, double H) lineU)
    {
        int n = poly.Count;
        var hits = new List<(int EdgeIdx, double s_edge, (double R, double H) Pt)>();
        for (int i = 0; i < n; i++)
        {
            var res = LineSegCross(lineP, lineU, poly[i], poly[(i + 1) % n]);
            if (res.HasValue)
                hits.Add((i, res.Value.s, res.Value.Pt));
        }
        double tOf(double R, double H)
        {
            double len = Math.Sqrt(lineU.R * lineU.R + lineU.H * lineU.H);
            if (len < 1e-12) return 0;
            return ((R - lineP.R) * lineU.R + (H - lineP.H) * lineU.H) / len;
        }
        return hits.OrderBy(h => tOf(h.Pt.R, h.Pt.H)).ToList();
    }

    // ── Polygon aufteilen an zwei Grenzpunkten ────────────────────────────────
    // Poly1 = Q1 → (vorwärts durch Polygon) → Q2
    // Poly2 = Q2 → (vorwärts durch Polygon) → Q1
    static (List<(double R, double H)> P1, List<(double R, double H)> P2) SplitPolygon(
        IList<(double R, double H)> poly,
        int e1, (double R, double H) Q1,
        int e2, (double R, double H) Q2)
    {
        int n = poly.Count;
        var p1 = new List<(double R, double H)> { Q1 };
        int i = (e1 + 1) % n;
        for (int guard = 0; guard <= n; guard++)
        {
            p1.Add(poly[i]);
            if (i == e2) break;
            i = (i + 1) % n;
        }
        p1.Add(Q2);

        var p2 = new List<(double R, double H)> { Q2 };
        i = (e2 + 1) % n;
        for (int guard = 0; guard <= n; guard++)
        {
            p2.Add(poly[i]);
            if (i == e1) break;
            i = (i + 1) % n;
        }
        p2.Add(Q1);

        return (p1, p2);
    }

    // Gibt die Sub-Polygone zurück; Poly1 hat kleinere Fläche wenn A1_soll < A/2
    static (List<(double R, double H)> A1Poly, List<(double R, double H)> A2Poly)
        OrderedByArea(List<(double R, double H)> p1, List<(double R, double H)> p2, double A1_soll)
    {
        double a1 = BerechneFläche(p1), a2 = BerechneFläche(p2);
        // A1Poly = der Polygon, dessen Fläche näher an A1_soll liegt
        if (Math.Abs(a1 - A1_soll) <= Math.Abs(a2 - A1_soll))
            return (p1, p2);
        return (p2, p1);
    }

    // ── Inward-Normal einer Polygon-Kante ─────────────────────────────────────
    static (double nx, double ny) InwardNormal(
        IList<(double R, double H)> poly, int edgeIdx)
    {
        int n = poly.Count;
        var (Ra, Ha) = poly[edgeIdx];
        var (Rb, Hb) = poly[(edgeIdx + 1) % n];
        double ex = Rb - Ra, ey = Hb - Ha;
        double L  = Math.Sqrt(ex * ex + ey * ey);
        if (L < 1e-9) return (0, 1);
        ex /= L; ey /= L;
        double nx = -ey, ny = ex; // linke Senkrechte (inward für CCW-Polygon)
        double avgDot = poly.Average(p => (p.R - Ra) * nx + (p.H - Ha) * ny);
        return avgDot >= 0 ? (nx, ny) : (-nx, -ny);
    }

    // ── Verfahren 1: Parallele zur Grundseite ─────────────────────────────────
    public static FlächenteilungErgebnis Verfahren1_Parallele(
        IList<(double R, double H)> poly, int baselineEdge, double A1_soll)
    {
        double A_ges = BerechneFläche(poly);
        if (A_ges < 1e-6) throw new InvalidOperationException("Polygon hat keine Fläche.");
        A1_soll = Math.Max(1e-4, Math.Min(A_ges - 1e-4, A1_soll));

        int n = poly.Count;
        var (Ra, Ha) = poly[baselineEdge];
        var (nx, ny) = InwardNormal(poly, baselineEdge);
        var lineU    = (R: -ny, H: nx); // parallel to baseline

        double d_max = poly.Max(p => (p.R - Ra) * nx + (p.H - Ha) * ny) * 1.001;

        int iters = 0;
        (List<(double R, double H)> p1, List<(double R, double H)> p2,
         int e1, (double R, double H) Q1, int e2, (double R, double H) Q2) Compute(double d)
        {
            var lineP = (R: Ra + d * nx, H: Ha + d * ny);
            var hits  = FindIntersections(poly, lineP, lineU);
            if (hits.Count < 2) return (new(), new(), -1, (0,0), -1, (0,0));
            var h1 = hits[0]; var h2 = hits[hits.Count - 1];
            var (q1, q2) = SplitPolygon(poly, h1.EdgeIdx, h1.Pt, h2.EdgeIdx, h2.Pt);
            return (q1, q2, h1.EdgeIdx, h1.Pt, h2.EdgeIdx, h2.Pt);
        }

        double lo = 0, hi = d_max;
        for (iters = 0; iters < 60; iters++)
        {
            double mid = (lo + hi) * 0.5;
            var (p1, p2, _, _, _, _) = Compute(mid);
            if (p1.Count < 3) continue;
            var (a1p, _) = OrderedByArea(p1, p2, A1_soll);
            double area  = BerechneFläche(a1p);
            if (Math.Abs(area - A1_soll) < 0.0001) { lo = hi = mid; break; }
            if (area < A1_soll) lo = mid; else hi = mid;
        }

        var (poly1, poly2, e1f, Q1f, e2f, Q2f) = Compute((lo + hi) * 0.5);
        var (A1Poly, A2Poly) = OrderedByArea(poly1, poly2, A1_soll);

        return new FlächenteilungErgebnis
        {
            A_gesamt = A_ges, A1_soll = A1_soll,
            A1_ist   = BerechneFläche(A1Poly),
            A2       = BerechneFläche(A2Poly),
            Polygon1 = A1Poly, Polygon2 = A2Poly,
            NeueGrenzpunkte = new() { ("GP1", Q1f.R, Q1f.H), ("GP2", Q2f.R, Q2f.H) },
            Iterationen = iters, Verfahren = "Parallele zur Grundseite"
        };
    }

    // ── Verfahren 2: Durch festen Punkt ──────────────────────────────────────
    public static FlächenteilungErgebnis Verfahren2_FesterPunkt(
        IList<(double R, double H)> poly, (double R, double H) fixPt, double A1_soll)
    {
        double A_ges = BerechneFläche(poly);
        A1_soll = Math.Max(1e-4, Math.Min(A_ges - 1e-4, A1_soll));

        int n = poly.Count;
        // Kritische Winkel = Richtungen zu allen Eckpunkten
        var angles = poly.Select(p =>
            ((Math.Atan2(p.R - fixPt.R, p.H - fixPt.H) * 200.0 / Math.PI % 400.0 + 400.0) % 400.0)
        ).OrderBy(a => a).ToList();
        angles.Add(angles[0] + 400); // wrap-around

        int iters = 0;
        double bestAngle = angles[0];

        (double area, int e1, (double R, double H) Q1, int e2, (double R, double H) Q2)
            Compute(double t_gon)
        {
            double tR = Math.Sin(t_gon * GON2RAD), tH = Math.Cos(t_gon * GON2RAD);
            var hits = FindIntersections(poly, fixPt, (tR, tH));
            if (hits.Count < 2) return (0, -1, (0,0), -1, (0,0));
            var h1 = hits[0]; var h2 = hits[hits.Count - 1];
            var (p1, p2) = SplitPolygon(poly, h1.EdgeIdx, h1.Pt, h2.EdgeIdx, h2.Pt);
            var (a1p, _)  = OrderedByArea(p1, p2, A1_soll);
            return (BerechneFläche(a1p), h1.EdgeIdx, h1.Pt, h2.EdgeIdx, h2.Pt);
        }

        // Suche das Intervall, in dem A1(t) = A1_soll liegt
        double lo = angles[0], hi = angles[0] + 400;
        // Grobe Rastersuche
        for (int k = 0; k < angles.Count - 1; k++)
        {
            double t0 = angles[k] % 400.0, t1 = angles[k + 1] % 400.0;
            if (Math.Abs(t1 - t0) < 1e-6) continue;
            double a0 = Compute(t0).area, a1 = Compute(t1).area;
            if ((a0 <= A1_soll && A1_soll <= a1) || (a1 <= A1_soll && A1_soll <= a0))
            {
                lo = t0; hi = t1; break;
            }
        }

        for (iters = 0; iters < 60; iters++)
        {
            double mid  = (lo + hi) * 0.5;
            double area = Compute(mid).area;
            if (Math.Abs(area - A1_soll) < 0.0001) { bestAngle = mid; break; }
            double aLo  = Compute(lo).area;
            if ((aLo <= A1_soll && area >= A1_soll) || (aLo >= A1_soll && area <= A1_soll))
                hi = mid;
            else
                lo = mid;
            bestAngle = mid;
        }

        var (aFin, e1f, Q1f, e2f, Q2f) = Compute(bestAngle);
        var (poly1, poly2) = SplitPolygon(poly, e1f, Q1f, e2f, Q2f);
        var (A1Poly, A2Poly) = OrderedByArea(poly1, poly2, A1_soll);

        return new FlächenteilungErgebnis
        {
            A_gesamt = A_ges, A1_soll = A1_soll,
            A1_ist   = BerechneFläche(A1Poly),
            A2       = BerechneFläche(A2Poly),
            Polygon1 = A1Poly, Polygon2 = A2Poly,
            NeueGrenzpunkte = new() { ("GP1", Q1f.R, Q1f.H), ("GP2", Q2f.R, Q2f.H) },
            Iterationen = iters, Verfahren = "Durch festen Punkt"
        };
    }

    // ── Verfahren 3: Gegebene Richtung ────────────────────────────────────────
    public static FlächenteilungErgebnis Verfahren3_GegebeneRichtung(
        IList<(double R, double H)> poly, double richtung_gon, double A1_soll)
    {
        double A_ges = BerechneFläche(poly);
        A1_soll = Math.Max(1e-4, Math.Min(A_ges - 1e-4, A1_soll));

        double tR = Math.Sin(richtung_gon * GON2RAD);
        double tH = Math.Cos(richtung_gon * GON2RAD);
        // Quer-Richtung (Verschiebungsrichtung): 90° rechts
        double nR = tH, nH = -tR;

        // Projektion aller Eckpunkte auf die Querrichtung
        double lo = poly.Min(p => p.R * nR + p.H * nH);
        double hi = poly.Max(p => p.R * nR + p.H * nH);

        int iters = 0;
        (int e1, (double R, double H) Q1, int e2, (double R, double H) Q2) lastHits = (-1, (0,0), -1, (0,0));

        for (iters = 0; iters < 60; iters++)
        {
            double mid   = (lo + hi) * 0.5;
            var    lineP = (R: nR * mid, H: nH * mid); // point on line at offset mid
            var    hits  = FindIntersections(poly, lineP, (tR, tH));
            if (hits.Count < 2) { lo = (lo + mid) * 0.5; continue; }
            var h1 = hits[0]; var h2 = hits[hits.Count - 1];
            var (p1, p2) = SplitPolygon(poly, h1.EdgeIdx, h1.Pt, h2.EdgeIdx, h2.Pt);
            var (a1p, _) = OrderedByArea(p1, p2, A1_soll);
            double area  = BerechneFläche(a1p);
            lastHits     = (h1.EdgeIdx, h1.Pt, h2.EdgeIdx, h2.Pt);
            if (Math.Abs(area - A1_soll) < 0.0001) break;
            if (area < A1_soll) lo = mid; else hi = mid;
        }

        if (lastHits.e1 < 0) throw new InvalidOperationException("Keine gültige Trennlinie gefunden.");
        var (lp1, lp2) = SplitPolygon(poly, lastHits.e1, lastHits.Q1, lastHits.e2, lastHits.Q2);
        var (A1Poly, A2Poly) = OrderedByArea(lp1, lp2, A1_soll);

        return new FlächenteilungErgebnis
        {
            A_gesamt = A_ges, A1_soll = A1_soll,
            A1_ist   = BerechneFläche(A1Poly),
            A2       = BerechneFläche(A2Poly),
            Polygon1 = A1Poly, Polygon2 = A2Poly,
            NeueGrenzpunkte = new() { ("GP1", lastHits.Q1.R, lastHits.Q1.H), ("GP2", lastHits.Q2.R, lastHits.Q2.H) },
            Iterationen = iters, Verfahren = $"Gegebene Richtung {richtung_gon:F4} gon"
        };
    }

    // ── Verfahren 4: Dreiecksabtrennung vom Eckpunkt ──────────────────────────
    public static FlächenteilungErgebnis Verfahren4_Dreieck(
        IList<(double R, double H)> poly, int vertexIdx, double A1_soll)
    {
        double A_ges = BerechneFläche(poly);
        A1_soll = Math.Max(1e-4, Math.Min(A_ges - 1e-4, A1_soll));

        int n  = poly.Count;
        int k  = vertexIdx;
        var Pk = poly[k];

        double cumArea = 0;
        int targetEdge = -1;
        double t_Q = 0;

        for (int step = 1; step < n - 1; step++)
        {
            int j   = (k + step) % n;
            int j1  = (k + step + 1) % n;
            var Pj  = poly[j];
            var Pj1 = poly[j1];

            // Fläche Dreieck Pk, Pj, Pj1
            double det = (Pj.R - Pk.R) * (Pj1.H - Pk.H)
                       - (Pj.H - Pk.H) * (Pj1.R - Pk.R);
            double triArea = Math.Abs(det) * 0.5;

            if (cumArea + triArea >= A1_soll - 1e-9)
            {
                // Q liegt auf Kante Pj → Pj1
                double D_j = Math.Abs((Pj.R - Pk.R) * (Pj1.H - Pj.H)
                                    - (Pj.H - Pk.H) * (Pj1.R - Pj.R));
                if (D_j < 1e-12) D_j = 1e-12;
                t_Q        = (A1_soll - cumArea) * 2.0 / D_j;
                t_Q        = Math.Max(0, Math.Min(1, t_Q));
                targetEdge = j;
                break;
            }
            cumArea += triArea;
        }

        if (targetEdge < 0) throw new InvalidOperationException("Keine gültige Trennlinie gefunden.");

        var Pte  = poly[targetEdge];
        var Pte1 = poly[(targetEdge + 1) % n];
        var Q    = (R: Pte.R + t_Q * (Pte1.R - Pte.R),
                    H: Pte.H + t_Q * (Pte1.H - Pte.H));

        // Teilpolygon: P_k, P_{k+1}, ..., P_{targetEdge}, Q
        var poly1 = new List<(double R, double H)>();
        poly1.Add(Pk);
        int cur = (k + 1) % n;
        while (cur != targetEdge)
        {
            poly1.Add(poly[cur]);
            cur = (cur + 1) % n;
        }
        poly1.Add(Pte);
        poly1.Add(Q);

        var poly2 = new List<(double R, double H)> { Q };
        cur = (targetEdge + 1) % n;
        while (cur != k)
        {
            poly2.Add(poly[cur]);
            cur = (cur + 1) % n;
        }
        poly2.Add(Pk);

        return new FlächenteilungErgebnis
        {
            A_gesamt = A_ges, A1_soll = A1_soll,
            A1_ist   = BerechneFläche(poly1),
            A2       = BerechneFläche(poly2),
            Polygon1 = poly1, Polygon2 = poly2,
            NeueGrenzpunkte = new() { ("GP1", Q.R, Q.H) },
            Iterationen = 0, Verfahren = $"Dreiecksabtrennung von Eckpunkt {vertexIdx + 1}"
        };
    }

    // ── Verfahren 5: Verhältnisteilung ────────────────────────────────────────
    public static FlächenteilungErgebnis Verfahren5_Verhältnis(
        IList<(double R, double H)> poly,
        double a, double b,
        int basisVerfahren, // 1, 2, or 3
        int baselineEdge = 0, double richtung_gon = 0,
        (double R, double H)? fixPt = null)
    {
        double A_ges  = BerechneFläche(poly);
        double A1soll = A_ges * a / (a + b);

        var erg = basisVerfahren switch
        {
            2 when fixPt.HasValue => Verfahren2_FesterPunkt(poly, fixPt.Value, A1soll),
            3                    => Verfahren3_GegebeneRichtung(poly, richtung_gon, A1soll),
            _                    => Verfahren1_Parallele(poly, baselineEdge, A1soll),
        };
        erg.Verfahren = $"Verhältnis {a:G}:{b:G} – " + erg.Verfahren;
        return erg;
    }

    // ── Verfahren 6: Streifenabtrennung ───────────────────────────────────────
    public static FlächenteilungErgebnis Verfahren6_Streifen(
        IList<(double R, double H)> poly, int baselineEdge, double breite_m)
    {
        double A_ges = BerechneFläche(poly);
        int n = poly.Count;
        var (Ra, Ha)    = poly[baselineEdge];
        var (nx, ny)    = InwardNormal(poly, baselineEdge);
        var lineU       = (R: -ny, H: nx);
        double d        = breite_m;

        var lineP       = (R: Ra + d * nx, H: Ha + d * ny);
        var hits        = FindIntersections(poly, lineP, lineU);
        if (hits.Count < 2) throw new InvalidOperationException("Breite zu groß für dieses Polygon.");

        var h1 = hits[0]; var h2 = hits[hits.Count - 1];
        var (p1, p2)    = SplitPolygon(poly, h1.EdgeIdx, h1.Pt, h2.EdgeIdx, h2.Pt);
        // A1 = Seite, die die Grundkante enthält (kleineres Polygon bei kleiner Breite)
        double a1 = BerechneFläche(p1), a2 = BerechneFläche(p2);
        var (A1Poly, A2Poly) = a1 <= a2 ? (p1, p2) : (p2, p1);

        return new FlächenteilungErgebnis
        {
            A_gesamt = A_ges, A1_soll = BerechneFläche(A1Poly),
            A1_ist   = BerechneFläche(A1Poly),
            A2       = BerechneFläche(A2Poly),
            Polygon1 = A1Poly, Polygon2 = A2Poly,
            NeueGrenzpunkte = new() { ("GP1", h1.Pt.R, h1.Pt.H), ("GP2", h2.Pt.R, h2.Pt.H) },
            Iterationen = 0, Verfahren = $"Streifenabtrennung b={breite_m:F3} m"
        };
    }
}
