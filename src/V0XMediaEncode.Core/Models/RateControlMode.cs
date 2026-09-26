namespace V0XMediaEncode.Core.Models;

public enum RateControlMode
{
    /// <summary>Constant bitrate.</summary>
    Cbr,

    /// <summary>Variable bitrate (target + max).</summary>
    Vbr,

    /// <summary>Constant Rate Factor (quality-based, no target bitrate).</summary>
    Crf,
}
