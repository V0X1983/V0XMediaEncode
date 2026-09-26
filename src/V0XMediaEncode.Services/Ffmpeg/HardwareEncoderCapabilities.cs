namespace V0XMediaEncode.Services.Ffmpeg;

/// <summary>Which hardware-accelerated encoders this machine's ffmpeg build reports as available.</summary>
public sealed record HardwareEncoderCapabilities(
    bool HasNvencH264,
    bool HasNvencHevc,
    bool HasQsvH264,
    bool HasQsvHevc,
    bool HasAmfH264,
    bool HasAmfHevc)
{
    public static HardwareEncoderCapabilities None { get; } = new(false, false, false, false, false, false);

    public bool HasAnyHardwareEncoder =>
        HasNvencH264 || HasNvencHevc || HasQsvH264 || HasQsvHevc || HasAmfH264 || HasAmfHevc;
}
