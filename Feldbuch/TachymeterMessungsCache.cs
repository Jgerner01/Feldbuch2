namespace Feldbuch;

// ══════════════════════════════════════════════════════════════════════════════
// TachymeterMessungsCache  –  globaler Vermittler für Tachymetermessungen
//
// Lauscht auf TachymeterVerbindung.DatenEmpfangen, parst eingehende Daten
// und hält die letzte Vollmessung / Winkelmessung vor.
//
// Initialisierung einmal in Program.cs über Initialisieren().
// Alle anderen Komponenten abonnieren NeueVollmessung / NeueWinkelmessung.
// ══════════════════════════════════════════════════════════════════════════════
public static class TachymeterMessungsCache
{
    public static TachymeterMessung?           LetzteVollmessung   { get; private set; }
    public static TachymeterMessung?           LetzteWinkelmessung { get; private set; }

    public static event Action<TachymeterMessung>? NeueVollmessung;
    public static event Action<TachymeterMessung>? NeueWinkelmessung;

    public static void Initialisieren()
    {
        TachymeterVerbindung.DatenEmpfangen += OnDatenEmpfangen;
    }

    private static void OnDatenEmpfangen(object? sender, string roh)
    {
        var parser = TachymeterBefehlsgeberFactory.ErzeugeParser(TachymeterVerbindung.Modell);

        foreach (var zeile in ZeilenAus(roh))
        {
            var m = parser.ParseZeile(zeile);
            if (m == null) continue;

            if (m.IstVollmessung)
            {
                LetzteVollmessung   = m;
                LetzteWinkelmessung = m;
                NeueVollmessung?.Invoke(m);
            }
            else if (m.HatWinkel)
            {
                LetzteWinkelmessung = m;
                NeueWinkelmessung?.Invoke(m);
            }
        }
    }

    private static IEnumerable<string> ZeilenAus(string text)
    {
        int start = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\r' || c == '\n')
            {
                if (i > start)
                    yield return text[start..i];
                // CRLF: überspring LF
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    i++;
                start = i + 1;
            }
        }
        if (start < text.Length)
            yield return text[start..];
    }
}
