using System.Globalization;

namespace V0XMediaEncode.Services.Ffmpeg;

/// <summary>Pure formatting of a <see cref="TimeSpan"/> into the `HH:MM:SS.ff` form ffmpeg's `-ss` expects.</summary>
public static class FFmpegTimestampFormatter
{
    public static string Format(TimeSpan value)
    {
        if (value < TimeSpan.Zero)
        {
            value = TimeSpan.Zero;
        }

        // TimeSpan's built-in "hh" custom format only shows the hour-of-day component (wraps at 24,
        // rolling the excess into a "d" days component), whereas ffmpeg's -ss just wants total hours
        // with no cap — so the hour part is computed by hand instead of via ToString("hh\\:...").
        var totalHours = (int)value.TotalHours;
        var centiseconds = value.Milliseconds / 10;
        return string.Create(CultureInfo.InvariantCulture, $"{totalHours:00}:{value.Minutes:00}:{value.Seconds:00}.{centiseconds:00}");
    }
}
