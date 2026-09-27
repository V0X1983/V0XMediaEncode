using System.Diagnostics;
using System.Runtime.CompilerServices;
using Serilog;
using V0XMediaEncode.Core.Models;

namespace V0XMediaEncode.Services.Ffmpeg;

/// <summary>
/// Drives one ffmpeg encode: builds the argument list, launches the process, pumps stderr into
/// <see cref="FFmpegProgressParser"/> for live progress, and cancels cooperatively (`q` on stdin)
/// instead of killing the process, so ffmpeg gets a chance to finalize the output file.
/// Everything here runs off the calling (UI) thread's synchronous path via async/await.
/// </summary>
public sealed class FFmpegProcessService(IFFmpegLocator locator, FFmpegHardwareDetectionService hardwareDetectionService, ILogger logger)
{
    private const int GracefulStopTimeoutMs = 5000;
    private const int ErrorTailLineCount = 40;

    public async Task<EncodeResult> EncodeAsync(EncodeJob job, IProgress<EncodeProgress>? progress, CancellationToken cancellationToken = default)
    {
        if (job.Preset is not { } preset)
        {
            throw new InvalidOperationException($"Le job '{job.FileName}' n'a pas de preset assigné.");
        }

        var capabilities = await hardwareDetectionService.DetectAsync(cancellationToken).ConfigureAwait(false);
        var arguments = FFmpegArgumentBuilder.Build(job, preset, capabilities);
        var totalDuration = job.MediaInfo?.Duration;

        var startInfo = new ProcessStartInfo(locator.FFmpegPath)
        {
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        logger.Information("Lancement de ffmpeg pour {File} : {Arguments}", job.FileName, string.Join(' ', arguments));

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        var errorTail = new Queue<string>(ErrorTailLineCount + 1);

        process.Start();

        var pumpTask = Task.Run(async () =>
        {
            string? line;
            while ((line = await process.StandardError.ReadLineAsync().ConfigureAwait(false)) is not null)
            {
                errorTail.Enqueue(line);
                if (errorTail.Count > ErrorTailLineCount)
                {
                    errorTail.Dequeue();
                }

                if (FFmpegProgressParser.TryParse(line, totalDuration, out var parsed))
                {
                    progress?.Report(parsed);
                }
            }
        });

        // A plain bool set by RequestGracefulStop itself (not read from cancellationToken below) so
        // "did we actually intervene" reflects what really happened to the process rather than the
        // token's live IsCancellationRequested flag, which can already be true by the time ffmpeg
        // finishes on its own a moment after Cancel() was called — that race used to mislabel a
        // successful, untouched completion as Cancelled.
        var stopWasAttempted = new StrongBox<bool>(false);
        await using var cancellationRegistration = cancellationToken.Register(() =>
            _ = Task.Run(() => RequestGracefulStop(process, job.FileName, stopWasAttempted, logger)));

        await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
        await pumpTask.ConfigureAwait(false);

        var success = process.ExitCode == 0 && !stopWasAttempted.Value;
        var errorTailText = errorTail.Count > 0 ? string.Join(Environment.NewLine, errorTail) : null;

        if (!success)
        {
            logger.Warning("ffmpeg s'est terminé avec le code {ExitCode} pour {File}", process.ExitCode, job.FileName);
        }

        return new EncodeResult(success, process.ExitCode, errorTailText);
    }

    private static void RequestGracefulStop(Process process, string fileName, StrongBox<bool> stopWasAttempted, ILogger logger)
    {
        try
        {
            if (process.HasExited)
            {
                return;
            }

            stopWasAttempted.Value = true;
            logger.Information("Annulation demandée pour {File}, envoi de 'q' à ffmpeg.", fileName);

            // 'q' asks ffmpeg to stop reading input and finish writing the current output file's
            // trailer/index cleanly. Process.Kill() would leave a truncated, likely unplayable file.
            process.StandardInput.Write('q');
            process.StandardInput.Flush();

            if (!process.WaitForExit(GracefulStopTimeoutMs))
            {
                logger.Warning("ffmpeg n'a pas répondu à 'q' après {TimeoutMs} ms pour {File}, arrêt forcé.", GracefulStopTimeoutMs, fileName);
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // Process exited between the HasExited check and the write — nothing left to stop.
        }
        catch (Exception ex)
        {
            // Kill() can throw (Win32Exception from a permission/AV lock, a race walking the process
            // tree, ...). Previously unhandled here, this became an unobserved exception on the
            // fire-and-forget Task.Run and left the process running with EncodeAsync's
            // WaitForExitAsync(CancellationToken.None) awaiting it forever. Logging at least surfaces
            // the failure; there's no further fallback to force-terminate a process that resists Kill().
            logger.Warning(ex, "Échec de l'arrêt forcé de ffmpeg pour {File}.", fileName);
        }
    }
}
