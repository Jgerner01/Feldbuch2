namespace Feldbuch;

using System.Xml;

// ──────────────────────────────────────────────────────────────────────────────
// RtfProtokollGenerator
//
// Liest eine XML-Steuerdatei und erzeugt daraus eine RTF-Protokolldatei.
//
// Datenübergabe:
//   felder        – einfache Schlüssel/Wert-Paare (vorformatierte Strings)
//   tabellen      – benannte Tabellen: Schlüssel = Tabellenname (aus XML name="...")
//                   Rückwärtskomp.: Überladung mit List<> legt die unter "" ab;
//                   <Abschnitt typ="Tabelle"> ohne name-Attribut sucht ebenfalls ""
//
// XML-Abschnitt-Typen:
//   Titel        – Überschriftszeile (Paragraph-Unterlinie möglich)
//   Zeile        – Normale Textzeile mit Feldern
//   Monoblock    – Mehrzeiliger vorformatierter Block (Wert aus felder, \n-getrennt)
//   Tabelle      – Kopfzeile + Trennlinie + Datenzeilen
//   Leerzeile    – Leerer Paragraph (Abstand)
//   Trennlinie   – Horizontale Linie ohne Text
//
// Neue XML-Features gegenüber v1:
//   <Farben>/<Farbe id r g b>  – Farbtabelle (erzeugt \colortbl; cf1=erste Farbe usw.)
//   <Seite querformat="true">  – Querformat (\landscape)
//   <Abschnitt zeigenWenn="K"> – Abschnitt nur ausgeben wenn felder["K"] == "1"
//   <Abschnitt farbe="N">      – Standardfarbe aller Felder dieses Abschnitts
//   <Feld farbe="N">           – Überschreibt Abschnittsfarbe für dieses Feld
//   <Feld groesse="N">         – Überschreibt Schriftgröße für dieses Feld (Punkt)
//   <Feld fett="true">         – Fettschrift nur für dieses Feld
//   <Tabelle name="...">       – Tabellenname, referenziert benannte tabellen-Liste
//   <Tabelle ampel="true">     – Zeilenfärbung über _ampel-Schlüssel: 1=cf2 2=cf3 3=cf4+fett
//   <Spalte zeigenWenn="K">    – Spalte nur ausgeben wenn felder["K"] == "1"
// ──────────────────────────────────────────────────────────────────────────────
public static class RtfProtokollGenerator
{
    // ── Rückwärtskompatible Überladung (einzelne Tabellenliste) ───────────────
    public static void Schreiben(
        string                            vorlagePfad,
        Dictionary<string, string>        felder,
        List<Dictionary<string, string>>  tabellenzeilen,
        string                            zielPfad)
    {
        Schreiben(vorlagePfad, felder,
            new Dictionary<string, List<Dictionary<string, string>>> { [""] = tabellenzeilen },
            zielPfad);
    }

    // ── Hauptüberladung mit benannten Tabellen ────────────────────────────────
    public static void Schreiben(
        string                                               vorlagePfad,
        Dictionary<string, string>                           felder,
        Dictionary<string, List<Dictionary<string, string>>> tabellen,
        string                                               zielPfad)
    {
        var doc = new XmlDocument();
        doc.Load(vorlagePfad);
        var root = doc.DocumentElement!;

        var sb = new System.Text.StringBuilder(8192);

        // ── RTF-Kopf ──────────────────────────────────────────────────────────
        sb.AppendLine(@"{\rtf1\ansi\ansicpg1252\deff0");

        // Schriftentabelle
        sb.Append(@"{\fonttbl");
        foreach (XmlElement s in root.SelectNodes("Schriften/Schrift")!)
        {
            string id  = s.GetAttribute("id");
            string nam = s.GetAttribute("name");
            string typ = s.GetAttribute("typ") switch {
                "swiss"  => @"\fswiss",
                "modern" => @"\fmodern",
                "roman"  => @"\froman",
                _        => @"\fnil"
            };
            sb.Append($@"{{\f{id}{typ} {nam};}}");
        }
        sb.AppendLine("}");

        // Farbtabelle (optional)
        if (root.SelectSingleNode("Farben") is XmlElement farbenNode)
        {
            sb.Append(@"{\colortbl;");   // führendes ; = cf0 = auto
            foreach (XmlElement fc in farbenNode.SelectNodes("Farbe")!)
                sb.Append($@"\red{Int(fc,"r",0)}\green{Int(fc,"g",0)}\blue{Int(fc,"b",0)};");
            sb.AppendLine("}");
        }

        // Seitenformat (mm → Twips: 1 mm ≈ 56.69 Twips)
        if (root.SelectSingleNode("Seite") is XmlElement seite)
        {
            bool quer = seite.GetAttribute("querformat") == "true";
            sb.Append(
                $@"\paperw{Mm(seite,"breite",210)}\paperh{Mm(seite,"hoehe",297)}" +
                $@"\margl{Mm(seite,"randLinks",20)}\margr{Mm(seite,"randRechts",20)}" +
                $@"\margt{Mm(seite,"randOben",20)}\margb{Mm(seite,"randUnten",20)}");
            if (quer) sb.Append(@"\landscape");
            sb.AppendLine();
        }
        sb.AppendLine(@"\widowctrl\hyphauto");

        // ── Inhalt ────────────────────────────────────────────────────────────
        foreach (XmlElement a in root.SelectNodes("Inhalt/Abschnitt")!)
        {
            // Bedingte Sichtbarkeit
            string zeigenWenn = a.GetAttribute("zeigenWenn");
            if (!string.IsNullOrEmpty(zeigenWenn) &&
                !(felder.TryGetValue(zeigenWenn, out string? zwv) && zwv == "1"))
                continue;

            string typ    = a.GetAttribute("typ");
            int    f      = Int(a, "schrift",      1);
            int    fs     = Int(a, "groesse",     10) * 2;
            bool   fett   = a.GetAttribute("fett")       == "true";
            bool   kurs   = a.GetAttribute("kursiv")     == "true";
            bool   ulin   = a.GetAttribute("unterlinie") == "true";
            int    sa     = Int(a, "abstandNach",  0);
            int    aFarbe = Int(a, "farbe",         0);
            string bord   = ulin ? @"\brdrb\brdrs\brdrw15\brsp20" : "";

            switch (typ)
            {
                // ── Leere Zeile ───────────────────────────────────────────────
                case "Leerzeile":
                    sb.AppendLine($@"\pard\sa{sa} {{\f{f}\fs{fs} }}\par");
                    break;

                // ── Horizontale Trennlinie ────────────────────────────────────
                case "Trennlinie":
                    sb.AppendLine($@"\pard\brdrb\brdrs\brdrw15\brsp20\sa{sa} {{\f{f}\fs{fs} }}\par");
                    break;

                // ── Textzeile / Titel ─────────────────────────────────────────
                // Jedes Feld bekommt sein eigenes Format-Group; Attribute am Feld
                // überschreiben die Abschnittsdefaults (farbe, groesse, fett, kursiv).
                case "Titel":
                case "Zeile":
                {
                    sb.Append($@"\pard{bord}\sa{sa} ");
                    foreach (XmlElement feld in a.SelectNodes("Feld")!)
                    {
                        int  fFarbe   = feld.HasAttribute("farbe")   ? Int(feld, "farbe",  0)  : aFarbe;
                        int  fGroesse = feld.HasAttribute("groesse")  ? Int(feld, "groesse", 10) * 2 : fs;
                        bool fFett    = feld.HasAttribute("fett")     ? feld.GetAttribute("fett")   == "true" : fett;
                        bool fKurs    = feld.HasAttribute("kursiv")   ? feld.GetAttribute("kursiv") == "true" : kurs;

                        string fmt = $@"\f{f}\fs{fGroesse}";
                        if (fFett)         fmt += @"\b";
                        if (fKurs)         fmt += @"\i";
                        if (fFarbe > 0)    fmt += $@"\cf{fFarbe}";

                        sb.Append($@"{{{fmt} {E(FeldWert(feld, felder))}}}");
                    }
                    sb.AppendLine(@"\par");
                    break;
                }

                // ── Monoblock: Feldwert mit \n-Trennung als einzelne Zeilen ──
                case "Monoblock":
                {
                    string varName = a.GetAttribute("var");
                    string content = felder.TryGetValue(varName, out string? mv) ? mv ?? "" : "";
                    if (aFarbe > 0) sb.AppendLine($@"{{\cf{aFarbe}");  // optionale Abschnittsfarbe
                    foreach (string line in content.Split('\n'))
                    {
                        sb.Append($@"\pard\sa0 {{{Fmt(f, fs, fett, kurs)} ");
                        sb.Append(E(line.TrimEnd('\r')));
                        sb.AppendLine(@"}\par");
                    }
                    if (aFarbe > 0) sb.AppendLine(@"\cf0}");
                    break;
                }

                // ── Tabelle ───────────────────────────────────────────────────
                case "Tabelle":
                {
                    bool   kopfFett   = a.GetAttribute("kopfFett") == "true";
                    bool   ampelAktiv = a.GetAttribute("ampel")    == "true";
                    string tblName    = a.GetAttribute("name");   // "" wenn kein Attribut

                    tabellen.TryGetValue(tblName, out var zeilen);
                    zeilen ??= new List<Dictionary<string, string>>();

                    // Sichtbare Spalten filtern
                    var spalten = new List<XmlElement>();
                    foreach (XmlElement s in a.SelectNodes("Spalte")!)
                    {
                        string spZW = s.GetAttribute("zeigenWenn");
                        bool sichtbar = string.IsNullOrEmpty(spZW)
                            || (felder.TryGetValue(spZW, out string? spv) && spv == "1");
                        if (sichtbar) spalten.Add(s);
                    }

                    // Kopfzeile
                    sb.Append($@"\pard\sa0 {{{Fmt(f, fs, kopfFett, false)} ");
                    sb.Append(E(Kopfzeile(spalten)));
                    sb.AppendLine(@"}\par");

                    // Trennlinie
                    sb.Append($@"\pard\sa0 {{\f{f}\fs{fs} ");
                    sb.Append(E(new string('-', Gesamtbreite(spalten))));
                    sb.AppendLine(@"}\par");

                    // Datenzeilen mit optionaler Ampel-Färbung
                    foreach (var zeile in zeilen)
                    {
                        string ak  = ampelAktiv && zeile.TryGetValue("_ampel", out string? av) ? av ?? "" : "";
                        string pre = ak switch { "1" => @"\cf2 ", "2" => @"\cf3 ", "3" => @"\cf4\b ", _ => "" };
                        string suf = ak switch { "3" => @"\b0\cf0", "1" or "2" => @"\cf0", _  => "" };

                        sb.Append($@"\pard\sa0 {{{Fmt(f, fs, false, false)} {pre}");
                        sb.Append(E(Datenzeile(spalten, zeile)));
                        sb.Append(suf);
                        sb.AppendLine(@"}\par");
                    }
                    break;
                }
            }
        }

        sb.Append('}');
        File.WriteAllText(zielPfad, sb.ToString(), System.Text.Encoding.ASCII);
    }

    // ── Feld-Wert auflösen ────────────────────────────────────────────────────
    private static string FeldWert(XmlElement feld, Dictionary<string, string> felder)
    {
        if (feld.HasAttribute("text"))
            return feld.GetAttribute("text");

        string var   = feld.GetAttribute("var");
        string label = feld.GetAttribute("label");
        string einh  = feld.GetAttribute("einheit");
        string wert  = felder.TryGetValue(var, out string? v) ? v : $"[{var}]";
        return label + wert + einh;
    }

    // ── Tabellen-Hilfsmethoden ────────────────────────────────────────────────
    private static string Kopfzeile(List<XmlElement> spalten)
    {
        var sb = new System.Text.StringBuilder();
        bool first = true;
        foreach (XmlElement s in spalten)
        {
            if (!first) sb.Append(' '); first = false;
            int    b = Int(s, "breite", 8);
            bool   r = s.GetAttribute("ausrichtung") == "R";
            string h = s.GetAttribute("header");
            sb.Append(r ? h.PadLeft(b) : h.PadRight(b));
        }
        return sb.ToString();
    }

    private static string Datenzeile(List<XmlElement> spalten, Dictionary<string, string> zeile)
    {
        var sb = new System.Text.StringBuilder();
        bool first = true;
        foreach (XmlElement s in spalten)
        {
            if (!first) sb.Append(' '); first = false;
            int    b   = Int(s, "breite", 8);
            bool   r   = s.GetAttribute("ausrichtung") == "R";
            string var = s.GetAttribute("var");
            string w   = zeile.TryGetValue(var, out string? v) ? v ?? "" : "";
            sb.Append(r ? w.PadLeft(b) : w.PadRight(b));
        }
        return sb.ToString();
    }

    private static int Gesamtbreite(List<XmlElement> spalten)
    {
        int total = 0; bool first = true;
        foreach (XmlElement s in spalten)
        {
            if (!first) total++; first = false;
            total += Int(s, "breite", 8);
        }
        return total;
    }

    // ── RTF-Hilfsmethoden ─────────────────────────────────────────────────────
    private static string Fmt(int f, int fs, bool fett, bool kurs)
    {
        string c = $@"\f{f}\fs{fs}";
        if (fett) c += @"\b";
        if (kurs) c += @"\i";
        return c;
    }

    private static int Mm(XmlElement el, string attr, int def) =>
        (int)Math.Round((int.TryParse(el.GetAttribute(attr), out int v) ? v : def) * 56.69);

    private static int Int(XmlElement el, string attr, int def) =>
        int.TryParse(el.GetAttribute(attr), out int v) ? v : def;

    /// <summary>Alle Nicht-ASCII-Zeichen als RTF-Escape ausgeben.</summary>
    private static string E(string text)
    {
        var sb = new System.Text.StringBuilder(text.Length + 16);
        foreach (char c in text)
        {
            switch (c)
            {
                case '\\': sb.Append(@"\\");   break;
                case '{':  sb.Append(@"\{");   break;
                case '}':  sb.Append(@"\}");   break;
                case 'ä':  sb.Append(@"\'e4"); break;
                case 'ö':  sb.Append(@"\'f6"); break;
                case 'ü':  sb.Append(@"\'fc"); break;
                case 'Ä':  sb.Append(@"\'c4"); break;
                case 'Ö':  sb.Append(@"\'d6"); break;
                case 'Ü':  sb.Append(@"\'dc"); break;
                case 'ß':  sb.Append(@"\'df"); break;
                default:
                    if (c > 127) sb.Append($@"\u{(int)c}?");
                    else         sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }
}
