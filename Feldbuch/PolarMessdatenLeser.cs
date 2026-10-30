// Zweck:    Liest die Messdaten-Datei ({Projekt}-dat.csv) für das Polarmodul – alte Dateien
//           mit 7 Spalten und neue mit 11 Spalten (Exzentrizitäten, Standpunkt, Typ).
//           Die Datei wird nicht verändert (Rückschreiben: MessdatenCSV.SchreibeExzentrizitaeten).
// Hardware: keine
// Autor:    Johann Gerner

using System.Globalization;
using System.Text;

namespace Feldbuch;

/// <summary>Eine Messzeile der dat.csv.</summary>
public class PolarMessung
{
    /// <summary>0-basierter Zeilenindex in der Datei (für das Rückschreiben).</summary>
    public int    ZeilenIndex   { get; set; }
    public string PunktNr       { get; set; } = "";
    public double Hz_gon        { get; set; }
    public double V_gon         { get; set; }
    public double Strecke_m     { get; set; }
    public double Zielhoehe_m   { get; set; }
    public string Code          { get; set; } = "";
    public string Zeitstempel   { get; set; } = "";
    public double ExzLaengs_m   { get; set; }
    public double ExzQuer_m     { get; set; }
    public string StandpunktNr  { get; set; } = "";
    /// <summary>"AP" (Anschlusspunkt) oder "NP" (Neupunkt).</summary>
    public string Typ           { get; set; } = MessdatenCSV.TYP_NEUPUNKT;
    /// <summary>True, wenn der Typ nicht in der Datei stand, sondern aus den Koordinaten abgeleitet wurde.</summary>
    public bool   TypGeschaetzt { get; set; }

    public bool IstAnschlusspunkt => Typ == MessdatenCSV.TYP_ANSCHLUSSPUNKT;
}

/// <summary>Inhalt einer dat.csv.</summary>
public class PolarMessdaten
{
    public string             Pfad              { get; set; } = "";
    public string             KopfStandpunktNr  { get; set; } = "";
    public double             KopfInstrHoehe    { get; set; }
    public List<PolarMessung> Messungen         { get; } = new();

    /// <summary>Standpunkte in Reihenfolge ihres ersten Auftretens.</summary>
    public List<string> Standpunkte =>
        Messungen.Select(m => m.StandpunktNr)
                 .Where(s => !string.IsNullOrEmpty(s))
                 .Distinct(StringComparer.Ordinal)
                 .ToList();
}

public static class PolarMessdatenLeser
{
    private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    // Spaltenindizes (siehe MessdatenCSV)
    private const int SPALTE_PUNKTNR  = 0;
    private const int SPALTE_HZ       = 1;
    private const int SPALTE_V        = 2;
    private const int SPALTE_STRECKE  = 3;
    private const int SPALTE_ZIELH    = 4;
    private const int SPALTE_CODE     = 5;
    private const int SPALTE_ZEIT     = 6;
    private const int SPALTE_EXZ_L    = 7;
    private const int SPALTE_EXZ_Q    = 8;
    private const int SPALTE_SP       = 9;
    private const int SPALTE_TYP      = 10;

    private const string KOMMENTAR_STATIONIERUNG = "# STATIONIERUNG:";
    private const string KOMMENTAR_SP_SCHLUESSEL = "SP=";

    /// <summary>Liest eine Datei. <paramref name="istKoordinatenpunkt"/> leitet den Typ für alte Dateien ab.</summary>
    public static PolarMessdaten Lese(string pfad, Func<string, bool>? istKoordinatenpunkt = null)
    {
        var daten = Parse(File.ReadAllLines(pfad, Encoding.UTF8), istKoordinatenpunkt);
        daten.Pfad = pfad;
        return daten;
    }

    /// <summary>Parst die Zeilen einer dat.csv (testbar, ohne Dateizugriff).</summary>
    public static PolarMessdaten Parse(IReadOnlyList<string> zeilen, Func<string, bool>? istKoordinatenpunkt = null)
    {
        var daten = new PolarMessdaten();

        string sektion  = "";
        bool   datenKopf = false;
        var kommentare = new List<(int Index, string Sp)>();

        for (int i = 0; i < zeilen.Count; i++)
        {
            string line = zeilen[i].Trim();
            if (line.Length == 0 || line == "---") continue;

            if (line.StartsWith(KOMMENTAR_STATIONIERUNG, StringComparison.OrdinalIgnoreCase))
            {
                string sp = LeseSpAusKommentar(line);
                if (sp.Length > 0) kommentare.Add((i, sp));
                continue;
            }

            if (line.StartsWith('#'))
            {
                sektion   = line.TrimStart('#').Trim().ToUpperInvariant();
                datenKopf = false;
                continue;
            }

            if (sektion == "METADATA")
            {
                int dp = line.IndexOf(':');
                if (dp < 0) continue;
                string key = line[..dp].Trim().ToLowerInvariant();
                string val = line[(dp + 1)..].Trim();
                if (key == "standpunktnr") daten.KopfStandpunktNr = val;
                else if (key == "instrumentenhoehe" && double.TryParse(val, NumberStyles.Float, IC, out double ih))
                    daten.KopfInstrHoehe = ih;
                continue;
            }

            if (sektion != "DATEN") continue;
            if (!datenKopf) { datenKopf = true; continue; }      // Spaltenzeile überspringen

            var m = LeseZeile(i, line, istKoordinatenpunkt);
            if (m != null) daten.Messungen.Add(m);
        }

        WeiseStandpunkteZu(daten, kommentare);
        return daten;
    }

    // ── Hilfsfunktionen ───────────────────────────────────────────────────────

    private static PolarMessung? LeseZeile(int index, string line, Func<string, bool>? istKoordinatenpunkt)
    {
        string[] t = line.Split(';').Select(s => s.Trim()).ToArray();
        if (t.Length <= SPALTE_ZIELH) return null;

        if (!Zahl(t[SPALTE_HZ], out double hz) || !Zahl(t[SPALTE_V], out double v) ||
            !Zahl(t[SPALTE_STRECKE], out double s))
            return null;

        Zahl(t[SPALTE_ZIELH], out double zh);

        var m = new PolarMessung
        {
            ZeilenIndex = index,
            PunktNr     = t[SPALTE_PUNKTNR],
            Hz_gon      = hz,
            V_gon       = v,
            Strecke_m   = s,
            Zielhoehe_m = zh,
            Code        = Feld(t, SPALTE_CODE),
            Zeitstempel = Feld(t, SPALTE_ZEIT),
            StandpunktNr = Feld(t, SPALTE_SP)
        };

        if (Zahl(Feld(t, SPALTE_EXZ_L), out double eL)) m.ExzLaengs_m = eL;
        if (Zahl(Feld(t, SPALTE_EXZ_Q), out double eQ)) m.ExzQuer_m   = eQ;

        string typ = Feld(t, SPALTE_TYP).ToUpperInvariant();
        if (typ == MessdatenCSV.TYP_ANSCHLUSSPUNKT || typ == MessdatenCSV.TYP_NEUPUNKT)
        {
            m.Typ = typ;
        }
        else
        {
            m.TypGeschaetzt = true;
            m.Typ = istKoordinatenpunkt != null && istKoordinatenpunkt(m.PunktNr)
                ? MessdatenCSV.TYP_ANSCHLUSSPUNKT
                : MessdatenCSV.TYP_NEUPUNKT;
        }
        return m;
    }

    // Alte Dateien ohne StandpunktNr-Spalte: Anschlusspunkte gehören zum nächsten
    // "# STATIONIERUNG"-Kommentar (er wird nach der Berechnung geschrieben), Neupunkte zum vorherigen.
    private static void WeiseStandpunkteZu(PolarMessdaten daten, List<(int Index, string Sp)> kommentare)
    {
        foreach (var m in daten.Messungen)
        {
            if (m.StandpunktNr.Length > 0) continue;

            string? davor = null, danach = null;
            foreach (var k in kommentare)
            {
                if (k.Index < m.ZeilenIndex) davor = k.Sp;
                else if (danach == null)     danach = k.Sp;
            }

            m.StandpunktNr = m.IstAnschlusspunkt
                ? danach ?? davor ?? daten.KopfStandpunktNr
                : davor ?? daten.KopfStandpunktNr;
        }
    }

    private static string LeseSpAusKommentar(string zeile)
    {
        int i = zeile.IndexOf(KOMMENTAR_SP_SCHLUESSEL, StringComparison.OrdinalIgnoreCase);
        if (i < 0) return "";
        string rest = zeile[(i + KOMMENTAR_SP_SCHLUESSEL.Length)..];
        int ende = rest.IndexOfAny([' ', '\t']);
        return (ende < 0 ? rest : rest[..ende]).Trim();
    }

    private static string Feld(string[] t, int index) => index < t.Length ? t[index] : "";

    private static bool Zahl(string s, out double wert)
        => double.TryParse(s.Replace(',', '.'), NumberStyles.Float, IC, out wert);
}
