namespace Feldbuch;

using System.Xml;

// ──────────────────────────────────────────────────────────────────────────────
// ProjektManager – verwaltet das aktive Projekt (Name + Verzeichnis).
//
// Das zuletzt geöffnete Projekt wird in Einstellungen.xml (AppBaseDirectory)
// gespeichert und beim nächsten Start automatisch wieder geladen.
// ──────────────────────────────────────────────────────────────────────────────
public static class ProjektManager
{
    private static readonly string EinstellungenPfad =
        AppPfade.Get("Einstellungen.xml");

    // ── Eigenschaften ─────────────────────────────────────────────────────────
    public static string ProjektName        { get; private set; } = "";
    public static string ProjektVerzeichnis { get; private set; } = AppPfade.Basis;

    public static bool IstGeladen => !string.IsNullOrEmpty(ProjektName);

    // ── Optionen (5 Checkboxen im Hauptfenster) ───────────────────────────────
    /// <summary>Protokollierung in Protokoll_YYYY-MM-DD.txt aktivieren.</summary>
    public static bool ProtokollAktiv          { get; set; } = true;
    /// <summary>Vor dem Speichern eine Sicherungskopie anlegen (Platzhalter).</summary>
    public static bool AutoBackup              { get; set; } = false;
    /// <summary>Koordinaten-Tooltip im DXF-Viewer anzeigen (Platzhalter).</summary>
    public static bool KoordinatenTooltip      { get; set; } = true;
    /// <summary>Systemton nach abgeschlossener Berechnung ausgeben (Platzhalter).</summary>
    public static bool TonBeiBerechnung        { get; set; } = false;
    /// <summary>Erweiterte Protokollierung (Eingabedaten, Rohmessungen) (Platzhalter).</summary>
    public static bool ErweiterteProtokollierung { get; set; } = false;

    // ── Koordinatentransformation ─────────────────────────────────────────────
    /// <summary>Fester Maßstab m=1 bei Koordinatentransformation.</summary>
    public static bool TransformFesterMassstab { get; set; } = false;
    /// <summary>Letzter verwendeter Transformationstyp (ComboBox-Index).</summary>
    public static int  TransformTypIndex       { get; set; } = 0;

    // ── Neupunkt-/Messzähler & DXF-Viewer-Optionen ───────────────────────────
    /// <summary>Aktueller Neupunkt-Zähler (auto-increment im DXF-Viewer).</summary>
    public static int    NeupunktZaehler       { get; set; } = 1;
    /// <summary>Aktueller Stationierungs-Zähler für auto-generierte Standpunktnummern.</summary>
    public static int    StationierungZaehler  { get; set; } = 1;
    /// <summary>Instrumentenhöhe [m] (wird im DXF-Viewer gesetzt).</summary>
    public static double InstrumentenHoehe     { get; set; } = 0.0;
    /// <summary>Neupunkte-Overlay im DXF-Viewer eingeblendet.</summary>
    public static bool   NeupunkteVisible      { get; set; } = true;
    /// <summary>Residual-Marker im DXF-Viewer eingeblendet.</summary>
    public static bool   ResidualVisible       { get; set; } = true;
    /// <summary>Letzte aktive Standpunktnummer im DXF-Viewer.</summary>
    public static string LetzteStandpunktNr   { get; set; } = "";

    // ── Messgerät-Verbindungseinstellungen ────────────────────────────────────
    /// <summary>
    /// True = am COM-Port hängt ein GNSS-Empfänger (NMEA 0183).
    /// False = Tachymeter (GeoCOM / GSI / Manuell).
    /// </summary>
    /// <summary>True wenn GNSS-Empfänger ausgewählt ist (abgeleitet aus TachymeterModell).</summary>
    public static bool IstGnssGeraet => TachymeterModell == TachymeterModell.GnssNmea;
    public static TachymeterModell TachymeterModell   { get; set; } = TachymeterModell.Manuell;
    public static string           TachymeterPort     { get; set; } = "";
    public static int              TachymeterBaudRate { get; set; } = 9600;
    public static int              TachymeterDataBits { get; set; } = 8;
    public static string           TachymeterParitaet { get; set; } = "None";
    public static string           TachymeterStopBits { get; set; } = "1";

    // ── Gespeicherter DXF-Zoom ────────────────────────────────────────────────
    // Zoom wird pro DXF-Dateipfad gespeichert, sodass er nach erneutem Öffnen
    // automatisch wiederhergestellt wird.
    private static string _zoomDatei = "";
    private static double _zoomScale = 0;   // 0 = kein gespeicherter Zoom
    private static double _zoomPanX  = 0;
    private static double _zoomPanY  = 0;

    /// <summary>Gibt den Pfad zu einer Datei im Projektverzeichnis zurück.</summary>
    public static string GetPfad(string dateiname) =>
        Path.Combine(ProjektVerzeichnis, dateiname);

    /// <summary>Pfad zur DXF-Datei mit Projektname (z. B. Projekt.dxf).</summary>
    public static string DxfPfad =>
        IstGeladen ? Path.Combine(ProjektVerzeichnis, ProjektName + ".dxf") : "";

    public static bool HatDxfDatei =>
        !string.IsNullOrEmpty(DxfPfad) && File.Exists(DxfPfad);

    // ── Zoom speichern / laden ────────────────────────────────────────────────
    /// <summary>Speichert Zoom-Zustand für eine bestimmte DXF-Datei.</summary>
    public static void SpeichereZoom(string dxfPfad, double scale, double panX, double panY)
    {
        _zoomDatei = dxfPfad;
        _zoomScale = scale;
        _zoomPanX  = panX;
        _zoomPanY  = panY;
        Speichern();
    }

    /// <summary>
    /// Gibt gespeicherten Zoom zurück, wenn er zu <paramref name="dxfPfad"/> passt.
    /// Gibt null zurück, wenn kein passender Zoom vorhanden ist.
    /// </summary>
    public static (double Scale, double PanX, double PanY)? LadeZoom(string dxfPfad)
    {
        if (_zoomScale > 0 &&
            string.Equals(_zoomDatei, dxfPfad, StringComparison.OrdinalIgnoreCase))
            return (_zoomScale, _zoomPanX, _zoomPanY);
        return null;
    }

    // ── Aktives Projekt setzen ────────────────────────────────────────────────
    public static void SetProjekt(string name, string verzeichnis)
    {
        ProjektName        = name.Trim();
        ProjektVerzeichnis = verzeichnis.TrimEnd('\\', '/');
        Directory.CreateDirectory(ProjektVerzeichnis);
        Speichern();
    }

    // ── Letztes Projekt laden (beim Programmstart) ────────────────────────────
    public static void Laden()
    {
        if (!File.Exists(EinstellungenPfad)) return;
        try
        {
            var doc  = new XmlDocument();
            doc.Load(EinstellungenPfad);
            var root = doc.DocumentElement!;

            string name = root.SelectSingleNode("LetztesProjektName")?.InnerText?.Trim() ?? "";
            string verz = root.SelectSingleNode("LetztesProjektVerzeichnis")?.InnerText?.Trim() ?? "";

            if (!string.IsNullOrEmpty(name) && Directory.Exists(verz))
            {
                ProjektName        = name;
                ProjektVerzeichnis = verz;
            }

            TransformFesterMassstab   = LadeBool(root, "TransformFesterMassstab",  defaultVal: false);
            if (int.TryParse(root.SelectSingleNode("TransformTypIndex")?.InnerText, out var tti)) TransformTypIndex = tti;

            ProtokollAktiv            = LadeBool(root, "ProtokollAktiv",           defaultVal: true);
            AutoBackup                = LadeBool(root, "AutoBackup",               defaultVal: false);
            KoordinatenTooltip        = LadeBool(root, "KoordinatenTooltip",       defaultVal: true);
            TonBeiBerechnung          = LadeBool(root, "TonBeiBerechnung",         defaultVal: false);
            ErweiterteProtokollierung = LadeBool(root, "ErweiterteProtokollierung",defaultVal: false);

            // Messzähler & Viewer-Optionen
            if (int.TryParse(root.SelectSingleNode("NeupunktZaehler")?.InnerText,    out var npz)) NeupunktZaehler      = npz;
            if (int.TryParse(root.SelectSingleNode("StationierungZaehler")?.InnerText, out var spz)) StationierungZaehler = spz;
            double.TryParse(root.SelectSingleNode("InstrumentenHoehe")?.InnerText,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var ih);
            InstrumentenHoehe  = ih;
            NeupunkteVisible   = LadeBool(root, "NeupunkteVisible",  defaultVal: true);
            ResidualVisible    = LadeBool(root, "ResidualVisible",   defaultVal: true);
            LetzteStandpunktNr = root.SelectSingleNode("LetzteStandpunktNr")?.InnerText?.Trim() ?? "";

            // Tachymeter-Modell (mit Migration alter Enum-Namen)
            var tachModellText = root.SelectSingleNode("TachymeterModell")?.InnerText?.Trim() ?? "";
            // Migration: alte Enum-Namen auf neue abbilden
            tachModellText = tachModellText switch
            {
                "SokkiaSET"    => "SokkiaSDR",
                "TopconGPT3000" => "TopconGTS",
                _ => tachModellText
            };
            if (Enum.TryParse<TachymeterModell>(tachModellText, out var tm))
                TachymeterModell = tm;
            // Migration: altes IstGnssGeraet-Flag auf neues GnssNmea-Modell übertragen
            if (TachymeterModell != TachymeterModell.GnssNmea
                && root.SelectSingleNode("IstGnssGeraet")?.InnerText?.Trim().ToLower() == "true")
                TachymeterModell = TachymeterModell.GnssNmea;
            TachymeterPort     = root.SelectSingleNode("TachymeterPort")    ?.InnerText?.Trim() ?? "";
            if (int.TryParse(root.SelectSingleNode("TachymeterBaudRate")?.InnerText, out var baud)) TachymeterBaudRate = baud;
            if (int.TryParse(root.SelectSingleNode("TachymeterDataBits")?.InnerText, out var bits)) TachymeterDataBits = bits;
            TachymeterParitaet = root.SelectSingleNode("TachymeterParitaet")?.InnerText?.Trim() ?? "None";
            TachymeterStopBits = root.SelectSingleNode("TachymeterStopBits")?.InnerText?.Trim() ?? "1";

            _zoomDatei = root.SelectSingleNode("DxfZoomDatei")?.InnerText?.Trim() ?? "";
            double.TryParse(root.SelectSingleNode("DxfZoomScale")?.InnerText,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out _zoomScale);
            double.TryParse(root.SelectSingleNode("DxfZoomPanX")?.InnerText,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out _zoomPanX);
            double.TryParse(root.SelectSingleNode("DxfZoomPanY")?.InnerText,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out _zoomPanY);
        }
        catch (Exception ex) { ErrorLogger.Log("ProjektManager.Laden", ex); }
    }

    // ── Nur Optionen speichern (ohne Projektpfad zu ändern) ──────────────────
    /// <summary>Speichert die 5 Checkbox-Optionen sofort in Einstellungen.xml.</summary>
    public static void SpeichereOptionen() => Speichern();

    // ── Einstellungen speichern ───────────────────────────────────────────────
    private static void Speichern()
    {
        try
        {
            var doc  = new XmlDocument();
            var decl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(decl);

            var root = doc.CreateElement("FeldbuchEinstellungen");
            doc.AppendChild(root);

            Append(doc, root, "LetztesProjektName",          ProjektName);
            Append(doc, root, "LetztesProjektVerzeichnis",  ProjektVerzeichnis);
            Append(doc, root, "TransformFesterMassstab", TransformFesterMassstab.ToString().ToLower());
            Append(doc, root, "TransformTypIndex",      TransformTypIndex.ToString());
            Append(doc, root, "ProtokollAktiv",             ProtokollAktiv.ToString().ToLower());
            Append(doc, root, "AutoBackup",                 AutoBackup.ToString().ToLower());
            Append(doc, root, "KoordinatenTooltip",         KoordinatenTooltip.ToString().ToLower());
            Append(doc, root, "TonBeiBerechnung",           TonBeiBerechnung.ToString().ToLower());
            Append(doc, root, "ErweiterteProtokollierung",  ErweiterteProtokollierung.ToString().ToLower());
            Append(doc, root, "NeupunktZaehler",      NeupunktZaehler.ToString());
            Append(doc, root, "StationierungZaehler", StationierungZaehler.ToString());
            Append(doc, root, "InstrumentenHoehe",    InstrumentenHoehe.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
            Append(doc, root, "NeupunkteVisible",     NeupunkteVisible.ToString().ToLower());
            Append(doc, root, "ResidualVisible",      ResidualVisible.ToString().ToLower());
            Append(doc, root, "LetzteStandpunktNr",  LetzteStandpunktNr);
            Append(doc, root, "TachymeterModell",   TachymeterModell.ToString());
            Append(doc, root, "TachymeterPort",     TachymeterPort);
            Append(doc, root, "TachymeterBaudRate", TachymeterBaudRate.ToString());
            Append(doc, root, "TachymeterDataBits", TachymeterDataBits.ToString());
            Append(doc, root, "TachymeterParitaet", TachymeterParitaet);
            Append(doc, root, "TachymeterStopBits", TachymeterStopBits);
            Append(doc, root, "DxfZoomDatei",  _zoomDatei);
            Append(doc, root, "DxfZoomScale",  _zoomScale.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            Append(doc, root, "DxfZoomPanX",   _zoomPanX .ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            Append(doc, root, "DxfZoomPanY",   _zoomPanY .ToString("R", System.Globalization.CultureInfo.InvariantCulture));

            doc.Save(EinstellungenPfad);
        }
        catch (Exception ex) { ErrorLogger.Log("ProjektManager.Speichern", ex); }
    }

    static void Append(XmlDocument doc, XmlElement parent, string tag, string value)
    {
        var elem = doc.CreateElement(tag);
        elem.InnerText = value;
        parent.AppendChild(elem);
    }

    static bool LadeBool(XmlElement root, string tag, bool defaultVal)
    {
        string? text = root.SelectSingleNode(tag)?.InnerText?.Trim().ToLower();
        if (text == null) return defaultVal;
        return text is "true" or "1" or "yes";
    }
}
