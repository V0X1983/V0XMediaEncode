using System.Diagnostics;

namespace V0XMediaEncode.Services.Ffmpeg;

/// <summary>
/// Extracts a single frame as a PNG via `ffmpeg -ss {t} -i {file} -vframes 1 -f image2pipe -vcodec png pipe:1`,
/// piped straight into memory (no temp file). Returns raw bytes; turning them into a displayable
/// bitmap is the App layer's job since that requires a WinUI/UI-thread type this Services project
/// deliberately doesn't reference.
/// </summary>
public sealed class FFmpegThumbnailService(IFFmpegLocator locator)
{
    public async Task<byte[]> ExtractThumbnailAsync(string sourcePath, TimeSpan position, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo(locator.FFmpegPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        startInfo.ArgumentList.Add("-loglevel");
        startInfo.ArgumentList.Add("error");
        startInfo.ArgumentList.Add("-ss");
        startInfo.ArgumentList.Add(FFmpegTimestampFormatter.Format(position));
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(sourcePath);
        startInfo.ArgumentList.Add("-vframes");
        startInfo.ArgumentList.Add("1");
        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add("image2pipe");
        startInfo.ArgumentList.Add("-vcodec");
        startInfo.ArgumentList.Add("png");
        startInfo.ArgumentList.Add("pipe:1");

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        using var buffer = new MemoryStream();
        var copyTask = process.StandardOutput.BaseStream.CopyToAsync(buffer, cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        await copyTask.ConfigureAwait(false);

        if (process.ExitCode != 0 || buffer.Length == 0)
        {
            var stderr = await stderrTask.ConfigureAwait(false);
            throw new InvalidOperationException(
                $"Échec de l'extraction de la miniature pour '{sourcePath}' à {position} (code {process.ExitCode}) : {stderr}");
        }

        return buffer.ToArray();
    }
}
