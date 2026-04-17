using System;

namespace MangelManager.Models;

public class Mangel
{
    public int Id { get; set; }
    public string MangelNummer { get; set; } = "";
    public string Titel { get; set; } = "";
    public string Beschreibung { get; set; } = "";
    public string Gewerk { get; set; } = "";
    public string Ort { get; set; } = "";
    public string Etage { get; set; } = "";
    public string Raum { get; set; } = "";
    public MangelStatus Status { get; set; } = MangelStatus.Offen;
    public MangelPrioritaet Prioritaet { get; set; } = MangelPrioritaet.Mittel;
    public DateTime ErfasstAm { get; set; } = DateTime.Now;
    public DateTime? FaelligAm { get; set; }
    public DateTime? ErledigtAm { get; set; }
    public string Erfasser { get; set; } = "";
    public string Zustaendig { get; set; } = "";
    public string Bemerkung { get; set; } = "";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool HatFotos => Fotos.Count > 0;
    public System.Collections.Generic.List<MangelFoto> Fotos { get; set; } = new();
}
