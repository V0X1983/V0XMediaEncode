using Serilog;
using V0XMediaEncode.Core.Models;
using V0XMediaEncode.Services.Ffmpeg;
using V0XMediaEncode.Services.History;

namespace V0XMediaEncode.Services.Queue;

/// <summary>
/// Runs a batch of <see cref="EncodeJob"/>s through <see cref="FFmpegProcessService"/>.
/// Sequential by default (<see cref="MaxParallelJobs"/> = 1); raising it lets several ffmpeg
/// processes run at once, which only makes sense up to the number of hardware encoder sessions
/// actually available (see <see cref="FFmpegHardwareDetectionService"/>) since software encodes
/// already saturate all CPU cores on their own. Every job — succeeded, failed, or cancelled — is
/// recorded to <see cref="IHistoryRepository"/> so the Historique page sees it regardless of which
/// UI (the queue page or a watch folder) triggered the run.
/// </summary>
public sealed class EncodeQueueOrchestrator(FFmpegProcessService ffmpegProcessService, IHistoryRepository historyRepository, ILogger logger)
{
    public int MaxParallelJobs { get; set; } = 1;

    /// <param name="onJobCompleted">
    /// Invoked once per job right after its history entry is recorded — e.g. the App layer uses
    /// this to fire a toast notification without the orchestrator itself depending on WinUI.
    /// </param>
    public async Task RunAsync(IReadOnlyList<EncodeJob> jobs, CancellationToken cancellationToken = default, Action<EncodeJob>? onJobCompleted = null)
    {
        using var semaphore = new SemaphoreSlim(Math.Max(1, MaxParallelJobs));
        await Task.WhenAll(jobs.Select(job => RunJobAsync(job, semaphore, cancellationToken, onJobCompleted))).ConfigureAwait(false);
    }

    private async Task RunJobAsync(EncodeJob job, SemaphoreSlim semaphore, CancellationToken cancellationToken, Action<EncodeJob>? onJobCompleted)
    {
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        string? errorTail = null;
        try
        {
            job.Status = EncodeStatus.Encoding;
            job.StartedAt = DateTimeOffset.Now;
            job.ErrorMessage = null;

            // EncodeJob raises PropertyChanged off this background thread; the queue page binds to it
            // with classic {Binding} (not x:Bind), which the WinUI binding engine marshals to the UI
            // thread automatically, so no DispatcherQueue plumbing is needed here.
            var progress = new Progress<EncodeProgress>(p =>
            {
                job.ProgressPercent = p.PercentComplete;
                job.Eta = p.Eta;
                job.SpeedFactor = p.SpeedFactor;
            });

            var result = await ffmpegProcessService.EncodeAsync(job, progress, cancellationToken).ConfigureAwait(false);
            errorTail = result.ErrorTail;

            job.CompletedAt = DateTimeOffset.Now;
            if (cancellationToken.IsCancellationRequested)
            {
                job.Status = EncodeStatus.Cancelled;
            }
            else if (result.Success)
            {
                job.Status = EncodeStatus.Completed;
                job.ProgressPercent = 100;
            }
            else
            {
                job.Status = EncodeStatus.Failed;
                job.ErrorMessage = result.ErrorTail;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.Error(ex, "Échec inattendu de l'encodage pour {File}", job.FileName);
            job.CompletedAt = DateTimeOffset.Now;
            job.Status = EncodeStatus.Failed;
            job.ErrorMessage = ex.Message;
        }
        finally
        {
            semaphore.Release();
        }

        await RecordHistoryAsync(job, errorTail).ConfigureAwait(false);
        onJobCompleted?.Invoke(job);
    }

    private async Task RecordHistoryAsync(EncodeJob job, string? errorTail)
    {
        try
        {
            var entry = new HistoryEntry
            {
                SourcePath = job.SourcePath,
                OutputPath = job.OutputPath,
                PresetName = job.Preset?.Name,
                Status = job.Status,
                StartedAt = job.StartedAt ?? DateTimeOffset.Now,
                CompletedAt = job.CompletedAt ?? DateTimeOffset.Now,
                ErrorMessage = job.ErrorMessage,
                LogTail = errorTail,
            };

            // CancellationToken.None: a cancelled run should still leave a history record behind.
            await historyRepository.AddAsync(entry, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Échec de l'enregistrement de l'historique pour {File}", job.FileName);
        }
    }
}
