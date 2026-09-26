namespace V0XMediaEncode.Core.Models;

/// <summary>One snapshot of ffmpeg's progress, produced by parsing a `-progress`/stderr status line.</summary>
public readonly record struct EncodeProgress(
    long FrameNumber,
    double? Fps,
    TimeSpan TimeProcessed,
    double? SpeedFactor,
    long? OutputSizeBytes,
    double PercentComplete,
    TimeSpan? Eta);
