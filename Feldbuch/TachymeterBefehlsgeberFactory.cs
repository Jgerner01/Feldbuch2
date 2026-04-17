namespace Feldbuch;

// ══════════════════════════════════════════════════════════════════════════════
// TachymeterBefehlsgeberFactory  –  erzeugt Befehlsgeber + Parser je Modell
// ══════════════════════════════════════════════════════════════════════════════
public static class TachymeterBefehlsgeberFactory
{
    /// <summary>Erstellt den passenden Befehlsgeber für das gegebene Tachymeter-Modell.</summary>
    public static ITachymeterBefehlsgeber Erstellen(TachymeterModell modell) => modell switch
    {
        TachymeterModell.SokkiaSDR => new SokkiaSDRBefehlsgeber(),
        TachymeterModell.TopconGTS => new TopconBefehlsgeber(),
        _                          => new GeoCOMBefehlsgeber()
    };

    /// <summary>Erstellt den passenden Parser für das gegebene Tachymeter-Modell.</summary>
    public static ITachymeterDatenParser ErzeugeParser(TachymeterModell modell) => modell switch
    {
        TachymeterModell.SokkiaSDR => new SokkiaSDRParser(),
        TachymeterModell.TopconGTS => new TopconParser(),
        _                          => new GeoCOMParser()
    };

    /// <summary>True wenn das Modell das Leica GeoCOM-Protokoll verwendet.</summary>
    public static bool IstGeoCOM(TachymeterModell modell) => modell switch
    {
        TachymeterModell.SokkiaSDR => false,
        TachymeterModell.TopconGTS => false,
        TachymeterModell.GnssNmea  => false,
        TachymeterModell.Manuell   => false,
        _                          => true
    };
}
