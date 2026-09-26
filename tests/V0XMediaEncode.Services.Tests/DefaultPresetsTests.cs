using V0XMediaEncode.Core.Models;
using V0XMediaEncode.Services.Ffmpeg;
using V0XMediaEncode.Services.Presets;

namespace V0XMediaEncode.Services.Tests;

/// <summary>
/// Every built-in preset must actually resolve to a real ffmpeg encoder name for both its video
/// and audio codec, otherwise the first job a user runs with it would fail. This doesn't invoke
/// ffmpeg itself (see FFmpegArgumentBuilderTests for that layer) - it only catches a preset
/// referencing a (codec, container) combination FFmpegArgumentBuilder has no mapping for.
/// </summary>
public class DefaultPresetsTests
{
    [Fact]
    public void CreateAll_HasNoDuplicateNames()
    {
        var presets = DefaultPresets.CreateAll();

        var names = presets.Select(p => p.Name).ToList();
        Assert.Equal(names.Distinct().Count(), names.Count);
    }

    [Fact]
    public void CreateAll_EveryPresetIsBuiltIn()
    {
        Assert.All(DefaultPresets.CreateAll(), preset => Assert.True(preset.IsBuiltIn));
    }

    [Theory]
    [MemberData(nameof(GetPresetNames))]
    public void CreateAll_EveryPresetResolvesToAValidFfmpegEncoder(string presetName)
    {
        var preset = DefaultPresets.CreateAll().Single(p => p.Name == presetName);

        if (!preset.IsAudioOnly && preset.VideoCodec is not (VideoCodec.None or VideoCodec.Copy))
        {
            var videoEncoder = FFmpegArgumentBuilder.ResolveVideoEncoderName(preset.VideoCodec, preset.HardwareEncoder);
            Assert.False(string.IsNullOrWhiteSpace(videoEncoder));
        }

        if (preset.AudioCodec is not (AudioCodec.None or AudioCodec.Copy))
        {
            var audioEncoder = FFmpegArgumentBuilder.ResolveAudioEncoderName(preset.AudioCodec);
            Assert.False(string.IsNullOrWhiteSpace(audioEncoder));
        }

        // The muxer/extension lookup must also not throw for the preset's container.
        Assert.False(string.IsNullOrWhiteSpace(preset.Container.ToExtension()));
    }

    public static TheoryData<string> GetPresetNames() =>
        new(DefaultPresets.CreateAll().Select(p => p.Name));
}
