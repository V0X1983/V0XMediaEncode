using System.Globalization;
using V0XMediaEncode.Core.Models;

namespace V0XMediaEncode.Services.Ffmpeg;

/// <summary>
/// Pure translation of an <see cref="EncodeJob"/> + <see cref="EncodePreset"/> into an ffmpeg argument list.
/// Returns a token list (not a single string) so callers feed it straight into
/// <see cref="System.Diagnostics.ProcessStartInfo.ArgumentList"/> and never have to worry about quoting.
/// </summary>
public static class FFmpegArgumentBuilder
{
    public static IReadOnlyList<string> Build(EncodeJob job, EncodePreset preset)
    {
        var args = new List<string> { "-y", "-i", job.SourcePath };

        if (preset.IsAudioOnly || preset.VideoCodec == VideoCodec.None)
        {
            args.Add("-vn");
        }
        else if (preset.VideoCodec == VideoCodec.Copy)
        {
            args.Add("-c:v");
            args.Add("copy");
        }
        else
        {
            AppendVideoArgs(args, preset);
        }

        if (preset.AudioCodec == AudioCodec.None)
        {
            args.Add("-an");
        }
        else if (preset.AudioCodec == AudioCodec.Copy)
        {
            args.Add("-c:a");
            args.Add("copy");
        }
        else
        {
            AppendAudioArgs(args, preset);
        }

        args.Add(job.OutputPath);
        return args;
    }

    private static void AppendVideoArgs(List<string> args, EncodePreset preset)
    {
        var encoderName = ResolveVideoEncoderName(preset.VideoCodec, preset.HardwareEncoder);
        args.Add("-c:v");
        args.Add(encoderName);

        if (preset.VideoCodec is VideoCodec.ProRes422 or VideoCodec.ProRes422Hq)
        {
            args.Add("-profile:v");
            args.Add(preset.VideoCodec == VideoCodec.ProRes422Hq ? "3" : "2");
        }
        else
        {
            AppendRateControlArgs(args, preset);
        }

        if (preset.Width is { } width && preset.Height is { } height)
        {
            args.Add("-vf");
            args.Add(FormattableString.Invariant($"scale={width}:{height}"));
        }

        if (preset.FrameRate is { } frameRate)
        {
            args.Add("-r");
            args.Add(frameRate.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void AppendRateControlArgs(List<string> args, EncodePreset preset)
    {
        switch (preset.RateControlMode)
        {
            case RateControlMode.Crf when preset.CrfValue is { } crf:
                args.Add("-crf");
                args.Add(crf.ToString(CultureInfo.InvariantCulture));
                break;

            case RateControlMode.Cbr when preset.VideoBitrateKbps is { } cbrBitrate:
                args.Add("-b:v");
                args.Add($"{cbrBitrate}k");
                args.Add("-minrate");
                args.Add($"{cbrBitrate}k");
                args.Add("-maxrate");
                args.Add($"{cbrBitrate}k");
                args.Add("-bufsize");
                args.Add($"{cbrBitrate * 2}k");
                break;

            case RateControlMode.Vbr when preset.VideoBitrateKbps is { } targetBitrate:
                args.Add("-b:v");
                args.Add($"{targetBitrate}k");
                if (preset.MaxVideoBitrateKbps is { } maxBitrate)
                {
                    args.Add("-maxrate");
                    args.Add($"{maxBitrate}k");
                    args.Add("-bufsize");
                    args.Add($"{maxBitrate * 2}k");
                }
                break;
        }
    }

    private static void AppendAudioArgs(List<string> args, EncodePreset preset)
    {
        args.Add("-c:a");
        args.Add(ResolveAudioEncoderName(preset.AudioCodec));

        if (preset.AudioBitrateKbps is { } audioBitrate)
        {
            args.Add("-b:a");
            args.Add($"{audioBitrate}k");
        }

        if (preset.AudioSampleRateHz is { } sampleRate)
        {
            args.Add("-ar");
            args.Add(sampleRate.ToString(CultureInfo.InvariantCulture));
        }

        if (preset.AudioChannels is { } channels)
        {
            args.Add("-ac");
            args.Add(channels.ToString(CultureInfo.InvariantCulture));
        }
    }

    public static string ResolveVideoEncoderName(VideoCodec codec, HardwareEncoderKind hardware) => (codec, hardware) switch
    {
        (VideoCodec.H264, HardwareEncoderKind.Nvenc) => "h264_nvenc",
        (VideoCodec.H264, HardwareEncoderKind.Qsv) => "h264_qsv",
        (VideoCodec.H264, HardwareEncoderKind.Amf) => "h264_amf",
        (VideoCodec.H264, _) => "libx264",

        (VideoCodec.H265, HardwareEncoderKind.Nvenc) => "hevc_nvenc",
        (VideoCodec.H265, HardwareEncoderKind.Qsv) => "hevc_qsv",
        (VideoCodec.H265, HardwareEncoderKind.Amf) => "hevc_amf",
        (VideoCodec.H265, _) => "libx265",

        (VideoCodec.ProRes422, _) => "prores_ks",
        (VideoCodec.ProRes422Hq, _) => "prores_ks",
        (VideoCodec.Vp9, _) => "libvpx-vp9",

        _ => throw new ArgumentOutOfRangeException(nameof(codec), codec, "No ffmpeg encoder mapping for this codec."),
    };

    public static string ResolveAudioEncoderName(AudioCodec codec) => codec switch
    {
        AudioCodec.Aac => "aac",
        AudioCodec.Mp3 => "libmp3lame",
        AudioCodec.Ac3 => "ac3",
        AudioCodec.Flac => "flac",
        AudioCodec.PcmS16Le => "pcm_s16le",
        _ => throw new ArgumentOutOfRangeException(nameof(codec), codec, "No ffmpeg encoder mapping for this codec."),
    };
}
