using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Serilog;
using V0XMediaEncode.Core.Models;
using V0XMediaEncode.Services.Presets;
using V0XMediaEncode.Services.WatchFolders;

namespace V0XMediaEncode.App.ViewModels;

/// <summary>
/// Backs the "Dossiers surveillés" page: configured watch folders, plus wiring detected files
/// straight into the encode queue under each folder's own default preset.
/// </summary>
public sealed partial class WatchFoldersViewModel : ObservableObject, IDisposable
{
    private readonly IWatchFolderRepository _watchFolderRepository;
    private readonly WatchFolderService _watchFolderService;
    private readonly IPresetRepository _presetRepository;
    private readonly QueueViewModel _queueViewModel;
    private readonly ILogger _logger;
    private readonly DispatcherQueue _dispatcherQueue;

    public ObservableCollection<WatchedFolder> Folders { get; } = [];

    /// <summary>Display-ready projection of <see cref="Folders"/> that the page actually binds to.</summary>
    public ObservableCollection<WatchedFolderRow> Rows { get; } = [];

    public ObservableCollection<EncodePreset> AvailablePresets { get; } = [];

    public WatchFoldersViewModel(
        IWatchFolderRepository watchFolderRepository,
        WatchFolderService watchFolderService,
        IPresetRepository presetRepository,
        QueueViewModel queueViewModel,
        ILogger logger)
    {
        _watchFolderRepository = watchFolderRepository;
        _watchFolderService = watchFolderService;
        _presetRepository = presetRepository;
        _queueViewModel = queueViewModel;
        _logger = logger;

        // Captured here (constructed the first time WatchFoldersPage is navigated to, on the UI
        // thread) so OnFileDetected — raised from WatchFolderService's background poll — can hop
        // back to the UI thread before touching QueueViewModel's ObservableCollection.
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException("WatchFoldersViewModel doit être créé sur le thread UI.");

        _watchFolderService.FileDetected += OnFileDetected;
    }

    public async Task InitializeAsync()
    {
        await _watchFolderRepository.InitializeAsync().ConfigureAwait(true);
        await _presetRepository.InitializeAsync().ConfigureAwait(true);

        var presets = await _presetRepository.GetAllAsync().ConfigureAwait(true);
        AvailablePresets.Clear();
        foreach (var preset in presets)
        {
            AvailablePresets.Add(preset);
        }

        await ReloadFoldersAsync().ConfigureAwait(true);
    }

    private async Task ReloadFoldersAsync()
    {
        var folders = await _watchFolderRepository.GetAllAsync().ConfigureAwait(true);

        Folders.Clear();
        Rows.Clear();
        foreach (var folder in folders)
        {
            Folders.Add(folder);
            Rows.Add(new WatchedFolderRow(folder, GetPresetName(folder.DefaultPresetId)));
        }

        _watchFolderService.SetActiveFolders(Folders);
    }

    public string GetPresetName(Guid? presetId) =>
        presetId is { } id
            ? AvailablePresets.FirstOrDefault(p => p.Id == id)?.Name ?? "(preset introuvable)"
            : "(aucun)";

    public async Task AddFolderAsync(string path, Guid? defaultPresetId, bool includeSubdirectories)
    {
        var folder = new WatchedFolder
        {
            Path = path,
            DefaultPresetId = defaultPresetId,
            IncludeSubdirectories = includeSubdirectories,
        };

        await _watchFolderRepository.SaveAsync(folder).ConfigureAwait(true);
        await ReloadFoldersAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task RemoveFolderAsync(WatchedFolder? folder)
    {
        if (folder is null)
        {
            return;
        }

        await _watchFolderRepository.DeleteAsync(folder.Id).ConfigureAwait(true);
        await ReloadFoldersAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task ToggleEnabledAsync(WatchedFolder? folder)
    {
        if (folder is null)
        {
            return;
        }

        folder.IsEnabled = !folder.IsEnabled;
        await _watchFolderRepository.SaveAsync(folder).ConfigureAwait(true);
        _watchFolderService.SetActiveFolders(Folders);
    }

    private void OnFileDetected(object? sender, FileDetectedEventArgs e)
    {
        _logger.Information("Fichier détecté par la surveillance : {Path}", e.FilePath);

        _dispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                await _queueViewModel.AddFilesAsync([e.FilePath], e.Folder.DefaultPresetId).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Échec de l'ajout automatique à la file pour {Path}", e.FilePath);
            }
        });
    }

    public void Dispose()
    {
        _watchFolderService.FileDetected -= OnFileDetected;
    }
}
