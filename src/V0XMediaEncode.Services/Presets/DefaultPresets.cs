using V0XMediaEncode.Core.Models;

namespace V0XMediaEncode.Services.Presets;

/// <summary>Seed presets modeled after Adobe Media Encoder's built-in list, applied on first run.</summary>
public static class DefaultPresets
{
    public static IReadOnlyList<EncodePreset> CreateAll() =>
    [
        // --- Web / streaming (H.264, MP4) ---
        new()
        {
            Name = "H.264 - YouTube 4K",
            IsBuiltIn = true,
            Container = ContainerFormat.Mp4,
            VideoCodec = VideoCodec.H264,
            RateControlMode = RateControlMode.Vbr,
            VideoBitrateKbps = 35000,
            MaxVideoBitrateKbps = 45000,
            Width = 3840,
            Height = 2160,
            FrameRate = 30,
            AudioCodec = AudioCodec.Aac,
            AudioBitrateKbps = 384,
            AudioSampleRateHz = 48000,
            AudioChannels = 2,
        },
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
            Name = "H.264 - YouTube 720p",
            IsBuiltIn = true,
            Container = ContainerFormat.Mp4,
            VideoCodec = VideoCodec.H264,
            RateControlMode = RateControlMode.Vbr,
            VideoBitrateKbps = 5000,
            MaxVideoBitrateKbps = 7500,
            Width = 1280,
            Height = 720,
            FrameRate = 30,
            AudioCodec = AudioCodec.Aac,
            AudioBitrateKbps = 256,
            AudioSampleRateHz = 48000,
            AudioChannels = 2,
        },
        new()
        {
            Name = "H.264 - Vimeo Haute Qualité",
            IsBuiltIn = true,
            Container = ContainerFormat.Mp4,
            VideoCodec = VideoCodec.H264,
            RateControlMode = RateControlMode.Vbr,
            VideoBitrateKbps = 15000,
            MaxVideoBitrateKbps = 20000,
            Width = 1920,
            Height = 1080,
            FrameRate = 30,
            AudioCodec = AudioCodec.Aac,
            AudioBitrateKbps = 320,
            AudioSampleRateHz = 48000,
            AudioChannels = 2,
        },

        // --- Réseaux sociaux (formats verticaux / carrés) ---
        new()
        {
            Name = "Réseaux sociaux - Story/Reel vertical (9:16)",
            IsBuiltIn = true,
            Container = ContainerFormat.Mp4,
            VideoCodec = VideoCodec.H264,
            RateControlMode = RateControlMode.Vbr,
            VideoBitrateKbps = 6000,
            MaxVideoBitrateKbps = 9000,
            Width = 1080,
            Height = 1920,
            FrameRate = 30,
            AudioCodec = AudioCodec.Aac,
            AudioBitrateKbps = 192,
            AudioSampleRateHz = 48000,
            AudioChannels = 2,
        },
        new()
        {
            Name = "Réseaux sociaux - Publication carrée (1:1)",
            IsBuiltIn = true,
            Container = ContainerFormat.Mp4,
            VideoCodec = VideoCodec.H264,
            RateControlMode = RateControlMode.Vbr,
            VideoBitrateKbps = 5000,
            MaxVideoBitrateKbps = 8000,
            Width = 1080,
            Height = 1080,
            FrameRate = 30,
            AudioCodec = AudioCodec.Aac,
            AudioBitrateKbps = 192,
            AudioSampleRateHz = 48000,
            AudioChannels = 2,
        },

        // --- H.265 / HEVC (plus efficace, fichiers plus légers) ---
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
            Name = "H.265 - 1080p Compact",
            IsBuiltIn = true,
            Container = ContainerFormat.Mp4,
            VideoCodec = VideoCodec.H265,
            RateControlMode = RateControlMode.Crf,
            CrfValue = 23,
            Width = 1920,
            Height = 1080,
            AudioCodec = AudioCodec.Aac,
            AudioBitrateKbps = 192,
            AudioSampleRateHz = 48000,
            AudioChannels = 2,
        },

        // --- Web ouvert (VP9/WebM, sans brevet) ---
        new()
        {
            Name = "VP9 - Web (WebM)",
            IsBuiltIn = true,
            Container = ContainerFormat.WebM,
            VideoCodec = VideoCodec.Vp9,
            RateControlMode = RateControlMode.Vbr,
            VideoBitrateKbps = 4000,
            MaxVideoBitrateKbps = 6000,
            Width = 1920,
            Height = 1080,
            FrameRate = 30,
            AudioCodec = AudioCodec.Opus,
            AudioBitrateKbps = 128,
            AudioSampleRateHz = 48000,
            AudioChannels = 2,
        },

        // --- Montage / post-production (qualité maximale) ---
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
            Name = "ProRes 422 HQ",
            IsBuiltIn = true,
            Container = ContainerFormat.Mov,
            VideoCodec = VideoCodec.ProRes422Hq,
            RateControlMode = RateControlMode.Vbr,
            AudioCodec = AudioCodec.PcmS16Le,
            AudioSampleRateHz = 48000,
            AudioChannels = 2,
        },
        new()
        {
            Name = "MKV - Archivage (H.265 + FLAC sans perte)",
            IsBuiltIn = true,
            Container = ContainerFormat.Mkv,
            VideoCodec = VideoCodec.H265,
            RateControlMode = RateControlMode.Crf,
            CrfValue = 18,
            AudioCodec = AudioCodec.Flac,
            AudioSampleRateHz = 48000,
            AudioChannels = 2,
        },

        // --- Appareils ---
        new()
        {
            Name = "Appareils Apple (iPhone/iPad)",
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

        // --- Audio uniquement ---
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
        new()
        {
            Name = "Audio uniquement - WAV (non compressé)",
            IsBuiltIn = true,
            Container = ContainerFormat.Wav,
            VideoCodec = VideoCodec.None,
            RateControlMode = RateControlMode.Cbr,
            AudioCodec = AudioCodec.PcmS16Le,
            AudioSampleRateHz = 48000,
            AudioChannels = 2,
        },
        new()
        {
            Name = "Audio uniquement - FLAC (sans perte)",
            IsBuiltIn = true,
            Container = ContainerFormat.Flac,
            VideoCodec = VideoCodec.None,
            RateControlMode = RateControlMode.Cbr,
            AudioCodec = AudioCodec.Flac,
            AudioSampleRateHz = 48000,
            AudioChannels = 2,
        },
    ];
}
