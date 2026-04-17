namespace Feldbuch;

using System.Globalization;

// ─────────────────────────────────────────────────────────────────────────────
// KoordTransformationsRechner
//
// Unterstützte Methoden
//   • 2D Helmert  (4 Parameter: dx, dy, Rotation α, Maßstab m)
//   • 3D Helmert  (7 Parameter: dx,dy,dz  +  rx,ry,rz  +  m)
//     = Bursa-Wolf-Modell, linearisiert (kleine Winkel, |m|≪1)
//   • 7-Parameter  (identisch mit 3D Helmert, explizite Benennung)
//   • 9-Parameter  (wie 7-Param, aber getrennte Maßstäbe mx,my,mz)
//
// Ausgleichung: kleinste Quadrate über Normalgleichungen  A^T·A·x = A^T·l
// Mindestpunktanzahl:  2D=2, 3D/7P=3, 9P=3 (r≥0 gesichert)
// ─────────────────────────────────────────────────────────────────────────────

public enum TransformationsTyp
{
    Helmert2D   = 0,
    Helmert3D   = 1,
    Parameter7  = 2,
    Parameter9  = 3,
}

public class TransformPunkt
{
    public string PunktNr { get; set; } = "";
    public double X       { get; set; }   // Rechtswert (R)
    public double Y       { get; set; }   // Hochwert   (H)
    public double Z       { get; set; }   // Höhe
    public bool   UseZ    { get; set; } = true;  // Z-Gleichung in Ausgleichung berücksichtigen
}

public class TransformationsParameter
{
    // gemeinsame Parameter (alle Typen)
    public double Dx      { get; set; }
    public double Dy      { get; set; }
    public double Dz      { get; set; }

    // 2D Helmert
    public double A       { get; set; }   // = m·cos(α)
    public double B       { get; set; }   // = m·sin(α)
    public double Alpha_gon  { get; set; }
    public double Massstab2D { get; set; }

    // 3D / 7P / 9P
    public double Rx_rad  { get; set; }   // Rotation X [rad]
    public double Ry_rad  { get; set; }   // Rotation Y [rad]
    public double Rz_rad  { get; set; }   // Rotation Z [rad]
    public double M       { get; set; }   // Maßstabskorrektion (ppm-skaliert, dimensionslos)
    public double Mx      { get; set; }   // Maßstab X (9P)
    public double My      { get; set; }   // Maßstab Y (9P)
    public double Mz      { get; set; }   // Maßstab Z (9P)
}

public class TransformationsResiduum
{
    public string PunktNr    { get; set; } = "";
    public double vX_mm      { get; set; }
    public double vY_mm      { get; set; }
    public double vZ_mm      { get; set; }
    public double vGesamt_mm { get; set; }
    public bool   HoeheAktiv { get; set; } = true;  // false wenn UseZ=false war
}

public class TransformationsErgebnis
{
    public TransformationsTyp     Typ           { get; set; }
    public TransformationsParameter Parameter   { get; set; } = new();
    public double                  S0_mm        { get; set; }
    public int                     Redundanz    { get; set; }
    public bool                    Konvergiert  { get; set; } = true;
    public bool                    FesterMassstab { get; set; } = false;
    public List<TransformationsResiduum> Residuen { get; set; } = new();
    public string                  FehlerMeldung { get; set; } = "";

    // Transformierte Quellpunkte (mit berechneten Zielkoordinaten)
    public List<TransformPunkt>    TransformiertePunkte { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
public static class KoordTransformationsRechner
{
    private const double RAD2GON = 200.0 / Math.PI;

    // ── Haupteinstieg ─────────────────────────────────────────────────────────
    /// <summary>
    /// Berechnet die Transformationsparameter aus korrespondierenden Punktpaaren.
    /// quelle[i] ↔ ziel[i]  müssen paarweise sortiert sein.
    /// festerMassstab = true: Maßstab wird auf 1 fixiert (weniger Unbekannte).
    /// </summary>
    public static TransformationsErgebnis Berechnen(
        TransformationsTyp     typ,
        List<TransformPunkt>   quelle,
        List<TransformPunkt>   ziel,
        bool                   festerMassstab = false)
    {
        if (quelle.Count != ziel.Count)
            return Fehler($"Interne Inkonsistenz: Quell- ({quelle.Count}) ≠ Ziel-Punkte ({ziel.Count}).");
        int minP = MinPunkte(typ, festerMassstab);
        if (quelle.Count < minP)
            return Fehler($"Mindestens {minP} Punktpaare erforderlich ({quelle.Count} vorhanden).");

        var erg = typ switch
        {
            TransformationsTyp.Helmert2D  => festerMassstab
                                                ? Helmert2DFixedScale(quelle, ziel)
                                                : Helmert2D(quelle, ziel),
            TransformationsTyp.Helmert3D  => festerMassstab
                                                ? Helmert6P(quelle, ziel, TransformationsTyp.Helmert3D)
                                                : Helmert7P(quelle, ziel, TransformationsTyp.Helmert3D),
            TransformationsTyp.Parameter7 => festerMassstab
                                                ? Helmert6P(quelle, ziel, TransformationsTyp.Parameter7)
                                                : Helmert7P(quelle, ziel, TransformationsTyp.Parameter7),
            TransformationsTyp.Parameter9 => festerMassstab
                                                ? Helmert6P(quelle, ziel, TransformationsTyp.Parameter9)
                                                : Helmert9P(quelle, ziel),
            _                             => Fehler("Unbekannter Transformationstyp.")
        };
        erg.FesterMassstab = festerMassstab;
        return erg;
    }

    // ── Mindestzahl Punkte ────────────────────────────────────────────────────
    public static int MinPunkte(TransformationsTyp t, bool festerMassstab = false)
    {
        // Fester Maßstab: 3D braucht nur 2 Punkte (6 Unbekannte, 3 Obs/Punkt)
        if (festerMassstab) return t == TransformationsTyp.Helmert2D ? 2 : 2;
        return t switch
        {
            TransformationsTyp.Helmert2D  => 2,
            TransformationsTyp.Helmert3D  => 3,
            TransformationsTyp.Parameter7 => 3,
            TransformationsTyp.Parameter9 => 3,
            _ => 99
        };
    }

    public static string TypName(TransformationsTyp t) => t switch
    {
        TransformationsTyp.Helmert2D  => "2D Helmert (4 Parameter)",
        TransformationsTyp.Helmert3D  => "3D Helmert (7 Parameter)",
        TransformationsTyp.Parameter7 => "7-Parameter Bursa-Wolf",
        TransformationsTyp.Parameter9 => "9-Parameter (getrennte Maßstäbe)",
        _ => "Unbekannt"
    };

    // ─────────────────────────────────────────────────────────────────────────
    // 2D Helmert – Ähnlichkeitstransformation
    //
    //   X' = a·x − b·y + dx
    //   Y' = b·x + a·y + dy
    //   Unbekannte x = [a, b, dx, dy]
    //
    //   Designmatrix-Zeilen (je Punkt 2 Zeilen):
    //     [ xi  −yi  1  0 ] → Xi'
    //     [ yi   xi  0  1 ] → Yi'
    // ─────────────────────────────────────────────────────────────────────────
    private static TransformationsErgebnis Helmert2D(
        List<TransformPunkt> src, List<TransformPunkt> dst)
    {
        int n  = src.Count;
        int nobs = 2 * n;
        int nun  = 4;       // a, b, dx, dy

        double[] l = new double[nobs];
        double[,] A = new double[nobs, nun];

        for (int i = 0; i < n; i++)
        {
            int r0 = 2 * i;
            // Xi' observation
            A[r0,0] =  src[i].X; A[r0,1] = -src[i].Y; A[r0,2] = 1; A[r0,3] = 0;
            l[r0]   = dst[i].X;
            // Yi' observation
            A[r0+1,0] = src[i].Y; A[r0+1,1] = src[i].X; A[r0+1,2] = 0; A[r0+1,3] = 1;
            l[r0+1]   = dst[i].Y;
        }

        double[]? x = LeastSquares(A, l, nobs, nun);
        if (x == null) return Fehler("Normalgleichungen singulär (linear abhängige Punkte?).");

        double a = x[0], b = x[1], dx = x[2], dy = x[3];
        double m     = Math.Sqrt(a * a + b * b);
        double alpha = Math.Atan2(b, a) * RAD2GON;

        var par = new TransformationsParameter
        {
            Dx = dx, Dy = dy,
            A = a, B = b,
            Alpha_gon  = ((alpha % 400) + 400) % 400,
            Massstab2D = m
        };

        // Residuen
        var res = new List<TransformationsResiduum>();
        for (int i = 0; i < n; i++)
        {
            double Xcomp = a * src[i].X - b * src[i].Y + dx;
            double Ycomp = b * src[i].X + a * src[i].Y + dy;
            double vx = (Xcomp - dst[i].X) * 1000;
            double vy = (Ycomp - dst[i].Y) * 1000;
            res.Add(new TransformationsResiduum
            {
                PunktNr    = dst[i].PunktNr,
                vX_mm      = vx,
                vY_mm      = vy,
                vZ_mm      = 0,
                vGesamt_mm = Math.Sqrt(vx*vx + vy*vy)
            });
        }

        double s0 = S0(res, nobs, nun);
        int    red = nobs - nun;

        var trans = new List<TransformPunkt>();
        foreach (var p in src)
            trans.Add(new TransformPunkt
            {
                PunktNr = p.PunktNr,
                X = a * p.X - b * p.Y + dx,
                Y = b * p.X + a * p.Y + dy,
                Z = p.Z
            });

        return new TransformationsErgebnis
        {
            Typ        = TransformationsTyp.Helmert2D,
            Parameter  = par,
            S0_mm      = s0,
            Redundanz  = red,
            Konvergiert= true,
            Residuen   = res,
            TransformiertePunkte = trans
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3D Helmert / 7-Parameter (Bursa-Wolf, linearisiert)
    //
    //   Unbekannte: [dx, dy, dz, rx, ry, rz, m]
    //
    //   Designmatrix-Zeilen (je Punkt 3 Zeilen):
    //     [1 0 0   0   Zi  −Yi  Xi] → X'i
    //     [0 1 0  −Zi  0    Xi  Yi] → Y'i
    //     [0 0 1   Yi −Xi   0   Zi] → Z'i
    //
    //   Beobachtungsvektor l = [X'i − Xi, Y'i − Yi, Z'i − Zi] (Differenz Ziel−Quelle)
    // ─────────────────────────────────────────────────────────────────────────
    private static TransformationsErgebnis Helmert7P(
        List<TransformPunkt> src, List<TransformPunkt> dst, TransformationsTyp typ)
    {
        int n    = src.Count;
        int nobs = src.Sum(p => p.UseZ ? 3 : 2);
        int nun  = 7;

        double[] l   = new double[nobs];
        double[,] A  = new double[nobs, nun];

        int row = 0;
        for (int i = 0; i < n; i++)
        {
            double Xi = src[i].X, Yi = src[i].Y, Zi = src[i].Z;
            // X' row
            A[row,0]=1; A[row,1]=0; A[row,2]=0; A[row,3]=0;   A[row,4]=Zi;  A[row,5]=-Yi; A[row,6]=Xi;
            l[row]  = dst[i].X - Xi; row++;
            // Y' row
            A[row,0]=0; A[row,1]=1; A[row,2]=0; A[row,3]=-Zi; A[row,4]=0;   A[row,5]=Xi;  A[row,6]=Yi;
            l[row] = dst[i].Y - Yi; row++;
            // Z' row (nur wenn Höhe aktiv)
            if (src[i].UseZ)
            {
                A[row,0]=0; A[row,1]=0; A[row,2]=1; A[row,3]=Yi; A[row,4]=-Xi; A[row,5]=0; A[row,6]=Zi;
                l[row] = dst[i].Z - Zi; row++;
            }
        }

        double[]? x = LeastSquares(A, l, nobs, nun);
        if (x == null) return Fehler("Normalgleichungen singulär.");

        double dx=x[0], dy=x[1], dz=x[2];
        double rx=x[3], ry=x[4], rz=x[5], m=x[6];

        var par = new TransformationsParameter
        {
            Dx=dx, Dy=dy, Dz=dz,
            Rx_rad=rx, Ry_rad=ry, Rz_rad=rz,
            M=m
        };

        var res = new List<TransformationsResiduum>();
        for (int i = 0; i < n; i++)
        {
            double Xi=src[i].X, Yi=src[i].Y, Zi=src[i].Z;
            double Xcomp = Xi + dx + m*Xi - rz*Yi + ry*Zi;
            double Ycomp = Yi + dy + rz*Xi + m*Yi - rx*Zi;
            double Zcomp = Zi + dz - ry*Xi + rx*Yi + m*Zi;
            double vx = (Xcomp - dst[i].X)*1000;
            double vy = (Ycomp - dst[i].Y)*1000;
            double vz = src[i].UseZ ? (Zcomp - dst[i].Z)*1000 : 0;
            res.Add(new TransformationsResiduum
            {
                PunktNr    = dst[i].PunktNr,
                vX_mm      = vx, vY_mm = vy, vZ_mm = vz,
                vGesamt_mm = src[i].UseZ
                    ? Math.Sqrt(vx*vx+vy*vy+vz*vz)
                    : Math.Sqrt(vx*vx+vy*vy),
                HoeheAktiv = src[i].UseZ
            });
        }

        var trans = new List<TransformPunkt>();
        foreach (var p in src)
        {
            double Xi=p.X, Yi=p.Y, Zi=p.Z;
            trans.Add(new TransformPunkt
            {
                PunktNr = p.PunktNr,
                X = Xi + dx + m*Xi - rz*Yi + ry*Zi,
                Y = Yi + dy + rz*Xi + m*Yi  - rx*Zi,
                Z = Zi + dz - ry*Xi + rx*Yi + m*Zi
            });
        }

        return new TransformationsErgebnis
        {
            Typ       = typ,
            Parameter = par,
            S0_mm     = S0(res, nobs, nun),
            Redundanz = nobs - nun,
            Konvergiert=true,
            Residuen  = res,
            TransformiertePunkte = trans
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 9-Parameter: getrennte Maßstäbe je Achse (mx, my, mz)
    //
    //   Unbekannte: [dx, dy, dz, rx, ry, rz, mx, my, mz]
    //
    //   Designmatrix-Zeilen (je Punkt 3 Zeilen):
    //     [1 0 0   0   Zi  −Yi  Xi  0   0 ] → X'i
    //     [0 1 0  −Zi  0    Xi  0   Yi  0 ] → Y'i
    //     [0 0 1   Yi −Xi   0   0   0   Zi] → Z'i
    // ─────────────────────────────────────────────────────────────────────────
    private static TransformationsErgebnis Helmert9P(
        List<TransformPunkt> src, List<TransformPunkt> dst)
    {
        int n    = src.Count;
        int nobs = src.Sum(p => p.UseZ ? 3 : 2);
        int nun  = 9;

        double[] l  = new double[nobs];
        double[,] A = new double[nobs, nun];

        int row = 0;
        for (int i = 0; i < n; i++)
        {
            double Xi=src[i].X, Yi=src[i].Y, Zi=src[i].Z;
            // X'
            A[row,0]=1; A[row,1]=0; A[row,2]=0; A[row,3]=0;   A[row,4]=Zi; A[row,5]=-Yi; A[row,6]=Xi; A[row,7]=0;  A[row,8]=0;
            l[row]  = dst[i].X - Xi; row++;
            // Y'
            A[row,0]=0; A[row,1]=1; A[row,2]=0; A[row,3]=-Zi; A[row,4]=0;  A[row,5]=Xi;  A[row,6]=0;  A[row,7]=Yi; A[row,8]=0;
            l[row] = dst[i].Y - Yi; row++;
            // Z' (nur wenn Höhe aktiv)
            if (src[i].UseZ)
            {
                A[row,0]=0; A[row,1]=0; A[row,2]=1; A[row,3]=Yi; A[row,4]=-Xi; A[row,5]=0; A[row,6]=0; A[row,7]=0; A[row,8]=Zi;
                l[row] = dst[i].Z - Zi; row++;
            }
        }

        double[]? x = LeastSquares(A, l, nobs, nun);
        if (x == null) return Fehler("Normalgleichungen singulär.");

        double dx=x[0],dy=x[1],dz=x[2];
        double rx=x[3],ry=x[4],rz=x[5];
        double mx=x[6],my=x[7],mz=x[8];

        var par = new TransformationsParameter
        {
            Dx=dx, Dy=dy, Dz=dz,
            Rx_rad=rx, Ry_rad=ry, Rz_rad=rz,
            Mx=mx, My=my, Mz=mz
        };

        var res = new List<TransformationsResiduum>();
        for (int i = 0; i < n; i++)
        {
            double Xi=src[i].X, Yi=src[i].Y, Zi=src[i].Z;
            double Xcomp = Xi + dx + mx*Xi - rz*Yi + ry*Zi;
            double Ycomp = Yi + dy + rz*Xi + my*Yi  - rx*Zi;
            double Zcomp = Zi + dz - ry*Xi + rx*Yi  + mz*Zi;
            double vx=(Xcomp-dst[i].X)*1000;
            double vy=(Ycomp-dst[i].Y)*1000;
            double vz=src[i].UseZ ? (Zcomp-dst[i].Z)*1000 : 0;
            res.Add(new TransformationsResiduum
            {
                PunktNr    = dst[i].PunktNr,
                vX_mm      = vx, vY_mm = vy, vZ_mm = vz,
                vGesamt_mm = src[i].UseZ
                    ? Math.Sqrt(vx*vx+vy*vy+vz*vz)
                    : Math.Sqrt(vx*vx+vy*vy),
                HoeheAktiv = src[i].UseZ
            });
        }

        var trans = new List<TransformPunkt>();
        foreach (var p in src)
        {
            double Xi=p.X,Yi=p.Y,Zi=p.Z;
            trans.Add(new TransformPunkt
            {
                PunktNr=p.PunktNr,
                X = Xi + dx + mx*Xi - rz*Yi + ry*Zi,
                Y = Yi + dy + rz*Xi + my*Yi  - rx*Zi,
                Z = Zi + dz - ry*Xi + rx*Yi  + mz*Zi
            });
        }

        return new TransformationsErgebnis
        {
            Typ       = TransformationsTyp.Parameter9,
            Parameter = par,
            S0_mm     = S0(res, nobs, nun),
            Redundanz = nobs - nun,
            Konvergiert=true,
            Residuen  = res,
            TransformiertePunkte = trans
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2D Helmert, fester Maßstab m=1 – iterative Linearisierung
    //
    //   Modell:  X' = cos(α)·X − sin(α)·Y + dx
    //            Y' = sin(α)·X + cos(α)·Y + dy
    //   Unbekannte x = [δα, dx, dy]  (3 statt 4, nicht-linear in α → Iteration)
    //
    //   Startnäherung aus freier Helmert-Lösung.
    //   Iteration bis Korrektur < 1e-10.
    // ─────────────────────────────────────────────────────────────────────────
    private static TransformationsErgebnis Helmert2DFixedScale(
        List<TransformPunkt> src, List<TransformPunkt> dst)
    {
        int n = src.Count;

        // Startnäherung aus freier Helmert-Lösung
        var frei = Helmert2D(src, dst);
        if (!string.IsNullOrEmpty(frei.FehlerMeldung)) return frei;
        double alpha = frei.Parameter.Alpha_gon * Math.PI / 200.0;  // gon → rad
        double dx    = frei.Parameter.Dx;
        double dy    = frei.Parameter.Dy;

        const int    MAX_ITER = 20;
        const double EPS      = 1e-10;

        for (int iter = 0; iter < MAX_ITER; iter++)
        {
            double cosA = Math.Cos(alpha);
            double sinA = Math.Sin(alpha);

            int nobs = 2 * n, nun = 3;
            double[]  l = new double[nobs];
            double[,] A = new double[nobs, nun];

            for (int i = 0; i < n; i++)
            {
                int    r0 = 2 * i;
                double Xi = src[i].X, Yi = src[i].Y;
                // X-Beobachtung: ∂X'/∂α = −sinA·X − cosA·Y
                A[r0,0] = -sinA*Xi - cosA*Yi;  A[r0,1] = 1; A[r0,2] = 0;
                l[r0]   = dst[i].X - (cosA*Xi - sinA*Yi + dx);
                // Y-Beobachtung: ∂Y'/∂α =  cosA·X − sinA·Y
                A[r0+1,0] = cosA*Xi - sinA*Yi;  A[r0+1,1] = 0; A[r0+1,2] = 1;
                l[r0+1]   = dst[i].Y - (sinA*Xi + cosA*Yi + dy);
            }

            double[]? dX = LeastSquares(A, l, nobs, nun);
            if (dX == null) return Fehler("Normalgleichungen singulär (fester Maßstab 2D).");

            alpha += dX[0]; dx += dX[1]; dy += dX[2];
            if (Math.Abs(dX[0]) + Math.Abs(dX[1]) + Math.Abs(dX[2]) < EPS) break;
        }

        double cA = Math.Cos(alpha), sA = Math.Sin(alpha);
        double alphaGon = ((alpha * RAD2GON) % 400 + 400) % 400;

        var par = new TransformationsParameter
        {
            Dx = dx, Dy = dy, A = cA, B = sA,
            Alpha_gon = alphaGon, Massstab2D = 1.0
        };

        var res = new List<TransformationsResiduum>();
        for (int i = 0; i < n; i++)
        {
            double vx = (cA*src[i].X - sA*src[i].Y + dx - dst[i].X) * 1000;
            double vy = (sA*src[i].X + cA*src[i].Y + dy - dst[i].Y) * 1000;
            res.Add(new TransformationsResiduum
            {
                PunktNr    = dst[i].PunktNr,
                vX_mm      = vx, vY_mm = vy, vZ_mm = 0,
                vGesamt_mm = Math.Sqrt(vx*vx + vy*vy)
            });
        }

        int nobs2 = 2 * n, nun2 = 3;
        var trans = src.Select(p => new TransformPunkt
        {
            PunktNr = p.PunktNr,
            X = cA*p.X - sA*p.Y + dx, Y = sA*p.X + cA*p.Y + dy, Z = p.Z
        }).ToList();

        return new TransformationsErgebnis
        {
            Typ        = TransformationsTyp.Helmert2D,
            Parameter  = par,
            S0_mm      = S0(res, nobs2, nun2),
            Redundanz  = nobs2 - nun2,
            Konvergiert= true,
            Residuen   = res,
            TransformiertePunkte = trans
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 6-Parameter (Translations + Rotationen, Maßstab m = 1 fest)
    //
    //   Gilt für 3D Helmert, 7P und 9P je mit fixiertem Maßstab.
    //   Unbekannte: [dx, dy, dz, rx, ry, rz]
    //   Designmatrix wie 7P, aber ohne Maßstab-Spalte(n).
    // ─────────────────────────────────────────────────────────────────────────
    private static TransformationsErgebnis Helmert6P(
        List<TransformPunkt> src, List<TransformPunkt> dst, TransformationsTyp typ)
    {
        int n    = src.Count;
        int nobs = src.Sum(p => p.UseZ ? 3 : 2);
        int nun  = 6;   // [dx, dy, dz, rx, ry, rz]

        double[]  l = new double[nobs];
        double[,] A = new double[nobs, nun];

        int row = 0;
        for (int i = 0; i < n; i++)
        {
            double Xi = src[i].X, Yi = src[i].Y, Zi = src[i].Z;
            // X': [1  0  0   0   Zi  −Yi]
            A[row,0]=1; A[row,1]=0; A[row,2]=0; A[row,3]=0;   A[row,4]=Zi;  A[row,5]=-Yi;
            l[row]  = dst[i].X - Xi; row++;
            // Y': [0  1  0  −Zi  0    Xi]
            A[row,0]=0; A[row,1]=1; A[row,2]=0; A[row,3]=-Zi; A[row,4]=0;   A[row,5]=Xi;
            l[row] = dst[i].Y - Yi; row++;
            // Z': [0  0  1   Yi  −Xi  0] (nur wenn Höhe aktiv)
            if (src[i].UseZ)
            {
                A[row,0]=0; A[row,1]=0; A[row,2]=1; A[row,3]=Yi; A[row,4]=-Xi; A[row,5]=0;
                l[row] = dst[i].Z - Zi; row++;
            }
        }

        double[]? x = LeastSquares(A, l, nobs, nun);
        if (x == null) return Fehler("Normalgleichungen singulär (6-Parameter, fester Maßstab).");

        double dx=x[0], dy=x[1], dz=x[2], rx=x[3], ry=x[4], rz=x[5];

        var par = new TransformationsParameter
        { Dx=dx, Dy=dy, Dz=dz, Rx_rad=rx, Ry_rad=ry, Rz_rad=rz, M=0, Mx=0, My=0, Mz=0 };

        var res = new List<TransformationsResiduum>();
        for (int i = 0; i < n; i++)
        {
            double Xi=src[i].X, Yi=src[i].Y, Zi=src[i].Z;
            double Xcomp = Xi + dx        - rz*Yi + ry*Zi;
            double Ycomp = Yi + dy + rz*Xi        - rx*Zi;
            double Zcomp = Zi + dz - ry*Xi + rx*Yi;
            double vx=(Xcomp-dst[i].X)*1000, vy=(Ycomp-dst[i].Y)*1000;
            double vz=src[i].UseZ ? (Zcomp-dst[i].Z)*1000 : 0;
            res.Add(new TransformationsResiduum
            {
                PunktNr    = dst[i].PunktNr,
                vX_mm      = vx, vY_mm = vy, vZ_mm = vz,
                vGesamt_mm = src[i].UseZ
                    ? Math.Sqrt(vx*vx+vy*vy+vz*vz)
                    : Math.Sqrt(vx*vx+vy*vy),
                HoeheAktiv = src[i].UseZ
            });
        }

        var trans = src.Select(p =>
        {
            double Xi=p.X, Yi=p.Y, Zi=p.Z;
            return new TransformPunkt
            {
                PunktNr = p.PunktNr,
                X = Xi + dx        - rz*Yi + ry*Zi,
                Y = Yi + dy + rz*Xi        - rx*Zi,
                Z = Zi + dz - ry*Xi + rx*Yi
            };
        }).ToList();

        return new TransformationsErgebnis
        {
            Typ        = typ,
            Parameter  = par,
            S0_mm      = S0(res, nobs, nun),
            Redundanz  = nobs - nun,
            Konvergiert= true,
            Residuen   = res,
            TransformiertePunkte = trans
        };
    }

    // ── Wendet die berechneten Parameter auf neue Punkte an ───────────────────
    public static TransformPunkt Transformiere(
        TransformationsTyp typ, TransformationsParameter p, TransformPunkt src)
    {
        double Xi=src.X, Yi=src.Y, Zi=src.Z;
        double xT, yT, zT;
        switch (typ)
        {
            case TransformationsTyp.Helmert2D:
                xT = p.A*Xi - p.B*Yi + p.Dx;
                yT = p.B*Xi + p.A*Yi + p.Dy;
                zT = Zi;
                break;
            case TransformationsTyp.Helmert3D:
            case TransformationsTyp.Parameter7:
                xT = Xi + p.Dx + p.M*Xi - p.Rz_rad*Yi + p.Ry_rad*Zi;
                yT = Yi + p.Dy + p.Rz_rad*Xi + p.M*Yi  - p.Rx_rad*Zi;
                zT = Zi + p.Dz - p.Ry_rad*Xi + p.Rx_rad*Yi + p.M*Zi;
                break;
            case TransformationsTyp.Parameter9:
                xT = Xi + p.Dx + p.Mx*Xi - p.Rz_rad*Yi + p.Ry_rad*Zi;
                yT = Yi + p.Dy + p.Rz_rad*Xi + p.My*Yi  - p.Rx_rad*Zi;
                zT = Zi + p.Dz - p.Ry_rad*Xi + p.Rx_rad*Yi + p.Mz*Zi;
                break;
            default:
                xT = Xi; yT = Yi; zT = Zi;
                break;
        }
        return new TransformPunkt { PunktNr=src.PunktNr, X=xT, Y=yT, Z=zT };
    }

    // ── Kleinste-Quadrate via Normalgleichungen ───────────────────────────────
    // Löst  (A^T·A)·x = A^T·l  mit Gauss-Elimination.
    // Gibt null zurück wenn singulär.
    private static double[]? LeastSquares(double[,] A, double[] l, int nobs, int nun)
    {
        // Normalgleichungs-Matrix  N = A^T·A  und  rhs = A^T·l
        double[,] N   = new double[nun, nun];
        double[]  rhs = new double[nun];

        for (int i = 0; i < nun; i++)
        {
            for (int j = 0; j < nun; j++)
            {
                double s = 0;
                for (int k = 0; k < nobs; k++) s += A[k,i]*A[k,j];
                N[i,j] = s;
            }
            double r = 0;
            for (int k = 0; k < nobs; k++) r += A[k,i]*l[k];
            rhs[i] = r;
        }

        return GaussElimination(N, rhs, nun);
    }

    // ── Gauss-Elimination mit Pivotisierung ───────────────────────────────────
    private static double[]? GaussElimination(double[,] M, double[] b, int n)
    {
        // Augmentierte Matrix
        double[,] aug = new double[n, n+1];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++) aug[i,j] = M[i,j];
            aug[i,n] = b[i];
        }

        for (int col = 0; col < n; col++)
        {
            // Pivotsuche
            int piv = col;
            for (int row = col+1; row < n; row++)
                if (Math.Abs(aug[row,col]) > Math.Abs(aug[piv,col])) piv = row;

            // Zeilen tauschen
            for (int j = 0; j <= n; j++)
                (aug[col,j], aug[piv,j]) = (aug[piv,j], aug[col,j]);

            if (Math.Abs(aug[col,col]) < 1e-14) return null;   // singulär

            double pivot = aug[col,col];
            for (int row = col+1; row < n; row++)
            {
                double f = aug[row,col] / pivot;
                for (int j = col; j <= n; j++) aug[row,j] -= f * aug[col,j];
            }
        }

        // Rücksubstitution
        double[] x = new double[n];
        for (int i = n-1; i >= 0; i--)
        {
            double s = aug[i,n];
            for (int j = i+1; j < n; j++) s -= aug[i,j]*x[j];
            x[i] = s / aug[i,i];
        }
        return x;
    }

    // ── Standardabweichung s0 [mm] ────────────────────────────────────────────
    // nobs = Gesamtzahl Beobachtungen, nun = Anzahl Unbekannte → r = nobs − nun
    private static double S0(List<TransformationsResiduum> res, int nobs, int nun)
    {
        double vtv = res.Sum(r =>
            r.vX_mm*r.vX_mm + r.vY_mm*r.vY_mm + r.vZ_mm*r.vZ_mm);
        int r = nobs - nun;
        if (r <= 0) return 0;
        return Math.Sqrt(vtv / r);
    }

    private static TransformationsErgebnis Fehler(string msg) =>
        new() { FehlerMeldung = msg };
}
