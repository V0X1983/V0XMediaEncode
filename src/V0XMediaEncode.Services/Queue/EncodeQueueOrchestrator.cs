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
        // Captured here, before any ConfigureAwait(false) boundary is crossed below, so this is the
        // UI thread's DispatcherQueueSynchronizationContext (RunJobAsync always starts synchronously
        // on the UI thread - QueueViewModel.StartQueueAsync calls RunAsync directly from a UI-bound
        // AsyncRelayCommand). EncodeJob raises PropertyChanged synchronously on whatever thread sets
        // its properties, and the queue page's data-bound ListView needs that raised on the UI
        // thread; FFmpegProcessService uses ConfigureAwait(false) throughout, so any job.* mutation
        // after awaiting it below resumes on a ThreadPool thread and must be posted back through this
        // context - exactly what the Progress<T> below already does correctly for
        // ProgressPercent/Eta/SpeedFactor (it captures the same context at construction time), now
        // extended to Status/CompletedAt/ErrorMessage too. Without this, setting job.Status from that
        // background thread threw COMException 0x8001010E (WinRT's PropertyChanged ABI wrapper
        // rejecting a cross-apartment call) - silently leaving the job's Status stuck on whatever it
        // last was ("Encoding") since the exception was thrown out of a plain property setter, not
        // observed as an encode failure.
        var uiContext = SynchronizationContext.Current;

        // Send (blocking until the posted delegate runs), not Post: RecordHistoryAsync right below
        // reads job.Status/CompletedAt/ErrorMessage immediately after RunJobAsync's try/catch/finally
        // finishes, so the mutation must have actually committed by the time SetJobState returns -
        // Post would just queue it and could race past RecordHistoryAsync's read. Safe to block here
        // since this always runs on a background thread by the time it's called (past the
        // ConfigureAwait(false) boundary above), never on the UI thread itself.
        void SetJobState(Action mutate)
        {
            if (uiContext is null)
            {
                mutate();
            }
            else
            {
                uiContext.Send(_ => mutate(), null);
            }
        }

        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        string? errorTail = null;
        try
        {
            job.Status = EncodeStatus.Encoding;
            job.StartedAt = DateTimeOffset.Now;
            job.ErrorMessage = null;

            var progress = new Progress<EncodeProgress>(p =>
            {
                job.ProgressPercent = p.PercentComplete;
                job.Eta = p.Eta;
                job.SpeedFactor = p.SpeedFactor;
            });

            var result = await ffmpegProcessService.EncodeAsync(job, progress, cancellationToken).ConfigureAwait(false);
            errorTail = result.ErrorTail;

            SetJobState(() =>
            {
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
            });
        }
        catch (Exception ex)
        {
            // Deliberately catches OperationCanceledException too (no longer excluded): the
            // cooperative-cancel path above never throws - EncodeAsync returns normally and the
            // SetJobState call above checks cancellationToken.IsCancellationRequested - so an OCE
            // reaching here is always an unexpected failure (e.g. a shared dependency observing a
            // stale/cancelled token), not a normal user cancel. Excluding it used to let it escape
            // RunJobAsync entirely, skipping RecordHistoryAsync below and silently dropping that job
            // from Historique.
            logger.Error(ex, "Échec inattendu de l'encodage pour {File}", job.FileName);
            SetJobState(() =>
            {
                job.CompletedAt = DateTimeOffset.Now;
                job.Status = EncodeStatus.Failed;
                job.ErrorMessage = ex.Message;
            });
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
