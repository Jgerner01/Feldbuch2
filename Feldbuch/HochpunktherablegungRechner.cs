namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// Hochpunktherablegung (Trigonometrische Höhenübertragung zu unzugänglichem Punkt)
//
// Unbekannter Hochpunkt P aus ≥2 Stationen mit bekannten Koordinaten und Höhen.
// Jede Station misst Horizontalrichtung Hz und Zenitwinkel V zu P.
//
// Schritt 1 – Planimetrie (Vorwärtschnitt):
//   t_i = Hz_i + z_i  (orientierter Richtungswinkel Station→P)
//   Gleicher LS-Ansatz wie VorwaertsschnittRechner.
//   Redundanz r_Plan = n_aktiv_Dir − 2
//
// Schritt 2 – Höhe (trigonometrisch):
//   s_i = Horizontalstrecke Station→P  (aus Planimetrieergebnis)
//   ΔH_i = s_i · cot(V_i_rad)  (cot = cos/sin des Zenitwinkels)
//   Höhe_P_i = Höhe_i + iH_i + ΔH_i − zh_i
//   Gewichtetes Mittel: w_i = sin²(V_i_rad) / s_i  (steiler + näher = besser)
//   Redundanz r_H = n_aktiv_H − 1
// ──────────────────────────────────────────────────────────────────────────────

public class HochpunktMessung
{
    public string PunktNr   { get; set; } = "";
    public double R         { get; set; }   // Rechtswert der Station [m]
    public double H         { get; set; }   // Hochwert der Station [m]
    public double Hoehe     { get; set; }   // Höhe der Station [m]
    public double iH        { get; set; }   // Instrumentenhöhe [m]
    public double Hz        { get; set; }   // Horizontalrichtung zu P [gon]
    public double z         { get; set; }   // Orientierungsunbekannte der Station [gon]
    public double V         { get; set; }   // Zenitwinkel zu P [gon]
    public double Zielhöhe  { get; set; }   // Zielhöhe am Hochpunkt [m]
}

public class HochpunktResidum
{
    public string PunktNr   { get; set; } = "";
    public double s_horiz   { get; set; }   // Horizontalstrecke Station→P [m]
    public double vDir_cc   { get; set; }   // Richtungsresiduum [cc]
    public double HoehePi   { get; set; }   // Einzelne Höhenschätzung [m]
    public double vH_mm     { get; set; }   // Höhenresiduum [mm]
    public bool   AktivDir  { get; set; } = true;
    public bool   AktivH    { get; set; } = true;
}

public class HochpunktErgebnis
{
    public double R            { get; set; }
    public double H            { get; set; }
    public double Hoehe        { get; set; }   // Höhe des Hochpunkts [m]
    public double s0Dir_mm     { get; set; }   // Standardabw. Richtungen [mm an Maßstab]
    public double s0H_mm       { get; set; }   // Standardabw. Höhe [mm]
    public int    RedundanzDir { get; set; }
    public int    RedundanzH   { get; set; }
    public int    Iterationen  { get; set; }
    public bool   Konvergiert  { get; set; }
    public List<HochpunktResidum> Residuen { get; set; } = new();
}

public static class HochpunktherablegungRechner
{
    const double GON2RAD = Math.PI / 200.0;
    const double CC2RAD  = Math.PI / 2_000_000.0;
    const double RAD2CC  = 2_000_000.0 / Math.PI;

    public static HochpunktErgebnis Berechnen(
        List<HochpunktMessung> messungen,
        bool[]? aktiv  = null,
        double  mR_cc  = 9.0,
        double  m0_mm  = 1.0)
    {
        int n = messungen.Count;
        if (n < 2)
            throw new InvalidOperationException("Mindestens 2 Stationen erforderlich.");

        bool[] akt  = aktiv ?? Enumerable.Repeat(true, n).ToArray();
        int    nAkt = akt.Count(b => b);
        if (nAkt < 2)
            throw new InvalidOperationException(
                $"Mindestens 2 aktive Stationen erforderlich (aktiv: {nAkt}).");

        double[] t_obs = messungen.Select(m => NormWinkel2Pi((m.Hz + m.z) * GON2RAD)).ToArray();
        double[] V_rad = messungen.Select(m => m.V * GON2RAD).ToArray();

        // ── Schritt 1: Vorwärtschnitt (Planimetrie) ─────────────────────────
        int i0 = -1, i1 = -1;
        for (int i = 0; i < n; i++) { if (akt[i]) { if (i0 < 0) i0 = i; else if (i1 < 0) { i1 = i; break; } } }

        double R_P, H_P;
        if (!Schnitt2Strahlen(messungen[i0].R, messungen[i0].H, t_obs[i0],
                              messungen[i1].R, messungen[i1].H, t_obs[i1],
                              out R_P, out H_P))
        {
            R_P = messungen.Where((_, i) => akt[i]).Average(m => m.R);
            H_P = messungen.Where((_, i) => akt[i]).Average(m => m.H);
        }

        double m0_m   = m0_mm / 1000.0;
        double mR_rad = mR_cc * CC2RAD;
        int    iter   = 0;
        bool   konv   = false;

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

                double t_i0  = Math.Atan2(dR_i, dH_i);
                double l_i   = NormWinkel(t_obs[i] - t_i0) * s_i;
                double cos_t = dH_i / s_i;
                double sin_t = dR_i / s_i;

                double sig_i = mR_rad * s_i;
                double p_i   = (m0_m * m0_m) / (sig_i * sig_i);

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

            if (Math.Abs(dx[0]) < 1e-7 && Math.Abs(dx[1]) < 1e-7) { konv = true; break; }
        }

        // ── Schritt 2: Höhenberechnung ───────────────────────────────────────
        // Prüfe: Zenitwinkel gültig (sin(V) > 0, also 0 < V < 200 gon)?
        double sumWH  = 0;
        double sumWHv = 0;

        for (int i = 0; i < n; i++)
        {
            if (!akt[i]) continue;
            double sinV = Math.Sin(V_rad[i]);
            if (Math.Abs(sinV) < 1e-6) continue;   // V ≈ 0 oder 200 gon → ungültig

            double dR_i   = R_P - messungen[i].R;
            double dH_i   = H_P - messungen[i].H;
            double s_i    = Math.Sqrt(dR_i * dR_i + dH_i * dH_i);
            double cosV   = Math.Cos(V_rad[i]);
            double dHi    = s_i * cosV / sinV;   // ΔH = s · cot(V)
            double H_P_i  = messungen[i].Hoehe + messungen[i].iH + dHi - messungen[i].Zielhöhe;

            double w_i = sinV * sinV / Math.Max(s_i, 0.01);
            sumWH  += w_i * H_P_i;
            sumWHv += w_i;
        }

        double Hoehe_P = sumWHv > 0 ? sumWH / sumWHv : 0;

        // ── Residuen + s0 ────────────────────────────────────────────────────
        int    r_dir = nAkt - 2;
        int    r_H   = -1;   // wird unten gesetzt
        double vTPv_dir = 0;
        double vTPv_H   = 0;
        int    n_H      = 0;

        var res = new List<HochpunktResidum>();

        for (int i = 0; i < n; i++)
        {
            double dR_i  = R_P - messungen[i].R;
            double dH_i  = H_P - messungen[i].H;
            double s_i   = Math.Sqrt(dR_i * dR_i + dH_i * dH_i);
            double t_i0  = Math.Atan2(dR_i, dH_i);
            double v_dir = NormWinkel(t_obs[i] - t_i0) * RAD2CC;

            double H_Pi  = double.NaN;
            double v_H   = double.NaN;
            bool   hasH  = false;

            double sinV = Math.Sin(V_rad[i]);
            if (akt[i] && Math.Abs(sinV) > 1e-6 && s_i > 1e-6)
            {
                double cosV  = Math.Cos(V_rad[i]);
                double dHi   = s_i * cosV / sinV;
                H_Pi  = messungen[i].Hoehe + messungen[i].iH + dHi - messungen[i].Zielhöhe;
                v_H   = (H_Pi - Hoehe_P) * 1000.0;
                double w_i = sinV * sinV / s_i;
                vTPv_H += w_i * v_H * v_H;
                hasH = true;
                n_H++;
            }

            if (akt[i] && s_i > 1e-6)
            {
                double sig_i = mR_rad * s_i;
                double p_i   = (m0_m * m0_m) / (sig_i * sig_i);
                double v_rad = NormWinkel(t_obs[i] - t_i0);
                vTPv_dir += p_i * (v_rad * s_i) * (v_rad * s_i);
            }

            res.Add(new HochpunktResidum
            {
                PunktNr  = messungen[i].PunktNr,
                s_horiz  = s_i,
                vDir_cc  = v_dir,
                HoehePi  = H_Pi,
                vH_mm    = v_H,
                AktivDir = akt[i],
                AktivH   = akt[i] && hasH,
            });
        }

        r_H = Math.Max(0, n_H - 1);
        double s0Dir_mm = r_dir > 0 ? Math.Sqrt(vTPv_dir / r_dir) * 1000.0 : 0;
        double s0H_mm   = r_H   > 0 ? Math.Sqrt(vTPv_H   / r_H)             : 0;

        return new HochpunktErgebnis
        {
            R            = R_P,
            H            = H_P,
            Hoehe        = Hoehe_P,
            s0Dir_mm     = s0Dir_mm,
            s0H_mm       = s0H_mm,
            RedundanzDir = r_dir,
            RedundanzH   = r_H,
            Iterationen  = iter,
            Konvergiert  = konv,
            Residuen     = res,
        };
    }

    static bool Schnitt2Strahlen(double Ra, double Ha, double tA,
                                  double Rb, double Hb, double tB,
                                  out double R_P, out double H_P)
    {
        double det = Math.Sin(tA) * (-Math.Cos(tB)) - (-Math.Sin(tB)) * Math.Cos(tA);
        if (Math.Abs(det) < 1e-9) { R_P = H_P = 0; return false; }
        double lam = ((Rb - Ra) * (-Math.Cos(tB)) - (Hb - Ha) * (-Math.Sin(tB))) / det;
        R_P = Ra + lam * Math.Sin(tA);
        H_P = Ha + lam * Math.Cos(tA);
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
