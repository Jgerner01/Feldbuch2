using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using MangelManager.Models;

namespace MangelManager.Data;

public class MangelRepository
{
    private readonly DatabaseContext _db;

    public MangelRepository(DatabaseContext db)
    {
        _db = db;
    }

    // Alle Mängel laden
    public List<Mangel> GetAll()
    {
        var maengel = new List<Mangel>();
        using var connection = _db.CreateConnection();
        connection.Open();

        var sql = "SELECT * FROM Mangel ORDER BY ErfasstAm DESC";
        using var command = new SqliteCommand(sql, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            maengel.Add(ReadMangel(reader));
        }

        // Fotos für jeden Mangel laden
        foreach (var mangel in maengel)
        {
            mangel.Fotos = GetFotosForMangel(mangel.Id, connection);
        }

        return maengel;
    }

    // Mangel nach ID laden
    public Mangel? GetById(int id)
    {
        using var connection = _db.CreateConnection();
        connection.Open();

        var sql = "SELECT * FROM Mangel WHERE Id = @id";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        using var reader = command.ExecuteReader();

        if (reader.Read())
        {
            var mangel = ReadMangel(reader);
            mangel.Fotos = GetFotosForMangel(mangel.Id, connection);
            return mangel;
        }
        return null;
    }

    // Mängel filtern
    public List<Mangel> FilterBy(MangelStatus? status = null, MangelPrioritaet? prioritaet = null, string? gewerk = null, string? suchbegriff = null)
    {
        var results = GetAll();

        if (status.HasValue)
            results = results.Where(m => m.Status == status.Value).ToList();

        if (prioritaet.HasValue)
            results = results.Where(m => m.Prioritaet == prioritaet.Value).ToList();

        if (!string.IsNullOrWhiteSpace(gewerk))
            results = results.Where(m => m.Gewerk.Equals(gewerk, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrWhiteSpace(suchbegriff))
        {
            var such = suchbegriff.ToLower();
            results = results.Where(m =>
                m.Titel.ToLower().Contains(such) ||
                m.Beschreibung.ToLower().Contains(such) ||
                m.MangelNummer.ToLower().Contains(such) ||
                m.Ort.ToLower().Contains(such)
            ).ToList();
        }

        return results;
    }

    // Neuen Mangel speichern
    public int Insert(Mangel mangel)
    {
        using var connection = _db.CreateConnection();
        connection.Open();

        var sql = """
            INSERT INTO Mangel (MangelNummer, Titel, Beschreibung, Gewerk, Ort, Etage, Raum, Status, Prioritaet,
                                ErfasstAm, FaelligAm, Erfasser, Zustaendig, Bemerkung, Latitude, Longitude)
            VALUES (@MangelNummer, @Titel, @Beschreibung, @Gewerk, @Ort, @Etage, @Raum, @Status, @Prioritaet,
                    @ErfasstAm, @FaelligAm, @Erfasser, @Zustaendig, @Bemerkung, @Latitude, @Longitude);
            SELECT last_insert_rowid();
            """;

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@MangelNummer", mangel.MangelNummer);
        command.Parameters.AddWithValue("@Titel", mangel.Titel);
        command.Parameters.AddWithValue("@Beschreibung", mangel.Beschreibung ?? "");
        command.Parameters.AddWithValue("@Gewerk", mangel.Gewerk ?? "");
        command.Parameters.AddWithValue("@Ort", mangel.Ort ?? "");
        command.Parameters.AddWithValue("@Etage", mangel.Etage ?? "");
        command.Parameters.AddWithValue("@Raum", mangel.Raum ?? "");
        command.Parameters.AddWithValue("@Status", (int)mangel.Status);
        command.Parameters.AddWithValue("@Prioritaet", (int)mangel.Prioritaet);
        command.Parameters.AddWithValue("@ErfasstAm", mangel.ErfasstAm.ToString("O"));
        command.Parameters.AddWithValue("@FaelligAm", mangel.FaelligAm?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Erfasser", mangel.Erfasser ?? "");
        command.Parameters.AddWithValue("@Zustaendig", mangel.Zustaendig ?? "");
        command.Parameters.AddWithValue("@Bemerkung", mangel.Bemerkung ?? "");
        command.Parameters.AddWithValue("@Latitude", mangel.Latitude ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Longitude", mangel.Longitude ?? (object)DBNull.Value);

        var id = Convert.ToInt64(command.ExecuteScalar());
        mangel.Id = (int)id;
        return mangel.Id;
    }

    // Mangel aktualisieren
    public void Update(Mangel mangel)
    {
        using var connection = _db.CreateConnection();
        connection.Open();

        var sql = """
            UPDATE Mangel SET
                MangelNummer = @MangelNummer, Titel = @Titel, Beschreibung = @Beschreibung,
                Gewerk = @Gewerk, Ort = @Ort, Etage = @Etage, Raum = @Raum,
                Status = @Status, Prioritaet = @Prioritaet, FaelligAm = @FaelligAm,
                ErledigtAm = @ErledigtAm, Zustaendig = @Zustaendig, Bemerkung = @Bemerkung,
                Latitude = @Latitude, Longitude = @Longitude
            WHERE Id = @Id
            """;

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", mangel.Id);
        command.Parameters.AddWithValue("@MangelNummer", mangel.MangelNummer);
        command.Parameters.AddWithValue("@Titel", mangel.Titel);
        command.Parameters.AddWithValue("@Beschreibung", mangel.Beschreibung ?? "");
        command.Parameters.AddWithValue("@Gewerk", mangel.Gewerk ?? "");
        command.Parameters.AddWithValue("@Ort", mangel.Ort ?? "");
        command.Parameters.AddWithValue("@Etage", mangel.Etage ?? "");
        command.Parameters.AddWithValue("@Raum", mangel.Raum ?? "");
        command.Parameters.AddWithValue("@Status", (int)mangel.Status);
        command.Parameters.AddWithValue("@Prioritaet", (int)mangel.Prioritaet);
        command.Parameters.AddWithValue("@FaelligAm", mangel.FaelligAm?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ErledigtAm", mangel.ErledigtAm?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Zustaendig", mangel.Zustaendig ?? "");
        command.Parameters.AddWithValue("@Bemerkung", mangel.Bemerkung ?? "");
        command.Parameters.AddWithValue("@Latitude", mangel.Latitude ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Longitude", mangel.Longitude ?? (object)DBNull.Value);

        command.ExecuteNonQuery();
    }

    // Mangel löschen
    public void Delete(int id)
    {
        using var connection = _db.CreateConnection();
        connection.Open();

        // Zuerst Fotos löschen
        using var delFotos = new SqliteCommand("DELETE FROM MangelFoto WHERE MangelId = @id", connection);
        delFotos.Parameters.AddWithValue("@id", id);
        delFotos.ExecuteNonQuery();

        using var delMangel = new SqliteCommand("DELETE FROM Mangel WHERE Id = @id", connection);
        delMangel.Parameters.AddWithValue("@id", id);
        delMangel.ExecuteNonQuery();
    }

    // Nächste Mangelnummer generieren
    public string GetNextMangelNummer()
    {
        using var connection = _db.CreateConnection();
        connection.Open();

        var sql = "SELECT COUNT(*) FROM Mangel";
        using var command = new SqliteCommand(sql, connection);
        var count = Convert.ToInt32(command.ExecuteScalar());
        return $"M-{DateTime.Now:yyyy}-{count + 1:D4}";
    }

    // Alle Gewerke laden (für Filter)
    public List<string> GetAllGewerke()
    {
        var gewerke = new List<string>();
        using var connection = _db.CreateConnection();
        connection.Open();

        var sql = "SELECT DISTINCT Gewerk FROM Mangel WHERE Gewerk != '' ORDER BY Gewerk";
        using var command = new SqliteCommand(sql, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            gewerke.Add(reader.GetString(0));
        }
        return gewerke;
    }

    // Fotos für einen Mangel
    private List<MangelFoto> GetFotosForMangel(int mangelId, SqliteConnection connection)
    {
        var fotos = new List<MangelFoto>();
        var sql = "SELECT * FROM MangelFoto WHERE MangelId = @id ORDER BY ErstelltAm";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@id", mangelId);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            fotos.Add(ReadMangelFoto(reader));
        }
        return fotos;
    }

    public void InsertFoto(MangelFoto foto)
    {
        using var connection = _db.CreateConnection();
        connection.Open();

        var sql = "INSERT INTO MangelFoto (MangelId, Dateipfad, Beschreibung, ErstelltAm) VALUES (@MangelId, @Dateipfad, @Beschreibung, @ErstelltAm); SELECT last_insert_rowid();";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@MangelId", foto.MangelId);
        command.Parameters.AddWithValue("@Dateipfad", foto.Dateipfad);
        command.Parameters.AddWithValue("@Beschreibung", foto.Beschreibung ?? "");
        command.Parameters.AddWithValue("@ErstelltAm", foto.ErstelltAm.ToString("O"));

        foto.Id = Convert.ToInt32(command.ExecuteScalar());
    }

    public void DeleteFoto(int fotoId, string dateipfad)
    {
        try
        {
            if (File.Exists(dateipfad))
                File.Delete(dateipfad);
        }
        catch { /* Ignorieren, wenn Datei nicht gefunden */ }

        using var connection = _db.CreateConnection();
        connection.Open();
        using var command = new SqliteCommand("DELETE FROM MangelFoto WHERE Id = @id", connection);
        command.Parameters.AddWithValue("@id", fotoId);
        command.ExecuteNonQuery();
    }

    private static Mangel ReadMangel(SqliteDataReader reader)
    {
        return new Mangel
        {
            Id = reader.GetInt32(0),
            MangelNummer = reader.GetString(1),
            Titel = reader.GetString(2),
            Beschreibung = reader.IsDBNull(3) ? "" : reader.GetString(3),
            Gewerk = reader.IsDBNull(4) ? "" : reader.GetString(4),
            Ort = reader.IsDBNull(5) ? "" : reader.GetString(5),
            Etage = reader.IsDBNull(6) ? "" : reader.GetString(6),
            Raum = reader.IsDBNull(7) ? "" : reader.GetString(7),
            Status = (MangelStatus)reader.GetInt32(8),
            Prioritaet = (MangelPrioritaet)reader.GetInt32(9),
            ErfasstAm = DateTime.Parse(reader.GetString(10)),
            FaelligAm = reader.IsDBNull(11) ? null : DateTime.Parse(reader.GetString(11)),
            ErledigtAm = reader.IsDBNull(12) ? null : DateTime.Parse(reader.GetString(12)),
            Erfasser = reader.IsDBNull(13) ? "" : reader.GetString(13),
            Zustaendig = reader.IsDBNull(14) ? "" : reader.GetString(14),
            Bemerkung = reader.IsDBNull(15) ? "" : reader.GetString(15),
            Latitude = reader.IsDBNull(16) ? null : reader.GetDouble(16),
            Longitude = reader.IsDBNull(17) ? null : reader.GetDouble(17)
        };
    }

    private static MangelFoto ReadMangelFoto(SqliteDataReader reader)
    {
        return new MangelFoto
        {
            Id = reader.GetInt32(0),
            MangelId = reader.GetInt32(1),
            Dateipfad = reader.GetString(2),
            Beschreibung = reader.IsDBNull(3) ? "" : reader.GetString(3),
            ErstelltAm = DateTime.Parse(reader.GetString(4))
        };
    }
}
