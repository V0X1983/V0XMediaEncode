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
        // context - exactly what the Progress<T> below already does for ProgressPercent/Eta/
        // SpeedFactor (it captures the same context at construction time). Without this, setting
        // job.Status from that background thread threw COMException 0x8001010E (WinRT's
        // PropertyChanged ABI wrapper rejecting a cross-apartment call).
        //
        // Deliberately fire-and-forget (Post, not awaited): SynchronizationContext.Send isn't
        // supported by WinUI3's DispatcherQueueSynchronizationContext (throws NotSupportedException),
        // and awaiting a TaskCompletionSource completed from inside the posted callback risks hanging
        // this job forever if that callback is ever dropped instead of run (e.g. window not focused/
        // suspended) - which is exactly what silently stalled every job at 0% "Encoding" while a
        // previous version of this fix awaited it. Job progression must never depend on the UI thread
        // actually picking up a posted update, so every value RecordHistoryAsync below needs is
        // tracked in local variables instead of read back from job's properties, which stay
        // best-effort/eventually-consistent for display only.
        var uiContext = SynchronizationContext.Current;

        void SetJobState(Action mutate)
        {
            if (uiContext is null)
            {
                mutate();
            }
            else
            {
                uiContext.Post(_ => mutate(), null);
            }
        }

        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        var startedAt = DateTimeOffset.Now;
        SetJobState(() =>
        {
            job.Status = EncodeStatus.Encoding;
            job.StartedAt = startedAt;
            job.ErrorMessage = null;
        });

        string? errorTail = null;
        DateTimeOffset completedAt;
        EncodeStatus finalStatus;
        string? finalErrorMessage = null;
        try
        {
            var progress = new Progress<EncodeProgress>(p => SetJobState(() =>
            {
                job.ProgressPercent = p.PercentComplete;
                job.Eta = p.Eta;
                job.SpeedFactor = p.SpeedFactor;
            }));

            var result = await ffmpegProcessService.EncodeAsync(job, progress, cancellationToken).ConfigureAwait(false);
            errorTail = result.ErrorTail;
            completedAt = DateTimeOffset.Now;

            if (cancellationToken.IsCancellationRequested)
            {
                finalStatus = EncodeStatus.Cancelled;
            }
            else if (result.Success)
            {
                finalStatus = EncodeStatus.Completed;
            }
            else
            {
                finalStatus = EncodeStatus.Failed;
                finalErrorMessage = result.ErrorTail;
            }
        }
        catch (Exception ex)
        {
            // Deliberately catches OperationCanceledException too (no longer excluded): the
            // cooperative-cancel path above never throws - EncodeAsync returns normally and the
            // check above reads cancellationToken.IsCancellationRequested - so an OCE reaching here
            // is always an unexpected failure (e.g. a shared dependency observing a stale/cancelled
            // token), not a normal user cancel. Excluding it used to let it escape RunJobAsync
            // entirely, skipping RecordHistoryAsync below and silently dropping that job from
            // Historique.
            logger.Error(ex, "Échec inattendu de l'encodage pour {File}", job.FileName);
            completedAt = DateTimeOffset.Now;
            finalStatus = EncodeStatus.Failed;
            finalErrorMessage = ex.Message;
        }
        finally
        {
            semaphore.Release();
        }

        SetJobState(() =>
        {
            job.CompletedAt = completedAt;
            job.Status = finalStatus;
            job.ErrorMessage = finalErrorMessage;
            job.Eta = null;
            job.SpeedFactor = null;
            if (finalStatus == EncodeStatus.Completed)
            {
                job.ProgressPercent = 100;
            }
        });

        await RecordHistoryAsync(job, startedAt, completedAt, finalStatus, finalErrorMessage, errorTail).ConfigureAwait(false);
        onJobCompleted?.Invoke(job);
    }

    private async Task RecordHistoryAsync(
        EncodeJob job,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt,
        EncodeStatus status,
        string? errorMessage,
        string? errorTail)
    {
        try
        {
            var entry = new HistoryEntry
            {
                SourcePath = job.SourcePath,
                OutputPath = job.OutputPath,
                PresetName = job.Preset?.Name,
                Status = status,
                StartedAt = startedAt,
                CompletedAt = completedAt,
                ErrorMessage = errorMessage,
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
