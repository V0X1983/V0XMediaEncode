using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Serilog;
using V0XMediaEncode.App.Notifications;
using V0XMediaEncode.Core.Models;
using V0XMediaEncode.Services.Ffmpeg;
using V0XMediaEncode.Services.Presets;
using V0XMediaEncode.Services.Queue;
using Windows.Storage.Streams;

namespace V0XMediaEncode.App.ViewModels;

/// <summary>Backs the "File d'attente" page: the encode queue itself, plus add/remove/start/cancel/preview.</summary>
public sealed partial class QueueViewModel : ObservableObject
{
    private readonly FFprobeService _ffprobeService;
    private readonly FFmpegThumbnailService _thumbnailService;
    private readonly EncodeQueueOrchestrator _orchestrator;
    private readonly IPresetRepository _presetRepository;
    private readonly ToastNotificationService _toastNotificationService;
    private readonly ILogger _logger;

    private CancellationTokenSource? _runCts;
    private CancellationTokenSource? _thumbnailCts;

    public ObservableCollection<EncodeJob> Jobs { get; } = [];

    public ObservableCollection<EncodePreset> AvailablePresets { get; } = [];

    [ObservableProperty]
    private EncodePreset? _selectedPreset;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartQueueCommand))]
    private bool _isEncoding;

    /// <summary>The job currently shown in the "Aperçu" panel (Phase 7) — not necessarily one being encoded.</summary>
    [ObservableProperty]
    private EncodeJob? _selectedJob;

    [ObservableProperty]
    private BitmapImage? _previewThumbnail;

    public QueueViewModel(
        FFprobeService ffprobeService,
        FFmpegThumbnailService thumbnailService,
        EncodeQueueOrchestrator orchestrator,
        IPresetRepository presetRepository,
        ToastNotificationService toastNotificationService,
        ILogger logger)
    {
        _ffprobeService = ffprobeService;
        _thumbnailService = thumbnailService;
        _orchestrator = orchestrator;
        _presetRepository = presetRepository;
        _toastNotificationService = toastNotificationService;
        _logger = logger;
    }

    partial void OnSelectedJobChanged(EncodeJob? value)
    {
        _thumbnailCts?.Cancel();
        _thumbnailCts?.Dispose();
        _thumbnailCts = null;
        PreviewThumbnail = null;

        if (value is null)
        {
            return;
        }

        var cts = new CancellationTokenSource();
        _thumbnailCts = cts;
        _ = LoadPreviewThumbnailAsync(value, cts.Token);
    }

    /// <summary>Grabs a poster frame at roughly the midpoint of the source (Phase 7 thumbnail extraction).</summary>
    private async Task LoadPreviewThumbnailAsync(EncodeJob job, CancellationToken cancellationToken)
    {
        var duration = job.MediaInfo?.Duration ?? TimeSpan.Zero;
        var position = duration > TimeSpan.FromSeconds(2) ? duration / 2 : TimeSpan.FromSeconds(1);

        try
        {
            var pngBytes = await _thumbnailService.ExtractThumbnailAsync(job.SourcePath, position, cancellationToken).ConfigureAwait(true);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            PreviewThumbnail = await CreateBitmapImageAsync(pngBytes).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.Warning(ex, "Échec de la génération de la miniature pour {File}", job.FileName);
        }
    }

    private static async Task<BitmapImage> CreateBitmapImageAsync(byte[] pngBytes)
    {
        using var stream = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(stream))
        {
            writer.WriteBytes(pngBytes);
            await writer.StoreAsync();
            await writer.FlushAsync();
            writer.DetachStream();
        }
        stream.Seek(0);

        var bitmap = new BitmapImage();
        await bitmap.SetSourceAsync(stream);
        return bitmap;
    }

    public async Task InitializeAsync()
    {
        await _presetRepository.InitializeAsync().ConfigureAwait(true);
        var presets = await _presetRepository.GetAllAsync().ConfigureAwait(true);

        AvailablePresets.Clear();
        foreach (var preset in presets)
        {
            AvailablePresets.Add(preset);
        }

        SelectedPreset ??= AvailablePresets.FirstOrDefault();
    }

    /// <summary>
    /// Adds source files to the queue and kicks off an ffprobe pass on each (Phase 3 import).
    /// Also the entry point watch folders (Phase 6) use to auto-queue a detected file — in that
    /// case <paramref name="presetIdOverride"/> is the watched folder's own default preset, which
    /// takes priority over whatever preset happens to be selected in the toolbar.
    /// </summary>
    public async Task AddFilesAsync(IEnumerable<string> filePaths, Guid? presetIdOverride = null)
    {
        var preset = presetIdOverride is { } id
            ? AvailablePresets.FirstOrDefault(p => p.Id == id) ?? SelectedPreset
            : SelectedPreset;

        foreach (var path in filePaths)
        {
            var job = new EncodeJob
            {
                SourcePath = path,
                OutputPath = BuildDefaultOutputPath(path, preset),
                Preset = preset,
                Status = EncodeStatus.Probing,
            };
            Jobs.Add(job);

            try
            {
                job.MediaInfo = await _ffprobeService.ProbeAsync(path).ConfigureAwait(true);
                job.Status = EncodeStatus.Ready;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Échec de l'analyse ffprobe pour {File}", path);
                job.Status = EncodeStatus.Failed;
                job.ErrorMessage = ex.Message;
            }
        }
    }

    private static string BuildDefaultOutputPath(string sourcePath, EncodePreset? preset)
    {
        var directory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(sourcePath);
        var extension = preset?.Container.ToExtension() ?? "mp4";
        return Path.Combine(directory, $"{name}_v0x.{extension}");
    }

    [RelayCommand]
    private void RemoveJob(EncodeJob? job)
    {
        if (job is not null)
        {
            Jobs.Remove(job);
        }
    }

    private bool CanStartQueue() => !IsEncoding;

    [RelayCommand(CanExecute = nameof(CanStartQueue))]
    private async Task StartQueueAsync()
    {
        var pending = Jobs.Where(j => j.Status is EncodeStatus.Ready or EncodeStatus.Failed).ToList();
        if (pending.Count == 0)
        {
            return;
        }

        foreach (var job in pending.Where(j => j.Preset is null))
        {
            job.Preset = SelectedPreset;
        }

        IsEncoding = true;
        _runCts = new CancellationTokenSource();
        try
        {
            await _orchestrator.RunAsync(pending, _runCts.Token, onJobCompleted: _toastNotificationService.NotifyJobCompleted).ConfigureAwait(true);
        }
        finally
        {
            IsEncoding = false;
            _runCts.Dispose();
            _runCts = null;
        }
    }

    [RelayCommand]
    private void CancelQueue() => _runCts?.Cancel();
}
