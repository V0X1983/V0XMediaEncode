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
    private readonly object _detectionLock = new();
    private Task<HardwareEncoderCapabilities>? _cachedDetection;

    /// <summary>
    /// Detects once per app run and reuses the result: each probe launches a handful of ffmpeg
    /// subprocesses, which is fine on startup but too slow to repeat before every single encode job
    /// now that <see cref="Core.Models.HardwareEncoderKind.Automatique"/> needs this on the hot path.
    /// Deliberately does NOT thread <paramref name="cancellationToken"/> into the shared detection
    /// itself: this is a one-time, app-lifetime probe shared by every caller, so one encode job being
    /// cancelled must never cancel (and thereby permanently poison the cached result for) detection
    /// that every other job also depends on. The lock plus the completed-unsuccessfully check below
    /// also make this safe against two callers racing to start the first detection, and retry instead
    /// of caching a failed attempt forever.
    /// </summary>
    public Task<HardwareEncoderCapabilities> DetectAsync(CancellationToken cancellationToken = default)
    {
        lock (_detectionLock)
        {
            if (_cachedDetection is { IsCompleted: true, IsCompletedSuccessfully: false })
            {
                _cachedDetection = null;
            }

            return _cachedDetection ??= DetectCoreAsync();
        }
    }

    private async Task<HardwareEncoderCapabilities> DetectCoreAsync()
    {
        var compiledIn = await ParseCompiledInEncodersAsync().ConfigureAwait(false);
        if (!compiledIn.HasAnyHardwareEncoder)
        {
            return HardwareEncoderCapabilities.None;
        }

        return new HardwareEncoderCapabilities(
            HasNvencH264: compiledIn.HasNvencH264 && await ProbeEncoderAsync("h264_nvenc").ConfigureAwait(false),
            HasNvencHevc: compiledIn.HasNvencHevc && await ProbeEncoderAsync("hevc_nvenc").ConfigureAwait(false),
            HasQsvH264: compiledIn.HasQsvH264 && await ProbeEncoderAsync("h264_qsv").ConfigureAwait(false),
            HasQsvHevc: compiledIn.HasQsvHevc && await ProbeEncoderAsync("hevc_qsv").ConfigureAwait(false),
            HasAmfH264: compiledIn.HasAmfH264 && await ProbeEncoderAsync("h264_amf").ConfigureAwait(false),
            HasAmfHevc: compiledIn.HasAmfHevc && await ProbeEncoderAsync("hevc_amf").ConfigureAwait(false));
    }

    private async Task<HardwareEncoderCapabilities> ParseCompiledInEncodersAsync()
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

            var stdout = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            await process.WaitForExitAsync().ConfigureAwait(false);

            return FFmpegEncoderCapabilitiesParser.Parse(stdout);
        }
        catch (Exception)
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
    private async Task<bool> ProbeEncoderAsync(string encoderName)
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

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync().ConfigureAwait(false);
            await stdoutTask.ConfigureAwait(false);
            await stderrTask.ConfigureAwait(false);

            return process.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
