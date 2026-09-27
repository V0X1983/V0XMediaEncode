namespace V0XMediaEncode.Core.Models;

/// <summary>
/// An encoding recipe: container + codecs + rate control, independent of any specific source file.
/// Mirrors the Format/Video/Audio tabs of the Adobe Media Encoder preset editor.
/// </summary>
public sealed class EncodePreset
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public bool IsBuiltIn { get; set; }

    public ContainerFormat Container { get; set; } = ContainerFormat.Mp4;

    // --- Video ---
    public VideoCodec VideoCodec { get; set; } = VideoCodec.H264;

    public HardwareEncoderKind HardwareEncoder { get; set; } = HardwareEncoderKind.Automatique;

    public RateControlMode RateControlMode { get; set; } = RateControlMode.Vbr;

    /// <summary>Target bitrate in kbit/s. Used for CBR and as the target for VBR.</summary>
    public int? VideoBitrateKbps { get; set; }

    /// <summary>Max bitrate in kbit/s for VBR.</summary>
    public int? MaxVideoBitrateKbps { get; set; }

    /// <summary>Constant Rate Factor (0-51 for x264/x265, lower = better quality). Only used when <see cref="RateControlMode"/> is Crf.</summary>
    public int? CrfValue { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public double? FrameRate { get; set; }

    // --- Audio ---
    public AudioCodec AudioCodec { get; set; } = AudioCodec.Aac;

    public int? AudioBitrateKbps { get; set; } = 192;

    public int? AudioSampleRateHz { get; set; }

    public int? AudioChannels { get; set; }

    public bool IsAudioOnly => Container.IsAudioOnly() || VideoCodec == VideoCodec.None;
}
