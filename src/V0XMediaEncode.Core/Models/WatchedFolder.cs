namespace V0XMediaEncode.Core.Models;

/// <summary>A folder monitored for new source files, each auto-queued under a default preset.</summary>
public sealed class WatchedFolder
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Path { get; set; }

    public Guid? DefaultPresetId { get; set; }

    public bool IsEnabled { get; set; } = true;

    public bool IncludeSubdirectories { get; set; }
}
