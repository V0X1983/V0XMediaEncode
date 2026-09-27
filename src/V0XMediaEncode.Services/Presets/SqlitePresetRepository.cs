using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using V0XMediaEncode.Core.Models;

namespace V0XMediaEncode.Services.Presets;

/// <summary>
/// Persists <see cref="EncodePreset"/> as a JSON blob per row in a small SQLite database under
/// %LocalAppData%\V0XMediaEncode\presets.db. A blob column (rather than one column per preset field)
/// keeps the schema stable while the preset editor's fields evolve.
/// </summary>
public sealed class SqlitePresetRepository : IPresetRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _connectionString;

    public SqlitePresetRepository(string? databasePath = null)
    {
        var path = databasePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "V0XMediaEncode",
            "presets.db");

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ToString();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using (var createTable = connection.CreateCommand())
        {
            createTable.CommandText =
                """
                CREATE TABLE IF NOT EXISTS Presets (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    IsBuiltIn INTEGER NOT NULL,
                    Json TEXT NOT NULL
                );
                """;
            await createTable.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        var existingBuiltInNames = new HashSet<string>(StringComparer.Ordinal);
        await using (var namesCommand = connection.CreateCommand())
        {
            namesCommand.CommandText = "SELECT Name FROM Presets WHERE IsBuiltIn = 1;";
            await using var reader = await namesCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                existingBuiltInNames.Add(reader.GetString(0));
            }
        }

        // Seeds on first run, and on every later launch adds whichever built-in presets a newer
        // app version introduced since this database was created - matched by Name rather than Id
        // since DefaultPresets.CreateAll() mints a fresh Id every call. Existing rows (built-in or
        // user-created) are left untouched either way.
        foreach (var preset in DefaultPresets.CreateAll())
        {
            if (existingBuiltInNames.Contains(preset.Name))
            {
                continue;
            }

            await UpsertAsync(connection, preset, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<EncodePreset>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Json FROM Presets ORDER BY IsBuiltIn DESC, Name ASC;";

        var results = new List<EncodePreset>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var json = reader.GetString(0);
            try
            {
                var preset = JsonSerializer.Deserialize<EncodePreset>(json, JsonOptions);
                if (preset is not null)
                {
                    results.Add(preset);
                }
            }
            catch (JsonException)
            {
                // One row with truncated/corrupted Json (e.g. from a process killed mid-write) used
                // to throw out of GetAllAsync entirely, losing every preset - including all built-ins
                // - not just the bad one. Skip just that row instead.
            }
        }

        return results;
    }

    public async Task SaveAsync(EncodePreset preset, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await UpsertAsync(connection, preset, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Presets WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task UpsertAsync(SqliteConnection connection, EncodePreset preset, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO Presets (Id, Name, IsBuiltIn, Json) VALUES ($id, $name, $isBuiltIn, $json)
            ON CONFLICT(Id) DO UPDATE SET Name = excluded.Name, IsBuiltIn = excluded.IsBuiltIn, Json = excluded.Json;
            """;
        command.Parameters.AddWithValue("$id", preset.Id.ToString());
        command.Parameters.AddWithValue("$name", preset.Name);
        command.Parameters.AddWithValue("$isBuiltIn", preset.IsBuiltIn ? 1 : 0);
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(preset, JsonOptions));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
