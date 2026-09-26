using V0XMediaEncode.Services.Ffmpeg;

namespace V0XMediaEncode.Services.Tests;

public class FFmpegEncoderCapabilitiesParserTests
{
    private const string SampleOutput =
        """
        Encoders:
         V..... = Video
         A..... = Audio
         S..... = Subtitle
         .F.... = Frame-level multithreading
         ..S... = Slice-level multithreading
         ...X.. = Codec is experimental
         ....B. = Supports draw_horiz_band
         .....D = Supports direct rendering method 1
         ------
         V....D libx264              libx264 H.264 / AVC / MPEG-4 AVC (codec h264)
         V....D h264_nvenc           NVIDIA NVENC H.264 encoder (codec h264)
         V....D hevc_nvenc           NVIDIA NVENC hevc encoder (codec hevc)
         V....D h264_qsv             H264 (Intel Quick Sync Video acceleration) (codec h264)
         A..... aac                  AAC (Advanced Audio Coding)
        """;

    [Fact]
    public void Parse_DetectsAvailableHardwareEncoders()
    {
        var capabilities = FFmpegEncoderCapabilitiesParser.Parse(SampleOutput);

        Assert.True(capabilities.HasNvencH264);
        Assert.True(capabilities.HasNvencHevc);
        Assert.True(capabilities.HasQsvH264);
        Assert.True(capabilities.HasAnyHardwareEncoder);

        Assert.False(capabilities.HasQsvHevc);
        Assert.False(capabilities.HasAmfH264);
        Assert.False(capabilities.HasAmfHevc);
    }

    [Fact]
    public void Parse_ReturnsNoneCapabilities_ForSoftwareOnlyBuild()
    {
        const string softwareOnly =
            """
            Encoders:
             V..... libx264              libx264 H.264 / AVC / MPEG-4 AVC (codec h264)
             V..... libx265              libx265 H.265 / HEVC (codec hevc)
            """;

        var capabilities = FFmpegEncoderCapabilitiesParser.Parse(softwareOnly);

        Assert.False(capabilities.HasAnyHardwareEncoder);
    }
}
