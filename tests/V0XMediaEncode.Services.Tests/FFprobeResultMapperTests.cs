using V0XMediaEncode.Services.Ffmpeg;

namespace V0XMediaEncode.Services.Tests;

public class FFprobeResultMapperTests
{
    [Fact]
    public void Map_ExtractsVideoAndAudioStreams()
    {
        var output = new FFprobeOutput
        {
            Format = new FFprobeFormat { Duration = "125.5", BitRate = "5000000", FormatName = "mov,mp4,m4a,3gp,3g2,mj2" },
            Streams =
            [
                new FFprobeStream { CodecType = "video", CodecName = "h264", Width = 1920, Height = 1080, RFrameRate = "30000/1001" },
                new FFprobeStream { CodecType = "audio", CodecName = "aac", SampleRate = "48000", Channels = 2 },
            ],
        };

        var result = FFprobeResultMapper.Map(output);

        Assert.Equal(TimeSpan.FromSeconds(125.5), result.Duration);
        Assert.Equal(5_000_000, result.BitrateBps);
        Assert.Equal("h264", result.VideoCodecName);
        Assert.Equal(1920, result.Width);
        Assert.Equal(1080, result.Height);
        Assert.Equal(30000.0 / 1001.0, result.FrameRate!.Value, 4);
        Assert.Equal("aac", result.AudioCodecName);
        Assert.Equal(48000, result.AudioSampleRateHz);
        Assert.Equal(2, result.AudioChannels);
        Assert.True(result.HasVideo);
        Assert.True(result.HasAudio);
    }

    [Fact]
    public void Map_HandlesAudioOnlyFile()
    {
        var output = new FFprobeOutput
        {
            Format = new FFprobeFormat { Duration = "200" },
            Streams = [new FFprobeStream { CodecType = "audio", CodecName = "mp3", SampleRate = "44100", Channels = 2 }],
        };

        var result = FFprobeResultMapper.Map(output);

        Assert.False(result.HasVideo);
        Assert.True(result.HasAudio);
        Assert.Null(result.FrameRate);
    }

    [Fact]
    public void Map_ReturnsZeroDuration_WhenFormatMissing()
    {
        var result = FFprobeResultMapper.Map(new FFprobeOutput());

        Assert.Equal(TimeSpan.Zero, result.Duration);
        Assert.False(result.HasVideo);
        Assert.False(result.HasAudio);
    }
}
