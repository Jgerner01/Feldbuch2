namespace Feldbuch;

using System.Xml;

// ──────────────────────────────────────────────────────────────────────────────
// RechenparameterManager – liest und schreibt Freie-Stationierung.xml
//
// Enthält:
//   • Fehlergrenzen (Winkel [cc], Strecke [mm], Höhe [mm])
//   • FreierMassstab (true = Maßstab als freier Parameter, false = m=1 fixiert)
// ──────────────────────────────────────────────────────────────────────────────
public static class RechenparameterManager
{
    private static string _path = "";
    private static Rechenparameter _params = new();

    public static Rechenparameter Params => _params;

    // ── Initialisierung ───────────────────────────────────────────────────────
    public static void Initialize(string path)
    {
        _path = path;
        if (!File.Exists(path))
        {
            _params = new Rechenparameter();
            Save();
        }
        else
        {
            Load();
        }
    }

    // ── Laden aus XML ─────────────────────────────────────────────────────────
    static void Load()
    {
        try
        {
            var doc = new XmlDocument();
            doc.Load(_path);
            var root = doc.DocumentElement!;

            _params = new Rechenparameter
            {
                FehlergrenzCC_Winkel       = ReadDouble(root, "FehlergrenzCC_Winkel",       20.0),
                FehlergrenzeMM_Strecke     = ReadDouble(root, "FehlergrenzeMM_Strecke",     10.0),
                FehlergrenzeMM_Hoehe       = ReadDouble(root, "FehlergrenzeMM_Hoehe",       10.0),
                FehlergrenzeMM_s0AutoMatch = ReadDouble(root, "FehlergrenzeMM_s0AutoMatch", 20.0),
                FreierMassstab             = ReadBool  (root, "FreierMassstab",             true),
                Berechnung3D               = ReadBool  (root, "Berechnung3D",               true),
                SigmaFaktor                = (int)ReadDouble(root, "SigmaFaktor",           1),
                PolarMassstabModus         = ReadModus (root, "PolarMassstabModus",         MassstabModus.Eins),
                PolarMassstabWert          = ReadDouble(root, "PolarMassstabWert",          1.0),
                PolarMassstabQuelle        = ReadString(root, "PolarMassstabQuelle",        "manuell"),
                GewichtungNaheZiele        = ReadBool  (root, "GewichtungNaheZiele",        true),
                ZentrierfehlerZiel_mm      = ReadDouble(root, "ZentrierfehlerZiel_mm",      Rechenparameter.ZENTRIERFEHLER_ZIEL_STANDARD_MM),
                ZentrierfehlerStandpunkt_mm = ReadDouble(root, "ZentrierfehlerStandpunkt_mm", Rechenparameter.ZENTRIERFEHLER_STANDPUNKT_STANDARD_MM)
            };
        }
        catch (Exception ex)
        {
            ErrorLogger.Log("RechenparameterManager.Load", ex);
            _params = new Rechenparameter();   // Standardwerte bei Fehler
        }
    }

    // ── Speichern in XML ──────────────────────────────────────────────────────
    public static void Save()
    {
        if (string.IsNullOrEmpty(_path)) return;

        var doc  = new XmlDocument();
        var decl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
        doc.AppendChild(decl);

        var root = doc.CreateElement("RechenparameterFreieStationierung");
        doc.AppendChild(root);

        AppendElem(doc, root, "FehlergrenzCC_Winkel",       _params.FehlergrenzCC_Winkel.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
        AppendElem(doc, root, "FehlergrenzeMM_Strecke",   _params.FehlergrenzeMM_Strecke.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
        AppendElem(doc, root, "FehlergrenzeMM_Hoehe",     _params.FehlergrenzeMM_Hoehe.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
        AppendElem(doc, root, "FehlergrenzeMM_s0AutoMatch", _params.FehlergrenzeMM_s0AutoMatch.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
        AppendElem(doc, root, "FreierMassstab",            _params.FreierMassstab ? "true" : "false");
        AppendElem(doc, root, "Berechnung3D",              _params.Berechnung3D   ? "true" : "false");
        AppendElem(doc, root, "SigmaFaktor",               SigmaFaktor.ToString());
        var ic = System.Globalization.CultureInfo.InvariantCulture;
        AppendElem(doc, root, "PolarMassstabModus",        _params.PolarMassstabModus.ToString());
        AppendElem(doc, root, "PolarMassstabWert",         _params.PolarMassstabWert.ToString("F6", ic));
        AppendElem(doc, root, "PolarMassstabQuelle",       _params.PolarMassstabQuelle);
        AppendElem(doc, root, "GewichtungNaheZiele",       _params.GewichtungNaheZiele ? "true" : "false");
        AppendElem(doc, root, "ZentrierfehlerZiel_mm",     _params.ZentrierfehlerZiel_mm.ToString("F1", ic));
        AppendElem(doc, root, "ZentrierfehlerStandpunkt_mm", _params.ZentrierfehlerStandpunkt_mm.ToString("F1", ic));

        doc.Save(_path);
    }

    // ── Ausgabe der Standardabweichung als 1-, 2- oder 3-fache s0 ─────────────
    /// <summary>Faktor für die Ausgabe der Standardabweichung (1, 2 oder 3).</summary>
    public static int SigmaFaktor => Math.Clamp(_params.SigmaFaktor, 1, 3);

    /// <summary>Bezeichnung: "s₀", "2·s₀" oder "3·s₀".</summary>
    public static string SigmaBez => SigmaFaktor == 1 ? "s₀" : $"{SigmaFaktor}·s₀";

    /// <summary>Wert in mm, mit dem eingestellten Faktor multipliziert.</summary>
    public static double SigmaWert(double s0_mm) => s0_mm * SigmaFaktor;

    /// <summary>z. B. "2·s₀=31.2 mm" (Anzeige, aktuelle Kultur).</summary>
    public static string SigmaText(double s0_mm, string format = "F1")
        => $"{SigmaBez}={SigmaWert(s0_mm).ToString(format)} mm";

    /// <summary>Schreibt die Standardabweichung (mit Faktor) in die Projektdaten.</summary>
    public static void SchreibeSigmaProjektdaten(double s0_mm)
        => ProjektdatenManager.SetValue("Freie Stationierung", "Standardabweichung",
            $"{SigmaFaktor}σ = {SigmaWert(s0_mm).ToString("F2", System.Globalization.CultureInfo.InvariantCulture)} mm");

    // ── Hilfsmethoden ─────────────────────────────────────────────────────────
    static double ReadDouble(XmlElement root, string tag, double def)
    {
        var node = root.SelectSingleNode(tag);
        if (node == null) return def;
        return double.TryParse(node.InnerText,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : def;
    }

    static string ReadString(XmlElement root, string tag, string def)
    {
        var node = root.SelectSingleNode(tag);
        return node == null || string.IsNullOrWhiteSpace(node.InnerText) ? def : node.InnerText.Trim();
    }

    static MassstabModus ReadModus(XmlElement root, string tag, MassstabModus def)
    {
        var node = root.SelectSingleNode(tag);
        return node != null && Enum.TryParse(node.InnerText.Trim(), true, out MassstabModus m) ? m : def;
    }

    static bool ReadBool(XmlElement root, string tag, bool def)
    {
        var node = root.SelectSingleNode(tag);
        if (node == null) return def;
        return node.InnerText.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    static void AppendElem(XmlDocument doc, XmlElement parent, string tag, string value)
    {
        var elem = doc.CreateElement(tag);
        elem.InnerText = value;
        parent.AppendChild(elem);
    }
}

// ── Rechenparameter-Datenklasse ───────────────────────────────────────────────
public class Rechenparameter
{
    /// <summary>Fehlergrenze für Winkelresiduen [cc]</summary>
    public double FehlergrenzCC_Winkel   { get; set; } = 20.0;
    /// <summary>Fehlergrenze für Streckenresiduen [mm]</summary>
    public double FehlergrenzeMM_Strecke { get; set; } = 10.0;
    /// <summary>Fehlergrenze für Höhenresiduen [mm]</summary>
    public double FehlergrenzeMM_Hoehe   { get; set; } = 10.0;
    /// <summary>s0-Schwellwert für Auto-Match-Freigabe [mm] – überschritten = Auto-Match gesperrt</summary>
    public double FehlergrenzeMM_s0AutoMatch { get; set; } = 20.0;
    /// <summary>Freier Maßstab (true) oder Maßstab fixiert auf m=1 (false)</summary>
    public bool   FreierMassstab         { get; set; } = true;
    /// <summary>3D-Berechnung inkl. Höhen (true) oder rein 2D (false)</summary>
    public bool   Berechnung3D           { get; set; } = true;
    /// <summary>Ausgabe der Standardabweichung: 1-, 2- oder 3-fach (nur Anzeige/Protokoll)</summary>
    public int    SigmaFaktor            { get; set; } = 1;

    // ── Polarpunkte ───────────────────────────────────────────────────────────
    public const double ZENTRIERFEHLER_ZIEL_STANDARD_MM       = 2.0;
    public const double ZENTRIERFEHLER_STANDPUNKT_STANDARD_MM = 0.0;

    /// <summary>Maßstab der Polarstrecken: 1 (Standard), aus der Stationierung berechnet oder vorgegeben</summary>
    public MassstabModus PolarMassstabModus { get; set; } = MassstabModus.Eins;
    /// <summary>Vorgegebener Maßstab (Modus Vorgegeben)</summary>
    public double PolarMassstabWert         { get; set; } = 1.0;
    /// <summary>Herkunft des vorgegebenen Maßstabs: "manuell" oder "UTM"</summary>
    public string PolarMassstabQuelle       { get; set; } = "manuell";
    /// <summary>Gewichtung bei nahen Zielen (Zentrierfehler-Modell) – wirkt nur im Polarmodul</summary>
    public bool   GewichtungNaheZiele       { get; set; } = true;
    /// <summary>Zentrier-/Lotfehler am Ziel [mm]</summary>
    public double ZentrierfehlerZiel_mm     { get; set; } = ZENTRIERFEHLER_ZIEL_STANDARD_MM;
    /// <summary>Zentrierfehler am Standpunkt [mm] (nur bei bekanntem Standpunkt wirksam)</summary>
    public double ZentrierfehlerStandpunkt_mm { get; set; } = ZENTRIERFEHLER_STANDPUNKT_STANDARD_MM;
}
