namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// Bogenschnitt (Distance Intersection)
//
// Unbekannter Neupunkt P aus ≥2 gemessenen Horizontalstrecken zu
// bekannten Anschlusspunkten.
//
// Unbekannte: R_P, H_P  (2 Unbekannte)
// Beobachtungen: s_i [m]  (Horizontalstrecke von Station i zu P)
// Redundanz: r = n_aktiv − 2
//
// Ausgleichungsansatz:
//   s_i^0 = sqrt((R_P − R_i)² + (H_P − H_i)²)
//   l_i   = s_i_obs − s_i^0              [m, beob − berechnet]
//   A[i,0] = sin(t_i) = (R_P − R_i) / s_i^0
//   A[i,1] = cos(t_i) = (H_P − H_i) / s_i^0
//   Gewicht: p_i = m0² / (msk + s_i · msv)²
//
// Näherungskoordinaten: Schnitt der ersten beiden Kreise (Radikalachse).
// Bei 2-Punkt-Sonderfall: 2 Lösungen möglich – es wird die zum Schwerpunkt
// nächste (oder erste geometrisch berechnete) verwendet; ≥3 Punkte erzwingen
// eine eindeutige Lösung.
// ──────────────────────────────────────────────────────────────────────────────

public class BogenschnittMessung
{
    public string PunktNr { get; set; } = "";
    public double R       { get; set; }
    public double H       { get; set; }
    public double Strecke { get; set; }  // Horizontalstrecke Station→P [m]
}

public class BogenschnittResidum
{
    public string PunktNr   { get; set; } = "";
    public double vStrecke_mm { get; set; }  // Streckenresiduum [mm]
    public bool   Aktiv     { get; set; } = true;
}

public class BogenschnittErgebnis
{
    public double R           { get; set; }
    public double H           { get; set; }
    public double s0_mm       { get; set; }
    public int    Redundanz   { get; set; }
    public int    Iterationen { get; set; }
    public bool   Konvergiert { get; set; }
    public bool   ZweiLoesungen { get; set; }  // true wenn r=0 und zwei geom. Lösungen existieren
    public double R2          { get; set; }    // zweite Lösung (nur wenn ZweiLoesungen=true)
    public double H2          { get; set; }
    public List<BogenschnittResidum> Residuen { get; set; } = new();
}

public static class BogenschnittRechner
{
    // Standardgenauigkeiten Streckenmessung
    const double DEF_MSK_MM  = 3.0;   // Konstanter Anteil [mm]
    const double DEF_MSV_PPM = 2.0;   // Maßstabsanteil [ppm]
    const double DEF_M0_MM   = 1.0;   // Gewichtseinheit [mm]

    public static BogenschnittErgebnis Berechnen(
        List<BogenschnittMessung> messungen,
        bool[]? aktiv    = null,
        double  msk_mm   = DEF_MSK_MM,
        double  msv_ppm  = DEF_MSV_PPM,
        double  m0_mm    = DEF_M0_MM)
    {
        int n = messungen.Count;
        if (n < 2)
            throw new InvalidOperationException("Mindestens 2 Anschlusspunkte erforderlich.");

        bool[] akt  = aktiv ?? Enumerable.Repeat(true, n).ToArray();
        int    nAkt = akt.Count(b => b);
        if (nAkt < 2)
            throw new InvalidOperationException(
                $"Mindestens 2 aktive Anschlusspunkte erforderlich (aktiv: {nAkt}).");

        double m0_m = m0_mm / 1000.0;

        // ── Näherungskoordinaten: Kreisschnitt der ersten 2 aktiven Punkte ──
        int i0 = -1, i1 = -1;
        for (int i = 0; i < n; i++) { if (akt[i]) { if (i0 < 0) i0 = i; else if (i1 < 0) { i1 = i; break; } } }

        bool zweiLoesungen = false;
        double R2_alt = 0, H2_alt = 0;

        var (R_P, H_P, ok2, Rx, Hx) = KreisSchnitt(
            messungen[i0].R, messungen[i0].H, messungen[i0].Strecke,
            messungen[i1].R, messungen[i1].H, messungen[i1].Strecke);

        if (!ok2)
        {
            // Kreise schneiden sich nicht – Schwerpunkt als Notlösung
            R_P = messungen.Where((_, i) => akt[i]).Average(m => m.R);
            H_P = messungen.Where((_, i) => akt[i]).Average(m => m.H);
        }
        else if (nAkt == 2)
        {
            // Zwei Lösungen: merken für Ausgabe
            zweiLoesungen = true;
            R2_alt = Rx; H2_alt = Hx;
            // Wähle die zur Schwerpunktlage aller Messstationen konsistentere Lösung
            // (hier einfach P1 = erste Lösung; bei r≥1 konvergiert LS zur richtigen)
        }
        else
        {
            // Dritten aktiven Punkt für Lösungsauswahl verwenden
            int i2 = -1;
            for (int i = 0; i < n; i++) if (akt[i] && i != i0 && i != i1) { i2 = i; break; }
            if (i2 >= 0)
            {
                double d1 = Math.Abs(Dist(messungen[i2].R, messungen[i2].H, R_P,  H_P)  - messungen[i2].Strecke);
                double d2 = Math.Abs(Dist(messungen[i2].R, messungen[i2].H, Rx, Hx) - messungen[i2].Strecke);
                if (d2 < d1) { R_P = Rx; H_P = Hx; }
            }
        }

        // ── Iterative Ausgleichung ────────────────────────────────────────────
        int  iter        = 0;
        bool konvergiert = false;

        for (iter = 0; iter < 50; iter++)
        {
            double[,] N = new double[2, 2];
            double[]  b = new double[2];

            for (int i = 0; i < n; i++)
            {
                if (!akt[i]) continue;
                double dR_i = R_P - messungen[i].R;
                double dH_i = H_P - messungen[i].H;
                double s0_i = Math.Sqrt(dR_i * dR_i + dH_i * dH_i);
                if (s0_i < 1e-6) continue;

                double l_i   = messungen[i].Strecke - s0_i;   // obs − berechnet [m]
                double sin_t = dR_i / s0_i;
                double cos_t = dH_i / s0_i;

                double sig_i = (msk_mm + s0_i * msv_ppm / 1000.0) / 1000.0;  // [m]
                double p_i   = (m0_m * m0_m) / (sig_i * sig_i);

                double[] A = { sin_t, cos_t };
                for (int j = 0; j < 2; j++)
                {
                    b[j] += A[j] * p_i * l_i;
                    for (int k = 0; k < 2; k++)
                        N[j, k] += A[j] * p_i * A[k];
                }
            }

            double[]? dx = GaussElim(N, b);
            if (dx == null) break;

            R_P += dx[0];
            H_P += dx[1];

            if (Math.Abs(dx[0]) < 1e-7 && Math.Abs(dx[1]) < 1e-7)
            { konvergiert = true; break; }
        }

        // ── s0 und Residuen ──────────────────────────────────────────────────
        int    r    = nAkt - 2;
        double vTPv = 0;
        var    res  = new List<BogenschnittResidum>();

        for (int i = 0; i < n; i++)
        {
            double dR_i    = R_P - messungen[i].R;
            double dH_i    = H_P - messungen[i].H;
            double s0_i    = Math.Sqrt(dR_i * dR_i + dH_i * dH_i);
            double v_m     = messungen[i].Strecke - s0_i;
            double v_mm    = v_m * 1000.0;

            if (akt[i] && s0_i > 1e-6)
            {
                double sig_i = (msk_mm + s0_i * msv_ppm / 1000.0) / 1000.0;
                double p_i   = (m0_m * m0_m) / (sig_i * sig_i);
                vTPv += p_i * v_m * v_m;
            }

            res.Add(new BogenschnittResidum
            {
                PunktNr     = messungen[i].PunktNr,
                vStrecke_mm = v_mm,
                Aktiv       = akt[i],
            });
        }

        double s0_mm_val = r > 0 ? Math.Sqrt(vTPv / r) * 1000.0 : 0;

        return new BogenschnittErgebnis
        {
            R             = R_P,
            H             = H_P,
            s0_mm         = s0_mm_val,
            Redundanz     = r,
            Iterationen   = iter,
            Konvergiert   = konvergiert,
            ZweiLoesungen = zweiLoesungen,
            R2            = R2_alt,
            H2            = H2_alt,
            Residuen      = res,
        };
    }

    // Schnitt zweier Kreise; liefert (P1, hatSchnitt, P2)
    // P1 und P2 sind die beiden Schnittpunkte (identisch wenn tangential)
    static (double R1, double H1, bool ok, double R2, double H2) KreisSchnitt(
        double Ra, double Ha, double sA,
        double Rb, double Hb, double sB)
    {
        double dR = Rb - Ra;
        double dH = Hb - Ha;
        double d  = Math.Sqrt(dR * dR + dH * dH);

        if (d < 1e-9 || sA + sB < d - 1e-6 || Math.Abs(sA - sB) > d + 1e-6)
            return (0, 0, false, 0, 0);

        double a   = (sA * sA - sB * sB + d * d) / (2.0 * d);
        double h2  = sA * sA - a * a;
        double h   = h2 > 0 ? Math.Sqrt(h2) : 0;

        // Einheitsvektor A→B und Senkrechte
        double uR = dR / d;
        double uH = dH / d;

        double pmR = Ra + a * uR;
        double pmH = Ha + a * uH;

        double R1 = pmR - h * uH;
        double H1 = pmH + h * uR;
        double R2 = pmR + h * uH;
        double H2 = pmH - h * uR;

        return (R1, H1, true, R2, H2);
    }

    static double Dist(double R1, double H1, double R2, double H2)
        => Math.Sqrt((R1 - R2) * (R1 - R2) + (H1 - H2) * (H1 - H2));

    static double[]? GaussElim(double[,] A, double[] b)
    {
        int n = b.Length;
        double[,] M = new double[n, n + 1];
        for (int i = 0; i < n; i++) { for (int j = 0; j < n; j++) M[i, j] = A[i, j]; M[i, n] = b[i]; }
        for (int col = 0; col < n; col++)
        {
            int pr = col;
            for (int r = col + 1; r < n; r++) if (Math.Abs(M[r, col]) > Math.Abs(M[pr, col])) pr = r;
            for (int k = 0; k <= n; k++) (M[col, k], M[pr, k]) = (M[pr, k], M[col, k]);
            if (Math.Abs(M[col, col]) < 1e-14) return null;
            for (int r = 0; r < n; r++)
            {
                if (r == col) continue;
                double f = M[r, col] / M[col, col];
                for (int k = col; k <= n; k++) M[r, k] -= f * M[col, k];
            }
        }
        double[] x = new double[n];
        for (int i = 0; i < n; i++) x[i] = M[i, n] / M[i, i];
        return x;
    }
}
