using System.Text.Json.Serialization;

namespace V0XMediaEncode.Services.Ffmpeg;

// Mirrors the subset of `ffprobe -print_format json -show_format -show_streams` that the app needs.
// Property names match ffprobe's JSON verbatim (snake_case), hence the explicit JsonPropertyName attributes.

public sealed class FFprobeOutput
{
    [JsonPropertyName("format")]
    public FFprobeFormat? Format { get; set; }

    [JsonPropertyName("streams")]
    public List<FFprobeStream> Streams { get; set; } = [];
}

public sealed class FFprobeFormat
{
    [JsonPropertyName("duration")]
    public string? Duration { get; set; }

    [JsonPropertyName("bit_rate")]
    public string? BitRate { get; set; }

    [JsonPropertyName("format_name")]
    public string? FormatName { get; set; }
}

public sealed class FFprobeStream
{
    [JsonPropertyName("codec_type")]
    public string? CodecType { get; set; }

    [JsonPropertyName("codec_name")]
    public string? CodecName { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    /// <summary>e.g. "30000/1001".</summary>
    [JsonPropertyName("r_frame_rate")]
    public string? RFrameRate { get; set; }

    [JsonPropertyName("sample_rate")]
    public string? SampleRate { get; set; }

    [JsonPropertyName("channels")]
    public int? Channels { get; set; }
}

[JsonSerializable(typeof(FFprobeOutput))]
public partial class FFprobeJsonContext : JsonSerializerContext;
