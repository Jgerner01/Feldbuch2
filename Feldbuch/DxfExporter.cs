namespace Feldbuch;

using System.Globalization;
using System.Text;

// ──────────────────────────────────────────────────────────────────────────────
// DxfExporter – schreibt alle Feldbuchpunkte als eigenständige DXF-Datei (AC1009).
//
// Symbolgrößen und Textgrößen werden auf den Zielmaßstab skaliert:
//   Weltgröße [m] = Papiergröße [mm] × Maßstab / 1000
//
// Layer-Struktur (dynamisch, je nach vorhandenen Daten):
//
//   Berechnete Punkte (Freie Stationierung):
//     Feldbuch_Standpunkt_Symbol/Nummer  – rot         (ACI  1)
//     Feldbuch_Standpunkt_Hoehe          – dunkelbraun  (ACI 30)
//     Feldbuch_Neupunkt_Symbol/Nummer    – grün         (ACI  3)
//     Feldbuch_Neupunkt_Hoehe            – dunkelbraun  (ACI 30)
//
//   Eingelesene Koordinatenpunkte (je Quelldatei):
//     Koordinaten_{Dateiname}_Symbol  – rot         (ACI  1)
//     Koordinaten_{Dateiname}_Nummer  – grün        (ACI  3)
//     Koordinaten_{Dateiname}_Hoehe   – dunkelbraun (ACI 30)
//
//   Gemessene Neupunkte (je Standpunkt):
//     Gemessen_{StandpunktNr}_Symbol/Nummer  – blau        (ACI  5)
//     Gemessen_{StandpunktNr}_Hoehe          – dunkelbraun (ACI 30)
// ──────────────────────────────────────────────────────────────────────────────
public static class DxfExporter
{
    static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    // ACI-Farben passend zur Grafik (DxfOverlayEntity.cs)
    const int ACI_ROT   = 1;   // (200, 30,  30) – Standpunkte + Import-Symbol
    const int ACI_GRUEN = 3;   // ( 20, 150, 40) – Neupunkte + Import-Beschriftung
    const int ACI_BLAU  = 5;   // ( 20,  80,200) – Gemessene Neupunkte
    const int ACI_BRAUN = 30;  // (127, 63,   0) – Höhentexte (dunkles Braun)

    /// <summary>
    /// Exportiert alle Punkt-Kategorien als DXF-Datei mit kategorie- und
    /// quellspezifischen Layern.
    /// </summary>
    public static void Exportieren(
        IReadOnlyList<FeldbuchPunkt>        feldbuchPunkte,
        IReadOnlyList<ImportPunkt>          importPunkte,
        IReadOnlyList<NeupunktErgebnis>     gemessenePunkte,
        string zielPfad,
        double massstab     = 1000,
        double symbolSizeMm = 1.5,
        double textSizeMm   = 2.0,
        IReadOnlyList<OverlayFehlerEllipse>? fehlerEllipsen = null)
    {
        double symbolR = symbolSizeMm * massstab / 2000.0;
        double textH   = textSizeMm   * massstab / 1000.0;
        double textOff = textH * 0.3;

        // ── Layer-Liste dynamisch aufbauen ─────────────────────────────────────
        var layers = new List<(string Name, int Aci)>();

        bool hatStandpunkte = feldbuchPunkte.Any(p => p.Typ == "Standpunkt");
        bool hatFbNeupunkte = feldbuchPunkte.Any(p => p.Typ == "Neupunkt");

        if (hatStandpunkte)
        {
            layers.Add(("Feldbuch_Standpunkt_Symbol", ACI_ROT));
            layers.Add(("Feldbuch_Standpunkt_Nummer", ACI_ROT));
            layers.Add(("Feldbuch_Standpunkt_Hoehe",  ACI_BRAUN));
        }
        if (hatFbNeupunkte)
        {
            layers.Add(("Feldbuch_Neupunkt_Symbol", ACI_GRUEN));
            layers.Add(("Feldbuch_Neupunkt_Nummer", ACI_GRUEN));
            layers.Add(("Feldbuch_Neupunkt_Hoehe",  ACI_BRAUN));
        }

        // Je Quelldatei einen Satz Import-Layer
        // Symbol rot (wie OverlayImportPunkt.FarbeSymbol), Beschriftung grün (FarbeBeschr)
        var importQuellen = importPunkte
            .Select(p => p.Quelle)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var q in importQuellen)
        {
            string pfx = "Koordinaten_" + SanitizeLayerName(q);
            layers.Add(($"{pfx}_Symbol", ACI_ROT));
            layers.Add(($"{pfx}_Nummer", ACI_GRUEN));
            layers.Add(($"{pfx}_Hoehe",  ACI_BRAUN));
        }

        // Je Standpunkt einen Satz Mess-Layer
        // Blau (wie OverlayGemessenerNeupunkt.FarbeSymbol / FarbeBeschr)
        var stationen = gemessenePunkte
            .Select(p => string.IsNullOrEmpty(p.StandpunktNr) ? "Unbekannt" : p.StandpunktNr)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var sp in stationen)
        {
            string pfx = "Gemessen_" + sp;
            layers.Add(($"{pfx}_Symbol", ACI_BLAU));
            layers.Add(($"{pfx}_Nummer", ACI_BLAU));
            layers.Add(($"{pfx}_Hoehe",  ACI_BRAUN));
        }

        if (fehlerEllipsen?.Count > 0)
            layers.Add(("Fehlerellipsen", ACI_BLAU));

        var sb = new StringBuilder();

        // ── HEADER ────────────────────────────────────────────────────────────
        G(sb, 0, "SECTION");
        G(sb, 2, "HEADER");
        G(sb, 9, "$ACADVER");
        G(sb, 1, "AC1009");
        G(sb, 0, "ENDSEC");

        // ── TABLES ────────────────────────────────────────────────────────────
        G(sb, 0, "SECTION");
        G(sb, 2, "TABLES");

        G(sb, 0, "TABLE");
        G(sb, 2, "LTYPE");
        G(sb, 70, 1);
        G(sb, 0, "LTYPE");
        G(sb, 2, "CONTINUOUS");
        G(sb, 70, 0);
        G(sb, 3, "Solid line");
        G(sb, 72, 65);
        G(sb, 73, 0);
        G(sb, 40, 0.0);
        G(sb, 0, "ENDTAB");

        // +1 für den obligatorischen Layer "0"
        G(sb, 0, "TABLE");
        G(sb, 2, "LAYER");
        G(sb, 70, layers.Count + 1);
        G(sb, 0, "LAYER");
        G(sb, 2, "0");
        G(sb, 70, 0);
        G(sb, 62, 7);
        G(sb, 6, "CONTINUOUS");
        foreach (var (name, aci) in layers)
        {
            G(sb, 0, "LAYER");
            G(sb, 2, name);
            G(sb, 70, 0);
            G(sb, 62, aci);
            G(sb, 6, "CONTINUOUS");
        }
        G(sb, 0, "ENDTAB");

        G(sb, 0, "ENDSEC");

        // ── ENTITIES ──────────────────────────────────────────────────────────
        G(sb, 0, "SECTION");
        G(sb, 2, "ENTITIES");

        // Berechnete Punkte (Freie Stationierung)
        foreach (var p in feldbuchPunkte)
        {
            bool   istSP = p.Typ == "Standpunkt";
            string lSym  = istSP ? "Feldbuch_Standpunkt_Symbol" : "Feldbuch_Neupunkt_Symbol";
            string lNr   = istSP ? "Feldbuch_Standpunkt_Nummer" : "Feldbuch_Neupunkt_Nummer";
            string lH    = istSP ? "Feldbuch_Standpunkt_Hoehe"  : "Feldbuch_Neupunkt_Hoehe";

            Kreis(sb, p.R, p.H, symbolR, lSym);
            if (istSP)
            {
                double arm = symbolR * 1.6;
                double r6  = symbolR * 0.6;
                Linie(sb, p.R - arm, p.H - arm, p.R - r6,  p.H - r6,  lSym);
                Linie(sb, p.R + r6,  p.H + r6,  p.R + arm, p.H + arm, lSym);
                Linie(sb, p.R + arm, p.H - arm, p.R + r6,  p.H - r6,  lSym);
                Linie(sb, p.R - r6,  p.H + r6,  p.R - arm, p.H + arm, lSym);
            }
            Text(sb, p.R + symbolR + textOff, p.H + symbolR * 0.3 + textOff,             textH, p.PunktNr,          lNr);
            if (p.IstBerechnung3D)
                Text(sb, p.R + symbolR + textOff, p.H - symbolR * 0.3 - textOff - textH, textH, $"{p.Hoehe:F3}",    lH);
        }

        // Eingelesene Koordinatenpunkte (je Quelldatei)
        foreach (var p in importPunkte)
        {
            string pfx  = "Koordinaten_" + SanitizeLayerName(p.Quelle);
            string lSym = $"{pfx}_Symbol";
            string lNr  = $"{pfx}_Nummer";
            string lH   = $"{pfx}_Hoehe";

            Kreis(sb, p.R, p.H, symbolR, lSym);
            Text(sb, p.R + symbolR + textOff, p.H + symbolR * 0.3 + textOff,             textH, p.PunktNr,          lNr);
            if (p.Hoehe != 0)
                Text(sb, p.R + symbolR + textOff, p.H - symbolR * 0.3 - textOff - textH, textH, $"{p.Hoehe:F3}",    lH);
        }

        // Gemessene Neupunkte (je Standpunkt)
        foreach (var p in gemessenePunkte)
        {
            string sp   = string.IsNullOrEmpty(p.StandpunktNr) ? "Unbekannt" : p.StandpunktNr;
            string pfx  = "Gemessen_" + sp;
            string lSym = $"{pfx}_Symbol";
            string lNr  = $"{pfx}_Nummer";
            string lH   = $"{pfx}_Hoehe";

            Kreis(sb, p.R, p.H, symbolR, lSym);
            Text(sb, p.R + symbolR + textOff, p.H + symbolR * 0.3 + textOff,             textH, p.PunktNr,          lNr);
            if (p.Ist3D)
                Text(sb, p.R + symbolR + textOff, p.H - symbolR * 0.3 - textOff - textH, textH, $"{p.Hoehe:F3}",    lH);
        }

        // Fehlerellipsen (als geschlossene Polylinien)
        if (fehlerEllipsen != null)
        {
            foreach (var e in fehlerEllipsen)
            {
                double a = Math.Abs(e.VStrecke_mm) * 0.001 * e.Skalierung;
                double b = Math.Abs(e.VQuer_mm)    * 0.001 * e.Skalierung;
                Ellipse(sb, e.R, e.H, a, b, e.Phi_rad, "Fehlerellipsen");
            }
        }

        G(sb, 0, "ENDSEC");
        G(sb, 0, "EOF");

        File.WriteAllText(zielPfad, sb.ToString(), Encoding.ASCII);
    }

    // ── Hilfsfunktionen ───────────────────────────────────────────────────────

    // Bereinigt einen Dateinamen zu einem gültigen DXF-Layer-Namen.
    static string SanitizeLayerName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Unbekannt";
        string s = Path.GetFileNameWithoutExtension(name);
        var result = new StringBuilder();
        foreach (char c in s)
            result.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');
        string trimmed = result.ToString().Trim('_');
        return string.IsNullOrEmpty(trimmed) ? "Unbekannt" : trimmed;
    }

    // ── DXF-Primitive ─────────────────────────────────────────────────────────

    // Ellipse als geschlossene POLYLINE (AC1009 kennt kein ELLIPSE-Entity).
    static void Ellipse(StringBuilder sb,
        double r, double h, double a_m, double b_m, double phi_rad, string layer)
    {
        const int N = 36;
        double cosPhi = Math.Cos(phi_rad);
        double sinPhi = Math.Sin(phi_rad);
        G(sb, 0, "POLYLINE");
        G(sb, 8, layer);
        G(sb, 66, 1);
        G(sb, 70, 1);    // geschlossen
        G(sb, 10, 0.0);
        G(sb, 20, 0.0);
        for (int i = 0; i < N; i++)
        {
            double t  = 2 * Math.PI * i / N;
            double lx = a_m * Math.Cos(t);
            double ly = b_m * Math.Sin(t);
            G(sb, 0, "VERTEX");
            G(sb, 8, layer);
            G(sb, 10, r + lx * cosPhi - ly * sinPhi);
            G(sb, 20, h + lx * sinPhi + ly * cosPhi);
        }
        G(sb, 0, "SEQEND");
        G(sb, 8, layer);
    }

    static void Kreis(StringBuilder sb, double cx, double cy, double r, string layer)
    {
        G(sb, 0, "CIRCLE");
        G(sb, 8, layer);
        G(sb, 10, cx);
        G(sb, 20, cy);
        G(sb, 40, r);
    }

    static void Linie(StringBuilder sb,
        double x1, double y1, double x2, double y2, string layer)
    {
        G(sb, 0, "LINE");
        G(sb, 8, layer);
        G(sb, 10, x1);
        G(sb, 20, y1);
        G(sb, 11, x2);
        G(sb, 21, y2);
    }

    static void Text(StringBuilder sb,
        double x, double y, double h, string text, string layer)
    {
        G(sb, 0, "TEXT");
        G(sb, 8, layer);
        G(sb, 10, x);
        G(sb, 20, y);
        G(sb, 40, h);
        G(sb, 1, text);
    }

    // ── Gruppen-Ausgabe ───────────────────────────────────────────────────────
    static void G(StringBuilder sb, int code, string val) =>
        sb.AppendLine($"{code,3}").AppendLine(val);
    static void G(StringBuilder sb, int code, int val) =>
        sb.AppendLine($"{code,3}").AppendLine(val.ToString(IC));
    static void G(StringBuilder sb, int code, double val) =>
        sb.AppendLine($"{code,3}").AppendLine(val.ToString("F6", IC));
}
