using System.Diagnostics;

namespace V0XMediaEncode.Services.Ffmpeg;

/// <summary>
/// Detects which hardware encoders actually work on this machine, not just which ones ffmpeg was
/// compiled with. A full ffmpeg build (e.g. the Gyan Windows builds) statically links NVENC, Quick
/// Sync and AMF support regardless of which GPU is installed, so `ffmpeg -encoders` alone reports
/// all three as "available" even on a machine with, say, only an NVIDIA GPU - QSV/AMF would then
/// fail the moment a user actually tries to encode with them. To catch that, every encoder that
/// <see cref="FFmpegEncoderCapabilitiesParser"/> finds compiled in is additionally probed with a
/// real, near-instant 1-frame encode; only encoders that pass both checks are reported as available.
/// </summary>
public sealed class FFmpegHardwareDetectionService(IFFmpegLocator locator)
{
    private Task<HardwareEncoderCapabilities>? _cachedDetection;

    /// <summary>
    /// Detects once per app run and reuses the result: each probe launches a handful of ffmpeg
    /// subprocesses, which is fine on startup but too slow to repeat before every single encode job
    /// now that <see cref="Core.Models.HardwareEncoderKind.Automatique"/> needs this on the hot path.
    /// </summary>
    public Task<HardwareEncoderCapabilities> DetectAsync(CancellationToken cancellationToken = default) =>
        _cachedDetection ??= DetectCoreAsync(cancellationToken);

    private async Task<HardwareEncoderCapabilities> DetectCoreAsync(CancellationToken cancellationToken)
    {
        var compiledIn = await ParseCompiledInEncodersAsync(cancellationToken).ConfigureAwait(false);
        if (!compiledIn.HasAnyHardwareEncoder)
        {
            return HardwareEncoderCapabilities.None;
        }

        return new HardwareEncoderCapabilities(
            HasNvencH264: compiledIn.HasNvencH264 && await ProbeEncoderAsync("h264_nvenc", cancellationToken).ConfigureAwait(false),
            HasNvencHevc: compiledIn.HasNvencHevc && await ProbeEncoderAsync("hevc_nvenc", cancellationToken).ConfigureAwait(false),
            HasQsvH264: compiledIn.HasQsvH264 && await ProbeEncoderAsync("h264_qsv", cancellationToken).ConfigureAwait(false),
            HasQsvHevc: compiledIn.HasQsvHevc && await ProbeEncoderAsync("hevc_qsv", cancellationToken).ConfigureAwait(false),
            HasAmfH264: compiledIn.HasAmfH264 && await ProbeEncoderAsync("h264_amf", cancellationToken).ConfigureAwait(false),
            HasAmfHevc: compiledIn.HasAmfHevc && await ProbeEncoderAsync("hevc_amf", cancellationToken).ConfigureAwait(false));
    }

    private async Task<HardwareEncoderCapabilities> ParseCompiledInEncodersAsync(CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(locator.FFmpegPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("-hide_banner");
        startInfo.ArgumentList.Add("-encoders");

        try
        {
            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            return FFmpegEncoderCapabilitiesParser.Parse(stdout);
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested is false)
        {
            // ffmpeg missing/unreachable: degrade to software-only encoding rather than crashing startup.
            return HardwareEncoderCapabilities.None;
        }
    }

    /// <summary>
    /// Attempts a real, near-instant 1-frame encode with the given encoder and reports whether
    /// ffmpeg actually succeeded. 320x240 (not something tinier like 64x64): NVENC in particular
    /// rejects frame dimensions below its hardware minimum, which would otherwise register as a
    /// false negative for a GPU that works fine at real encode resolutions.
    /// </summary>
    private async Task<bool> ProbeEncoderAsync(string encoderName, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(locator.FFmpegPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in new[]
                 {
                     "-y", "-v", "error",
                     "-f", "lavfi", "-i", "nullsrc=size=320x240:rate=1",
                     "-frames:v", "1",
                     "-c:v", encoderName,
                     "-f", "null", "-",
                 })
        {
            startInfo.ArgumentList.Add(arg);
        }

        try
        {
            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            await stdoutTask.ConfigureAwait(false);
            await stderrTask.ConfigureAwait(false);

            return process.ExitCode == 0;
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested is false)
        {
            return false;
        }
    }
}
