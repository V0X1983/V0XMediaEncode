using V0XMediaEncode.Core.Models;
using V0XMediaEncode.Services.Ffmpeg;

namespace V0XMediaEncode.Services.Tests;

public class FFmpegArgumentBuilderTests
{
    private static EncodeJob CreateJob(string output = @"C:\out\video.mp4") => new()
    {
        SourcePath = @"C:\in\video.mov",
        OutputPath = output,
    };

    [Fact]
    public void Build_H264SoftwareCrf_ProducesExpectedArguments()
    {
        var preset = new EncodePreset
        {
            Name = "test",
            Container = ContainerFormat.Mp4,
            VideoCodec = VideoCodec.H264,
            HardwareEncoder = HardwareEncoderKind.None,
            RateControlMode = RateControlMode.Crf,
            CrfValue = 20,
            AudioCodec = AudioCodec.Aac,
            AudioBitrateKbps = 192,
        };

        var args = FFmpegArgumentBuilder.Build(CreateJob(), preset);

        Assert.Equal(
        [
            "-y", "-i", @"C:\in\video.mov",
            "-c:v", "libx264",
            "-crf", "20",
            "-c:a", "aac",
            "-b:a", "192k",
            @"C:\out\video.mp4",
        ], args);
    }

    [Fact]
    public void Build_H264Nvenc_UsesHardwareEncoderName()
    {
        var preset = new EncodePreset
        {
            Name = "test",
            VideoCodec = VideoCodec.H264,
            HardwareEncoder = HardwareEncoderKind.Nvenc,
            RateControlMode = RateControlMode.Vbr,
            VideoBitrateKbps = 8000,
            MaxVideoBitrateKbps = 12000,
            AudioCodec = AudioCodec.Copy,
        };

        var args = FFmpegArgumentBuilder.Build(CreateJob(), preset);

        Assert.Contains("h264_nvenc", args);
        Assert.Contains("-maxrate", args);
        Assert.Contains("12000k", args);
        Assert.Contains("copy", args);
    }

    [Fact]
    public void Build_AudioOnlyPreset_OmitsVideoStream()
    {
        var preset = new EncodePreset
        {
            Name = "mp3",
            Container = ContainerFormat.Mp3,
            VideoCodec = VideoCodec.None,
            AudioCodec = AudioCodec.Mp3,
            AudioBitrateKbps = 320,
        };

        var args = FFmpegArgumentBuilder.Build(CreateJob(@"C:\out\audio.mp3"), preset);

        Assert.Contains("-vn", args);
        Assert.DoesNotContain("-c:v", args);
        Assert.Contains("libmp3lame", args);
    }

    [Fact]
    public void Build_ProRes_AddsProfileArgument()
    {
        var preset = new EncodePreset
        {
            Name = "prores",
            Container = ContainerFormat.Mov,
            VideoCodec = VideoCodec.ProRes422Hq,
            AudioCodec = AudioCodec.PcmS16Le,
        };

        var args = FFmpegArgumentBuilder.Build(CreateJob(@"C:\out\video.mov"), preset);

        Assert.Contains("prores_ks", args);
        var profileIndex = args.ToList().IndexOf("-profile:v");
        Assert.True(profileIndex >= 0);
        Assert.Equal("3", args[profileIndex + 1]);
    }

    [Theory]
    [InlineData(VideoCodec.H264, HardwareEncoderKind.None, "libx264")]
    [InlineData(VideoCodec.H264, HardwareEncoderKind.Qsv, "h264_qsv")]
    [InlineData(VideoCodec.H264, HardwareEncoderKind.Amf, "h264_amf")]
    [InlineData(VideoCodec.H265, HardwareEncoderKind.Nvenc, "hevc_nvenc")]
    [InlineData(VideoCodec.Vp9, HardwareEncoderKind.None, "libvpx-vp9")]
    public void ResolveVideoEncoderName_MapsCodecAndHardware(VideoCodec codec, HardwareEncoderKind hardware, string expected)
    {
        Assert.Equal(expected, FFmpegArgumentBuilder.ResolveVideoEncoderName(codec, hardware));
    }
}
