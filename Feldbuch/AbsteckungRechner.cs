namespace Feldbuch;

using System.Globalization;

// ──────────────────────────────────────────────────────────────────────────────
// Absteckungsmodul – Datenklassen und Berechnungen
// ──────────────────────────────────────────────────────────────────────────────

public class StandpunktInfo
{
    public string PunktNr         { get; set; } = "";
    public double R               { get; set; }
    public double H               { get; set; }
    public double Orientierung_gon { get; set; }
}

public class AbsteckPunkt
{
    public string PunktNr      { get; set; } = "";
    public string Label        { get; set; } = "";  // Station, Rasterbez., etc.
    public double R_soll       { get; set; }
    public double H_soll       { get; set; }
    public double Hz_soll_gon  { get; set; }
    public double s_soll_m     { get; set; }
    public double? Abszisse_m  { get; set; }  // Stationsmaß (Achse)
    public double? Ordinate_m  { get; set; }  // Ordinate (Offset)
    public string Status       { get; set; } = "offen";  // "offen" | "abgesteckt"
}

public class ProfilAbsteckPunkt
{
    public string  PunktNr          { get; set; } = "";
    public double  Station_m        { get; set; }
    public double  R                { get; set; }
    public double  H                { get; set; }
    public double  H_plan           { get; set; }
    public double? H_Gelaende       { get; set; }
    public double? DeltaH_m         { get; set; }  // H_plan − H_Gelaende (+ = Auftrag)
    public double? BoeschLinks_m    { get; set; }  // horizontaler Abstand Böschungsfuß links
    public double? BoeschRechts_m   { get; set; }
    public double  Hz_soll_gon      { get; set; }
    public double  s_soll_m         { get; set; }
}

public static class AbsteckungRechner
{
    const double GON2RAD = Math.PI / 200.0;
    const double RAD2GON = 200.0 / Math.PI;

    // ── Standpunkt aus ProjektdatenManager laden ─────────────────────────────
    public static StandpunktInfo? LadeStandpunkt()
    {
        var IC = CultureInfo.InvariantCulture;
        bool TryP(string? s, out double v) =>
            double.TryParse(s?.Replace(',', '.'), NumberStyles.Any, IC, out v);

        // Rückwärtschnitt
        if (TryP(ProjektdatenManager.GetValue("Rückwärtschnitt", "R [m]"),             out double r) &&
            TryP(ProjektdatenManager.GetValue("Rückwärtschnitt", "H [m]"),             out double h) &&
            TryP(ProjektdatenManager.GetValue("Rückwärtschnitt", "Orientierung [gon]"), out double z))
            return new StandpunktInfo {
                PunktNr          = ProjektdatenManager.GetValue("Rückwärtschnitt", "Standpunkt") ?? "?",
                R = r, H = h, Orientierung_gon = z };

        // Freie Stationierung
        if (TryP(ProjektdatenManager.GetValue("Freie Stationierung", "R [m]"),             out r) &&
            TryP(ProjektdatenManager.GetValue("Freie Stationierung", "H [m]"),             out h) &&
            TryP(ProjektdatenManager.GetValue("Freie Stationierung", "Orientierung [gon]"), out z))
            return new StandpunktInfo {
                PunktNr          = ProjektdatenManager.GetValue("Freie Stationierung", "Standpunkt") ?? "?",
                R = r, H = h, Orientierung_gon = z };

        return null;
    }

    // ── Polare Absteckung: Hz und Strecke vom Standpunkt zum Sollpunkt ───────
    public static void BerechnePolareAbsteckung(StandpunktInfo st, AbsteckPunkt p)
    {
        double dR = p.R_soll - st.R;
        double dH = p.H_soll - st.H;
        double t_gon = Math.Atan2(dR, dH) * RAD2GON;
        p.Hz_soll_gon = ((t_gon - st.Orientierung_gon) % 400.0 + 400.0) % 400.0;
        p.s_soll_m    = Math.Sqrt(dR * dR + dH * dH);
    }

    // ── Orthogonalmaß: Abszisse + Ordinate zum Lotfußpunkt ───────────────────
    public static (double Abszisse, double Ordinate) BerechneOrthogonal(
        double R_A, double H_A, double R_E, double H_E, double R_P, double H_P)
    {
        double dR = R_E - R_A, dH = H_E - H_A;
        double L  = Math.Sqrt(dR * dR + dH * dH);
        if (L < 1e-9) return (0, 0);
        double ux = dR / L, uy = dH / L;
        double abszisse =  (R_P - R_A) * ux + (H_P - H_A) * uy;
        double ordinate = -(R_P - R_A) * uy + (H_P - H_A) * ux;
        return (abszisse, ordinate);
    }

    // ── Abweichung Ist→Soll (Längs/Quer) ─────────────────────────────────────
    public static (double Ds_mm, double Dq_mm) BerechneAbweichung(
        double R_soll, double H_soll, double R_ist, double H_ist,
        double t_soll_rad)
    {
        double dR = R_ist - R_soll;
        double dH = H_ist - H_soll;
        double ds =  dR * Math.Sin(t_soll_rad) + dH * Math.Cos(t_soll_rad);
        double dq = -dR * Math.Cos(t_soll_rad) + dH * Math.Sin(t_soll_rad);
        return (ds * 1000.0, dq * 1000.0);
    }

    // ── Achsabsteckung: Punkte entlang Achse mit optionalen Offsets ──────────
    public static List<AbsteckPunkt> BerechneAchspunkte(
        double R_A, double H_A, double R_E, double H_E,
        double intervall, double[] offsets, StandpunktInfo? st)
    {
        var result = new List<AbsteckPunkt>();
        double dR = R_E - R_A, dH = H_E - H_A;
        double L  = Math.Sqrt(dR * dR + dH * dH);
        if (L < 1e-6 || intervall < 0.01) return result;

        double ux = dR / L, uy = dH / L;
        double nx = -uy,   ny =  ux;  // linke Normale (CCW)

        double[] ofs = offsets.Length > 0 ? offsets : new[] { 0.0 };
        int idx = 0;

        foreach (double offset in ofs)
        {
            double station = 0;
            while (station <= L + 1e-6)
            {
                double s  = Math.Min(station, L);
                double R  = R_A + s * ux + offset * nx;
                double H  = H_A + s * uy + offset * ny;

                string stationStr = $"0+{s:000.0}";
                string offsetStr  = offset != 0.0 ? $" {offset:+0.00;-0.00}m" : "";
                string label      = stationStr + offsetStr;

                var p = new AbsteckPunkt
                {
                    PunktNr    = $"A{++idx:D3}",
                    Label      = label,
                    R_soll     = R,
                    H_soll     = H,
                    Abszisse_m = s,
                    Ordinate_m = offset,
                };
                if (st != null) BerechnePolareAbsteckung(st, p);
                result.Add(p);

                if (s >= L) break;
                station += intervall;
            }
        }
        return result;
    }

    // ── Schnurgerüst: Polygon nach außen versetzen ────────────────────────────
    public static List<AbsteckPunkt> BerechneSchnurgeruest(
        List<(double R, double H)> polygon, double abstand, StandpunktInfo? st)
    {
        int n = polygon.Count;
        if (n < 3) return new List<AbsteckPunkt>();

        // Vorzeichen bestimmen (Umlaufsinn)
        double signedArea = 0;
        for (int i = 0; i < n; i++)
        {
            var (r1, h1) = polygon[i];
            var (r2, h2) = polygon[(i + 1) % n];
            signedArea += (r2 - r1) * (h2 + h1);
        }
        double sign = signedArea > 0 ? -1.0 : 1.0;  // CW → sign=-1 für Außennormale

        // Kantennormalen berechnen
        var normals = new (double nx, double ny)[n];
        for (int i = 0; i < n; i++)
        {
            var (r1, h1) = polygon[i];
            var (r2, h2) = polygon[(i + 1) % n];
            double ex = r2 - r1, ey = h2 - h1;
            double L  = Math.Sqrt(ex * ex + ey * ey);
            if (L < 1e-9) { normals[i] = (0, 0); continue; }
            normals[i] = (sign * ey / L, sign * (-ex) / L);
        }

        var result = new List<AbsteckPunkt>();
        for (int i = 0; i < n; i++)
        {
            int prev = (i + n - 1) % n;
            var (nx1, ny1) = normals[prev];
            var (nx2, ny2) = normals[i];

            double bx = nx1 + nx2, by = ny1 + ny2;
            double bLen = Math.Sqrt(bx * bx + by * by);
            if (bLen < 1e-9) { bx = nx2; by = ny2; bLen = 1.0; }
            bx /= bLen; by /= bLen;

            double cosHalf = nx1 * bx + ny1 * by;
            double miter   = abstand / Math.Max(Math.Abs(cosHalf), 0.17);  // max 80° Gehrung

            var p = new AbsteckPunkt
            {
                PunktNr = $"SG{i + 1:D2}",
                Label   = $"Ecke {i + 1}",
                R_soll  = polygon[i].R + miter * bx,
                H_soll  = polygon[i].H + miter * by,
            };
            if (st != null) BerechnePolareAbsteckung(st, p);
            result.Add(p);
        }
        return result;
    }

    // ── Rasterabsteckung ──────────────────────────────────────────────────────
    public static List<AbsteckPunkt> BerechneRaster(
        double R0, double H0, double richtung_gon,
        double dS_m, double dQ_m, int nRows, int nCols, StandpunktInfo? st)
    {
        double phi = richtung_gon * GON2RAD;
        double ux  = Math.Sin(phi), uy =  Math.Cos(phi);  // Hauptrichtung
        double vx  = Math.Cos(phi), vy = -Math.Sin(phi);  // Rechtsquerrichtung

        var result = new List<AbsteckPunkt>();
        for (int row = 0; row < nRows; row++)
        {
            for (int col = 0; col < nCols; col++)
            {
                string label = $"{(char)('A' + row)}{col + 1}";
                var p = new AbsteckPunkt
                {
                    PunktNr = label,
                    Label   = label,
                    R_soll  = R0 + row * dS_m * ux + col * dQ_m * vx,
                    H_soll  = H0 + row * dS_m * uy + col * dQ_m * vy,
                };
                if (st != null) BerechnePolareAbsteckung(st, p);
                result.Add(p);
            }
        }
        return result;
    }

    // ── Profilabsteckung: Achspunkte + Böschungsberechnung ───────────────────
    public static List<ProfilAbsteckPunkt> BerechneProfilpunkte(
        double R_A, double H_A, double R_E, double H_E,
        double intervall, List<double> H_plan_liste,
        double planumHalbbreite, double boeschNeigung, StandpunktInfo? st)
    {
        double dR = R_E - R_A, dH = H_E - H_A;
        double L  = Math.Sqrt(dR * dR + dH * dH);
        if (L < 1e-6) return new List<ProfilAbsteckPunkt>();

        double ux = dR / L, uy = dH / L;
        var result = new List<ProfilAbsteckPunkt>();

        int nStationen = (int)Math.Round(L / intervall) + 1;
        for (int i = 0; i < nStationen; i++)
        {
            double station = Math.Min(i * intervall, L);
            double hPlan   = i < H_plan_liste.Count ? H_plan_liste[i] : 0.0;
            double R       = R_A + station * ux;
            double H       = H_A + station * uy;

            var p = new ProfilAbsteckPunkt
            {
                PunktNr   = $"P{i:D3}",
                Station_m = station,
                R         = R,
                H         = H,
                H_plan    = hPlan,
            };
            if (st != null)
            {
                double dRp = R - st.R, dHp = H - st.H;
                double tGon = Math.Atan2(dRp, dHp) * RAD2GON;
                p.Hz_soll_gon = ((tGon - st.Orientierung_gon) % 400.0 + 400.0) % 400.0;
                p.s_soll_m    = Math.Sqrt(dRp * dRp + dHp * dHp);
            }
            result.Add(p);
            if (station >= L) break;
        }
        return result;
    }

    // Einzelnen Profilpunkt mit Geländehöhe aktualisieren
    public static void AktualisiereProfilPunkt(
        ProfilAbsteckPunkt p, double hGelaende,
        double planumHalbbreite, double boeschNeigung)
    {
        p.H_Gelaende    = hGelaende;
        p.DeltaH_m      = p.H_plan - hGelaende;  // + = Auftrag, – = Aushub
        double abstand  = planumHalbbreite + Math.Abs(p.DeltaH_m.Value) * boeschNeigung;
        p.BoeschLinks_m  = abstand;
        p.BoeschRechts_m = abstand;
    }

    // ── Normierung 0..400 gon ─────────────────────────────────────────────────
    public static double Norm400(double gon) => ((gon % 400.0) + 400.0) % 400.0;
}
