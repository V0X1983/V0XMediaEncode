namespace V0XMediaEncode.Core.Models;

/// <summary>A completed (successful, failed, or cancelled) queue job, kept for the Historique page.</summary>
public sealed class HistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string SourcePath { get; set; }

    public required string OutputPath { get; set; }

    public string? PresetName { get; set; }

    public EncodeStatus Status { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset CompletedAt { get; set; }

    public TimeSpan Duration => CompletedAt - StartedAt;

    public string? ErrorMessage { get; set; }

    /// <summary>Last lines of ffmpeg's stderr output, kept for export/diagnostics.</summary>
    public string? LogTail { get; set; }

    public string FileName => Path.GetFileName(SourcePath);
}
