using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using V0XMediaEncode.Core.Models;

namespace V0XMediaEncode.App.Converters;

/// <summary>
/// Formats <see cref="EncodeJob.ProgressPercent"/> as "42 %". Split into three separate per-property
/// converters (this one, <see cref="SpeedFactorToTextConverter"/>, <see cref="EtaToTextConverter"/>)
/// bound from three <c>&lt;Run&gt;</c> inline elements inside one TextBlock, each to its own specific
/// property path, rather than one converter bound to the whole EncodeJob with an empty path: WinUI's
/// classic Binding only re-evaluates a converter when the bound PATH's property changes - a path-less
/// `{Binding}` binds to the object reference itself, which never changes, so the converter never
/// re-ran after the first render even though ProgressPercent/Eta/SpeedFactor kept updating (visible
/// live only on the ProgressBar next to it, which binds directly to ProgressPercent).
/// </summary>
public sealed class PercentToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is double percent ? $"{percent:0} %" : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Formats <see cref="EncodeJob.SpeedFactor"/> as " · 2.3x" (leading separator, empty when null).</summary>
public sealed class SpeedFactorToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is double speed ? $" · {speed:0.0}x" : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Formats <see cref="EncodeJob.Eta"/> as " · 1:23 restant" (leading separator, empty when null).</summary>
public sealed class EtaToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not TimeSpan eta)
        {
            return string.Empty;
        }

        if (eta < TimeSpan.Zero)
        {
            eta = TimeSpan.Zero;
        }

        var formatted = eta.TotalHours >= 1 ? eta.ToString(@"h\:mm\:ss") : eta.ToString(@"m\:ss");
        return $" · {formatted} restant";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
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
