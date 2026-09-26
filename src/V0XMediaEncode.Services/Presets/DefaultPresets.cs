using V0XMediaEncode.Core.Models;

namespace V0XMediaEncode.Services.Presets;

/// <summary>Seed presets modeled after Adobe Media Encoder's built-in list, applied on first run.</summary>
public static class DefaultPresets
{
    public static IReadOnlyList<EncodePreset> CreateAll() =>
    [
        new()
        {
            Name = "H.264 - YouTube 1080p",
            IsBuiltIn = true,
            Container = ContainerFormat.Mp4,
            VideoCodec = VideoCodec.H264,
            RateControlMode = RateControlMode.Vbr,
            VideoBitrateKbps = 8000,
            MaxVideoBitrateKbps = 12000,
            Width = 1920,
            Height = 1080,
            FrameRate = 30,
            AudioCodec = AudioCodec.Aac,
            AudioBitrateKbps = 384,
            AudioSampleRateHz = 48000,
            AudioChannels = 2,
        },
        new()
        {
            Name = "H.265 - 4K HDR",
            IsBuiltIn = true,
            Container = ContainerFormat.Mp4,
            VideoCodec = VideoCodec.H265,
            RateControlMode = RateControlMode.Crf,
            CrfValue = 20,
            Width = 3840,
            Height = 2160,
            AudioCodec = AudioCodec.Aac,
            AudioBitrateKbps = 384,
            AudioSampleRateHz = 48000,
            AudioChannels = 2,
        },
        new()
        {
            Name = "ProRes 422",
            IsBuiltIn = true,
            Container = ContainerFormat.Mov,
            VideoCodec = VideoCodec.ProRes422,
            RateControlMode = RateControlMode.Vbr,
            AudioCodec = AudioCodec.PcmS16Le,
            AudioSampleRateHz = 48000,
            AudioChannels = 2,
        },
        new()
        {
            Name = "Appareils Apple",
            IsBuiltIn = true,
            Container = ContainerFormat.Mp4,
            VideoCodec = VideoCodec.H264,
            RateControlMode = RateControlMode.Vbr,
            VideoBitrateKbps = 10000,
            MaxVideoBitrateKbps = 14000,
            Width = 1920,
            Height = 1080,
            FrameRate = 30,
            AudioCodec = AudioCodec.Aac,
            AudioBitrateKbps = 256,
            AudioSampleRateHz = 48000,
            AudioChannels = 2,
        },
        new()
        {
            Name = "Audio uniquement - MP3",
            IsBuiltIn = true,
            Container = ContainerFormat.Mp3,
            VideoCodec = VideoCodec.None,
            RateControlMode = RateControlMode.Cbr,
            AudioCodec = AudioCodec.Mp3,
            AudioBitrateKbps = 320,
            AudioSampleRateHz = 44100,
            AudioChannels = 2,
        },
        new()
        {
            Name = "Audio uniquement - AAC",
            IsBuiltIn = true,
            Container = ContainerFormat.Aac,
            VideoCodec = VideoCodec.None,
            RateControlMode = RateControlMode.Cbr,
            AudioCodec = AudioCodec.Aac,
            AudioBitrateKbps = 256,
            AudioSampleRateHz = 48000,
            AudioChannels = 2,
        },
    ];
}
