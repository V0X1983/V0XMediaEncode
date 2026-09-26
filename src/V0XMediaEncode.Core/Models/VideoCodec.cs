namespace V0XMediaEncode.Core.Models;

public enum VideoCodec
{
    None,
    Copy,
    H264,
    H265,
    ProRes422,
    ProRes422Hq,
    Vp9,
}

/// <summary>Which hardware acceleration path should be used to realize a <see cref="VideoCodec"/>.</summary>
public enum HardwareEncoderKind
{
    /// <summary>Software (libx264 / libx265 / ...).</summary>
    None,
    Nvenc,
    Qsv,
    Amf,
}
