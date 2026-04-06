namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// DxfKoordImporter – extrahiert Koordinaten aus einer DXF-Datei.
//
// Übernommen werden:
//   LINE    → Anfangs- und Endpunkt (je ein KonvertierungPunkt)
//   INSERT  → Einfügepunkt (Symbol-Koordinate)
//   POINT   → Punktkoordinate
//   CIRCLE  → Mittelpunkt
//
// TEXT/MTEXT, ARC, LWPOLYLINE werden nicht importiert (keine einzelnen Punkte).
// ──────────────────────────────────────────────────────────────────────────────
public static class DxfKoordImporter
{
    public static List<KonvertierungPunkt> Import(string path)
    {
        var entities = DxfReader.Read(path);
        var result   = new List<KonvertierungPunkt>();

        foreach (var entity in entities)
        {
            switch (entity)
            {
                case DxfLine line:
                    result.Add(new KonvertierungPunkt
                    {
                        Typ       = "DXF-Linie",
                        R         = line.X1,
                        H         = line.Y1,
                        Bemerkung = $"Layer={line.Layer}, Linienendpunkt 1"
                    });
                    result.Add(new KonvertierungPunkt
                    {
                        Typ       = "DXF-Linie",
                        R         = line.X2,
                        H         = line.Y2,
                        Bemerkung = $"Layer={line.Layer}, Linienendpunkt 2"
                    });
                    break;

                case DxfInsert ins:
                    result.Add(new KonvertierungPunkt
                    {
                        Typ        = "DXF-Symbol",
                        R          = ins.X,
                        H          = ins.Y,
                        Punktcode  = ins.BlockName,
                        Bemerkung  = $"Layer={ins.Layer}, Block={ins.BlockName}"
                    });
                    break;

                case DxfPoint pt:
                    result.Add(new KonvertierungPunkt
                    {
                        Typ       = "DXF-Punkt",
                        R         = pt.X,
                        H         = pt.Y,
                        Bemerkung = $"Layer={pt.Layer}"
                    });
                    break;

                case DxfCircle circ:
                    result.Add(new KonvertierungPunkt
                    {
                        Typ       = "DXF-Kreis",
                        R         = circ.CX,
                        H         = circ.CY,
                        Bemerkung = $"Layer={circ.Layer}, R={circ.Radius:F3}m"
                    });
                    break;
            }
        }

        return result;
    }
}
