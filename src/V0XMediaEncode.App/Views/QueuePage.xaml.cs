using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using V0XMediaEncode.App.ViewModels;
using V0XMediaEncode.Core.Models;
using Windows.ApplicationModel.DataTransfer;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace V0XMediaEncode.App.Views;

public sealed partial class QueuePage : Page
{
    public QueueViewModel ViewModel { get; }

    public QueuePage()
    {
        ViewModel = App.Services.GetRequiredService<QueueViewModel>();

        InitializeComponent();
        Loaded += QueuePage_Loaded;
    }

    private async void QueuePage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
    }

    private async void AddFilesButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.List,
            SuggestedStartLocation = PickerLocationId.VideosLibrary,
        };
        picker.FileTypeFilter.Add("*");

        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(MainWindow.Instance));

        var files = await picker.PickMultipleFilesAsync();
        if (files is { Count: > 0 })
        {
            await ViewModel.AddFilesAsync(files.Select(f => f.Path));
        }
    }

    private void JobsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var job = (sender as ListView)?.SelectedItem as EncodeJob;
        ViewModel.SelectedJob = job;

        PreviewPlayer.MediaPlayer?.Pause();
        PreviewPlayer.Source = job is not null ? MediaSource.CreateFromUri(new Uri(job.SourcePath)) : null;
    }

    private void RootGrid_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = e.DataView.Contains(StandardDataFormats.StorageItems)
            ? DataPackageOperation.Copy
            : DataPackageOperation.None;

        if (e.AcceptedOperation == DataPackageOperation.Copy)
        {
            e.DragUIOverride.Caption = "Ajouter à la file d'encodage";
            e.DragUIOverride.IsGlyphVisible = true;
        }
    }

    private async void RootGrid_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            return;
        }

        var items = await e.DataView.GetStorageItemsAsync();
        var filePaths = items.OfType<StorageFile>().Select(f => f.Path);
        await ViewModel.AddFilesAsync(filePaths);
    }
}
