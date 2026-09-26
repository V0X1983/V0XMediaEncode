using System.Text.Json;
using Microsoft.Data.Sqlite;
using V0XMediaEncode.Core.Models;

namespace V0XMediaEncode.Services.WatchFolders;

/// <summary>
/// Persists <see cref="WatchedFolder"/> configs as a JSON blob per row, in the same
/// %LocalAppData%\V0XMediaEncode folder as the preset database but its own file, since the two
/// tables have unrelated lifecycles.
/// </summary>
public sealed class SqliteWatchFolderRepository : IWatchFolderRepository
{
    private readonly string _connectionString;

    public SqliteWatchFolderRepository(string? databasePath = null)
    {
        var path = databasePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "V0XMediaEncode",
            "watchfolders.db");

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
            CREATE TABLE IF NOT EXISTS WatchFolders (
                Id TEXT PRIMARY KEY,
                Path TEXT NOT NULL,
                Json TEXT NOT NULL
            );
            """;
        await createTable.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WatchedFolder>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Json FROM WatchFolders ORDER BY Path ASC;";

        var results = new List<WatchedFolder>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var folder = JsonSerializer.Deserialize<WatchedFolder>(reader.GetString(0));
            if (folder is not null)
            {
                results.Add(folder);
            }
        }

        return results;
    }

    public async Task SaveAsync(WatchedFolder folder, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO WatchFolders (Id, Path, Json) VALUES ($id, $path, $json)
            ON CONFLICT(Id) DO UPDATE SET Path = excluded.Path, Json = excluded.Json;
            """;
        command.Parameters.AddWithValue("$id", folder.Id.ToString());
        command.Parameters.AddWithValue("$path", folder.Path);
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(folder));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM WatchFolders WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
