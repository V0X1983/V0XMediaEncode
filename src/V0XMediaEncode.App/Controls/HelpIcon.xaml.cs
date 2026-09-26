using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace V0XMediaEncode.App.Controls;

/// <summary>Small "?" circle with a tooltip, used next to a field's label to explain it to new users.</summary>
public sealed partial class HelpIcon : UserControl
{
    public static readonly DependencyProperty HelpTextProperty =
        DependencyProperty.Register(nameof(HelpText), typeof(string), typeof(HelpIcon), new PropertyMetadata(string.Empty));

    public string HelpText
    {
        get => (string)GetValue(HelpTextProperty);
        set => SetValue(HelpTextProperty, value);
    }

    public HelpIcon()
    {
        InitializeComponent();
    }
}
