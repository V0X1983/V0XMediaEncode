using System.Text.RegularExpressions;

namespace V0XMediaEncode.Services.Ffmpeg;

/// <summary>
/// Pure parsing of `ffmpeg -encoders` output into <see cref="HardwareEncoderCapabilities"/>.
/// No process I/O here so the detection logic is unit-testable against captured sample output.
/// </summary>
public static class FFmpegEncoderCapabilitiesParser
{
    // Each encoder line looks like " V....D h264_nvenc  NVIDIA NVENC H.264 encoder (codec h264)".
    // The flag block's exact width has changed across ffmpeg versions, so match it loosely.
    private static readonly Regex EncoderLineRegex = new(
        @"^\s*[VAS][A-Za-z.]{3,8}\s+(?<name>\S+)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    public static HardwareEncoderCapabilities Parse(string encodersOutput)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in EncoderLineRegex.Matches(encodersOutput))
        {
            names.Add(match.Groups["name"].Value);
        }

        return new HardwareEncoderCapabilities(
            HasNvencH264: names.Contains("h264_nvenc"),
            HasNvencHevc: names.Contains("hevc_nvenc"),
            HasQsvH264: names.Contains("h264_qsv"),
            HasQsvHevc: names.Contains("hevc_qsv"),
            HasAmfH264: names.Contains("h264_amf"),
            HasAmfHevc: names.Contains("hevc_amf"));
    }
}
