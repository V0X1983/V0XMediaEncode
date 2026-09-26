namespace V0XMediaEncode.Core.Models;

public enum ContainerFormat
{
    Mp4,
    Mov,
    Mkv,
    WebM,
    Mxf,
    Mp3,
    Aac,
    Wav,
    Flac,
}

public static class ContainerFormatExtensions
{
    /// <summary>File extension (without the leading dot) used for the output path and the ffmpeg muxer name.</summary>
    public static string ToExtension(this ContainerFormat format) => format switch
    {
        ContainerFormat.Mp4 => "mp4",
        ContainerFormat.Mov => "mov",
        ContainerFormat.Mkv => "mkv",
        ContainerFormat.WebM => "webm",
        ContainerFormat.Mxf => "mxf",
        ContainerFormat.Mp3 => "mp3",
        ContainerFormat.Aac => "m4a",
        ContainerFormat.Wav => "wav",
        ContainerFormat.Flac => "flac",
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };

    public static bool IsAudioOnly(this ContainerFormat format) => format is ContainerFormat.Mp3 or ContainerFormat.Aac or ContainerFormat.Wav or ContainerFormat.Flac;
}
