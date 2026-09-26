using System.Diagnostics;

namespace V0XMediaEncode.Services.Ffmpeg;

/// <summary>Runs `ffmpeg -encoders` once at startup so presets can offer NVENC/QSV/AMF only when actually available.</summary>
public sealed class FFmpegHardwareDetectionService(IFFmpegLocator locator)
{
    public async Task<HardwareEncoderCapabilities> DetectAsync(CancellationToken cancellationToken = default)
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
}
