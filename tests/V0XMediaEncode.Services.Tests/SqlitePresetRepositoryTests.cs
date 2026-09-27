using V0XMediaEncode.Core.Models;
using V0XMediaEncode.Services.Presets;

namespace V0XMediaEncode.Services.Tests;

/// <summary>
/// Covers the InitializeAsync seeding/migration behavior: a fresh database gets every built-in
/// preset, and a database from an older app version (missing built-ins added since) gets only the
/// missing ones added - existing rows (built-in or user-created) are left untouched.
/// </summary>
public sealed class SqlitePresetRepositoryTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"v0x-preset-tests-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task InitializeAsync_OnFreshDatabase_SeedsEveryBuiltInPreset()
    {
        var repository = new SqlitePresetRepository(_dbPath);
        await repository.InitializeAsync();

        var presets = await repository.GetAllAsync();

        Assert.Equal(DefaultPresets.CreateAll().Count, presets.Count);
        Assert.All(presets, p => Assert.True(p.IsBuiltIn));
    }

    [Fact]
    public async Task InitializeAsync_OnDatabaseMissingNewerBuiltIns_AddsOnlyTheMissingOnes()
    {
        var firstRunRepository = new SqlitePresetRepository(_dbPath);
        await firstRunRepository.InitializeAsync();

        var allBuiltIns = await firstRunRepository.GetAllAsync();
        var keptPreset = allBuiltIns.First();

        // Simulate an older install whose database only ever saw one built-in preset by deleting
        // the rest - this is the same shape as a real user's database from before newer built-ins
        // were added to DefaultPresets.CreateAll().
        foreach (var preset in allBuiltIns.Where(p => p.Id != keptPreset.Id))
        {
            await firstRunRepository.DeleteAsync(preset.Id);
        }

        var userPreset = new EncodePreset { Name = "Mon preset perso", IsBuiltIn = false };
        await firstRunRepository.SaveAsync(userPreset);

        var laterRunRepository = new SqlitePresetRepository(_dbPath);
        await laterRunRepository.InitializeAsync();

        var presetsAfterMigration = await laterRunRepository.GetAllAsync();

        Assert.Equal(DefaultPresets.CreateAll().Count + 1, presetsAfterMigration.Count);
        Assert.Contains(presetsAfterMigration, p => p.Id == keptPreset.Id);
        Assert.Contains(presetsAfterMigration, p => p.Id == userPreset.Id && !p.IsBuiltIn);
        Assert.Equal(
            DefaultPresets.CreateAll().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal),
            presetsAfterMigration.Where(p => p.IsBuiltIn).Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
    }

    public void Dispose()
    {
        try
        {
            File.Delete(_dbPath);
        }
        catch (IOException)
        {
        }
    }
}
