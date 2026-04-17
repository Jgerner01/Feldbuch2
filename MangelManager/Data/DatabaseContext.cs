using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace MangelManager.Data;

public class DatabaseContext
{
    private readonly string _connectionString;

    public DatabaseContext(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Tabelle Mängel
        CreateMangelTable(connection);
        // Tabelle Fotos
        CreateMangelFotoTable(connection);
        // Indexe für Performance
        CreateIndexes(connection);
    }

    private void CreateMangelTable(SqliteConnection connection)
    {
        var sql = """
            CREATE TABLE IF NOT EXISTS Mangel (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                MangelNummer TEXT NOT NULL UNIQUE,
                Titel TEXT NOT NULL,
                Beschreibung TEXT,
                Gewerk TEXT,
                Ort TEXT,
                Etage TEXT,
                Raum TEXT,
                Status INTEGER NOT NULL DEFAULT 0,
                Prioritaet INTEGER NOT NULL DEFAULT 1,
                ErfasstAm TEXT NOT NULL,
                FaelligAm TEXT,
                ErledigtAm TEXT,
                Erfasser TEXT,
                Zustaendig TEXT,
                Bemerkung TEXT,
                Latitude REAL,
                Longitude REAL
            )
            """;
        ExecuteNonQuery(connection, sql);
    }

    private void CreateMangelFotoTable(SqliteConnection connection)
    {
        var sql = """
            CREATE TABLE IF NOT EXISTS MangelFoto (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                MangelId INTEGER NOT NULL,
                Dateipfad TEXT NOT NULL,
                Beschreibung TEXT,
                ErstelltAm TEXT NOT NULL,
                FOREIGN KEY(MangelId) REFERENCES Mangel(Id) ON DELETE CASCADE
            )
            """;
        ExecuteNonQuery(connection, sql);
    }

    private void CreateIndexes(SqliteConnection connection)
    {
        ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_Mangel_Status ON Mangel(Status)");
        ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_Mangel_Prioritaet ON Mangel(Prioritaet)");
        ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_Mangel_Gewerk ON Mangel(Gewerk)");
        ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_MangelFoto_MangelId ON MangelFoto(MangelId)");
    }

    private static void ExecuteNonQuery(SqliteConnection connection, string sql)
    {
        using var command = new SqliteCommand(sql, connection);
        command.ExecuteNonQuery();
    }

    public SqliteConnection CreateConnection()
    {
        return new SqliteConnection(_connectionString);
    }
}
