using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using V0XMediaEncode.Core.Models;
using V0XMediaEncode.Services.History;

namespace V0XMediaEncode.App.ViewModels;

/// <summary>Backs the "Historique" page: the completed-job log plus text export (Phase 8).</summary>
public sealed partial class HistoryViewModel : ObservableObject
{
    private readonly IHistoryRepository _historyRepository;
    private readonly ILogger _logger;

    public ObservableCollection<HistoryEntry> Entries { get; } = [];

    public HistoryViewModel(IHistoryRepository historyRepository, ILogger logger)
    {
        _historyRepository = historyRepository;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        await _historyRepository.InitializeAsync().ConfigureAwait(true);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            var entries = await _historyRepository.GetAllAsync().ConfigureAwait(true);
            Entries.Clear();
            foreach (var entry in entries)
            {
                Entries.Add(entry);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Échec du chargement de l'historique");
        }
    }

    public string FormatAllForExport() => HistoryLogFormatter.FormatAll(Entries);

    public string FormatEntryForExport(HistoryEntry entry) => HistoryLogFormatter.FormatEntry(entry);
}
