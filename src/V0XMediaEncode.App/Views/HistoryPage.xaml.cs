using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using V0XMediaEncode.App.ViewModels;
using V0XMediaEncode.Core.Models;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace V0XMediaEncode.App.Views;

public sealed partial class HistoryPage : Page
{
    public HistoryViewModel ViewModel { get; }

    public HistoryPage()
    {
        ViewModel = App.Services.GetRequiredService<HistoryViewModel>();
        InitializeComponent();
        Loaded += HistoryPage_Loaded;
    }

    private async void HistoryPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
    }

    private async void ExportAllButton_Click(object sender, RoutedEventArgs e)
    {
        await ExportToFileAsync("v0x-historique", ViewModel.FormatAllForExport());
    }

    private async void ExportEntryButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not HistoryEntry entry)
        {
            return;
        }

        var suggestedName = Path.GetFileNameWithoutExtension(entry.FileName) + "_journal";
        await ExportToFileAsync(suggestedName, ViewModel.FormatEntryForExport(entry));
    }

    private async Task ExportToFileAsync(string suggestedFileName, string content)
    {
        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = suggestedFileName,
        };
        picker.FileTypeChoices.Add("Fichier texte", [".txt"]);

        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(MainWindow.Instance));

        var file = await picker.PickSaveFileAsync();
        if (file is null)
        {
            return;
        }

        await FileIO.WriteTextAsync(file, content);
    }
}
