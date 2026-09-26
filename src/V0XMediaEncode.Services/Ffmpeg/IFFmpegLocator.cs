namespace V0XMediaEncode.Services.Ffmpeg;

/// <summary>Resolves the ffmpeg/ffprobe binaries to invoke, whether bundled with the app or found on PATH.</summary>
public interface IFFmpegLocator
{
    string FFmpegPath { get; }

    string FFprobePath { get; }
}
