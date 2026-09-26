using System.Globalization;
using V0XMediaEncode.Core.Models;

namespace V0XMediaEncode.Services.Ffmpeg;

/// <summary>Pure mapping from the raw ffprobe JSON DTOs to the flattened, UI-facing <see cref="MediaProbeResult"/>.</summary>
public static class FFprobeResultMapper
{
    public static MediaProbeResult Map(FFprobeOutput output)
    {
        var videoStream = output.Streams.FirstOrDefault(s => s.CodecType == "video");
        var audioStream = output.Streams.FirstOrDefault(s => s.CodecType == "audio");

        return new MediaProbeResult
        {
            Duration = ParseDurationSeconds(output.Format?.Duration),
            BitrateBps = ParseLong(output.Format?.BitRate),
            FormatName = output.Format?.FormatName,
            VideoCodecName = videoStream?.CodecName,
            Width = videoStream?.Width,
            Height = videoStream?.Height,
            FrameRate = ParseFrameRate(videoStream?.RFrameRate),
            AudioCodecName = audioStream?.CodecName,
            AudioSampleRateHz = ParseInt(audioStream?.SampleRate),
            AudioChannels = audioStream?.Channels,
        };
    }

    private static TimeSpan ParseDurationSeconds(string? text) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            ? TimeSpan.FromSeconds(seconds)
            : TimeSpan.Zero;

    private static long? ParseLong(string? text) =>
        long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    private static int? ParseInt(string? text) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    /// <summary>ffprobe reports frame rate as a rational "num/den" string (e.g. "30000/1001").</summary>
    private static double? ParseFrameRate(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var parts = text.Split('/', 2);
        if (parts.Length != 2
            || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var numerator)
            || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var denominator)
            || denominator == 0)
        {
            return null;
        }

        return numerator / denominator;
    }
}
