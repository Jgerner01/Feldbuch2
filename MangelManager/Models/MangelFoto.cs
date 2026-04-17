using System;

namespace MangelManager.Models;

public class MangelFoto
{
    public int Id { get; set; }
    public int MangelId { get; set; }
    public string Dateipfad { get; set; } = "";
    public string Beschreibung { get; set; } = "";
    public DateTime ErstelltAm { get; set; } = DateTime.Now;
}
