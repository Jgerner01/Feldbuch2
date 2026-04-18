namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// Rückwärtschnitt (Resection)
//
// Unbekannter Standpunkt P aus ≥3 gemessenen Horizontalrichtungen zu
// bekannten Anschlusspunkten.
//
// Unbekannte: R_P, H_P, Orientierungsunbekannte z  (3 Unbekannte)
// Beobachtungen: Hz_i [gon]  (horizontale Kreisablesungen)
// Redundanz: r = n_aktiv − 3
//
// Ausgleichungsansatz (analog Freie Stationierung, nur Richtungen):
//   l_i = NormWinkel(t_i^0 − hz_i − z^0) · s_i          [m]
//   A[i,0] = cos(t_i) = (H_AP_i − H_P) / s_i
//   A[i,1] = −sin(t_i) = −(R_AP_i − R_P) / s_i
//   A[i,2] = s_i
//   Gewicht: p_i = m0² / (mR[rad] · s_i)²
// ──────────────────────────────────────────────────────────────────────────────

public class RueckwaertsschnittPunkt
{
    public string PunktNr { get; set; } = "";
    public double R       { get; set; }
    public double H       { get; set; }
    public double HZ      { get; set; }  // Horizontalrichtung [gon]
}

public class RueckwaertsschnittResidum
{
    public string PunktNr    { get; set; } = "";
    public double StreckeH   { get; set; }  // Horizontalstrecke zum AP [m]
    public double vWinkel_cc { get; set; }  // Richtungsresiduum [cc]
    public bool   Aktiv      { get; set; } = true;
}

public class RueckwaertsschnittErgebnis
{
    public double R                { get; set; }
    public double H                { get; set; }
    public double Orientierung_gon { get; set; }
    public double s0_mm            { get; set; }
    public int    Redundanz        { get; set; }
    public int    Iterationen      { get; set; }
    public bool   Konvergiert      { get; set; }
    /// <summary>Leer = kein Problem; sonst Beschreibung der Geometriewarnung.</summary>
    public string KritischerKreis  { get; set; } = "";
    public List<RueckwaertsschnittResidum> Residuen { get; set; } = new();
}

public static class RueckwaertsschnittRechner
{
    const double GON2RAD = Math.PI / 200.0;
    const double RAD2GON = 200.0  / Math.PI;
    const double CC2RAD  = Math.PI / 2_000_000.0;
    const double RAD2CC  = 2_000_000.0 / Math.PI;

    public static RueckwaertsschnittErgebnis Berechnen(
        List<RueckwaertsschnittPunkt> punkte,
        bool[]? aktiv  = null,
        double  mR_cc  = 9.0,
        double  m0_mm  = 1.0)
    {
        int n = punkte.Count;
        if (n < 3)
            throw new InvalidOperationException("Mindestens 3 Anschlusspunkte erforderlich.");

        bool[] akt  = aktiv ?? Enumerable.Repeat(true, n).ToArray();
        int    nAkt = akt.Count(b => b);
        if (nAkt < 3)
            throw new InvalidOperationException(
                $"Mindestens 3 aktive Anschlusspunkte erforderlich (aktiv: {nAkt}).");

        double[] hz = punkte.Select(p => p.HZ * GON2RAD).ToArray();

        // ── Näherungskoordinaten: Schwerpunkt der AP ──────────────────────────
        double R_P = punkte.Where((_, i) => akt[i]).Average(p => p.R);
        double H_P = punkte.Where((_, i) => akt[i]).Average(p => p.H);

        // Näherungsorientierung: Mittelwert aus (t_i^0 - hz_i)
        {
            double sumSin = 0, sumCos = 0;
            for (int i = 0; i < n; i++)
            {
                if (!akt[i]) continue;
                double t_i = Math.Atan2(punkte[i].R - R_P, punkte[i].H - H_P);
                double d   = NormWinkel(t_i - hz[i]);
                sumSin += Math.Sin(d);
                sumCos += Math.Cos(d);
            }
            // Kreismittelwert über sin/cos (robust gegen ±π-Sprünge)
        }
        // Einfachere Näherung: direkt gemittelt (ausreichend bei kleinen Streubereichen)
        double z = 0;
        {
            double sumZ = 0;
            for (int i = 0; i < n; i++)
            {
                if (!akt[i]) continue;
                double t_i = Math.Atan2(punkte[i].R - R_P, punkte[i].H - H_P);
                sumZ += NormWinkel(t_i - hz[i]);
            }
            z = NormWinkel(sumZ / nAkt);
        }

        double m0_m   = m0_mm  / 1000.0;
        double mR_rad = mR_cc  * CC2RAD;

        int  iter       = 0;
        bool konvergiert = false;

        for (iter = 0; iter < 50; iter++)
        {
            double[,] N = new double[3, 3];
            double[]  b = new double[3];

            for (int i = 0; i < n; i++)
            {
                if (!akt[i]) continue;
                double dR_i = punkte[i].R - R_P;
                double dH_i = punkte[i].H - H_P;
                double s_i  = Math.Sqrt(dR_i * dR_i + dH_i * dH_i);
                if (s_i < 1e-6) continue;

                double t_i    = Math.Atan2(dR_i, dH_i);
                double cos_t  = dH_i / s_i;
                double sin_t  = dR_i / s_i;
                double l_i    = NormWinkel(t_i - hz[i] - z) * s_i;

                double sig_i  = mR_rad * s_i;
                double p_i    = (m0_m * m0_m) / (sig_i * sig_i);

                double[] A = { cos_t, -sin_t, s_i };
                for (int j = 0; j < 3; j++)
                {
                    b[j] += A[j] * p_i * l_i;
                    for (int k = 0; k < 3; k++)
                        N[j, k] += A[j] * p_i * A[k];
                }
            }

            double[]? dx = GaussElim(N, b);
            if (dx == null) break;

            R_P += dx[0];
            H_P += dx[1];
            z    = NormWinkel(z + dx[2]);
            iter++;

            if (Math.Abs(dx[0]) < 1e-7 && Math.Abs(dx[1]) < 1e-7 && Math.Abs(dx[2]) < 1e-9)
            {
                konvergiert = true;
                break;
            }
        }

        // ── s0 und Residuen ──────────────────────────────────────────────────
        int    r    = nAkt - 3;
        double vTPv = 0;
        var    res  = new List<RueckwaertsschnittResidum>();

        for (int i = 0; i < n; i++)
        {
            double dR_i = punkte[i].R - R_P;
            double dH_i = punkte[i].H - H_P;
            double s_i  = Math.Sqrt(dR_i * dR_i + dH_i * dH_i);
            double t_i  = Math.Atan2(dR_i, dH_i);

            double v_rad = NormWinkel(t_i - hz[i] - z);
            double v_cc  = v_rad * RAD2CC;

            if (akt[i] && s_i > 1e-6)
            {
                double sig_i = mR_rad * s_i;
                double p_i   = (m0_m * m0_m) / (sig_i * sig_i);
                vTPv += p_i * (v_rad * s_i) * (v_rad * s_i);
            }

            res.Add(new RueckwaertsschnittResidum
            {
                PunktNr    = punkte[i].PunktNr,
                StreckeH   = s_i,
                vWinkel_cc = v_cc,
                Aktiv      = akt[i],
            });
        }

        double s0_mm_val = r > 0 ? Math.Sqrt(vTPv / r) * 1000.0 : 0;
        double orient    = ((z * RAD2GON) % 400 + 400) % 400;
        string warnKreis = PruefeKritischerKreis(punkte, akt, R_P, H_P);

        return new RueckwaertsschnittErgebnis
        {
            R                = R_P,
            H                = H_P,
            Orientierung_gon = orient,
            s0_mm            = s0_mm_val,
            Redundanz        = r,
            Iterationen      = iter,
            Konvergiert      = konvergiert,
            KritischerKreis  = warnKreis,
            Residuen         = res,
        };
    }

    // ── Kritischer-Kreis-Prüfung ─────────────────────────────────────────────
    // Der Rückwärtschnitt ist singulär, wenn die Station P auf dem Umkreis
    // der Anschlusspunkte liegt (Collins'scher Kreis).
    // Für alle C(n,3)-Kombinationen aktiver Punkte wird geprüft, ob P auf
    // dem jeweiligen Umkreis liegt.  Schwelle: 2 % des Umkreisradius.
    static string PruefeKritischerKreis(
        List<RueckwaertsschnittPunkt> punkte, bool[] akt, double R_P, double H_P)
    {
        var idx = new List<int>();
        for (int i = 0; i < punkte.Count; i++) if (akt[i]) idx.Add(i);
        if (idx.Count < 3) return "";

        for (int a = 0; a < idx.Count - 2; a++)
        for (int b = a + 1; b < idx.Count - 1; b++)
        for (int c = b + 1; c < idx.Count; c++)
        {
            var A = punkte[idx[a]]; var B = punkte[idx[b]]; var C = punkte[idx[c]];
            var (ok, cx, cy, rK) = Umkreis(A.R, A.H, B.R, B.H, C.R, C.H);
            if (!ok) continue;   // kollinear → Sonderfall, kein endlicher Umkreis

            double distP = Math.Sqrt((R_P - cx) * (R_P - cx) + (H_P - cy) * (H_P - cy));
            double relAbw = Math.Abs(distP - rK) / Math.Max(rK, 1.0);

            if (relAbw < 0.02)
                return $"Kritischer Kreis! Standpunkt liegt nahe am Umkreis von " +
                       $"{A.PunktNr}\u2013{B.PunktNr}\u2013{C.PunktNr} " +
                       $"(Abstand {relAbw * 100:F1} % des Umkreisradius). " +
                       "Ergebnis geometrisch unzuverlässig – andere Punktauswahl wählen!";
        }
        return "";
    }

    // Umkreis durch 3 Punkte; gibt (ok=false) zurück wenn Punkte kollinear sind.
    static (bool ok, double cx, double cy, double r) Umkreis(
        double x1, double y1, double x2, double y2, double x3, double y3)
    {
        double ax = x2 - x1, ay = y2 - y1;
        double bx = x3 - x1, by = y3 - y1;
        double D  = 2.0 * (ax * by - ay * bx);
        if (Math.Abs(D) < 1e-10) return (false, 0, 0, 0);   // kollinear

        double ux = (by * (ax * ax + ay * ay) - ay * (bx * bx + by * by)) / D + x1;
        double uy = (ax * (bx * bx + by * by) - bx * (ax * ax + ay * ay)) / D + y1;
        double rK = Math.Sqrt((ux - x1) * (ux - x1) + (uy - y1) * (uy - y1));
        return (true, ux, uy, rK);
    }

    static double NormWinkel(double a)
    {
        while (a >  Math.PI) a -= 2 * Math.PI;
        while (a < -Math.PI) a += 2 * Math.PI;
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
