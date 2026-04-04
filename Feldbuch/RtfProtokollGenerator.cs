namespace Feldbuch;

using System.Xml;

// ──────────────────────────────────────────────────────────────────────────────
// RtfProtokollGenerator
//
// Liest eine XML-Steuerdatei und erzeugt daraus eine RTF-Protokolldatei.
//
// Datenübergabe:
//   felder        – einfache Schlüssel/Wert-Paare (vorformatierte Strings)
//   tabellenzeilen– Liste von Dictionaries; Schlüssel = Spalten-"var"-Attribut
//
// XML-Abschnitt-Typen:
//   Titel        – Überschriftszeile (Paragraph-Unterlinie möglich)
//   Zeile        – Normale Textzeile mit Feldern
//   Tabelle      – Kopfzeile + Trennlinie + Datenzeilen
//   Leerzeile    – Leerer Paragraph (Abstand)
//   Trennlinie   – Horizontale Linie ohne Text
// ──────────────────────────────────────────────────────────────────────────────
public static class RtfProtokollGenerator
{
    public static void Schreiben(
        string                            vorlagePfad,
        Dictionary<string, string>        felder,
        List<Dictionary<string, string>>  tabellenzeilen,
        string                            zielPfad)
    {
        var doc = new XmlDocument();
        doc.Load(vorlagePfad);
        var root = doc.DocumentElement!;

        var sb = new System.Text.StringBuilder(8192);

        // ── RTF-Kopf ──────────────────────────────────────────────────────────
        sb.AppendLine(@"{\rtf1\ansi\ansicpg1252\deff0");

        // Schriftentabelle aus XML
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

        // Seitenformat (mm → Twips: 1 mm ≈ 56.69 Twips)
        if (root.SelectSingleNode("Seite") is XmlElement seite)
        {
            sb.AppendLine(
                $@"\paperw{Mm(seite,"breite",210)}\paperh{Mm(seite,"hoehe",297)}" +
                $@"\margl{Mm(seite,"randLinks",20)}\margr{Mm(seite,"randRechts",20)}" +
                $@"\margt{Mm(seite,"randOben",20)}\margb{Mm(seite,"randUnten",20)}");
        }
        sb.AppendLine(@"\widowctrl\hyphauto");

        // ── Inhalt ────────────────────────────────────────────────────────────
        foreach (XmlElement a in root.SelectNodes("Inhalt/Abschnitt")!)
        {
            string typ  = a.GetAttribute("typ");
            int    f    = Int(a, "schrift",     1);
            int    fs   = Int(a, "groesse",    10) * 2;   // Half-Points
            bool   fett = a.GetAttribute("fett")       == "true";
            bool   kurs = a.GetAttribute("kursiv")     == "true";
            bool   ulin = a.GetAttribute("unterlinie") == "true";
            int    sa   = Int(a, "abstandNach", 0);
            string bord = ulin ? @"\brdrb\brdrs\brdrw15\brsp20" : "";

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
                case "Titel":
                case "Zeile":
                {
                    sb.Append($@"\pard{bord}\sa{sa} {{{Fmt(f, fs, fett, kurs)} ");
                    foreach (XmlElement feld in a.SelectNodes("Feld")!)
                        sb.Append(E(FeldWert(feld, felder)));
                    sb.AppendLine(@"}\par");
                    break;
                }

                // ── Tabelle ───────────────────────────────────────────────────
                case "Tabelle":
                {
                    bool kopfFett = a.GetAttribute("kopfFett") == "true";
                    var  spalten  = a.SelectNodes("Spalte")!;

                    // Kopfzeile
                    sb.Append($@"\pard\sa0 {{{Fmt(f, fs, kopfFett, false)} ");
                    sb.Append(E(Kopfzeile(spalten)));
                    sb.AppendLine(@"}\par");

                    // Trennlinie aus Minuszeichen
                    sb.Append($@"\pard\sa0 {{\f{f}\fs{fs} ");
                    sb.Append(E(new string('-', Gesamtbreite(spalten))));
                    sb.AppendLine(@"}\par");

                    // Datenzeilen
                    foreach (var zeile in tabellenzeilen)
                    {
                        sb.Append($@"\pard\sa0 {{{Fmt(f, fs, false, false)} ");
                        sb.Append(E(Datenzeile(spalten, zeile)));
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

        string var    = feld.GetAttribute("var");
        string label  = feld.GetAttribute("label");
        string einh   = feld.GetAttribute("einheit");
        string wert   = felder.TryGetValue(var, out string? v) ? v : $"[{var}]";
        return label + wert + einh;
    }

    // ── Tabellen-Hilfsmethoden ────────────────────────────────────────────────
    private static string Kopfzeile(XmlNodeList spalten)
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

    private static string Datenzeile(XmlNodeList spalten, Dictionary<string, string> zeile)
    {
        var sb = new System.Text.StringBuilder();
        bool first = true;
        foreach (XmlElement s in spalten)
        {
            if (!first) sb.Append(' '); first = false;
            int    b   = Int(s, "breite", 8);
            bool   r   = s.GetAttribute("ausrichtung") == "R";
            string var = s.GetAttribute("var");
            string w   = zeile.TryGetValue(var, out string? v) ? v : "";
            sb.Append(r ? w.PadLeft(b) : w.PadRight(b));
        }
        return sb.ToString();
    }

    private static int Gesamtbreite(XmlNodeList spalten)
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
                case '\\': sb.Append(@"\\");    break;
                case '{':  sb.Append(@"\{");    break;
                case '}':  sb.Append(@"\}");    break;
                case 'ä':  sb.Append(@"\'e4");  break;
                case 'ö':  sb.Append(@"\'f6");  break;
                case 'ü':  sb.Append(@"\'fc");  break;
                case 'Ä':  sb.Append(@"\'c4");  break;
                case 'Ö':  sb.Append(@"\'d6");  break;
                case 'Ü':  sb.Append(@"\'dc");  break;
                case 'ß':  sb.Append(@"\'df");  break;
                default:
                    if (c > 127) sb.Append($@"\u{(int)c}?");
                    else         sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }
}
