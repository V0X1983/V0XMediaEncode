using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using V0XMediaEncode.App.Views;
using Windows.UI;

namespace V0XMediaEncode.App;

public sealed partial class MainWindow : Window
{
    /// <summary>The single main window, exposed so pages can get its HWND for WinRT picker interop (FileOpenPicker, etc.).</summary>
    public static MainWindow? Instance { get; private set; }

    public MainWindow()
    {
        InitializeComponent();
        Instance = this;

        Title = "V0X Media Encode";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = ElementTheme.Dark;
        }

        StyleCaptionButtons();
    }

    private void StyleCaptionButtons()
    {
        var titleBar = AppWindow.TitleBar;
        titleBar.BackgroundColor = Color.FromArgb(255, 0x18, 0x18, 0x18);
        titleBar.ForegroundColor = Colors.White;
        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonForegroundColor = Colors.White;
        titleBar.ButtonHoverBackgroundColor = Color.FromArgb(255, 0x2D, 0x7D, 0xD2);
        titleBar.ButtonHoverForegroundColor = Colors.White;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        NavView.SelectedItem = NavView.MenuItems[0];
        ContentFrame.Navigate(typeof(QueuePage));
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer is not NavigationViewItem item)
        {
            return;
        }

        var pageType = item.Tag?.ToString() switch
        {
            "Queue" => typeof(QueuePage),
            "Presets" => typeof(PresetsPage),
            "WatchFolders" => typeof(WatchFoldersPage),
            "History" => typeof(HistoryPage),
            "Settings" => typeof(SettingsPage),
            _ => typeof(QueuePage),
        };

        if (ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }
}
