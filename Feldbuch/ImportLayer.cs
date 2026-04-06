namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// ImportLayer – ein benannter Satz von Import-Entities (pro Datei einer)
// mit eigenem Sichtbarkeits-Flag.
// ──────────────────────────────────────────────────────────────────────────────
public class ImportLayer
{
    /// <summary>Dateiname (ohne Pfad) – wird als Label der Checkbox angezeigt.</summary>
    public string          Name     { get; }
    public List<DxfEntity> Entities { get; } = new();
    public bool            Visible  { get; set; } = true;

    public ImportLayer(string name) => Name = name;
}
