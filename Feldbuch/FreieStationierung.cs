namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// Algorithmus nach: Grillmayer & Blauensteiner, VGI 4/2022
// "Methoden der freien Stationierung", https://www.geoat.at
//
// Methode 3.1 – Helmert-Transformation als Näherungslösung
// Methode 3.2 – Vollständige Ausgleichung (Richtungen + Strecken, gewichtet)
// Datumsverfügung: Festpunktzwang (AP-Koordinaten fehlerfrei, Kap. 3.3.1)
//
// Erweiterung: Beobachtungen (Richtung / Strecke) pro Punkt einzeln aktivierbar.
//   • Mindestens 1 aktive Richtung  (Orientierungsunbekannte in 3. Spalte)
//   • Mindestens 3 aktive Beobachtungen gesamt  (n_dir + n_dist ≥ 3)
//
// 2-Punkt-Sonderfall (n = 2):
//   Direkte Ähnlichkeitstransformation (Helmert-Direktlösung, r = 0).
//   Kein iterativer Ausgleich – die Lösung ist eindeutig bestimmt.
// ──────────────────────────────────────────────────────────────────────────────

public class InstrumentParameter
{
    /// <summary>Richtungsgenauigkeit [cc]</summary>
    public double mR_cc   { get; set; } = 9.0;
    /// <summary>Streckenkonstante [mm]</summary>
    public double msk_mm  { get; set; } = 3.0;
    /// <summary>Streckenmaßstabsanteil [ppm]</summary>
    public double msv_ppm { get; set; } = 2.0;
    /// <summary>Standpunktzentrierfehler [mm] – bei freier Stationierung = 0</summary>
    public double mSPZ_mm { get; set; } = 0.0;
    /// <summary>Zielpunktzentrierfehler [mm]</summary>
    public double mZPZ_mm { get; set; } = 0.0;
    /// <summary>Gewichtseinheit [mm]</summary>
    public double m0_mm   { get; set; } = 1.0;
}

public class StationierungsPunkt
{
    public string PunktNr   { get; set; } = "";
    public double R         { get; set; }   // Rechtswert (Easting)
    public double H         { get; set; }   // Hochwert (Northing)
    public double Hoehe     { get; set; }   // Höhe [m]
    public double HZ        { get; set; }   // Horizontalrichtung (unorientiert) [gon]
    public double V         { get; set; }   // Zenitwinkel [gon]
    public double Strecke   { get; set; }   // Schrägstrecke [m]
    public double Zielhoehe { get; set; }   // Zielhöhe [m]
}

public class PunktResidum
{
    public string PunktNr      { get; set; } = "";
    public double StreckeH     { get; set; }   // Horizontalstrecke [m]
    public double vWinkel_cc   { get; set; }   // Winkelresiduum [cc]
    public double vStrecke_mm  { get; set; }   // Streckenresiduum [mm]
    public double vHoehe_mm    { get; set; }   // Höhenresiduum [mm]
    // Aktivierungsstatus der Einzelbeobachtungen
    public bool   RichtungAktiv { get; set; } = true;
    public bool   StreckeAktiv  { get; set; } = true;
}

public class StationierungsErgebnis
{
    public double R                  { get; set; }
    public double H                  { get; set; }
    public double Hoehe              { get; set; }
    public double Orientierung_gon   { get; set; }
    public double Massstab           { get; set; }
    public double s0_mm              { get; set; }
    public int    Redundanz          { get; set; }   // n_aktiv_dir + n_aktiv_dist − 3
    public int    Iterationen        { get; set; }
    public bool   Konvergiert        { get; set; }
    public bool   Berechnung3D       { get; set; } = true;
    public string WarnungHoehe       { get; set; } = "";
    public List<PunktResidum> Residuen { get; set; } = new();
}

public static class FreieStationierungRechner
{
    const double GON2RAD = Math.PI / 200.0;
    const double RAD2GON = 200.0 / Math.PI;
    const double CC2RAD  = Math.PI / 2_000_000.0;
    const double RAD2CC  = 2_000_000.0 / Math.PI;

    // ──────────────────────────────────────────────────────────────────────────
    // Hauptmethode
    //
    // aktivRichtung[i]  – Richtungsbeobachtung von Punkt i in Ausgleichung
    // aktivStrecke[i]   – Streckenbeobachtung von Punkt i in Ausgleichung
    // Beide Arrays sind optional; wird null übergeben, gelten alle als aktiv.
    // ──────────────────────────────────────────────────────────────────────────
    public static StationierungsErgebnis Berechnen(
        List<StationierungsPunkt> punkte,
        double instrumentenhoehe,
        InstrumentParameter? param    = null,
        bool freierMassstab           = true,
        bool[]? aktivRichtung         = null,
        bool[]? aktivStrecke          = null,
        bool berechnung3D             = true,
        double fehlergrenzeMM_Hoehe   = 10.0)
    {
        param ??= new InstrumentParameter();
        int n = punkte.Count;
        if (n < 2)
            throw new InvalidOperationException("Mindestens 2 Anschlusspunkte erforderlich.");

        // Aktivierungsarrays: Standard = alle aktiv
        bool[] aktHz  = aktivRichtung ?? Enumerable.Repeat(true, n).ToArray();
        bool[] aktStr = aktivStrecke  ?? Enumerable.Repeat(true, n).ToArray();

        // Validierung: Randbedingungen
        int nDir  = aktHz.Count(b => b);
        int nDist = aktStr.Count(b => b);
        if (nDir < 1)
            throw new InvalidOperationException(
                "Mindestens 1 aktive Richtungsbeobachtung erforderlich " +
                "(Orientierungsunbekannte sonst unbestimmt).");
        if (n == 2)
        {
            // Ähnlichkeitstransformation: Helmert-Direktlösung verwendet alle Punkte.
            // freierMassstab=true  → 4 Unbekannte (R, H, o0, m) → mind. 4 Beobachtungen
            // freierMassstab=false → 3 Unbekannte (R, H, o0)    → mind. 3 Beobachtungen
            int minObs = freierMassstab ? 4 : 3;
            if (nDir + nDist < minObs)
                throw new InvalidOperationException(
                    $"Für 2 Punkte {(freierMassstab ? "mit freiem Maßstab " : "")}sind " +
                    $"mindestens {minObs} aktive Beobachtungen erforderlich " +
                    $"(aktiv: {nDir} Richtungen + {nDist} Strecken = {nDir + nDist}).");
        }
        else
        {
            if (nDir + nDist < 3)
                throw new InvalidOperationException(
                    $"Mindestens 3 aktive Beobachtungen erforderlich " +
                    $"(aktiv: {nDir} Richtungen + {nDist} Strecken = {nDir + nDist}).");
        }

        double[] hz_rad = punkte.Select(p => p.HZ * GON2RAD).ToArray();
        double[] v_rad  = punkte.Select(p => p.V  * GON2RAD).ToArray();
        double[] dh     = punkte.Select((p, i) => p.Strecke * Math.Sin(v_rad[i])).ToArray();

        // ── Schritt 1: Helmert-Näherungslösung ───────────────────────────────
        // Für die Näherungskoordinaten alle Punkte verwenden, die mindestens
        // eine aktive Beobachtung (Hz oder Str) haben; die Helmert-Transformation
        // benötigt dh[], das aus den Eingabedaten stammt (nicht aus der Aktivierung).
        var (R0, H0, o0, massstab) = HelmertTransformation(punkte, hz_rad, dh, freierMassstab);

        // ── Schritt 2: Ausgleichung oder Direktlösung ─────────────────────────
        double   R_FS = R0, H_FS = H0;
        double[] vQuer_m, vStr_m;
        int      iter;
        bool     konvergiert;

        if (n >= 3)
        {
            // Vollständige gewichtete Ausgleichung (iterativ)
            (vQuer_m, vStr_m, iter, konvergiert) =
                VollAusgleichung(punkte, hz_rad, dh, param, aktHz, aktStr,
                                 ref R_FS, ref H_FS, ref o0);
        }
        else
        {
            // n == 2: Ähnlichkeitstransformation – Helmert-Direktlösung genügt.
            // Keine iterative Ausgleichung; Residuen werden aus der Helmert-Lösung berechnet.
            vQuer_m = new double[n];
            vStr_m  = new double[n];
            for (int i = 0; i < n; i++)
            {
                double dR  = punkte[i].R - R_FS;
                double dHH = punkte[i].H - H_FS;
                double s0i = Math.Sqrt(dR * dR + dHH * dHH);
                vQuer_m[i] = NormWinkel(Math.Atan2(dR, dHH) - hz_rad[i] - o0) * s0i;
                vStr_m[i]  = dh[i] - s0i;
            }
            iter        = 0;
            konvergiert = true;
        }

        // ── Schritt 3: Standardabweichung der Gewichtseinheit ─────────────────
        double m0_m = param.m0_mm / 1000.0;
        double mSPZ = param.mSPZ_mm / 1000.0;
        double mZPZ = param.mZPZ_mm / 1000.0;
        // n == 2: Direktlösung → r = 0 (freierMassstab=true: 4 Unbekannte; false: 3 Unbekannte)
        // n >= 3: Vollausgleichung → r = nDir + nDist − 3
        int r = n == 2
            ? 0
            : nDir + nDist - 3;
        double vTPv = 0;
        for (int i = 0; i < n; i++)
        {
            double s0_i  = Math.Sqrt(Math.Pow(punkte[i].R - R_FS, 2) +
                                     Math.Pow(punkte[i].H - H_FS, 2));
            if (aktHz[i])
            {
                double mR_m = param.mR_cc * CC2RAD * s0_i;
                double mQ2  = mR_m * mR_m + mSPZ * mSPZ + mZPZ * mZPZ;
                double p_d  = m0_m * m0_m / Math.Max(mQ2, 1e-20);
                vTPv += p_d * (vQuer_m[i] * 1000) * (vQuer_m[i] * 1000);
            }
            if (aktStr[i])
            {
                double ms_m = (param.msk_mm + s0_i / 1000.0 * param.msv_ppm) / 1000.0;
                double ms2  = ms_m * ms_m + mSPZ * mSPZ + mZPZ * mZPZ;
                double p_s  = m0_m * m0_m / Math.Max(ms2, 1e-20);
                vTPv += p_s * (vStr_m[i] * 1000) * (vStr_m[i] * 1000);
            }
        }
        double s0_mm = r > 0 ? Math.Sqrt(vTPv / r) : 0;

        // ── Schritt 4: Höhenberechnung ────────────────────────────────────────
        // Nur Punkte mit aktiver Streckenbeobachtung (benötigt S und V)
        double hMittel   = 0;
        bool   ist3D     = berechnung3D;
        string warnHoehe = "";

        if (berechnung3D)
        {
            double[] hStation_i = new double[n];
            double sumNum = 0, sumDen = 0;
            for (int i = 0; i < n; i++)
            {
                if (!aktStr[i]) continue;
                hStation_i[i] = punkte[i].Hoehe + punkte[i].Zielhoehe
                                - instrumentenhoehe
                                - punkte[i].Strecke * Math.Cos(v_rad[i]);
                double w = 1.0 / (dh[i] * dh[i] + 1e-9);
                sumNum += w * hStation_i[i];
                sumDen += w;
            }
            hMittel = sumDen > 0 ? sumNum / sumDen : 0;

            // Prüfe Höhenresiduen – Grenze = 3 × Fehlergrenze
            double grenze = 3.0 * fehlergrenzeMM_Hoehe;
            for (int i = 0; i < n; i++)
            {
                if (!aktStr[i]) continue;
                double vH_mm = (hStation_i[i] - hMittel) * 1000.0;
                if (Math.Abs(vH_mm) > grenze)
                {
                    warnHoehe = $"Höhenresiduen überschreiten das 3-fache der Fehlergrenze " +
                                $"({grenze:F0} mm).\n" +
                                $"Die Berechnung wird automatisch 2-dimensional durchgeführt.";
                    ist3D   = false;
                    hMittel = 0;
                    break;
                }
            }
        }

        // ── Schritt 5: Residuen zusammenstellen ──────────────────────────────
        var residuen = new List<PunktResidum>();
        for (int i = 0; i < n; i++)
        {
            double s0_i  = Math.Sqrt(Math.Pow(punkte[i].R - R_FS, 2) +
                                     Math.Pow(punkte[i].H - H_FS, 2));
            double vW_cc = (aktHz[i] && s0_i > 1e-6)
                ? (vQuer_m[i] / s0_i) * RAD2CC : 0;

            double vH_mm = double.NaN;
            if (ist3D && aktStr[i])
            {
                double hStation = punkte[i].Hoehe + punkte[i].Zielhoehe
                                  - instrumentenhoehe
                                  - punkte[i].Strecke * Math.Cos(v_rad[i]);
                vH_mm = (hStation - hMittel) * 1000.0;
            }

            residuen.Add(new PunktResidum
            {
                PunktNr        = punkte[i].PunktNr,
                StreckeH       = dh[i],
                vWinkel_cc     = aktHz[i]  ? vW_cc : double.NaN,
                vStrecke_mm    = aktStr[i] ? vStr_m[i] * 1000 : double.NaN,
                vHoehe_mm      = vH_mm,
                RichtungAktiv  = aktHz[i],
                StreckeAktiv   = aktStr[i]
            });
        }

        double orient_gon = ((o0 * RAD2GON) % 400 + 400) % 400;

        return new StationierungsErgebnis
        {
            R = R_FS, H = H_FS, Hoehe = hMittel,
            Orientierung_gon = orient_gon,
            Massstab         = massstab,
            s0_mm            = s0_mm,
            Redundanz        = r,
            Iterationen      = iter,
            Konvergiert      = konvergiert,
            Berechnung3D     = ist3D,
            WarnungHoehe     = warnHoehe,
            Residuen         = residuen
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helmert-Transformation (Methode 3.1)
    // Alle Punkte werden für die Näherungskoordinaten verwendet.
    // ──────────────────────────────────────────────────────────────────────────
    static (double R, double H, double o0_rad, double m) HelmertTransformation(
        List<StationierungsPunkt> punkte, double[] hz_rad, double[] dh,
        bool freierMassstab = true)
    {
        int n = punkte.Count;
        double[] xi  = new double[n];
        double[] eta = new double[n];
        for (int i = 0; i < n; i++)
        {
            xi[i]  = dh[i] * Math.Cos(hz_rad[i]);
            eta[i] = dh[i] * Math.Sin(hz_rad[i]);
        }

        double xi_S  = xi.Average();
        double eta_S = eta.Average();
        double R_S   = punkte.Select(p => p.R).Average();
        double H_S   = punkte.Select(p => p.H).Average();

        double[] xi_r  = xi.Select(v => v - xi_S).ToArray();
        double[] eta_r = eta.Select(v => v - eta_S).ToArray();
        double[] R_r   = punkte.Select(p => p.R - R_S).ToArray();
        double[] H_r   = punkte.Select(p => p.H - H_S).ToArray();

        double denom = 0, sumA = 0, sumB = 0;
        for (int i = 0; i < n; i++)
        {
            denom += eta_r[i] * eta_r[i] + xi_r[i] * xi_r[i];
            sumA  += xi_r[i]  * R_r[i] - eta_r[i] * H_r[i];
            sumB  += eta_r[i] * R_r[i] + xi_r[i]  * H_r[i];
        }
        if (Math.Abs(denom) < 1e-12)
            throw new InvalidOperationException("Helmert: Nenner zu klein (alle Punkte kollinear?).");

        double a      = sumA / denom;
        double b      = sumB / denom;
        double m_frei = Math.Sqrt(a * a + b * b);
        double o0     = Math.Atan2(a, b);

        double a_eff = freierMassstab ? a : Math.Sin(o0);
        double b_eff = freierMassstab ? b : Math.Cos(o0);

        double R_FS = R_S - b_eff * eta_S - a_eff * xi_S;
        double H_FS = H_S - b_eff * xi_S  + a_eff * eta_S;
        double m    = freierMassstab ? m_frei : 1.0;

        return (R_FS, H_FS, o0, m);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Vollständige gewichtete Ausgleichung (Methode 3.2)
    //
    // Inaktive Beobachtungen erhalten Gewicht p = 0 → kein Beitrag zur
    // Normalgleichungsmatrix N, aber die Residuen werden trotzdem berechnet
    // (als "wie wäre der Wert" ohne Einfluss auf die Lösung).
    // ──────────────────────────────────────────────────────────────────────────
    static (double[] vQ, double[] vS, int iter, bool ok) VollAusgleichung(
        List<StationierungsPunkt> punkte, double[] hz_rad, double[] dh,
        InstrumentParameter param,
        bool[] aktHz, bool[] aktStr,
        ref double R_FS, ref double H_FS, ref double o0)
    {
        int n     = punkte.Count;
        int m_obs = 2 * n;
        double m0_m = param.m0_mm / 1000.0;
        double mSPZ = param.mSPZ_mm / 1000.0;
        double mZPZ = param.mZPZ_mm / 1000.0;

        int  iterationen = 0;
        bool konvergiert = false;

        for (int iter = 0; iter < 50; iter++)
        {
            double[,] A = new double[m_obs, 3];
            double[]  l = new double[m_obs];
            double[]  p = new double[m_obs];

            for (int i = 0; i < n; i++)
            {
                double dR    = punkte[i].R - R_FS;
                double dHH   = punkte[i].H - H_FS;
                double s0    = Math.Sqrt(dR * dR + dHH * dHH);
                double t_i   = Math.Atan2(dR, dHH);
                double cos_t = Math.Cos(t_i);
                double sin_t = Math.Sin(t_i);

                // ── Richtungsbeobachtung ──
                int iD = 2 * i;
                l[iD]    = NormWinkel(t_i - hz_rad[i] - o0) * s0;
                A[iD, 0] =  cos_t;
                A[iD, 1] = -sin_t;
                A[iD, 2] =  s0;
                if (aktHz[i])
                {
                    double mR_m = param.mR_cc * CC2RAD * s0;
                    double mQ2  = mR_m * mR_m + mSPZ * mSPZ + mZPZ * mZPZ;
                    p[iD] = (m0_m * m0_m) / Math.Max(mQ2, 1e-20);
                }
                // sonst p[iD] = 0 (Standard-Initialisierung)

                // ── Streckenbeobachtung ──
                int iS = 2 * i + 1;
                l[iS]    = dh[i] - s0;
                A[iS, 0] = -sin_t;
                A[iS, 1] = -cos_t;
                A[iS, 2] =  0.0;
                if (aktStr[i])
                {
                    double ms_m = (param.msk_mm + s0 / 1000.0 * param.msv_ppm) / 1000.0;
                    double ms2  = ms_m * ms_m + mSPZ * mSPZ + mZPZ * mZPZ;
                    p[iS] = (m0_m * m0_m) / Math.Max(ms2, 1e-20);
                }
            }

            double[,] N    = new double[3, 3];
            double[]  bVec = new double[3];
            for (int i = 0; i < m_obs; i++)
                for (int j = 0; j < 3; j++)
                {
                    bVec[j] += A[i, j] * p[i] * l[i];
                    for (int k = 0; k < 3; k++)
                        N[j, k] += A[i, j] * p[i] * A[i, k];
                }

            double[]? dx = GaussElim(N, bVec);
            if (dx == null) break;

            R_FS += dx[0];
            H_FS += dx[1];
            o0   += dx[2];
            o0    = NormWinkel(o0);
            iterationen = iter + 1;

            if (Math.Abs(dx[0]) < 1e-7 && Math.Abs(dx[1]) < 1e-7 && Math.Abs(dx[2]) < 1e-9)
            { konvergiert = true; break; }
        }

        // Endresiduen für alle Punkte (auch inaktive, als Information)
        double[] vQuer = new double[n];
        double[] vStr  = new double[n];
        for (int i = 0; i < n; i++)
        {
            double dR  = punkte[i].R - R_FS;
            double dHH = punkte[i].H - H_FS;
            double s0  = Math.Sqrt(dR * dR + dHH * dHH);
            vQuer[i] = NormWinkel(Math.Atan2(dR, dHH) - hz_rad[i] - o0) * s0;
            vStr[i]  = dh[i] - s0;
        }

        return (vQuer, vStr, iterationen, konvergiert);
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
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++) M[i, j] = A[i, j];
            M[i, n] = b[i];
        }
        for (int col = 0; col < n; col++)
        {
            int pr = col;
            for (int r = col + 1; r < n; r++)
                if (Math.Abs(M[r, col]) > Math.Abs(M[pr, col])) pr = r;
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
