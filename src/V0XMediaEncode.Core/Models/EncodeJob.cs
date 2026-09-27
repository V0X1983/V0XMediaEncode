using CommunityToolkit.Mvvm.ComponentModel;

namespace V0XMediaEncode.Core.Models;

/// <summary>One item of the encode queue: a source file, a destination, the preset applied to it, and its live state.</summary>
public sealed partial class EncodeJob : ObservableObject
{
    public Guid Id { get; } = Guid.NewGuid();

    public required string SourcePath { get; init; }

    [ObservableProperty]
    private string _outputPath = string.Empty;

    [ObservableProperty]
    private EncodePreset? _preset;

    [ObservableProperty]
    private EncodeStatus _status = EncodeStatus.Queued;

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private TimeSpan? _eta;

    [ObservableProperty]
    private double? _speedFactor;

    [ObservableProperty]
    private MediaProbeResult? _mediaInfo;

    [ObservableProperty]
    private string? _errorMessage;

    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.Now;

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string FileName => Path.GetFileName(SourcePath);

    public string OutputFileName => Path.GetFileName(OutputPath);
}
