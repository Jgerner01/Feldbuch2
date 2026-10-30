// Zweck:    Übernimmt gemessene Neupunkte (Neupunkte.json) als feste Koordinaten nach
//           Feldbuchpunkte.json (Typ "Neupunkt", Quelle "Lokal"). Dort werden sie von der
//           Anschlusspunkt-Suche (Freie Stationierung, Polarpunkte) gefunden und bleiben
//           unverändert, auch wenn Neupunkte.json bei geänderter Stationierung neu berechnet wird.
// Hardware: keine
// Autor:    Johann Gerner

namespace Feldbuch;

public static class FestpunktUebernahme
{
    public const string TYP_NEUPUNKT  = "Neupunkt";
    public const string QUELLE_LOKAL  = "Lokal";
    private const string DATUM_FORMAT = "yyyy-MM-dd";

    /// <summary>Ergebnis einer Übernahme: Anzahl neu angelegter und ersetzter Festpunkte.</summary>
    public record UebernahmeErgebnis(int Neu, int Ersetzt);

    /// <summary>Gibt es unter dieser Nummer bereits einen Festpunkt (Typ Neupunkt)?</summary>
    public static bool Existiert(string punktNr)
        => FeldbuchpunkteManager.Punkte.Any(p =>
               p.Typ == TYP_NEUPUNKT &&
               string.Equals(p.PunktNr, punktNr, StringComparison.Ordinal));

    /// <summary>Gemessene Neupunkte, die übernommen werden können (Koordinaten vorhanden).</summary>
    public static IEnumerable<NeupunktErgebnis> Kandidaten()
        => NeupunkteManager.Koordinaten
            .Where(e => !string.IsNullOrWhiteSpace(e.PunktNr) && (e.R != 0 || e.H != 0))
            .OrderBy(e => e.PunktNr, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.StandpunktNr, StringComparer.OrdinalIgnoreCase);

    /// <summary>Kopiert die Koordinaten der übergebenen Neupunkte nach Feldbuchpunkte.json.</summary>
    public static UebernahmeErgebnis Uebernehme(IEnumerable<NeupunktErgebnis> punkte)
    {
        int neu = 0, ersetzt = 0;
        string datum = DateTime.Now.ToString(DATUM_FORMAT);

        foreach (var e in punkte)
        {
            if (Existiert(e.PunktNr)) ersetzt++; else neu++;

            FeldbuchpunkteManager.AddOrUpdate(new FeldbuchPunkt
            {
                PunktNr         = e.PunktNr,
                Typ             = TYP_NEUPUNKT,
                R               = e.R,
                H               = e.H,
                Hoehe           = e.Hoehe,
                IstBerechnung3D = e.Ist3D,
                Datum           = datum,
                Quelle          = QUELLE_LOKAL
            });
        }
        return new UebernahmeErgebnis(neu, ersetzt);
    }
}
