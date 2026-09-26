using System.Globalization;
using System.Text.RegularExpressions;
using V0XMediaEncode.Core.Models;

namespace V0XMediaEncode.Services.Ffmpeg;

/// <summary>
/// Pure parsing of ffmpeg's human-readable stderr status lines
/// (`frame=  123 fps= 30 q=28.0 size=    512kB time=00:00:04.10 bitrate= 999.0kbits/s speed=1.02x`)
/// into an <see cref="EncodeProgress"/>. Contains no I/O so it is directly unit-testable.
/// </summary>
public static class FFmpegProgressParser
{
    private static readonly Regex FrameRegex = new(@"frame=\s*(\d+)", RegexOptions.Compiled);
    private static readonly Regex FpsRegex = new(@"(?<!\w)fps=\s*([\d.]+)", RegexOptions.Compiled);
    private static readonly Regex TimeRegex = new(@"time=\s*(-?\d{2}:\d{2}:\d{2}\.\d{2})", RegexOptions.Compiled);
    private static readonly Regex SpeedRegex = new(@"speed=\s*([\d.]+)x", RegexOptions.Compiled);
    private static readonly Regex SizeRegex = new(@"\bL?size=\s*(\d+(?:\.\d+)?)\s*(kB|KiB|mB|MiB|B)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Attempts to parse a single stderr line emitted by a running ffmpeg process.
    /// Returns false for any line that isn't a progress report (banner, warnings, ...).
    /// </summary>
    public static bool TryParse(string line, TimeSpan? totalDuration, out EncodeProgress progress)
    {
        progress = default;

        var frameMatch = FrameRegex.Match(line);
        var timeMatch = TimeRegex.Match(line);
        if (!frameMatch.Success || !timeMatch.Success)
        {
            return false;
        }

        var frame = long.Parse(frameMatch.Groups[1].Value, CultureInfo.InvariantCulture);
        var time = ParseFfmpegTimestamp(timeMatch.Groups[1].Value);

        double? fps = TryParseDouble(FpsRegex.Match(line));
        double? speed = TryParseDouble(SpeedRegex.Match(line));
        long? sizeBytes = ParseSize(SizeRegex.Match(line));

        var (percent, eta) = ComputeProgress(time, speed, totalDuration);

        progress = new EncodeProgress(frame, fps, time, speed, sizeBytes, percent, eta);
        return true;
    }

    /// <summary>Parses an ffmpeg `HH:MM:SS.ff` timestamp (centisecond precision) using integer arithmetic
    /// throughout, so a value like ".23" always lands on exactly 230ms instead of drifting by a tick or two
    /// the way `double`-based seconds parsing would (0.23 has no exact binary floating-point representation).</summary>
    public static TimeSpan ParseFfmpegTimestamp(string text)
    {
        var negative = text.StartsWith('-');
        var span = negative ? text.AsSpan(1) : text.AsSpan();

        var hours = int.Parse(span[..2], NumberStyles.Integer, CultureInfo.InvariantCulture);
        var minutes = int.Parse(span[3..5], NumberStyles.Integer, CultureInfo.InvariantCulture);
        var seconds = int.Parse(span[6..8], NumberStyles.Integer, CultureInfo.InvariantCulture);

        var fraction = span.Length > 9 ? span[9..] : [];
        Span<char> msDigits = stackalloc char[3];
        for (var i = 0; i < 3; i++)
        {
            msDigits[i] = i < fraction.Length ? fraction[i] : '0';
        }
        var milliseconds = int.Parse(msDigits, NumberStyles.Integer, CultureInfo.InvariantCulture);

        var value = new TimeSpan(0, hours, minutes, seconds, milliseconds);
        return negative ? value.Negate() : value;
    }

    private static (double Percent, TimeSpan? Eta) ComputeProgress(TimeSpan time, double? speed, TimeSpan? totalDuration)
    {
        if (totalDuration is not { } duration || duration <= TimeSpan.Zero)
        {
            return (0, null);
        }

        var percent = Math.Clamp(time.TotalSeconds / duration.TotalSeconds * 100.0, 0, 100);

        TimeSpan? eta = null;
        if (speed is > 0)
        {
            var remainingSeconds = (duration - time).TotalSeconds;
            eta = TimeSpan.FromSeconds(Math.Max(0, remainingSeconds / speed.Value));
        }

        return (percent, eta);
    }

    private static double? TryParseDouble(Match match) =>
        match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    private static long? ParseSize(Match match)
    {
        if (!match.Success || !double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return null;
        }

        double multiplier = match.Groups[2].Value.ToLowerInvariant() switch
        {
            "kb" or "kib" => 1024,
            "mb" or "mib" => 1024 * 1024,
            _ => 1,
        };

        return (long)(value * multiplier);
    }
}
