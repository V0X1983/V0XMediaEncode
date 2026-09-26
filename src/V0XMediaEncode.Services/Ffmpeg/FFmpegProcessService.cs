using System.Diagnostics;
using Serilog;
using V0XMediaEncode.Core.Models;

namespace V0XMediaEncode.Services.Ffmpeg;

/// <summary>
/// Drives one ffmpeg encode: builds the argument list, launches the process, pumps stderr into
/// <see cref="FFmpegProgressParser"/> for live progress, and cancels cooperatively (`q` on stdin)
/// instead of killing the process, so ffmpeg gets a chance to finalize the output file.
/// Everything here runs off the calling (UI) thread's synchronous path via async/await.
/// </summary>
public sealed class FFmpegProcessService(IFFmpegLocator locator, ILogger logger)
{
    private const int GracefulStopTimeoutMs = 5000;
    private const int ErrorTailLineCount = 40;

    public async Task<EncodeResult> EncodeAsync(EncodeJob job, IProgress<EncodeProgress>? progress, CancellationToken cancellationToken = default)
    {
        if (job.Preset is not { } preset)
        {
            throw new InvalidOperationException($"Le job '{job.FileName}' n'a pas de preset assigné.");
        }

        var arguments = FFmpegArgumentBuilder.Build(job, preset);
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

        await using var cancellationRegistration = cancellationToken.Register(() =>
            _ = Task.Run(() => RequestGracefulStop(process, job.FileName, logger)));

        await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
        await pumpTask.ConfigureAwait(false);

        var success = process.ExitCode == 0 && !cancellationToken.IsCancellationRequested;
        var errorTailText = errorTail.Count > 0 ? string.Join(Environment.NewLine, errorTail) : null;

        if (!success)
        {
            logger.Warning("ffmpeg s'est terminé avec le code {ExitCode} pour {File}", process.ExitCode, job.FileName);
        }

        return new EncodeResult(success, process.ExitCode, errorTailText);
    }

    private static void RequestGracefulStop(Process process, string fileName, ILogger logger)
    {
        try
        {
            if (process.HasExited)
            {
                return;
            }

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
    }
}
