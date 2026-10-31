namespace Feldbuch;

public static class StationierungsQualitaet
{
    public static bool IstVerwendbar(StationierungsErgebnis? e) =>
        e is { Konvergiert: true }
        && double.IsFinite(e.R) && double.IsFinite(e.H) && double.IsFinite(e.Hoehe)
        && double.IsFinite(e.Orientierung_gon) && double.IsFinite(e.Massstab) && e.Massstab > 0
        && double.IsFinite(e.s0_mm) && e.s0_mm >= 0;

    public static void Pruefe(StationierungsErgebnis e)
    {
        if (!IstVerwendbar(e))
            throw new InvalidOperationException("Die Stationierung ist nicht konvergiert oder enthält ungültige Werte. "
                + "Bitte Anschlusspunkte und Messungen prüfen.");
    }
}
