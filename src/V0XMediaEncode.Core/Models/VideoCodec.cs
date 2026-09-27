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

    /// <summary>
    /// Let the app pick the best hardware encoder actually detected on this machine at encode time
    /// (falling back to software when none is available), instead of locking the preset to one
    /// vendor. This is the default for new presets so they stay portable across machines. Displayed
    /// as "Automatique" in the UI since the app shows enum names as-is (see PresetsPage.xaml.cs).
    /// </summary>
    Automatique,
    Nvenc,
    Qsv,
    Amf,
}
