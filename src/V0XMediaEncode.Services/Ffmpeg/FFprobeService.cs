using System.Diagnostics;
using System.Text.Json;
using V0XMediaEncode.Core.Models;

namespace V0XMediaEncode.Services.Ffmpeg;

/// <summary>Runs `ffprobe -show_format -show_streams` on a source file and maps the JSON result to <see cref="MediaProbeResult"/>.</summary>
public sealed class FFprobeService(IFFmpegLocator locator)
{
    public async Task<MediaProbeResult> ProbeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo(locator.FFprobePath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("-v");
        startInfo.ArgumentList.Add("quiet");
        startInfo.ArgumentList.Add("-print_format");
        startInfo.ArgumentList.Add("json");
        startInfo.ArgumentList.Add("-show_format");
        startInfo.ArgumentList.Add("-show_streams");
        startInfo.ArgumentList.Add(filePath);

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            var stderr = await stderrTask.ConfigureAwait(false);
            throw new InvalidOperationException($"ffprobe a échoué (code {process.ExitCode}) pour '{filePath}' : {stderr}");
        }

        var json = await stdoutTask.ConfigureAwait(false);
        var output = JsonSerializer.Deserialize(json, FFprobeJsonContext.Default.FFprobeOutput)
            ?? throw new InvalidOperationException($"ffprobe n'a retourné aucune donnée exploitable pour '{filePath}'.");

        return FFprobeResultMapper.Map(output);
    }
}
