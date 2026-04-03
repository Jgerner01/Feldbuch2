namespace Feldbuch;

using System.Globalization;
using System.Text;

// ──────────────────────────────────────────────────────────────────────────────
// DxfExporter – schreibt Feldbuchpunkte als eigenständige DXF-Datei (AC1009).
//
// Symbolgrößen und Textgrößen werden auf den Zielmaßstab skaliert:
//   Weltgröße [m] = Papiergröße [mm] × Maßstab / 1000
//
// Layer-Struktur:
//   Feldbuch_Standpunkt_Symbol   – Kreis + Kreuz (rot,  ACI 1)
//   Feldbuch_Standpunkt_Nummer   – Punktnummer-Text (rot)
//   Feldbuch_Standpunkt_Hoehe    – Höhen-Text (rot)
//   Feldbuch_Neupunkt_Symbol     – Kreis (grün, ACI 3)
//   Feldbuch_Neupunkt_Nummer     – Punktnummer-Text (grün)
//   Feldbuch_Neupunkt_Hoehe      – Höhen-Text (grün)
// ──────────────────────────────────────────────────────────────────────────────
public static class DxfExporter
{
    static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    // Layer-Definitionen: Name → ACI-Farbe
    static readonly (string Name, int Aci)[] Layers =
    {
        ("Feldbuch_Standpunkt_Symbol",  1),
        ("Feldbuch_Standpunkt_Nummer",  1),
        ("Feldbuch_Standpunkt_Hoehe",   1),
        ("Feldbuch_Neupunkt_Symbol",    3),
        ("Feldbuch_Neupunkt_Nummer",    3),
        ("Feldbuch_Neupunkt_Hoehe",     3),
    };

    /// <summary>
    /// Exportiert Feldbuchpunkte als DXF-Datei.
    /// </summary>
    /// <param name="punkte">Zu exportierende Punkte.</param>
    /// <param name="zielPfad">Ausgabedatei.</param>
    /// <param name="massstab">Zielmaßstab, z.B. 1000 für 1:1000.</param>
    /// <param name="symbolSizeMm">Symbolgröße in mm auf dem Papier (Kreisdurchmesser).</param>
    /// <param name="textSizeMm">Schriftgröße in mm auf dem Papier.</param>
    public static void Exportieren(
        IReadOnlyList<FeldbuchPunkt> punkte,
        string zielPfad,
        double massstab      = 1000,
        double symbolSizeMm  = 1.5,
        double textSizeMm    = 2.0)
    {
        // Weltmaße berechnen
        double symbolR   = symbolSizeMm * massstab / 2000.0;  // Radius in Metern
        double textH     = textSizeMm   * massstab / 1000.0;  // Texthöhe in Metern
        double textOff   = textH * 0.3;                        // Abstand Symbol → Text

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

        // Linientyp-Tabelle (CONTINUOUS erforderlich für AC1009)
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
        G(sb, 0, "ENDTABLE");

        // Layer-Tabelle
        G(sb, 0, "TABLE");
        G(sb, 2, "LAYER");
        G(sb, 70, Layers.Length);
        foreach (var (name, aci) in Layers)
        {
            G(sb, 0, "LAYER");
            G(sb, 2, name);
            G(sb, 70, 0);
            G(sb, 62, aci);
            G(sb, 6, "CONTINUOUS");
        }
        G(sb, 0, "ENDTABLE");

        G(sb, 0, "ENDSEC");

        // ── ENTITIES ──────────────────────────────────────────────────────────
        G(sb, 0, "SECTION");
        G(sb, 2, "ENTITIES");

        foreach (var p in punkte)
        {
            bool istSP   = p.Typ == "Standpunkt";
            string lSym  = istSP ? "Feldbuch_Standpunkt_Symbol" : "Feldbuch_Neupunkt_Symbol";
            string lNr   = istSP ? "Feldbuch_Standpunkt_Nummer" : "Feldbuch_Neupunkt_Nummer";
            string lH    = istSP ? "Feldbuch_Standpunkt_Hoehe"  : "Feldbuch_Neupunkt_Hoehe";

            // Symbol: Kreis
            Kreis(sb, p.R, p.H, symbolR, lSym);

            if (istSP)
            {
                // Standpunkt: diagonale Kreuzlinien außerhalb des Kreises
                double arm = symbolR * 1.6;
                double r6  = symbolR * 0.6;
                Linie(sb, p.R - arm, p.H - arm, p.R - r6, p.H - r6, lSym);
                Linie(sb, p.R + r6,  p.H + r6,  p.R + arm, p.H + arm, lSym);
                Linie(sb, p.R + arm, p.H - arm, p.R + r6,  p.H - r6,  lSym);
                Linie(sb, p.R - r6,  p.H + r6,  p.R - arm, p.H + arm, lSym);
            }

            // Punktnummer (oberhalb rechts)
            Text(sb,
                p.R + symbolR + textOff,
                p.H + symbolR * 0.3 + textOff,
                textH, p.PunktNr, lNr);

            // Höhe (unterhalb rechts, nur bei 3D)
            if (p.IstBerechnung3D)
                Text(sb,
                    p.R + symbolR + textOff,
                    p.H - symbolR * 0.3 - textOff - textH,
                    textH, $"{p.Hoehe:F3}", lH);
        }

        G(sb, 0, "ENDSEC");
        G(sb, 0, "EOF");

        File.WriteAllText(zielPfad, sb.ToString(), Encoding.ASCII);
    }

    // ── DXF-Primitive ─────────────────────────────────────────────────────────
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
    static void G(StringBuilder sb, int code, string  val) =>
        sb.AppendLine($"{code,3}").AppendLine(val);
    static void G(StringBuilder sb, int code, int     val) =>
        sb.AppendLine($"{code,3}").AppendLine(val.ToString(IC));
    static void G(StringBuilder sb, int code, double  val) =>
        sb.AppendLine($"{code,3}").AppendLine(val.ToString("F6", IC));
}
