namespace V0XMediaEncode.Services.Ffmpeg;

/// <summary>
/// Looks for ffmpeg.exe/ffprobe.exe next to the app first (a `ffmpeg\` folder shipped in the MSIX package),
/// then falls back to letting the OS resolve the bare file name against PATH.
/// </summary>
public sealed class FFmpegLocator : IFFmpegLocator
{
    public string FFmpegPath { get; }

    public string FFprobePath { get; }

    public FFmpegLocator()
    {
        FFmpegPath = ResolveBinary("ffmpeg.exe");
        FFprobePath = ResolveBinary("ffprobe.exe");
    }

    private static string ResolveBinary(string fileName)
    {
        var bundled = Path.Combine(AppContext.BaseDirectory, "ffmpeg", fileName);
        return File.Exists(bundled) ? bundled : fileName;
    }
}
