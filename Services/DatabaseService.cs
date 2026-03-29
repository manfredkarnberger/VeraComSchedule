using Microsoft.Data.Sqlite;
using System;
using VeraCom.Models;

namespace PcanSqliteSender.Services;

public class DatabaseService
{
    public List<CanMessage> LoadMessages(string dbPath)
    {
        var list = new List<CanMessage>();
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT CanID, DLC, Payload, Cycletime FROM HD";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new CanMessage
            {
                CanID = (uint)reader.GetInt32(0),
                DLC = Convert.ToByte(reader.GetInt32(1)),
                Payload = GetPayloadAsByteArray(reader, 2),
                CycleTimeMs = reader.GetInt32(3)
            });
        }
        return list;
    }

    private byte[] GetPayloadAsByteArray(Microsoft.Data.Sqlite.SqliteDataReader reader, int columnIndex)
    {
        // 1. Prüfen ob NULL
        if (reader.IsDBNull(columnIndex))
            return new byte[0];  // Leeres Array zurückgeben

        // 2. Größe des BLOBs ermitteln
        long blobSize = reader.GetBytes(columnIndex, 0, null, 0, 0);

        // 3. Buffer erstellen
        byte[] buffer = new byte[blobSize];

        // 4. BLOB-Daten in Buffer lesen
        reader.GetBytes(columnIndex, 0, buffer, 0, (int)blobSize);

        return buffer;
    }
}