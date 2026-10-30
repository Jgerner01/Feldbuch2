// Zweck:    Reine Rechenlogik der Polarpunktberechnung: Lage mit Maßstab und
//           Exzentrizitäten, Maßstabsermittlung nach Rechenparameter und
//           Orientierung aus einem einzelnen Anschlussziel bei bekanntem Standpunkt.
//           Keine Abhängigkeit zu Formularen oder Dateien -> vollständig testbar.
// Hardware: keine
// Autor:    Johann Gerner

namespace Feldbuch;

/// <summary>Auswahl des Maßstabs, der bei Polarpunkten auf die Horizontalstrecke wirkt.</summary>
public enum MassstabModus
{
    /// <summary>Kein Maßstab (m = 1) – Standard.</summary>
    Eins,
    /// <summary>Maßstab der Stationierung (Helmert/Ausgleichung des Standpunkts).</summary>
    Berechnet,
    /// <summary>Fest vorgegebener Wert (von Hand oder später aus UTM-Funktion).</summary>
    Vorgegeben
}

/// <summary>Verwendeter Maßstab samt Hinweistext (leer, wenn m = 1 und nichts zu melden ist).</summary>
public record MassstabErgebnis(double Wert, string Hinweis);

public static class PolarpunktRechner
{
    private const double GON2RAD = Math.PI / 200.0;
    private const double VOLLKREIS_GON = 400.0;

    /// <summary>Exzentrizitäten, die betragsmäßig größer sind, lösen eine Information aus [m].</summary>
    public const double EXZ_INFO_GRENZE_M = 2.0;

    /// <summary>Plausibler Bereich für Maßstäbe.</summary>
    public const double MASSSTAB_MIN = 0.9;
    public const double MASSSTAB_MAX = 1.1;

    /// <summary>Betrag, unter dem ein Maßstab als 1 gilt.</summary>
    public const double MASSSTAB_EINS_TOLERANZ = 1e-9;

    /// <summary>Unterhalb dieser Anzahl Anschlusspunkte wird eine Information ausgegeben.</summary>
    public const int MIN_ANSCHLUSSPUNKTE_INFO = 3;

    // ── Hilfsprüfungen ────────────────────────────────────────────────────────

    public static bool IstMassstabEins(double m) => Math.Abs(m - 1.0) < MASSSTAB_EINS_TOLERANZ;

    public static bool MassstabPlausibel(double m) => m > MASSSTAB_MIN && m < MASSSTAB_MAX;

    /// <summary>True, wenn Längs- oder Querexzentrizität betragsmäßig größer als 2 m ist.</summary>
    public static bool ExzentrizitaetGross(double eLaengs, double eQuer)
        => Math.Abs(eLaengs) > EXZ_INFO_GRENZE_M || Math.Abs(eQuer) > EXZ_INFO_GRENZE_M;

    /// <summary>Abweichung beim Überschreiben eines vorhandenen Polarpunkts, ab der nachgefragt wird [m].</summary>
    public const double UEBERSCHREIBEN_RUECKFRAGE_M = 0.05;

    /// <summary>Abweichung zu einem Festpunkt gleicher Nummer, ab der ein Hinweis erscheint [m].</summary>
    public const double FESTPUNKT_ABWEICHUNG_INFO_M = 0.05;

    /// <summary>Horizontaler Abstand aus Koordinatendifferenzen [m].</summary>
    public static double Abstand(double dR, double dH) => Math.Sqrt(dR * dR + dH * dH);

    // ── Lage ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lagekoordinaten eines Polarpunkts.
    /// <para>t = orientierter Richtungswinkel (Messrichtung) [gon], d = Horizontalstrecke zum Reflektor.</para>
    /// <para>eLaengs positiv = weiter vom Standpunkt weg, eQuer positiv = rechts der Messrichtung.
    /// Der Maßstab wirkt nur auf d, nicht auf die Exzentrizitäten.</para>
    /// </summary>
    public static (double R, double H) Lage(
        double rStand, double hStand, double hzOrientiert_gon,
        double dHoriz, double massstab, double eLaengs, double eQuer)
    {
        double t   = hzOrientiert_gon * GON2RAD;
        double sin = Math.Sin(t);
        double cos = Math.Cos(t);

        double r = rStand + massstab * dHoriz * sin + eLaengs * sin + eQuer * cos;
        double h = hStand + massstab * dHoriz * cos + eLaengs * cos - eQuer * sin;
        return (r, h);
    }

    // ── Maßstab nach Rechenparameter ──────────────────────────────────────────

    /// <summary>
    /// Ermittelt den anzuwendenden Maßstab. Bei m ≠ 1 oder Rückfall auf 1 enthält das Ergebnis einen Hinweis.
    /// </summary>
    public static MassstabErgebnis ErmittleMassstab(Rechenparameter p, StationierungsErgebnis? station)
    {
        switch (p.PolarMassstabModus)
        {
            case MassstabModus.Vorgegeben:
            {
                double m = p.PolarMassstabWert;
                if (!MassstabPlausibel(m))
                    return new MassstabErgebnis(1.0,
                        $"Vorgegebener Maßstab {m.ToString("F6", Fmt)} unplausibel – m = 1 verwendet");
                if (IstMassstabEins(m)) return new MassstabErgebnis(1.0, "");
                string quelle = string.IsNullOrWhiteSpace(p.PolarMassstabQuelle) ? "vorgegeben" : $"vorgegeben, {p.PolarMassstabQuelle}";
                return new MassstabErgebnis(m, $"Berechnung mit Maßstab m = {m.ToString("F6", Fmt)} ({quelle})");
            }
            case MassstabModus.Berechnet:
            {
                if (station == null || station.IstLokalesSystem)
                    return new MassstabErgebnis(1.0, "Maßstab nicht berechenbar – m = 1 verwendet");
                double m = station.Massstab;
                if (!MassstabPlausibel(m))
                    return new MassstabErgebnis(1.0, "Maßstab nicht berechenbar – m = 1 verwendet");
                if (IstMassstabEins(m))
                    return p.FreierMassstab
                        ? new MassstabErgebnis(1.0, "")
                        : new MassstabErgebnis(1.0, "Maßstab nicht berechenbar (freier Maßstab aus) – m = 1 verwendet");
                return new MassstabErgebnis(m, $"Berechnung mit Maßstab m = {m.ToString("F6", Fmt)} (berechnet)");
            }
            default:
                return new MassstabErgebnis(1.0, "");
        }
    }

    // ── Orientierung aus einem Anschlussziel ──────────────────────────────────

    /// <summary>
    /// Orientierung bei bekanntem Standpunkt und genau einem Anschlussziel: o = t(Standpunkt→Ziel) − Hz.
    /// <para>Der Maßstab (Koordinatenstrecke / gemessene Strecke) wird mitgeliefert, sofern die Strecke aktiv ist;
    /// die Streckenabweichung entspricht (gemessen − Koordinaten).
    /// Winkel- und Querabweichung sind nicht bestimmbar (NaN).</para>
    /// Gibt null zurück, wenn die Richtung nicht aktiv oder Standpunkt und Ziel identisch sind.
    /// </summary>
    public static StationierungsErgebnis? OrientierungAusEinemZiel(
        double rStand, double hStand, double hoeheStand,
        StationierungsPunkt ziel, double instrHoehe,
        bool aktHz, bool aktStr, bool aktHoe, bool berechnung3D)
    {
        if (!aktHz) return null;

        double dR   = ziel.R - rStand;
        double dH   = ziel.H - hStand;
        double dist = Math.Sqrt(dR * dR + dH * dH);
        if (dist < 1e-9) return null;

        double tGon = Math.Atan2(dR, dH) / GON2RAD;
        double o    = Norm400(tGon - ziel.HZ);

        double dh = ziel.Strecke * Math.Sin(ziel.V * GON2RAD);
        double m  = aktStr && dh > 1e-9 ? dist / dh : 1.0;

        bool   hatHoehe = berechnung3D && aktHoe && ziel.Hoehe != 0.0;
        double hoehe    = StandpunktHoehe(hatHoehe, ziel, instrHoehe, hoeheStand, berechnung3D);

        var res = new PunktResiduum
        {
            PunktNr       = ziel.PunktNr,
            StreckeH      = dh,
            vWinkel_cc    = double.NaN,
            vQuer_mm      = double.NaN,
            vStrecke_mm   = aktStr ? (dh - dist) * 1000.0 : double.NaN,
            vHoehe_mm     = hatHoehe ? 0.0 : double.NaN,
            RichtungAktiv = true,
            StreckeAktiv  = aktStr,
            HoeheAktiv    = hatHoehe,
            GewichtRichtung = double.NaN
        };

        return new StationierungsErgebnis
        {
            R = rStand, H = hStand, Hoehe = hoehe,
            Orientierung_gon = o,
            Massstab         = m,
            s0_mm            = 0,
            Redundanz        = 0,
            Iterationen      = 0,
            Konvergiert      = true,
            Berechnung3D     = berechnung3D && hoehe != 0.0,
            Residuen         = new List<PunktResiduum> { res }
        };
    }

    // Standpunkthöhe: aus dem Anschlussziel, sonst die bekannte Höhe des Standpunkts
    private static double StandpunktHoehe(bool ausZiel, StationierungsPunkt z, double ih, double hoeheStand, bool berechnung3D)
    {
        if (!berechnung3D) return 0.0;
        if (ausZiel)
            return z.Hoehe + z.Zielhoehe - ih - z.Strecke * Math.Cos(z.V * GON2RAD);
        return hoeheStand;
    }

    private static double Norm400(double gon)
    {
        double w = gon % VOLLKREIS_GON;
        return w < 0 ? w + VOLLKREIS_GON : w;
    }

    private static readonly System.Globalization.CultureInfo Fmt = System.Globalization.CultureInfo.InvariantCulture;
}
