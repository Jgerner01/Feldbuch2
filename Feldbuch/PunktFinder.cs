namespace Feldbuch;

// ══════════════════════════════════════════════════════════════════════════════
// PunktFinder  –  Kern-Algorithmus zur automatischen Punktidentifikation
//
// Sucht in DxfPunktIndex nach dem Anschlusspunkt, der am besten zur
// vorhergesagten Position (aus vorläufiger Station + Messung) passt.
//
// Intern Einheiten: Meter, Gon.
// ══════════════════════════════════════════════════════════════════════════════
public record PunktFinderTreffer(
    string PunktNr,
    double R,
    double H,
    double Abstand_m,
    bool   AutoMatch        // true: Abstand ≤ r_suche / 2 → kein Dialog
);

public class PunktFinderKonfig
{
    public double MindestRadius_m    { get; set; } = 0.10;
    public double MaximalRadius_m    { get; set; } = 2.00;
    public double SicherheitsFaktor  { get; set; } = 3.0;
    public double SigmaTheta_2Pkt_cc { get; set; } = 15.0;
    public double SigmaTheta_nPkt_cc { get; set; } =  5.0;
    public double WinkelToleranz_cc  { get; set; } = 15.0;
    public bool   DistanzPflicht     { get; set; } = true;
}

public static class PunktFinder
{
    private const double GON2RAD      = Math.PI / 200.0;
    private const double CC2RAD       = Math.PI / 2_000_000.0;

    /// <summary>True wenn Stationierungsqualität Auto-Match erlaubt.</summary>
    public static bool IstBrauchbar(double s0_mm, int redundanz, double grenzeMM = 20.0)
        => redundanz > 0 && s0_mm <= grenzeMM;

    // ── Vorhergesagte Position aus Station + Messung ──────────────────────────

    /// <summary>
    /// Berechnet die erwartete Zielposition.
    /// Maßstabskorrektur wird angewendet wenn massstab ≠ 1.0.
    /// </summary>
    public static (double E, double N) BerechnePriorPosition(
        double stationE, double stationN, double orientierung_gon,
        double hz_gon,   double v_gon,   double schraegestrecke_m,
        double massstab = 1.0)
    {
        // Horizontalstrecke + Maßstabskorrektur
        double d_h_roh = Math.Sin(v_gon * GON2RAD) * schraegestrecke_m;
        double d_h     = massstab == 1.0 || massstab <= 0 ? d_h_roh : d_h_roh / massstab;

        // Gitterrichtung
        double alpha = ((orientierung_gon + hz_gon) % 400.0 + 400.0) % 400.0;

        double e_pred = stationE + d_h * Math.Sin(alpha * GON2RAD);
        double n_pred = stationN + d_h * Math.Cos(alpha * GON2RAD);
        return (e_pred, n_pred);
    }

    // ── Suchradius ────────────────────────────────────────────────────────────

    public static double BerechneRadius(
        double s0_mm, int nPunkte, double horizontalstrecke_m,
        PunktFinderKonfig konfig)
    {
        double sigma_th_cc  = nPunkte <= 2 ? konfig.SigmaTheta_2Pkt_cc : konfig.SigmaTheta_nPkt_cc;
        double sigma_th_rad = sigma_th_cc * CC2RAD;
        double sigma_stat   = Math.Max(s0_mm / 1000.0, 0.005);
        double sigma_quer   = horizontalstrecke_m * sigma_th_rad;
        double sigma_ges    = Math.Sqrt(sigma_stat * sigma_stat + sigma_quer * sigma_quer);
        double r            = konfig.SicherheitsFaktor * sigma_ges;
        return Math.Clamp(r, konfig.MindestRadius_m, konfig.MaximalRadius_m);
    }

    // ── Suche nach Position in ImportPunkten (Vollmessung) ───────────────────

    public static List<PunktFinderTreffer> SucheNachPosition(
        double e_pred, double n_pred, double r_suche,
        IReadOnlyList<ImportPunkt> punkte,
        HashSet<string> bereitsGemessen)
    {
        double r2     = r_suche * r_suche;
        var    result = new List<PunktFinderTreffer>();
        foreach (var p in punkte)
        {
            if (bereitsGemessen.Contains(p.PunktNr)) continue;
            double dr   = p.R - e_pred;
            double dh   = p.H - n_pred;
            double abst2 = dr * dr + dh * dh;
            if (abst2 > r2) continue;
            double abst = Math.Sqrt(abst2);
            result.Add(new PunktFinderTreffer(p.PunktNr, p.R, p.H, abst,
                abst <= r_suche / 2.0));
        }
        return result.OrderBy(t => t.Abstand_m).ToList();
    }

    // ── Suche nach Richtung in ImportPunkten (Winkel-only, kein Auto-Match) ──

    public static List<PunktFinderTreffer> SucheNachRichtung(
        double stationE, double stationN, double richtung_gon,
        IReadOnlyList<ImportPunkt> punkte,
        HashSet<string> bereitsGemessen,
        PunktFinderKonfig konfig)
    {
        double toleranz_rad = konfig.WinkelToleranz_cc * CC2RAD;
        double alpha_rad    = richtung_gon * GON2RAD;

        var result = new List<PunktFinderTreffer>();
        foreach (var p in punkte)
        {
            if (bereitsGemessen.Contains(p.PunktNr)) continue;
            double dr   = p.R - stationE;
            double dh   = p.H - stationN;
            double dist = Math.Sqrt(dr * dr + dh * dh);
            if (dist < 0.5 || dist > 500.0) continue;
            double alpha_p = Math.Atan2(dr, dh);
            if (Math.Abs(NormRad(alpha_rad - alpha_p)) <= toleranz_rad)
                result.Add(new PunktFinderTreffer(p.PunktNr, p.R, p.H, dist, false));
        }
        return result.OrderBy(t => t.Abstand_m).ToList();

        static double NormRad(double a)
        {
            while (a >  Math.PI) a -= 2 * Math.PI;
            while (a < -Math.PI) a += 2 * Math.PI;
            return a;
        }
    }

    // ── Suche nach Position in DxfPunktIndex (Fallback) ─────────────────────

    public static List<PunktFinderTreffer> SucheNachPosition(
        double e_pred, double n_pred, double r_suche,
        DxfPunktIndex dxfIndex,
        HashSet<string> bereitsGemessen)
    {
        var kandidaten = dxfIndex.SucheNahe(e_pred, n_pred, r_suche);
        var result = new List<PunktFinderTreffer>();
        foreach (var k in kandidaten)
        {
            if (bereitsGemessen.Contains(k.PunktNr)) continue;
            double dr   = k.R - e_pred;
            double dh   = k.H - n_pred;
            double abst = Math.Sqrt(dr * dr + dh * dh);
            result.Add(new PunktFinderTreffer(k.PunktNr, k.R, k.H, abst,
                abst <= r_suche / 2.0));
        }
        return result.OrderBy(t => t.Abstand_m).ToList();
    }

    // ── Suche nach Richtung in DxfPunktIndex (Fallback) ─────────────────────

    public static List<PunktFinderTreffer> SucheNachRichtung(
        double stationE, double stationN, double richtung_gon,
        DxfPunktIndex dxfIndex,
        HashSet<string> bereitsGemessen,
        PunktFinderKonfig konfig)
    {
        double toleranz_gon = konfig.WinkelToleranz_cc / 100.0;
        var kandidaten = dxfIndex.SucheNachRichtung(
            stationE, stationN, richtung_gon, toleranz_gon);
        var result = new List<PunktFinderTreffer>();
        foreach (var (pkt, dist) in kandidaten)
        {
            if (bereitsGemessen.Contains(pkt.PunktNr)) continue;
            result.Add(new PunktFinderTreffer(pkt.PunktNr, pkt.R, pkt.H, dist, false));
        }
        return result;
    }
}
