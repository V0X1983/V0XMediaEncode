namespace V0XMediaEncode.Core.Models;

/// <summary>Flattened result of an `ffprobe -show_format -show_streams` call, enough to drive the UI and preset defaults.</summary>
public sealed class MediaProbeResult
{
    public TimeSpan Duration { get; set; }

    public long? BitrateBps { get; set; }

    public string? FormatName { get; set; }

    public string? VideoCodecName { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public double? FrameRate { get; set; }

    public string? AudioCodecName { get; set; }

    public int? AudioSampleRateHz { get; set; }

    public int? AudioChannels { get; set; }

    public bool HasVideo => VideoCodecName is not null;

    public bool HasAudio => AudioCodecName is not null;
}
