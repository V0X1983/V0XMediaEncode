using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using V0XMediaEncode.Core.Models;

namespace V0XMediaEncode.App.Converters;

/// <summary>
/// Formats an <see cref="EncodeJob"/>'s live progress (percent, speed, ETA) into one compact string
/// for the queue row, e.g. "42 % · 2.3x · 1:23 restant". Speed/ETA are only meaningful while actually
/// encoding (ffmpeg hasn't reported either yet for a queued/ready job, and they're stale once a job
/// finishes), so those two segments are omitted outside <see cref="EncodeStatus.Encoding"/>.
/// </summary>
public sealed class JobProgressDetailConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not EncodeJob job)
        {
            return string.Empty;
        }

        var percentText = $"{job.ProgressPercent:0} %";
        if (job.Status != EncodeStatus.Encoding)
        {
            return percentText;
        }

        var parts = new List<string> { percentText };
        if (job.SpeedFactor is { } speed)
        {
            parts.Add($"{speed:0.0}x");
        }
        if (job.Eta is { } eta)
        {
            parts.Add($"{FormatTimeSpan(eta)} restant");
        }

        return string.Join(" · ", parts);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();

    private static string FormatTimeSpan(TimeSpan value)
    {
        if (value < TimeSpan.Zero)
        {
            value = TimeSpan.Zero;
        }

        return value.TotalHours >= 1
            ? value.ToString(@"h\:mm\:ss")
            : value.ToString(@"m\:ss");
    }
}

/// <summary>Visible when the bound string has content — used for the per-row error message, which
/// only <see cref="EncodeJob.ErrorMessage"/> being non-empty (set exactly when a job actually failed)
/// should reveal.</summary>
public sealed class NotNullOrEmptyToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is string { Length: > 0 } ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
