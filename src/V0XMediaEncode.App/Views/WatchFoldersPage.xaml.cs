using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using V0XMediaEncode.App.ViewModels;
using V0XMediaEncode.Core.Models;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace V0XMediaEncode.App.Views;

public sealed partial class WatchFoldersPage : Page
{
    public WatchFoldersViewModel ViewModel { get; }

    public WatchFoldersPage()
    {
        ViewModel = App.Services.GetRequiredService<WatchFoldersViewModel>();
        InitializeComponent();
        Loaded += WatchFoldersPage_Loaded;
    }

    private async void WatchFoldersPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
    }

    private async void AddFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var folderPicker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.VideosLibrary,
        };
        folderPicker.FileTypeFilter.Add("*");

        InitializeWithWindow.Initialize(folderPicker, WindowNative.GetWindowHandle(MainWindow.Instance));

        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder is null)
        {
            return;
        }

        var presetCombo = new ComboBox
        {
            ItemsSource = ViewModel.AvailablePresets,
            DisplayMemberPath = nameof(EncodePreset.Name),
            PlaceholderText = "Preset par défaut",
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        if (ViewModel.AvailablePresets.Count > 0)
        {
            presetCombo.SelectedIndex = 0;
        }

        var includeSubfoldersCheckBox = new CheckBox { Content = "Inclure les sous-dossiers" };

        var content = new StackPanel { Spacing = 12 };
        content.Children.Add(new TextBlock { Text = folder.Path, TextWrapping = TextWrapping.Wrap });
        content.Children.Add(presetCombo);
        content.Children.Add(includeSubfoldersCheckBox);

        var dialog = new ContentDialog
        {
            Title = "Ajouter un dossier surveillé",
            Content = content,
            PrimaryButtonText = "Ajouter",
            CloseButtonText = "Annuler",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot,
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            var selectedPreset = presetCombo.SelectedItem as EncodePreset;
            await ViewModel.AddFolderAsync(folder.Path, selectedPreset?.Id, includeSubfoldersCheckBox.IsChecked == true);
        }
    }
}
