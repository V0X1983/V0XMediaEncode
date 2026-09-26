using System.Collections.Concurrent;
using Serilog;
using V0XMediaEncode.Core.Models;

namespace V0XMediaEncode.Services.WatchFolders;

/// <summary>
/// Runs one <see cref="FileSystemWatcher"/> per enabled <see cref="WatchedFolder"/>. A file showing
/// up raises <see cref="Created"/>/<see cref="Renamed"/> immediately — often while whatever produced
/// it (a copy, an export, a download) is still writing — so every detection is followed by a
/// background poll-until-stable wait before <see cref="FileDetected"/> fires. All of that runs off
/// the UI thread; callers just subscribe to the event.
/// </summary>
public sealed class WatchFolderService : IDisposable
{
    private const int StabilityPollIntervalMs = 500;
    private const int StabilityRequiredStableReads = 3;
    private const int MaxStabilityWaitMs = 30_000;

    private readonly ILogger _logger;
    private readonly Dictionary<Guid, FileSystemWatcher> _watchers = [];
    private readonly ConcurrentDictionary<string, byte> _pendingFiles = new(StringComparer.OrdinalIgnoreCase);

    public WatchFolderService(ILogger logger)
    {
        _logger = logger;
    }

    public event EventHandler<FileDetectedEventArgs>? FileDetected;

    /// <summary>Replaces whatever set of folders is currently being watched with <paramref name="folders"/>.</summary>
    public void SetActiveFolders(IReadOnlyList<WatchedFolder> folders)
    {
        StopAll();

        foreach (var folder in folders.Where(f => f.IsEnabled))
        {
            StartWatching(folder);
        }
    }

    private void StartWatching(WatchedFolder folder)
    {
        if (!Directory.Exists(folder.Path))
        {
            _logger.Warning("Dossier surveillé introuvable, ignoré : {Path}", folder.Path);
            return;
        }

        var watcher = new FileSystemWatcher(folder.Path)
        {
            IncludeSubdirectories = folder.IncludeSubdirectories,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
        };

        watcher.Created += (_, e) => OnFileAppeared(folder, e.FullPath);
        watcher.Renamed += (_, e) => OnFileAppeared(folder, e.FullPath);
        watcher.Error += (_, e) => _logger.Warning(e.GetException(), "Erreur FileSystemWatcher pour {Path}", folder.Path);
        watcher.EnableRaisingEvents = true;

        _watchers[folder.Id] = watcher;
        _logger.Information("Surveillance démarrée pour {Path}", folder.Path);
    }

    private void OnFileAppeared(WatchedFolder folder, string filePath)
    {
        if (!WatchFolderFileFilter.IsCandidateFile(filePath))
        {
            return;
        }

        if (!_pendingFiles.TryAdd(filePath, 0))
        {
            return; // Already being polled for stability from an earlier event on the same path.
        }

        _ = Task.Run(async () =>
        {
            try
            {
                if (await WaitUntilFileIsReadyAsync(filePath).ConfigureAwait(false))
                {
                    FileDetected?.Invoke(this, new FileDetectedEventArgs(folder, filePath));
                }
            }
            finally
            {
                _pendingFiles.TryRemove(filePath, out _);
            }
        });
    }

    /// <summary>
    /// Polls until the file can be opened exclusively (nobody else is writing to it) and its size
    /// has stopped changing for a few consecutive reads, or gives up after <see cref="MaxStabilityWaitMs"/>.
    /// </summary>
    private async Task<bool> WaitUntilFileIsReadyAsync(string filePath)
    {
        var stableReads = 0;
        long lastSize = -1;
        var elapsedMs = 0;

        while (elapsedMs < MaxStabilityWaitMs)
        {
            await Task.Delay(StabilityPollIntervalMs).ConfigureAwait(false);
            elapsedMs += StabilityPollIntervalMs;

            long currentSize;
            try
            {
                await using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                currentSize = stream.Length;
            }
            catch (IOException)
            {
                stableReads = 0; // still locked by whoever is writing it
                continue;
            }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
            {
                return false; // vanished (renamed again, deleted, was itself a temp artifact)
            }

            if (currentSize == lastSize && currentSize > 0)
            {
                if (++stableReads >= StabilityRequiredStableReads)
                {
                    return true;
                }
            }
            else
            {
                stableReads = 0;
                lastSize = currentSize;
            }
        }

        _logger.Warning("Le fichier {Path} n'est jamais devenu stable, abandon.", filePath);
        return false;
    }

    private void StopAll()
    {
        foreach (var watcher in _watchers.Values)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        _watchers.Clear();
    }

    public void Dispose() => StopAll();
}
