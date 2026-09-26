using System.Text.Json;
using Microsoft.Data.Sqlite;
using V0XMediaEncode.Core.Models;

namespace V0XMediaEncode.Services.History;

/// <summary>Append-only log of completed jobs, one JSON blob per row, in its own %LocalAppData%\V0XMediaEncode db.</summary>
public sealed class SqliteHistoryRepository : IHistoryRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    private readonly string _connectionString;

    public SqliteHistoryRepository(string? databasePath = null)
    {
        var path = databasePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "V0XMediaEncode",
            "history.db");

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ToString();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var createTable = connection.CreateCommand();
        createTable.CommandText =
            """
            CREATE TABLE IF NOT EXISTS History (
                Id TEXT PRIMARY KEY,
                CompletedAt TEXT NOT NULL,
                Json TEXT NOT NULL
            );
            """;
        await createTable.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<HistoryEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Json FROM History ORDER BY CompletedAt DESC;";

        var results = new List<HistoryEntry>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var entry = JsonSerializer.Deserialize<HistoryEntry>(reader.GetString(0), JsonOptions);
            if (entry is not null)
            {
                results.Add(entry);
            }
        }

        return results;
    }

    public async Task AddAsync(HistoryEntry entry, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO History (Id, CompletedAt, Json) VALUES ($id, $completedAt, $json);";
        command.Parameters.AddWithValue("$id", entry.Id.ToString());
        command.Parameters.AddWithValue("$completedAt", entry.CompletedAt.ToString("O"));
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(entry, JsonOptions));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM History;";
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
