using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace V0XMediaEncode.App.Converters;

/// <summary>Visible when the bound value is null, Collapsed otherwise — the inverse of the usual null-check
/// converter, used for a placeholder hint shown only until something (e.g. a preview selection) is set.</summary>
public sealed class NullToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>
/// NumberBox.Value is a non-nullable double that uses NaN to mean "empty"; the preset fields it edits
/// (CRF, bitrates, dimensions, ...) are nullable ints or doubles, so every NumberBox binding needs one
/// of these to bridge the two representations.
/// </summary>
public sealed class NullableIntToDoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is int i ? (double)i : double.NaN;

    public object? ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is double d && !double.IsNaN(d) ? (int)Math.Round(d) : null;
}

public sealed class NullableDoubleToDoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is double d ? d : double.NaN;

    public object? ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is double d && !double.IsNaN(d) ? d : null;
}
