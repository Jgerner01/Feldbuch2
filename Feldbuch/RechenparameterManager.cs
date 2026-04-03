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
                FehlergrenzCC_Winkel    = ReadDouble(root, "FehlergrenzCC_Winkel",    20.0),
                FehlergrenzeMM_Strecke  = ReadDouble(root, "FehlergrenzeMM_Strecke",  10.0),
                FehlergrenzeMM_Hoehe    = ReadDouble(root, "FehlergrenzeMM_Hoehe",    10.0),
                FreierMassstab          = ReadBool  (root, "FreierMassstab",          true),
                Berechnung3D            = ReadBool  (root, "Berechnung3D",            true)
            };
        }
        catch
        {
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

        AppendElem(doc, root, "FehlergrenzCC_Winkel",   _params.FehlergrenzCC_Winkel.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
        AppendElem(doc, root, "FehlergrenzeMM_Strecke", _params.FehlergrenzeMM_Strecke.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
        AppendElem(doc, root, "FehlergrenzeMM_Hoehe",   _params.FehlergrenzeMM_Hoehe.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
        AppendElem(doc, root, "FreierMassstab",          _params.FreierMassstab ? "true" : "false");
        AppendElem(doc, root, "Berechnung3D",            _params.Berechnung3D   ? "true" : "false");

        doc.Save(_path);
    }

    // ── Hilfsmethoden ─────────────────────────────────────────────────────────
    static double ReadDouble(XmlElement root, string tag, double def)
    {
        var node = root.SelectSingleNode(tag);
        if (node == null) return def;
        return double.TryParse(node.InnerText,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : def;
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
    /// <summary>Freier Maßstab (true) oder Maßstab fixiert auf m=1 (false)</summary>
    public bool   FreierMassstab         { get; set; } = true;
    /// <summary>3D-Berechnung inkl. Höhen (true) oder rein 2D (false)</summary>
    public bool   Berechnung3D           { get; set; } = true;
}
