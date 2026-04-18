namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// Vorwärtschnitt (Forward Intersection)
//
// Unbekannter Neupunkt P aus ≥2 bekannten Stationen mit orientierten
// Richtungswinkeln (Richtungswinkel = Hz + Orientierung).
//
// Unbekannte: R_P, H_P  (2 Unbekannte)
// Beobachtungen: t_i [gon]  (orientierter Richtungswinkel von Station i zu P)
//   t_i = hz_i + z_i  (gemessene Richtung + Orientierungsunbekannte der Station)
// Redundanz: r = n_aktiv − 2
//
// Ausgleichungsansatz:
//   t_i = atan2(R_P − R_i, H_P − H_i)  [Richtungswinkel Station→P]
//   l_i = NormWinkel(t_i_obs − t_i^0) · s_i   [m, beob − berechnet]
//   A[i,0] = cos(t_i^0) = (H_P^0 − H_i) / s_i^0
//   A[i,1] = −sin(t_i^0) = −(R_P^0 − R_i) / s_i^0
//   Gewicht: p_i = m0² / (mR[rad] · s_i)²
// ──────────────────────────────────────────────────────────────────────────────

public class VorwaertsschnittMessung
{
    public string PunktNr { get; set; } = "";
    public double R       { get; set; }  // Rechtswert der Station [m]
    public double H       { get; set; }  // Hochwert der Station [m]
    public double Hz      { get; set; }  // Horizontalrichtung von Station zu P [gon]
    public double z       { get; set; }  // Orientierungsunbekannte der Station [gon]
    // Orientierter Richtungswinkel: t = Hz + z
}

public class VorwaertsschnittResidum
{
    public string PunktNr    { get; set; } = "";
    public double StreckeH   { get; set; }  // Horizontalstrecke Station→P [m]
    public double vWinkel_cc { get; set; }  // Richtungsresiduum [cc]
    public bool   Aktiv      { get; set; } = true;
}

public class VorwaertsschnittErgebnis
{
    public double R          { get; set; }
    public double H          { get; set; }
    public double s0_mm      { get; set; }
    public int    Redundanz  { get; set; }
    public int    Iterationen { get; set; }
    public bool   Konvergiert { get; set; }
    public List<VorwaertsschnittResidum> Residuen { get; set; } = new();
}

public static class VorwaertsschnittRechner
{
    const double GON2RAD = Math.PI / 200.0;
    const double RAD2GON = 200.0  / Math.PI;
    const double CC2RAD  = Math.PI / 2_000_000.0;
    const double RAD2CC  = 2_000_000.0 / Math.PI;

    public static VorwaertsschnittErgebnis Berechnen(
        List<VorwaertsschnittMessung> messungen,
        bool[]? aktiv = null,
        double  mR_cc = 9.0,
        double  m0_mm = 1.0)
    {
        int n = messungen.Count;
        if (n < 2)
            throw new InvalidOperationException("Mindestens 2 Stationen erforderlich.");

        bool[] akt  = aktiv ?? Enumerable.Repeat(true, n).ToArray();
        int    nAkt = akt.Count(b => b);
        if (nAkt < 2)
            throw new InvalidOperationException(
                $"Mindestens 2 aktive Stationen erforderlich (aktiv: {nAkt}).");

        // Orientierte Richtungswinkel in Radiant
        double[] t_obs = messungen.Select(m => NormWinkel2Pi((m.Hz + m.z) * GON2RAD)).ToArray();

        // ── Näherungskoordinaten: Vorwärtschnitt aus ersten 2 aktiven Stationen ─
        int i0 = -1, i1 = -1;
        for (int i = 0; i < n; i++) { if (akt[i]) { if (i0 < 0) i0 = i; else if (i1 < 0) { i1 = i; break; } } }

        double R_P, H_P;
        if (i0 < 0 || i1 < 0)
            throw new InvalidOperationException("Zu wenige aktive Stationen.");

        if (!Schnitt2Strahlen(
                messungen[i0].R, messungen[i0].H, t_obs[i0],
                messungen[i1].R, messungen[i1].H, t_obs[i1],
                out R_P, out H_P))
        {
            // Parallele Strahlen → Schwerpunkt als Fallback
            R_P = messungen.Where((_, i) => akt[i]).Average(m => m.R);
            H_P = messungen.Where((_, i) => akt[i]).Average(m => m.H);
        }

        double m0_m   = m0_mm / 1000.0;
        double mR_rad = mR_cc * CC2RAD;

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
                double s_i  = Math.Sqrt(dR_i * dR_i + dH_i * dH_i);
                if (s_i < 1e-6) continue;

                double t_i0   = Math.Atan2(dR_i, dH_i);
                double cos_t  = dH_i / s_i;
                double sin_t  = dR_i / s_i;
                // l = (beob − berechnet) · s  → positive dR increases t for NE-direction ✓
                double l_i    = NormWinkel(t_obs[i] - t_i0) * s_i;

                double sig_i  = mR_rad * s_i;
                double p_i    = (m0_m * m0_m) / (sig_i * sig_i);

                double[] A = { cos_t, -sin_t };
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
            {
                konvergiert = true;
                break;
            }
        }

        // ── s0 und Residuen ──────────────────────────────────────────────────
        int    r    = nAkt - 2;
        double vTPv = 0;
        var    res  = new List<VorwaertsschnittResidum>();

        for (int i = 0; i < n; i++)
        {
            double dR_i = R_P - messungen[i].R;
            double dH_i = H_P - messungen[i].H;
            double s_i  = Math.Sqrt(dR_i * dR_i + dH_i * dH_i);
            double t_i0 = Math.Atan2(dR_i, dH_i);

            double v_rad = NormWinkel(t_obs[i] - t_i0);
            double v_cc  = v_rad * RAD2CC;

            if (akt[i] && s_i > 1e-6)
            {
                double sig_i = mR_rad * s_i;
                double p_i   = (m0_m * m0_m) / (sig_i * sig_i);
                vTPv += p_i * (v_rad * s_i) * (v_rad * s_i);
            }

            res.Add(new VorwaertsschnittResidum
            {
                PunktNr    = messungen[i].PunktNr,
                StreckeH   = s_i,
                vWinkel_cc = v_cc,
                Aktiv      = akt[i],
            });
        }

        double s0_mm_val = r > 0 ? Math.Sqrt(vTPv / r) * 1000.0 : 0;

        return new VorwaertsschnittErgebnis
        {
            R          = R_P,
            H          = H_P,
            s0_mm      = s0_mm_val,
            Redundanz  = r,
            Iterationen = iter,
            Konvergiert = konvergiert,
            Residuen   = res,
        };
    }

    // Schnitt zweier orientierter Strahlen
    static bool Schnitt2Strahlen(
        double R_A, double H_A, double t_A_rad,
        double R_B, double H_B, double t_B_rad,
        out double R_P, out double H_P)
    {
        // Löse: λ·sin(tA) − μ·sin(tB) = R_B − R_A
        //       λ·cos(tA) − μ·cos(tB) = H_B − H_A
        double det = Math.Sin(t_A_rad) * (-Math.Cos(t_B_rad))
                   - (-Math.Sin(t_B_rad)) * Math.Cos(t_A_rad);
        // det = sin(tB - tA)
        if (Math.Abs(det) < 1e-9) { R_P = H_P = 0; return false; }

        double dR = R_B - R_A;
        double dH = H_B - H_A;
        double lam = (dR * (-Math.Cos(t_B_rad)) - dH * (-Math.Sin(t_B_rad))) / det;

        R_P = R_A + lam * Math.Sin(t_A_rad);
        H_P = H_A + lam * Math.Cos(t_A_rad);
        return true;
    }

    static double NormWinkel(double a)
    {
        while (a >  Math.PI) a -= 2 * Math.PI;
        while (a < -Math.PI) a += 2 * Math.PI;
        return a;
    }

    static double NormWinkel2Pi(double a)
    {
        a %= (2 * Math.PI);
        if (a < 0) a += 2 * Math.PI;
        return a;
    }

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
